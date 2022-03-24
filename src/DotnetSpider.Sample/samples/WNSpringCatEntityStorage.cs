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

		public static  IDataFlow CreateFromOption(IConfiguration configuration)
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
	}
}
