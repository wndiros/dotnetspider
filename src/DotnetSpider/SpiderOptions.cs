namespace DotnetSpider
{
	public class SpiderOptions
	{
		/// <summary>
		/// Request queue limit
		/// </summary>
		public int RequestedQueueCount { get; set; } = 1000;

		/// <summary>
		/// request link depth limit
		/// </summary>
		public int Depth { get; set; }

		/// <summary>
		/// Request Retry Limit
		/// </summary>
		public int RetriedTimes { get; set; } = 3;

		/// <summary>
		/// Exit the crawler when there is no link in the queue after the timeout
		/// </summary>
		public int EmptySleepTime { get; set; } = 60;

		/// <summary>
		/// Crawler collection speed, 1 means 1 request per second, 0.5 means 0.5 requests per second, 5 means 5 requests per second
		/// </summary>
		public double Speed { get; set; } = 1;

		/// <summary>
		/// How many requests are fetched from the request queue at a time
		/// </summary>
		public uint Batch { get; set; } = 4;

		/// <summary>
		/// Whether to remove external links
		/// </summary>
		public bool RemoveOutboundLinks { get; set; } = false;

		/// <summary>
		/// memory type: FullTypeName, AssemblyName
		/// </summary>
		public string StorageType { get; set; } = "DotnetSpider.MySql.MySqlEntityStorage, DotnetSpider.MySql";

		/// <summary>
		/// Time interval to get new codes
		/// </summary>
		public int RefreshProxy { get; set; } = 30;
	}
}
