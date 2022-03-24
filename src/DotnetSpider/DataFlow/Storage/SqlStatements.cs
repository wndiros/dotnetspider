namespace DotnetSpider.DataFlow.Storage
{
	/// <summary>
	/// SQL statements
	/// </summary>
	public class SqlStatements
    {
		/// <summary>
		/// Database name SQL
		/// </summary>
		public string DatabaseSql { get; set; }

		/// <summary>
		/// SQL statement to create table
		/// </summary>
		public string CreateTableSql { get; set; }

		/// <summary>
		/// SQL statement to create database
		/// </summary>
		public string CreateDatabaseSql { get; set; }

		/// <summary>
		/// Inserted SQL statement
		/// </summary>
		public string InsertSql { get; set; }

		/// <summary>
		/// SQL statement to insert and ignore duplicate data
		/// </summary>
		public string InsertIgnoreDuplicateSql { get; set; }

		/// <summary>
		/// Update SQL statement
		/// </summary>
		public string UpdateSql { get; set; }

		/// <summary>
		/// Insert new or update old data SQL statement
		/// </summary>
		public string InsertAndUpdateSql { get; set; }
    }
}
