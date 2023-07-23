using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotnetSpider.DataFlow;
using DotnetSpider.DataFlow.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace DotnetSpider.MySql
{
	/// <summary>
	/// MySql save parsing (entity) results
	/// </summary>
	public class MySqlEntityStorage : RelationalDatabaseEntityStorageBase
	{
		protected virtual string Escape => "`";

		public static IDataFlow CreateFromOptions(IConfiguration configuration)
		{
			var options = new MySqlOptions(configuration);
			return new MySqlEntityStorage(options.Mode, options.ConnectionString)
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
		public MySqlEntityStorage(StorageMode mode,
			string connectionString) : base(mode,
			connectionString)
		{
		}

		public override Task InitializeAsync()
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Create database connection interface
		/// </summary>
		/// <param name="connectString">connection string</param>
		/// <returns></returns>
		protected override IDbConnection CreateDbConnection(string connectString)
		{
			return new MySqlConnection(connectString);
		}

		/// <summary>
		/// Generate SQL statement
		/// </summary>
		/// <param name="tableMetadata">table metadata</param>
		/// <returns></returns>
		protected override SqlStatements GenerateSqlStatements(TableMetadata tableMetadata)
		{
			var sqlStatements = new SqlStatements
			{
				InsertSql = GenerateInsertSql(tableMetadata, false),
				InsertIgnoreDuplicateSql = GenerateInsertSql(tableMetadata, true),
				InsertAndUpdateSql = GenerateInsertAndUpdateSql(tableMetadata),
				UpdateSql = GenerateUpdateSql(tableMetadata),
				CreateTableSql = GenerateCreateTableSql(tableMetadata),
				CreateDatabaseSql = GenerateCreateDatabaseSql(tableMetadata),
				DatabaseSql = string.IsNullOrWhiteSpace(tableMetadata.Schema.Database)
					? ""
					: $"{Escape}{GetNameSql(tableMetadata.Schema.Database)}{Escape}"
			};
			return sqlStatements;
		}

		/// <summary>
		/// Generate SQL statement to create database
		/// </summary>
		/// <param name="tableMetadata">table metadata</param>
		/// <returns>SQL statement</returns>
		protected virtual string GenerateCreateDatabaseSql(TableMetadata tableMetadata)
		{
			return string.IsNullOrWhiteSpace(tableMetadata.Schema.Database)
				? ""
				: $"CREATE SCHEMA IF NOT EXISTS {Escape}{GetNameSql(tableMetadata.Schema.Database)}{Escape} DEFAULT CHARACTER SET utf8mb4;";
		}

		/// <summary>
		/// Generate the SQL statement to create the table
		/// </summary>
		/// <param name="tableMetadata">table metadata</param>
		/// <returns>SQL statement</returns>
		protected virtual string GenerateCreateTableSql(TableMetadata tableMetadata)
		{
			var isAutoIncrementPrimary = tableMetadata.IsAutoIncrementPrimary;

			var tableSql = GenerateTableSql(tableMetadata);

			var builder = new StringBuilder($"CREATE TABLE IF NOT EXISTS {tableSql} (");

			foreach (var column in tableMetadata.Columns)
			{
				var isPrimary = tableMetadata.IsPrimary(column.Key);

				var columnSql = GenerateColumnSql(column.Value, isPrimary);

				if (isPrimary)
				{
					builder.Append(isAutoIncrementPrimary
						? $"{columnSql} AUTO_INCREMENT PRIMARY KEY, "
						: $"{columnSql} {(tableMetadata.Primary.Count > 1 ? "" : "PRIMARY KEY")}, ");
				}
				else
				{
					builder.Append($"{columnSql}, ");
				}
			}

			builder.Remove(builder.Length - 2, 2);

			if (tableMetadata.Primary != null && tableMetadata.Primary.Count > 1)
			{
				builder.Append(
					$", PRIMARY KEY ({string.Join(", ", tableMetadata.Primary.Select(c => $"{Escape}{GetNameSql(c)}{Escape}"))})");
			}

			if (tableMetadata.Indexes.Count > 0)
			{
				foreach (var index in tableMetadata.Indexes)
				{
					var name = index.Name;
					var columnNames = string.Join(", ", index.Columns.Select(c => $"{Escape}{GetNameSql(c)}{Escape}"));
					builder.Append($", {(index.IsUnique ? "UNIQUE" : "")} KEY {Escape}{name}{Escape} ({columnNames})");
				}
			}

			builder.Append(")");
			var sql = builder.ToString();
			return sql;
		}

		/// <summary>
		/// Generate SQL statement to insert data
		/// </summary>
		/// <param name="tableMetadata">table metadata</param>
		/// <param name="ignoreDuplicate">Whether to ignore data for duplicate keys</param>
		/// <returns>SQL statement</returns>
		protected virtual string GenerateInsertSql(TableMetadata tableMetadata, bool ignoreDuplicate)
		{
			var columns = tableMetadata.Columns;
			var isAutoIncrementPrimary = tableMetadata.IsAutoIncrementPrimary;
			// Remove the auto-incrementing primary key
			var insertColumns =
				(isAutoIncrementPrimary ? columns.Where(c1 => c1.Key != tableMetadata.Primary.First()) : columns)
				.ToArray();

			var columnsSql = string.Join(", ", insertColumns.Select(c => $"{Escape}{GetNameSql(c.Key)}{Escape}"));

			var columnsParamsSql = string.Join(", ", insertColumns.Select(p => $"@{p.Key}"));

			var tableSql = GenerateTableSql(tableMetadata);

			var sql =
				$"INSERT {(ignoreDuplicate ? "IGNORE" : "")} INTO {tableSql} ({columnsSql}) VALUES ({columnsParamsSql});";
			return sql;
		}

		/// <summary>
		/// Generate SQL statements to update data
		/// </summary>
		/// <param name="tableMetadata">table metadata</param>
		/// <returns>SQL statement</returns>
		protected virtual string GenerateUpdateSql(TableMetadata tableMetadata)
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
			var sql = $"UPDATE {tableSql} SET {setCols} WHERE {where};";
			return sql;
		}

		/// <summary>
		/// Generate SQL statements that insert new data or update old data
		/// </summary>
		/// <param name="tableMetadata">table metadata</param>
		/// <returns>SQL statement</returns>
		protected virtual string GenerateInsertAndUpdateSql(TableMetadata tableMetadata)
		{
			if (!tableMetadata.HasPrimary)
			{
				Logger?.LogWarning("The entity does not have a primary key set and the InsertAndUpdate statement cannot be generated");
				return null;
			}

			var columns = tableMetadata.Columns;
			var isAutoIncrementPrimary = tableMetadata.IsAutoIncrementPrimary;
			// Remove the auto-incrementing primary key
			var insertColumns =
				(isAutoIncrementPrimary ? columns.Where(c1 => c1.Key != tableMetadata.Primary.First()) : columns)
				.ToArray();

			var columnsSql = string.Join(", ", insertColumns.Select(c => $"{Escape}{GetNameSql(c.Key)}{Escape}"));

			var columnsParamsSql = string.Join(", ", insertColumns.Select(p => $"@{p.Key}"));

			var tableSql = GenerateTableSql(tableMetadata);
			var setCols = string.Join(", ",
				insertColumns.Select(c => $"{Escape}{GetNameSql(c.Key)}{Escape}= @{c.Key}"));
			var sql =
				$"INSERT INTO {tableSql} ({columnsSql}) VALUES ({columnsParamsSql}) ON DUPLICATE key UPDATE {setCols};";
			return sql;
		}

		/// <summary>
		/// Remove the auto-incrementing primary key
		/// </summary>
		/// <param name="tableMetadata">table metadata</param>
		/// <returns>SQL statement</returns>
		protected virtual string GenerateTableSql(TableMetadata tableMetadata)
		{
			var tableName = GetNameSql(GetTableName(tableMetadata));
			var database = GetNameSql(tableMetadata.Schema.Database);
			return string.IsNullOrWhiteSpace(database)
				? $"{Escape}{tableName}{Escape}"
				: $"{Escape}{database}{Escape}.{Escape}{tableName}{Escape}";
		}

		/// <summary>
		/// SQL to generate the column
		/// </summary>
		/// <returns>SQL statement</returns>
		protected virtual string GenerateColumnSql(Column column, bool isPrimary)
		{
			var columnName = GetNameSql(column.Name);
			var dataType = GenerateDataTypeSql(column.Type, column.Length);
			if (isPrimary || column.Required)
			{
				dataType = $"{dataType} NOT NULL";
			}

			return $"{Escape}{columnName}{Escape} {dataType}";
		}

		/// <summary>
		/// Generate SQL for data types
		/// </summary>
		/// <param name="type">type of data</param>
		/// <param name="length">Data length</param>
		/// <returns>SQL statement</returns>
		protected virtual string GenerateDataTypeSql(string type, int length)
		{
			string dataType;

			switch (type)
			{
				case BoolType:
				{
					dataType = "BOOL";
					break;
				}
				case DateTimeType:
				case DateTimeOffsetType:
				{
					dataType = "TIMESTAMP DEFAULT CURRENT_TIMESTAMP";
					break;
				}

				case DecimalType:
				{
					dataType = "DECIMAL(18,2)";
					break;
				}
				case DoubleType:
				{
					dataType = "DOUBLE";
					break;
				}
				case FloatType:
				{
					dataType = "FLOAT";
					break;
				}
				case IntType:
				{
					dataType = "INT";
					break;
				}
				case LongType:
				{
					dataType = "BIGINT";
					break;
				}
				case ByteType:
				{
					dataType = "INT";
					break;
				}
				case BlobType:
					{
						dataType = "LONGBLOB";
						break;
					}
				default:
				{
					dataType = length <= 0 || length > 8000 ? "LONGTEXT" : $"VARCHAR({length})";
					break;
				}
			}

			return dataType;
		}
	}
}
