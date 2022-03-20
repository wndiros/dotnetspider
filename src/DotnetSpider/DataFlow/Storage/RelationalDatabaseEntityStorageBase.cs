using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using Dapper;
using DotnetSpider.Infrastructure;
using Microsoft.Extensions.Logging;

namespace DotnetSpider.DataFlow.Storage
{
	/// <summary>
	/// Relational database saves entity resolution results
	/// </summary>
	public abstract class RelationalDatabaseEntityStorageBase : EntityStorageBase
	{
		private readonly ConcurrentDictionary<string, SqlStatements> _sqlStatementDict =
			new();

		private readonly ConcurrentDictionary<string, object> _executedCache =
			new();

		private readonly ConcurrentDictionary<Type, TableMetadata> _tableMetadataDict =
			new();

		protected const string BoolType = "Boolean";
		protected const string DateTimeType = "DateTime";
		protected const string DateTimeOffsetType = "DateTimeOffset";
		protected const string DecimalType = "Decimal";
		protected const string DoubleType = "Double";
		protected const string FloatType = "Single";
		protected const string IntType = "Int32";
		protected const string LongType = "Int64";
		protected const string ByteType = "Byte";
		protected const string ShortType = "Short";

		/// <summary>
		/// Create database connection interface
		/// </summary>
		/// <param name="connectString">connection string</param>
		/// <returns></returns>
		protected abstract IDbConnection CreateDbConnection(string connectString);

		/// <summary>
		/// Generate SQL statement
		/// </summary>
		/// <param name="tableMetadata">table metadata</param>
		/// <returns></returns>
		protected abstract SqlStatements GenerateSqlStatements(TableMetadata tableMetadata);

		/// <summary>
		/// Create databases and tables
		/// </summary>
		/// <param name="conn">Database linkage</param>
		/// <param name="sqlStatements">SQL statement</param>
		protected virtual void EnsureDatabaseAndTableCreated(IDbConnection conn,
			SqlStatements sqlStatements)
		{
			if (!string.IsNullOrWhiteSpace(sqlStatements.CreateDatabaseSql))
			{
				conn.Execute(sqlStatements.CreateDatabaseSql);
			}

			conn.Execute(sqlStatements.CreateTableSql);
		}

		/// <summary>
		/// Constructor method
		/// </summary>
		/// <param name="model">memory type</param>
		/// <param name="connectionString">connection string</param>
		protected RelationalDatabaseEntityStorageBase(StorageMode model, string connectionString)
		{
			connectionString.NotNullOrWhiteSpace(nameof(connectionString));
			ConnectionString = connectionString;
			Mode = model;
		}

		/// <summary>
		/// memory type
		/// </summary>
		public StorageMode Mode { get; set; }

		/// <summary>
		/// Database operation retries
		/// </summary>
		public int RetryTimes { get; set; } = 600;

		/// <summary>
		/// connection string
		/// </summary>
		public string ConnectionString { get; }

		/// <summary>
		/// Whether to use transaction operations. Not used by default.
		/// </summary>
		public bool UseTransaction { get; set; }

		/// <summary>
		/// Database ignores case
		/// </summary>
		public bool IgnoreCase { get; set; } = true;

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

		protected virtual string GetNameSql(string name)
		{
			if (string.IsNullOrWhiteSpace(name))
			{
				return null;
			}

			return IgnoreCase ? name.ToLowerInvariant() : name;
		}

		protected virtual string GetTableName(TableMetadata tableMetadata)
		{
			var tableName = tableMetadata.Schema.Table;
			switch (tableMetadata.Schema.TablePostfix)
			{
				case TablePostfix.Monday:
				{
					return $"{tableName}_{DateTimeHelper.Monday:yyyyMMdd}";
				}

				case TablePostfix.Month:
				{
					return $"{tableName}_{DateTimeHelper.FirstDayOfMonth:yyyyMMdd}";
				}

				case TablePostfix.Today:
				{
					return $"{tableName}_{DateTimeHelper.Today:yyyyMMdd}";
				}

				default:
				{
					return tableName;
				}
			}
		}

		private SqlStatements GetSqlStatements(TableMetadata tableMetadata)
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
	}
}
