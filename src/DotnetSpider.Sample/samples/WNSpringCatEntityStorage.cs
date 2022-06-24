using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using System.Threading;
using DotnetSpider.DataFlow;
using DotnetSpider.DataFlow.Parser;
using DotnetSpider.DataFlow.Parser.Formatters;
using DotnetSpider.DataFlow.Storage;
using DotnetSpider.Downloader;
using DotnetSpider.Http;
using DotnetSpider.Infrastructure;
using DotnetSpider.MySql;
using DotnetSpider.MySql.Scheduler;
using DotnetSpider.Scheduler;
using DotnetSpider.Scheduler.Component;
using DotnetSpider.Selector;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using System.Data;
using System.Collections;
using System.Collections.Concurrent;
using Dapper;
using System.IO;

namespace DotnetSpider.Sample.samples
{
	public class WNSpringCatSqlStatements: SqlStatements
	{
		/// <summary>
		/// SQL Select statement
		/// </summary>
		public string SelectSql { get; set; }


	}


	public class WNSpringCatEntityStorage : MySqlEntityStorage
	{
		private readonly ConcurrentDictionary<string, WNSpringCatSqlStatements> _sqlStatementDict =
			new();

		private readonly ConcurrentDictionary<string, object> _executedCache =
			new();

		private readonly ConcurrentDictionary<Type, TableMetadata> _tableMetadataDict =
			new();

		public new static IDataFlow CreateFromOptions(IConfiguration configuration)
		{
			var options = new MySqlOptions(configuration);
			return new WNSpringCatEntityStorage(options.Mode, options.ConnectionString)
			{
				UseTransaction = options.UseTransaction,
				IgnoreCase = options.IgnoreCase,
				RetryTimes = options.RetryTimes
			};

		}
		/// <summary>
		/// connection string
		/// </summary>
		/// <param name="mode">memory type</param>
		/// <param name="connectionString">connection string</param>
		public WNSpringCatEntityStorage(StorageMode mode,
			string connectionString) : base(mode,
			connectionString)
		{
		}

		public override Task InitializeAsync()
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Generate SQL statement
		/// </summary>
		/// <param name="tableMetadata">table metadata</param>
		/// <returns></returns>
		protected override WNSpringCatSqlStatements GenerateSqlStatements(TableMetadata tableMetadata)
		{
			SqlStatements sqlStatements = base.GenerateSqlStatements(tableMetadata);
			var WNSpringsqlStatements = new WNSpringCatSqlStatements
			{
				SelectSql = GenerateSelectSql(tableMetadata),
				InsertSql = sqlStatements.InsertSql,
				InsertIgnoreDuplicateSql = sqlStatements.InsertAndUpdateSql,
				InsertAndUpdateSql = sqlStatements.InsertAndUpdateSql,
				UpdateSql = sqlStatements.UpdateSql,
				CreateTableSql = sqlStatements.CreateTableSql,
				CreateDatabaseSql = sqlStatements.CreateDatabaseSql,
				DatabaseSql = string.IsNullOrWhiteSpace(tableMetadata.Schema.Database)
					? ""
					: $"{Escape}{GetNameSql(tableMetadata.Schema.Database)}{Escape}"
			};

			return WNSpringsqlStatements;
		}

		/// <summary>
		/// Generate SQL statements to retrieve data
		/// </summary>
		/// <param name="tableMetadata">table metadata</param>
		/// <returns>SQL statement</returns>
		protected virtual string GenerateSelectSql(TableMetadata tableMetadata)
		{
			if (tableMetadata.Updates == null || tableMetadata.Updates.Count == 0)
			{
				Logger?.LogWarning("The entity does not have a primary key set and the Update statement cannot be generated");
				return null;
			}

			var where = "";
			foreach (var column in tableMetadata.Primary)
			{
				where += $" {Escape}{GetNameSql(column)}{Escape} = @{column} AND";
			}

			where = where.Substring(0, where.Length - 3);

			var setCols = string.Join(", ",
				tableMetadata.Updates.Select(c => $"{Escape}{GetNameSql(c)}{Escape}= @{c}"));
			var tableSql = GenerateTableSql(tableMetadata);
			var sql = $"select {tableSql}  WHERE {where};";
			return sql;
		}

