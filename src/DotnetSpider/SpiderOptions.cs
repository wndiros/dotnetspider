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
		/// 请求重试次数限制
		/// </summary>
		public int RetriedTimes { get; set; } = 3;

		/// <summary>
		/// 当队列中无链接超时后退出爬虫
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
		/// 是否去除外链
		/// </summary>
		public bool RemoveOutboundLinks { get; set; } = false;

		/// <summary>
		/// 存储器类型: FullTypeName, AssemblyName
		/// </summary>
		public string StorageType { get; set; } = "DotnetSpider.MySql.MySqlEntityStorage, DotnetSpider.MySql";

		/// <summary>
		/// 获取新代码的时间间隔
		/// </summary>
		public int RefreshProxy { get; set; } = 30;
	}
}
