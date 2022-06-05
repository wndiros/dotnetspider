using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DotnetSpider.DataFlow
{
	/// <summary>
	/// data stream processor
	/// </summary>
	public interface IDataFlow : IDisposable
    {
		/// <summary>
		/// initialization
		/// </summary>
		/// <returns></returns>
		Task InitializeAsync();

		/// <summary>
		/// set log
		/// </summary>
		/// <param name="logger"></param>
		void SetLogger(ILogger logger);

		/// <summary>
		/// stream processing
		/// </summary>
		/// <param name="context">processing context</param>
		/// <returns></returns>
		Task HandleAsync(DataFlowContext context);
    }
}