		protected override async Task HandleAsync(DataFlowContext context,
			IDictionary<Type, ICollection<dynamic>> entities)
		{
			using var conn = TryCreateDbConnection();

			foreach (var kv in entities)
			{
				var list = (IList)kv.Value;
				var tableMetadata = _tableMetadataDict.GetOrAdd(kv.Key,
					_ => ((IEntity)list[0]).GetTableMetadata());

				var sqlStatements = GetSqlStatements(tableMetadata);

				// You cannot use initAsync to initialize the database because the same store will receive different data objects
				_executedCache.GetOrAdd(sqlStatements.CreateTableSql, _ =>
				{
					var conn1 = TryCreateDbConnection();
					EnsureDatabaseAndTableCreated(conn1, sqlStatements);
					return string.Empty;
				});
				for (var i = 0; i < RetryTimes; ++i)
				{
					IDbTransaction transaction = null;
					try
					{
						if (UseTransaction)
						{
							transaction = conn.BeginTransaction();
						}

						switch (Mode)
						{
							case StorageMode.Insert:
								{
									await conn.ExecuteAsync(sqlStatements.InsertSql, list, transaction);
									break;
								}
							case StorageMode.InsertIgnoreDuplicate:
								{
									await conn.ExecuteAsync(sqlStatements.InsertIgnoreDuplicateSql, list, transaction);
									break;
								}
							case StorageMode.Update:
								{
									if (string.IsNullOrWhiteSpace(sqlStatements.UpdateSql))
									{
										throw new SpiderException("Failed to generate update SQL");
									}

									await conn.ExecuteAsync(sqlStatements.UpdateSql, list, transaction);
									break;
								}
							case StorageMode.InsertAndUpdate:
								{
									await conn.ExecuteAsync(sqlStatements.InsertAndUpdateSql, list, transaction);
									break;
								}
							default:
								throw new ArgumentOutOfRangeException();
						}

						transaction?.Commit();
						break;
					}
					catch (Exception ex)
					{
						Logger.LogError($"Attempt to insert data failed: {ex}");

						// Network exceptions require retry and do not require Rollback
						if (!(ex.InnerException is EndOfStreamException))
						{
							try
							{
								transaction?.Rollback();
							}
							catch (Exception e)
							{
								Logger?.LogError($"Database rollback failed: {e}");
							}

							break;
						}
					}
					finally
					{
						transaction?.Dispose();
					}
				}
			}
		}

		private WNSpringCatSqlStatements GetSqlStatements(TableMetadata tableMetadata)
		{
			// Performing a table creation operation once a day can realize
			// the operation of one table per day, or create a new table
			// at runtime by dividing the table by week.
			var key = tableMetadata.TypeName;
			if (tableMetadata.Schema.TablePostfix != TablePostfix.None)
			{
				key = $"{key}-{DateTimeOffset.Now:yyyyMMdd}";
			}

			return _sqlStatementDict.GetOrAdd(key, _ => GenerateSqlStatements(tableMetadata));
		}
		private IDbConnection TryCreateDbConnection()
		{
			for (var i = 0; i < RetryTimes; ++i)
			{
				var conn = TryCreateDbConnection(ConnectionString);
				if (conn != null)
				{
					return conn;
				}
			}

			throw new SpiderException("No valid database connection configuration");
		}

		private IDbConnection TryCreateDbConnection(string connectionString)
		{
			try
			{
				var conn = CreateDbConnection(connectionString);
				conn.Open();
				return conn;
			}
			catch
			{
				Logger.LogError($"Could not open database connection: {connectionString}.");
			}

			return null;
		}

		public virtual async Task<bool> RecordExists(DataFlowContext context,
			IDictionary<Type, ICollection<dynamic>> entities)
		{
			using var conn = TryCreateDbConnection();

			foreach (var kv in entities)
			{
				var list = (IList)kv.Value;
				var tableMetadata = _tableMetadataDict.GetOrAdd(kv.Key,
					_ => ((IEntity)list[0]).GetTableMetadata());

				var sqlStatements = GetSqlStatements(tableMetadata);

				// You cannot use initAsync to initialize the database because the same store will receive different data objects
				_executedCache.GetOrAdd(sqlStatements.CreateTableSql, _ =>
				{
					var conn1 = TryCreateDbConnection();
					EnsureDatabaseAndTableCreated(conn1, sqlStatements);
					return string.Empty;
				});
				for (var i = 0; i < RetryTimes; ++i)
				{
					//IDbTransaction transaction = null;
					try
					{
						//if (UseTransaction)
						//{
						//	transaction = conn.BeginTransaction();
						//}

						var results = await conn.QueryAsync(sqlStatements.SelectSql, list);

						//transaction?.Commit();

						break;
					}
					catch (Exception ex)
					{
						Logger.LogError($"Attempt to query data failed: {ex}");

						// Network exceptions require retry and do not require Rollback
						if (!(ex.InnerException is EndOfStreamException))
						{
							try
							{
								//transaction?.Rollback();
							}
							catch (Exception e)
							{
								Logger?.LogError($"Database rollback failed: {e}");
							}

							break;
						}
					}
					finally
					{
						//transaction?.Dispose();
					}
				}
			}


			return false;

		}
	}
}
