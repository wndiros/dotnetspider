using System.IO;
using System.Threading.Tasks;
using DotnetSpider.DataFlow.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace DotnetSpider.DataFlow
{
	/// <summary>
	/// File save analysis results (all analysis results)
	/// Save path: [Current program running directory]/files/[Task ID]/[request.hash].data
	/// </summary>
	public class FileStorage : FileStorageBase
	{
		public static IDataFlow CreateFromOptions(IConfiguration configuration)
		{
			return new FileStorage();
		}

		public override async Task HandleAsync(DataFlowContext context)
		{
			if (IsNullOrEmpty(context))
			{
				Logger.LogWarning("The data flow context does not contain the analysis result");
				return;
			}

			var file = Path.Combine(GetDataFolder(context.Request.Owner),
				$"{context.Request.Hash}.json");
			using var writer = OpenWrite(file);
			var items = context
				.GetData();
			await writer.WriteLineAsync(System.Text.Json.JsonSerializer.Serialize(new
			{
				uri = context.Request.RequestUri.ToString(), data = items
			}));
		}
	}
}
