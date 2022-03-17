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
	public class WNSpringCatSpider : Spider
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

		public WNSpringCatSpider(IOptions<SpiderOptions> options, DependenceServices services,
			ILogger<Spider> logger) : base(
			options, services, logger)
		{
		}

		protected override async Task InitializeAsync(CancellationToken stoppingToken = default)
		{

			AddDataFlow(new SpringCatParser());
			//AddDataFlow(new CategoriesParser());
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
				//base.InitializeAsync();
				

				//AddFollowRequestQuerier(Selectors.XPath("//a[@class='category-list__title-link']/@href"));
				//AddFollowRequestQuerier(Selectors.XPath(".//a[@class='subject-list__link']"));
				//AddFollowRequestQuerier(Selectors.XPath("."));


				return Task.CompletedTask;

			}

			protected override Task ParseAsync(DataFlowContext context)
			{
				//base.ParseAsync(context);

				//AddFollowRequestQuerier(Selectors.XPath(".//li[@class='category-list__item']"));



				var typeName = typeof(CategoriesEntity).FullName;
				var results = 1;

				var catList = context.Selectable.SelectList(Selectors.XPath(".//li[@class='category-list__item']"));
				string urlfilter = "//a[@class='category-list__title-link']/@href";
				string titlefilter = ".//a[@class='category-list__title-link']//text()";
				string maincatfilter = ".//li[@class='breadcrumb__item lvl-1 list-item breadcrumb__item--current current']/a/span[@itemprop='title']/text()";
				int level = 1;

				if (catList == null)
				{
					var catList2 = context.Selectable.SelectList(Selectors.XPath(".//li[@class='subject-title subject-list__item']"));
					if (catList2 == null)
					{
						var catList3 = context.Selectable.SelectList(Selectors.XPath(".//li[@class='result-item product-item']"));
						catList = catList3;
						urlfilter = ".//h2[@class='as-h3 product-item__title']/a/@href";
						titlefilter = ".//h2[@class='as-h3 product-item__title']/a/@title";
						maincatfilter = ".//li[@class='breadcrumb__item lvl-1 list-item breadcrumb__item--current current']/a/span[@itemprop='title']/text()";
						level = 3;
					}
					else
					{
						catList = catList2;
						urlfilter = "//a[@class='subject-list__link']/@href";
						titlefilter = "//a[@class='subject-list__link']/span/text()";
						maincatfilter = "//li[@class='breadcrumb__item lvl-1 list-item breadcrumb__item--current current']/a/span[@itemprop='title']/text()";
						               
						level = 2;
					}
				}

				if (catList != null)
				{

					foreach (var category in catList)
					{
						var url = category.Select(Selectors.XPath(urlfilter))?.Value;
						var title = category.Select(Selectors.XPath(titlefilter))?.Value;						
						var maincategory = category.Select(Selectors.XPath(maincatfilter))?.Value;
						if (!string.IsNullOrWhiteSpace(url))
						{
							if (level == 1)
							{


								var request = context.CreateNewRequest(new Uri(url));
								request.Properties.Add("url", url);
								request.Properties.Add("page_title", title);
								request.Properties.Add("maincategory", maincategory);

								context.AddFollowRequests(request);
							}

							context.AddData(typeName,
							new CategoriesEntity
							{
								url = url,
								page_title = title,
							//maincategory = Int32.Parse(context.Request.Properties["maincategory"]?.ToString()?.Trim()),
								maincategory = maincategory,
								created_at = DateTime.Now,
								visited_at = DateTime.Now,
							}); ; ;

							results++;

							//results.Add(request);
							//		//AddFollowRequestQuerier(Selectors.XPath(".//div[@class='pager']"));
							if (results > 9)
							{
								break;
							}
						}


					}

				}

				//context.AddFollowRequests(results);



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
					Console.WriteLine($"URL: {category.url}, TITLE: {category.page_title},  Maincategory: {category.maincategory}");
				}

				return Task.CompletedTask;
			}
		}
		

		protected class CategoriesParser : DataParser
		{
			public override Task InitializeAsync()
			{
				//AddRequiredValidator("news\\.cnblogs\\.com/n/\\d+");
				return Task.CompletedTask;
			}

			protected override Task ParseAsync(DataFlowContext context)
			{
				var typeName = typeof(CategoriesEntity).FullName;

				
				context.AddData(typeName,
					new CategoriesEntity
					{
						url = context.Request.RequestUri.ToString(),
						page_title = context.Request.Properties["page_title"]?.ToString()?.Trim(),
						//maincategory = Int32.Parse(context.Request.Properties["maincategory"]?.ToString()?.Trim()),
						//maincategory = Int32.Parse(context.Request.Properties["maincategory"]?.ToString()?.Trim()),
						maincategory = context.Request.Properties["maincategory"]?.ToString()?.Trim(),
						created_at = DateTime.Now,
						visited_at = DateTime.Now,						
					}) ;; ;
				return Task.CompletedTask;
			}
		}
		/// <summary>
		/// docker run --name mariadbDSpiderDemo -p 3306:3306 -volume -v mariadbtest:/var/lib/mysql -e MYSQL_ROOT_PASSWORD=clamawu! -d mariadb
		/// </summary>

		[Schema("Springest", "Categories")]
		protected class CategoriesEntity : EntityBase<CategoriesEntity>
		{

			//
			/// <summary>
			/// Category Data			
			/// </summary>
			///

			public int Id { get; set; }
			public string url { get; set; }			
			public string page_title { get; set; }			
			public string maincategory { get; set; }			
			public DateTime created_at { get; set; }			
			public DateTime visited_at { get; set; }
			public int level { get; set; }
		}
	}
}
