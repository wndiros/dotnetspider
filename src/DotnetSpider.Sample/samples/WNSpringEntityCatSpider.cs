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
	public class WNSpringEntityCatSpider : Spider
	{
		public static async Task RunAsync()
		{
			var builder = Builder.CreateDefaultBuilder<WNSpringCatSpider>(options =>
			{
				options.Speed = 1;
				options.Depth = 20;
			});
			builder.UseDownloader<HttpClientDownloader>();
			builder.UseSerilog();
			builder.IgnoreServerCertificateError();
			builder.UseQueueDistinctBfsScheduler<HashSetDuplicateRemover>();
			await builder.Build().RunAsync();
		}

		public static async Task RunMySqlQueueAsync()
		{
			var builder = Builder.CreateDefaultBuilder<WNSpringCatSpider>(options =>
			{
				options.Speed = 1;
				options.Depth = 10;
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

		public WNSpringEntityCatSpider(IOptions<SpiderOptions> options, DependenceServices services,
			ILogger<Spider> logger) : base(
			options, services, logger)
		{
		}

		protected override async Task InitializeAsync(CancellationToken stoppingToken = default)
		{

			AddDataFlow(new SpringCatParser());
			//AddDataFlow(new DataParser<CategoriesEntity>());
			//AddDataFlow(new MyConsoleStorage());
			AddDataFlow(GetDefaultStorage());
			//await AddRequestsAsync(	new Request("https://springest.de/"));
			//
			// set Start Link
			//
			await AddRequestsAsync(
			new Request(
				"https://springest.de", new Dictionary<string, object> { { "website", "Springest" } })
			{ Timeout = 10000 } );



		}

		protected override SpiderId GenerateSpiderId()
		{
			return new(ObjectId.CreateId().ToString(), "Categories");
		}


		protected class SpringCatParser : DataParser<CategoriesEntity>
		{
			public override Task InitializeAsync()
			{

				//AddRequiredValidator((request =>
				//{
				//	var host = request.RequestUri.Host;
				//	var regex = host + "/$";
				//	return Regex.IsMatch(request.RequestUri.ToString(), regex);
				//}));


				// if you want to collect every pages
				//AddFollowRequestQuerier(Selectors.XPath(".//li[@class='category-list__item']"));
				base.InitializeAsync();
				

				//AddFollowRequestQuerier(Selectors.XPath("//a[@class='category-list__title-link']/@href"));
				//AddFollowRequestQuerier(Selectors.XPath(".//a[@class='subject-list__link']"));
				//AddFollowRequestQuerier(Selectors.XPath("."));


				return Task.CompletedTask;

			}

			protected override Task ParseAsync(DataFlowContext context)
			{
				base.ParseAsync(context);

				//AddFollowRequestQuerier(Selectors.XPath(".//li[@class='category-list__item']"));



				var typeName = typeof(CategoriesEntity).FullName;
				var results = new List<Request>();

				var catList = context.Selectable.SelectList(Selectors.XPath(".//li[@class='category-list__item']"));
				if (catList == null)
				{
					var catList2 = context.Selectable.SelectList(Selectors.XPath(".//li[@class='subject-title subject-list__item']"));
					if (catList2 == null)
					{
						var catList3 = context.Selectable.SelectList(Selectors.XPath(".//li[@class='result-item product-item']"));
						catList = catList3;
					}
					else
					{
						catList = catList2;
					}
				}

				foreach (var category in catList)
				{
					var url = category.Select(Selectors.XPath("//a[@class='category-list__title-link']/@href"))?.Value;
					var title = category.Select(Selectors.XPath(".//a[@class='category-list__title-link']//text()"))?.Value;

					if (!string.IsNullOrWhiteSpace(url))
					{
						var request = context.CreateNewRequest(new Uri(url));
						request.Properties.Add("url", url);
						//request.Properties.Add("page_title", title);
						//context.AddFollowRequests(request);


						results.Add(request);
						//		//AddFollowRequestQuerier(Selectors.XPath(".//div[@class='pager']"));
						if (results.Count > 10)
						{
							break;
						}
					}

				}

				context.AddFollowRequests(results);



				return Task.CompletedTask;
			}
		}

		protected class MyConsoleStorage : DataFlowBase
		{
			public override Task InitializeAsync()
			{
				return Task.CompletedTask;
			}

			public override Task HandleAsync(DataFlowContext context)
			{
				if (IsNullOrEmpty(context))
				{
					Logger.LogWarning("Dataflow context does not contain parsing results");
					return Task.CompletedTask;
				}

				var typeName = typeof(CategoriesEntity).FullName;
				var data = context.GetData(typeName);
				if (data is CategoriesEntity category)
				{
					Console.WriteLine($"URL: {category.url}, TITLE: {category.page_title}");
				}

				return Task.CompletedTask;
			}
		}
		protected class EntityParser : DataParser
		{
			public override Task InitializeAsync()
			{
				// AddRequiredValidator("www\\.springest\\.de/n/\\d+");
				//AddFollowRequestQuerier(Selectors.XPath("�.//a[@class='category-list__title-link']/@href"));
				return Task.CompletedTask;
			}

			protected override Task ParseAsync(DataFlowContext context)
			{
				var typeName = typeof(CategoriesEntity).FullName;
				context.AddData(typeName,
					new CategoriesEntity
					{
						url = context.Request.RequestUri.ToString(),
						page_title = context.Request.Properties["page_title"]?.ToString()?.Trim()
							//Summary = context.Request.Properties["summary"]?.ToString()?.Trim(),
							//Views = int.Parse(context.Request.Properties["views"]?.ToString()?.Trim() ?? "0"),
							//Content = context.Selectable.Select(Selectors.XPath(".//div[@id='news_body']")).Value
							?.Trim()
					});
				return Task.CompletedTask;
			}
		}
	
	/// <summary>
	/// docker run --name mariadbDSpiderDemo -p 3306:3306 -volume -v mariadbtest:/var/lib/mysql -e MYSQL_ROOT_PASSWORD=clamawu! -d mariadb
	/// </summary>

	[Schema("Springest", "Categories")]
		[EntitySelector(Expression = ".//li[@class='category-list__item']", Type = SelectorType.XPath)]
		//[GlobalValueSelector(Expression = ".//a[@class='category-list__title-link']/@href", Name = "URL", Type = SelectorType.XPath)]
		//[GlobalValueSelector(Expression = "//a[@class='category-list__title-link']//text()", Name = "Title", Type = SelectorType.XPath)]
		//[FollowRequestSelector(Expressions = new[] { "//div[@class='category-list__links']/a/@href" })]
		public class CategoriesEntity : EntityBase<CategoriesEntity>
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