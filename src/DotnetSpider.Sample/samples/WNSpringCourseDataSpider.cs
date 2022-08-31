using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using DotnetSpider.DataFlow;
using DotnetSpider.DataFlow.Parser;
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
	public class WNSpringCourseDataSpider : Spider
	{
		IDataFlowWN DataSource;
		public static async Task RunAsync()
		{
			var builder = Builder.CreateDefaultBuilder<WNSpringCourseDataSpider>(options =>
			{
				options.Speed = 0.1;
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
				options.Speed = 0.1;
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

		public WNSpringCourseDataSpider(IOptions<SpiderOptions> options, DependenceServices services,
			ILogger<Spider> logger) : base(
			options, services, logger)
		{
		}

		protected override async Task InitializeAsync(CancellationToken stoppingToken = default)
		{

			Type objType = typeof(WNSpringCatEntityStorage);

			//// Print the assembly full name.
			Console.WriteLine($"Assembly full name:\n   {objType.Assembly.FullName}.");

			//// Print the assembly qualified name.
			Console.WriteLine($"Assembly qualified name:\n   {objType.AssemblyQualifiedName}.");

			WNSpringCatEntityStorage wnEntityStorage = (WNSpringCatEntityStorage) GetDefaultStorage();
			AddDataFlow(new ProductsParser());
			AddDataFlow(wnEntityStorage);

			//
			// get Links from Database
			//
			IEnumerable<dynamic> results = await  wnEntityStorage.GetData("select * from Categories");

			var i = 0;
			foreach (var row in results)
			{
							
				await AddRequestsAsync(
					new Request(row.url, new Dictionary<string, object> { { "website", "Springest" } })
					//new Request(row.url)
					{ Timeout = 10000 });
				i += 1;
				if (i == 10)
				{
					break;
				}

			}
			
		}

		protected override SpiderId GenerateSpiderId()
		{
			return new(ObjectId.CreateId().ToString(), "Categories");
		}

		//public class sprintcatparser : DataParser<springestcategories>
		//{


		//}


		protected class ProductsParser : DataParser<ProductEntity>
		{
			//private readonly IDataFlowWN _wnEntityStorage;

			//public ProductsParser(IDataFlowWN entityStorage)
			//{
			//	//Was planed for querying if a specific record exists
			//	//Not necessary as unique records can be enforced by an unique index in the DB table
			//	//the definition of the index can be done in the custom entity Class
			//	_wnEntityStorage =  entityStorage;
				


			//}
			public override Task InitializeAsync()
			{

				//AddRequiredValidator((request =>
				//{
				//	var host = request.RequestUri.Host;
				//	var regex = host + "/$";
				//	return Regex.IsMatch(request.RequestUri.ToString(), regex);
				//}));


				// if you want to collect all pages
				//AddFollowRequestQuerier(Selectors.XPath(".//li[@class='category-list__item']"));
				//base.InitializeAsync();


				//AddFollowRequestQuerier(Selectors.XPath("//a[@class='category-list__title-link']/@href"));
				//AddFollowRequestQuerier(Selectors.XPath(".//a[@class='subject-list__link']"));
				//AddFollowRequestQuerier(Selectors.XPath("."));


				return Task.CompletedTask;

			}

			protected override Task ParseAsync(DataFlowContext context)
			{
				//var typeName = typeof(CategoriesEntity).FullName;
				List<ProductEntity> results = new List<ProductEntity>();
				var typeName = typeof(ProductEntity).FullName;
				var count = 1;

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
						maincatfilter = ".//li[@class='breadcrumb__item lvl-3 list-item']/a[@class='breadcrumb__link nav-link']/span[@itemprop='title']/text()";
						level = 3;
					}
					else
					{
						catList = catList2;
						urlfilter = "//a[@class='subject-list__link']/@href";
						titlefilter = "//a[@class='subject-list__link']/span/text()";
						maincatfilter = "//li[@class='breadcrumb__item lvl-1 list-item breadcrumb__item--current current']/a/*[@itemprop='title']/text()";
						//maincatfilter = "//li[@class='breadcrumb__item lvl-1 list-item breadcrumb__item--current current']/a/span/text()";
						//breadcrumb__item lvl-1 list-item breadcrumb__item--current current
						////breadcrumb__link nav-link 
						level = 2;
					}
				}

				if (catList != null)
				{

					foreach (var category in catList)
					{
						var url = category.Select(Selectors.XPath(urlfilter))?.Value;
						var title = category.Select(Selectors.XPath(titlefilter))?.Value;
						var maincategory = context.Selectable.Select(Selectors.XPath(maincatfilter))?.Value;
						//var subcategory = context.Selectable.Select(Selectors.XPath(subcatfilter))?.Value;



						//category.Select(Selectors.XPath(maincatfilter))?.Value;
						if (!string.IsNullOrWhiteSpace(url))
						{
							if (level == 1)
							{


								var request = context.CreateNewRequest(new Uri(url));
								request.Properties.Add("url", url);
								request.Properties.Add("page_title", title);
								request.Properties.Add("maincategory", maincategory);
								request.Properties.Add("level", level);

								context.AddFollowRequests(request);
							}

							results.Add(new ProductEntity
							{
								url = url,
								product_title = title,
								//maincategory = Int32.Parse(context.Request.Properties["maincategory"]?.ToString()?.Trim()),
								maincategory = maincategory,
								//subcategory = subcategory,
								created_at = DateTime.Now,
								visited_at = DateTime.Now,								
							});


							count++;

							//results.Add(request);
							//		//AddFollowRequestQuerier(Selectors.XPath(".//div[@class='pager']"));
							//if (count > 20)
							//{
							//	break;
							//}
						}



					}
					AddParsedResult(context, results);
					//context.AddData(typeName,CatList);

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

				var typeName = typeof(ProductEntity).FullName;
				var data = context.GetData(typeName);
				if (data is ProductEntity category)
				{
					Console.WriteLine($"URL: {category.url}, TITLE: {category.product_title},  Maincategory: {category.maincategory}");
				}

				return Task.CompletedTask;
			}
		}


		/// <summary>
		/// docker run --name mariadbDSpiderDemo -p 3306:3306 -volume -v mariadbtest:/var/lib/mysql -e MYSQL_ROOT_PASSWORD=clamawu! -d mariadb
		/// </summary>

		[Schema("Springest", "Product")]

		protected class ProductEntity : EntityBase<ProductEntity>
		{
			protected override void Configure()
			{
				HasIndex(x => x.url, true);
				//HasIndex(x => new { x.WebSite, x.Guid }, true);
			}
			//
			/// <summary>
			/// Product Data			
			/// </summary>
			///

			public int Id { get; set; }
			public string trainingId { get; set; }
			public string url { get; set; }			
			public string maincategory { get; set; }
			public string subcategory { get; set; }
			public string product_title { get; set; }
			public string description { get; set; }
			public double price { get; set; }
			public string rating { get; set; }
			public string maxparticipants { get; set; }
			public DateTime created_at { get; set; }
			public DateTime visited_at { get; set; }
			
		}
	}

	//public class CourseDataReader : DataFlowBase
	//{
	//	public override async Task InitializeAsync()
	//	{
	//		await using var conn =
	//			new MySqlConnection(
	//				"Database='mysql';Data Source=localhost;password=1qazZAQ!;User ID=root;Port=3306;");
	//		await conn.ExecuteAsync("create database if not exists cnblogs2;");
	//		await conn.ExecuteAsync($@"
	//								create table if not exists cnblogs2.news2
	//								(
	//									id       int auto_increment
	//									primary key,
	//									title    varchar(500)      not null,
	//									url      varchar(500)      not null,
	//									summary  varchar(1000)     null,
	//									views    int               null,
	//									content  varchar(2000)     null
	//								);
	//								");
	//	}

	//	public override async Task HandleAsync(DataFlowContext context)
	//	{
	//		if (IsNullOrEmpty(context))
	//		{
	//			Logger.LogWarning("Dataflow context does not contain parsing results");
	//			return;
	//		}

	//		var typeName = typeof(News).FullName;
	//		var data = (News)context.GetData(typeName);
	//		if (data != null)
	//		{
	//			await using var conn =
	//				new MySqlConnection(
	//					"Database='mysql';Data Source=localhost;password=1qazZAQ!;User ID=root;Port=3306;");
	//			await conn.ExecuteAsync(
	//				$"INSERT IGNORE INTO cnblogs2.news2 (title, url, summary, views, content) VALUES (@Title, @Url, @Summary, @Views, @Content);",
	//				data);
	//		}
	//	}
	//}


}
