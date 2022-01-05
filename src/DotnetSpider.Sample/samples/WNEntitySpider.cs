using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DotnetSpider.DataFlow;
using DotnetSpider.DataFlow.Parser;
using DotnetSpider.DataFlow.Parser.Formatters;
using DotnetSpider.DataFlow.Storage;
using DotnetSpider.Downloader;
using DotnetSpider.Http;
using DotnetSpider.Infrastructure;
using DotnetSpider.MySql.Scheduler;
using DotnetSpider.Scheduler;
using DotnetSpider.Scheduler.Component;
using DotnetSpider.Selector;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;

namespace DotnetSpider.Sample.samples
{
	public class WNEntitySpider : Spider
	{
		public static async Task RunAsync()
		{
			var builder = Builder.CreateDefaultBuilder<WNEntitySpider>(options =>
			{
				options.Speed = 1;
			});
			builder.UseDownloader<HttpClientDownloader>();
			builder.UseSerilog();
			builder.IgnoreServerCertificateError();
			builder.UseQueueDistinctBfsScheduler<HashSetDuplicateRemover>();
			await builder.Build().RunAsync();
		}

		public static async Task RunMySqlQueueAsync()
		{
			var builder = Builder.CreateDefaultBuilder<WNEntitySpider>(options =>
			{
				options.Speed = 1;
			});
			builder.UseDownloader<HttpClientDownloader>();
			builder.UseSerilog();
			builder.IgnoreServerCertificateError();
			builder.UseMySqlQueueBfsScheduler(x =>
			{
				x.ConnectionString = builder.Configuration["SchedulerConnectionString"];
			});
			await builder.Build().RunAsync();
		}

		public WNEntitySpider(IOptions<SpiderOptions> options, DependenceServices services,
			ILogger<Spider> logger) : base(
			options, services, logger)
		{
		}

		protected override async Task InitializeAsync(CancellationToken stoppingToken = default)
		{
			AddDataFlow(new DataParser<springestcategories>());
			AddDataFlow(GetDefaultStorage());
			await AddRequestsAsync(
				new Request(
					"https://springest.de", new Dictionary<string, object> {{ "website", "Springest"}}));
		}

		protected override SpiderId GenerateSpiderId()
		{
			return new(ObjectId.CreateId().ToString(), "Categories");
		}

		//protected class ListNewsParser : DataParser
		//{
		//	public override Task InitializeAsync()
		//	{
		//		// AddRequiredValidator("news\\.cnblogs\\.com/n/page");
		//		AddRequiredValidator((request =>
		//		{
		//			var host = request.RequestUri.Host;
		//			var regex = host + "/$";
		//			return Regex.IsMatch(request.RequestUri.ToString(), regex);
		//		}));
		//		// if you want to collect every pages
		//		// AddFollowRequestQuerier(Selectors.XPath(".//div[@class='pager']"));
		//		return Task.CompletedTask;
		//	}

		//	protected override Task ParseAsync(DataFlowContext context)
		//	{
		//		var newsList = context.Selectable.SelectList(Selectors.XPath(".//div[@class='news_block']"));
		//		foreach (var news in newsList)
		//		{
		//			var title = news.Select(Selectors.XPath(".//h2[@class='news_entry']"))?.Value;
		//			var url = news.Select(Selectors.XPath(".//h2[@class='news_entry']/a/@href"))?.Value;
					

		//			if (!string.IsNullOrWhiteSpace(url))
		//			{
		//				var request = context.CreateNewRequest(new Uri(url));
		//				request.Properties.Add("title", title);
		//				request.Properties.Add("url", url);					

		//				context.AddFollowRequests(request);
		//			}
		//		}

		//		return Task.CompletedTask;
		//	}
		//}

		/// <summary>
		/// docker run --name mariadbDSpiderDemo -p 3306:3306 -volume -v mariadbtest:/var/lib/mysql -e MYSQL_ROOT_PASSWORD=clamawu! -d mariadb
		/// </summary>

		[Schema("Springest", "Categories")]
		[EntitySelector(Expression = ".//li[@class='category-list__item']", Type = SelectorType.XPath)]		
		//[GlobalValueSelector(Expression = ".//a[@class='category-list__title-link']/@href", Name = "URL", Type = SelectorType.XPath)]
		//[GlobalValueSelector(Expression = "//a[@class='category-list__title-link']//text()", Name = "Title", Type = SelectorType.XPath)]
		//[FollowRequestSelector(Expressions = new[] { "//div[@class='pager']" })]
		public class springestcategories : EntityBase<springestcategories>
		{

			//
			/// <summary>
			/// Category Data			
			/// </summary>
			///

			public int Id { get; set; }

			[Required]
			//[ValueSelector(Expression = ".//h2[@class='news_entry']/a/@href")]
			[ValueSelector(Expression = "//a[@class='category-list__title-link']/@href")]
			//[ValueSelector(Expression = "Title", Type = SelectorType.Environment)]
			public string url { get; set; }

			[Required]
			//[ValueSelector(Expression = "Title", Type = SelectorType.Environment)]
			[ValueSelector(Expression = ".//a[@class='category-list__title-link']//text()")]
			public string page_title { get; set; }

			[Required]
			//[ValueSelector(Expression = ".//h2[@class='news_entry']/a/@href")]
			[ValueSelector(Expression = "int", Type = SelectorType.Environment)]
			public int maincategory { get; set; }

			[ValueSelector(Expression = "DATETIME", Type = SelectorType.Environment)]
			public DateTime created_at { get; set; }

			[ValueSelector(Expression = "DATETIME", Type = SelectorType.Environment)]
			public DateTime visited_at { get; set; }

			[ValueSelector(Expression = "INT", Type = SelectorType.Environment)]
			public int level { get; set; }
		}
	}
}
