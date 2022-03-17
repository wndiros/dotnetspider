using System.Threading.Tasks;
using DotnetSpider.Infrastructure;
using Microsoft.Extensions.Logging;

namespace DotnetSpider.DataFlow
{
	/// <summary>
	/// Data stream processor base class
	/// </summary>
	public abstract class DataFlowBase : IDataFlow
	{
		protected ILogger Logger { get; private set; }

		/// <summary>
		/// initialization
		/// </summary>
		/// <returns></returns>
		public abstract Task InitializeAsync();

		public void SetLogger(ILogger logger)
		{
			logger.NotNull(nameof(logger));
			Logger = logger;
		}

		/// <summary>
		/// stream processing
		/// </summary>
		/// <param name="context">processing context</param>
		/// <returns></returns>
		public abstract Task HandleAsync(DataFlowContext context);

		/// <summary>
		/// is empty
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		protected virtual bool IsNullOrEmpty(DataFlowContext context)
		{
			return context.IsEmpty;
		}

		/// <summary>
		/// 释放
		/// </summary>
		public virtual void Dispose()
		{
		}
	}
}
