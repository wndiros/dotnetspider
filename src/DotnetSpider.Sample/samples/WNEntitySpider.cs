using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
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
				options.Depth = 2;
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


		/// <summary>
		/// docker run --name mariadbDSpiderDemo -p 3306:3306 -volume -v mariadbtest:/var/lib/mysql -e MYSQL_ROOT_PASSWORD=clamawu! -d mariadb
		/// </summary>

		[Schema("Springest", "Categories")]
		[EntitySelector(Expression = ".//li[@class='category-list__item']", Type = SelectorType.XPath)]		
		//[GlobalValueSelector(Expression = "//a[@class='category-list__title-link']/@href", Name = "URL", Type = SelectorType.XPath)]
		//[GlobalValueSelector(Expression = ".//a[@class='category-list__title-link']//text()", Name = "Title", Type = SelectorType.XPath)]
		//[FollowRequestSelector(Expressions = new[] { "//a[@class='category-list__title-link']/@href" })]
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
			//[ValueSelector(Expression = "URL", Type = SelectorType.Environment)]
			public string url { get; set; }

			[Required]
			[ValueSelector(Expression = ".//a[@class='category-list__title-link']//text()")]
			//[ValueSelector(Expression = "Title", Type = SelectorType.Environment)]
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
