using System.Collections.Generic;
using System.Linq;

namespace DotnetSpider.DataFlow.Storage
{
	/// <summary>
	/// table metadata
	/// </summary>
	public class TableMetadata
	{
		/// <summary>
		/// entity type name
		/// </summary>
		public string TypeName { get; set; }

		/// <summary>
		/// Schema
		/// </summary>
		public Schema Schema { get; set; }

		/// <summary>
		/// primary key
		/// </summary>
		public HashSet<string> Primary { get; set; }

		/// <summary>
		/// index
		/// </summary>
		public HashSet<IndexMetadata> Indexes { get; }

		/// <summary>
		/// update column
		/// </summary>
		public HashSet<string> Updates { get; set; }

		/// <summary>
		/// attribute name, dictionary of attribute data types
		/// </summary>
		public Dictionary<string, Column> Columns { get; }

		/// <summary>
		/// Whether it is an auto-incrementing primary key
		/// </summary>
		public bool IsAutoIncrementPrimary => Primary != null && Primary.Count == 1 &&
		                                      (Columns[Primary.First()].Type == "Int32" ||
		                                       Columns[Primary.First()].Type == "Int64");

		/// <summary>
		/// Check if a column is in the primary key
		/// </summary>
		/// <param name="column">列</param>
		/// <returns></returns>
		public bool IsPrimary(string column)
		{
			return Primary != null && Primary.Contains(column);
		}

		/// <summary>
		/// Determine if there is a primary key
		/// </summary>
		public bool HasPrimary => Primary != null && Primary.Count > 0;

		/// <summary>
		/// Determine if there is an update column
		/// </summary>
		public bool HasUpdateColumns => Updates != null && Updates.Count > 0;

		/// <summary>
		/// Constructor
		/// </summary>
		public TableMetadata()
		{
			Indexes = new HashSet<IndexMetadata>();
			Columns = new Dictionary<string, Column>();
			Primary = new HashSet<string>();
			Updates = new HashSet<string>();
		}
	}
}
