using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
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

			WNSpringCatEntityStorage wnEntityStorage = (WNSpringCatEntityStorage)GetDefaultStorage();
			AddDataFlow(new ProductsParser());
			AddDataFlow(wnEntityStorage);

			//
			// get Links from Database
			//
			IEnumerable<dynamic> results = await wnEntityStorage.GetData("select * from Categories where level = 2");

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
				//string urlfilter = ".//*[@class='as-h3 product-item__title']/a/@href"; 
				string urlfilter = ".//*[@class='breadcrumb__item lvl-1 list - item breadcrumb__item--current current']/a/@href";
				string titlefilter = ".//*[@class='content__title product__title']/text()";
				string maincatfilter = "//*[@class='breadcrumb__item lvl-3 list-item ']/a/span/text()";
				string subcatfilter = "//*[@class='breadcrumb__item lvl-2 list-item ']/a/span/text()";
				string subcatfilter1 = "//*[@class='breadcrumb__item lvl-1 list-item breadcrumb__item--current current']/.//span/text()";
				//string descritiptionfilter = "string(//*[@class='product__description'])";
				string descritiptionfilter = "//*[@class='product__description']";
				string pagingfilter = "//li[@class='pagination__item hide-on-small']";
				string productfilter = "//*[@class='pagination__item hide-on-small']/a/@href";
				//string trainingidfilter = "string(.//*[@class='content content--medium aligned-right']/@ID)";
				string trainingidfilter = ".//*[@class='content content--medium aligned-right']/@ID";
				string pricefilter= "//*[contains(@class, 'detail-price')]/*[contains(@class, 'total')]/text()";


				int level = 1;

				//"//*[@class='ajax-wrapper']" +
				//"//*[@class='result-list']/*[@class='result-item product-item']//*[@class='as-h3 product-item__title']/a/@href" +
				//" | " +
				//"//li[@class='pagination__item hide-on-small']//a/@href"))
				var prodList = context.Selectable.SelectList(Selectors.XPath("" +
					"//*[@class='as-h3 product-item__title']/a/@href" +
					"|" +
					"//li[@class='pagination__item hide-on-small']//a/@href"));

				//	" | //*[@class='breadcrumb__item lvl-1 list-item has-dropdown'] /a/span/text()";

				if (prodList == null)
				{
					//
					// If prodlist == null we are on the level 2, the level of a specific seminar (learning product).
					//				

					var productdata = context.Selectable.Select(Selectors.XPath("//*[@class='content content--medium aligned-right']"));
					level = 3;

					urlfilter = "//*[@class='breadcrumb__item lvl-1 list-item breadcrumb__item--current current']/a/@href";
					level = 2;

				}

				//
				// If prodlist != null we are on our level 1 the level of a list with seminars and pagination-links.
				// We extract the links to the products and the next page links
				//
				if (prodList != null)
				{

					foreach (var category in prodList)
					{
						//var url = category.Select(Selectors.XPath(urlfilter))?.Value;
						var url = category.Value;
						//category.Select(Selectors.XPath(maincatfilter))?.Value;
						if (!string.IsNullOrWhiteSpace(url))
						{
							if (level == 1)
							{
								var request = context.CreateNewRequest(new Uri(url));

								request.Properties.Add("url", url);

								context.AddFollowRequests(request);
								

								continue;
							};

							count++;

						}
					}


					//var paginglist = context.Selectable.SelectList(Selectors.XPath(pagingfilter));

					//if (paginglist != null)
					//{
					//	foreach (var page in paginglist)
					//	{
					//		var url = page.Select(Selectors.XPath("//li[@class='pagination__item hide-on-small']//a/@href"))?.Value;
					//		var request = context.CreateNewRequest(new Uri(url));
					//		request.Properties.Add("url", url);
					//		context.AddFollowRequests(request);
					//		continue;
					//	}
					//}					

				}

				if (level == 2)
				{

					//var productdata = context.Selectable.Select(Selectors.XPath(productfilter));
					var url = context.Selectable.Select(Selectors.XPath(urlfilter))?.Value;
					var title = context.Selectable.Select(Selectors.XPath(titlefilter))?.Value;
					var maincategory = context.Selectable.Select(Selectors.XPath(maincatfilter))?.Value;
					var subcategory = context.Selectable.Select(Selectors.XPath(subcatfilter))?.Value;
					var subcategory1 = context.Selectable.Select(Selectors.XPath(subcatfilter1))?.Value;
					var description = context.Selectable.Select(Selectors.XPath(descritiptionfilter))?.Value;
					var trainingid = context.Selectable.Select(Selectors.XPath(trainingidfilter))?.Value;
					var price = context.Selectable.Select(Selectors.XPath(pricefilter))?.Value;

					results.Add(new ProductEntity
					{
						url = url,
						product_title = title,
						trainingid = trainingid,
						price = price,
						//maincategory = Int32.Parse(context.Request.Properties["maincategory"]?.ToString()?.Trim()),
						maincategory = maincategory,
						subcategory = subcategory,
						subcategory1 = subcategory1,
						created_at = DateTime.Now,
						visited_at = DateTime.Now,
						description = description

					});

					AddParsedResult(context, results);
				}


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
				//HasIndex(x => x.url, false);
				//HasIndex(x => new { x.WebSite, x.Guid }, true);
			}
			//
			/// <summary>
			/// Product Data			
			/// </summary>
			///

			public int Id { get; set; }
			public string trainingid { get; set; }
			public string url { get; set; }
			public string maincategory { get; set; }
			public string subcategory { get; set; }
			public string subcategory1 { get; set; }
			public string subcategory2 { get; set; }
			public string product_title { get; set; }

			[StringLength(8192)]
			public string description { get; set; }
			public string price { get; set; }
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
