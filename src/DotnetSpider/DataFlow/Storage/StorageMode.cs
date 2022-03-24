namespace DotnetSpider.DataFlow.Storage
{
	/// <summary>
	/// Storage type
	/// </summary>
	public enum StorageMode
    {
		/// <summary>
		///  Insert directly
		/// </summary>
		Insert,

		/// <summary>
		/// Insert unique data
		/// </summary>
		InsertIgnoreDuplicate,

		/// <summary>
		///insert if the primary key does not exist, update if it exists
		/// </summary>
		InsertAndUpdate,

		/// <summary>
		/// direct update
		/// </summary>
		Update
	}
}
