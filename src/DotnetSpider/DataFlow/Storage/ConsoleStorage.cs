using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace DotnetSpider.DataFlow
{
	/// <summary>
	/// Console print parsing results (all parsing results)
	/// The console prints the results of the analysis  (all analysis results)
	/// </summary>
	public class ConsoleStorage : DataFlowBase
	{
		public static IDataFlow CreateFromOptions(IConfiguration configuration)
		{
			return new ConsoleStorage();
		}

		public override Task InitializeAsync()
		{
			return Task.CompletedTask;
		}

		public override Task HandleAsync(DataFlowContext context)
		{
			if (IsNullOrEmpty(context))
			{
				Logger.LogWarning("The data flow context does not contain the analysis result");
				return Task.CompletedTask;
			}

			var data = context.GetData();

			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.WriteLine(
				$"{Environment.NewLine}DATA: {System.Text.Json.JsonSerializer.Serialize(data)}");

			return Task.CompletedTask;
		}
	}
}
