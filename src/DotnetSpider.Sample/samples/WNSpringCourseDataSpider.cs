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
				//if (i == 10)
				//{
				//	break;
				//}

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

				string subcatfilter = "//*[@class='breadcrumb__item lvl-2 list-item ']/a/span/text()"
										+ " | "
										+ "//*[@class='breadcrumb__item lvl-2 list-item breadcrumb-subject has-dropdown']/a/span/text()";

				string subcatfilter1 = "//*[@class='breadcrumb__item lvl-1 list-item breadcrumb__item--current current']/.//span/text()";
				//string descritiptionfilter = "string(//*[@class='product__description'])";
				string descritiptionfilter = "//*[@class='product__description']";
				// string pagingfilter = "//li[@class='pagination__item hide-on-small']";
				string pagingfilter = "//*[@class='pagination__button button--link']/@href";
				string productfilter = "//*[@class='pagination__item hide-on-small']/a/@href";
				//string trainingidfilter = "string(.//*[@class='content content--medium aligned-right']/@ID)";
				//string trainingidfilter = ".//*[@class='content content--medium aligned-right']/@ID";
				//string pricefilter = "//*[contains(@class, 'detail-price')]/*[contains(@class, 'total')]/text()";
				string pricefilter = "//*[@class='price-information']/span[@class='price']/text()";
				string vatinfofilter = "//*[@class='vat-info']/text()";
				string pricemetainfofilter = "//*[@class='tooltip align-left'][1]/*/*[@class='price-tooltip']/li[2]";
				string vatmetainfofilter = "//*[@class='tooltip align-left'][1]/*/*[@class='price-tooltip']/li[1]";


				string languagefilter = ".//*[@class='data-list']/*[@class='data-list__key' and text()='Sprache']/following-sibling::dd[1]";
				string timeofdayfilter = ".//*[@class='data-list']/*[@class='data-list__key' and text()='Tageszeit']/following-sibling::dd[1]";
				string producttypefilter = ".//*[@class='data-list']/*[@class='data-list__key' and text()='Produkttyp']/following-sibling::dd[1]";
				string maxparticipantsfilter = ".//*[@class='data-list']/*[@class='data-list__key' and text()='Teilnehmerzahl']/following-sibling::dd[1]";
				string trainingidfilter = ".//*[@class='data-list']/*[@class='data-list__key' and @id='training-id']/following-sibling::dd[1]";
				//string productprovidernamefilter = "//*[@class='product__provider']/a/@href";
				string productproviderurifilter = "//*[@class='product__provider']/a/@href";
				//string productratingfilter = "//*[@class='align-right tooltip align-top'][1]/span/text()]";
				string productratingfilter = "//*[@class='align-right tooltip align-top'][1]//*/strong[1]/text()";
				string productratingcountfilter = "//*[@class='align-right tooltip align-top'][1]//*/strong[2]/text()";
				string totaltimemetainfofilter = "//*[@class='data-list__value metadata-total-time']/text()";
				string timedurationinfofilter = ".//*[@class='data-list']/*[@class='data-list__key' and text()='Laufzeit / Stunden pro Tag']/following-sibling::dd[1]/text()";

				string startdatestableitemfilter = "//*[@class='startdates-table__item']";
				string locationfilter = "//div[@class='startdates-table__location']";
				string startdatefilter = "//*[@class='startdates-table__date']";


				int level = 1;

				var prodList = context.Selectable.SelectList(Selectors.XPath("" +
					"//*[@class='as-h3 product-item__title']/a/@href" +
					"|" +
					"//li[@class='pagination__item hide-on-small']//a/@href"));

				

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

					var pagingurl = context.Selectable.Select(Selectors.XPath(pagingfilter))?.Value;
					if (pagingurl != null)
					{
						var request = context.CreateNewRequest(new Uri(pagingurl));
						request.Properties.Add("url", pagingurl);
						context.AddFollowRequests(request);
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

					try
					{ //var productdata = context.Selectable.Select(Selectors.XPath(productfilter));
						var url = context.Selectable.Select(Selectors.XPath(urlfilter))?.Value;
						var title = context.Selectable.Select(Selectors.XPath(titlefilter))?.Value;
						var maincategory = context.Selectable.Select(Selectors.XPath(maincatfilter))?.Value;
						var subcategory = context.Selectable.Select(Selectors.XPath(subcatfilter))?.Value;
						var subcategory1 = context.Selectable.Select(Selectors.XPath(subcatfilter1))?.Value;
						var description = context.Selectable.Select(Selectors.XPath(descritiptionfilter))?.Value;
						//var trainingid = context.Selectable.Select(Selectors.XPath(trainingidfilter))?.Value;
						var price = context.Selectable.Select(Selectors.XPath(pricefilter))?.Value;
						var vatinfo = context.Selectable.Select(Selectors.XPath(vatinfofilter))?.Value;

						var language = context.Selectable.Select(Selectors.XPath(languagefilter))?.Value;
						var timeofday = context.Selectable.Select(Selectors.XPath(timeofdayfilter))?.Value;
						var producttype = context.Selectable.Select(Selectors.XPath(producttypefilter))?.Value;
						var maxparticipants = context.Selectable.Select(Selectors.XPath(maxparticipantsfilter))?.Value;
						var trainingid = context.Selectable.Select(Selectors.XPath(trainingidfilter))?.Value;
						var productprovideruri = context.Selectable.Select(Selectors.XPath(productproviderurifilter))?.Value;
						var productrating = context.Selectable.Select(Selectors.XPath(productratingfilter))?.Value;
						var productratingcount = context.Selectable.Select(Selectors.XPath(productratingcountfilter))?.Value;
						//
						// Name was not consistentently available, so this feature is removed vor now
						//
						//var productprovidername = context.Selectable.Select(Selectors.XPath(productprovidernamefilter))?.Value;
						//

						var pricemetainfo = context.Selectable.Select(Selectors.XPath(pricemetainfofilter))?.Value;
						var vatmetainfo = context.Selectable.Select(Selectors.XPath(vatmetainfofilter))?.Value;

						var totaltimemetainfo = context.Selectable.Select(Selectors.XPath(totaltimemetainfofilter))?.Value;
						var timedurationinfo = context.Selectable.Select(Selectors.XPath(timedurationinfofilter))?.Value;

						var starttableitemlist = context.Selectable.SelectList(Selectors.XPath(startdatestableitemfilter));

						if (starttableitemlist != null)
						{
							foreach (var item in starttableitemlist)
							{
								results.Add(new ProductEntity
								{
									url = url,
									product_title = title,
									trainingid = trainingid,
									price = price,
									vatinfo = vatinfo,
									pricemetainfo = pricemetainfo,
									vatmetainfo = vatmetainfo,
									//maincategory = Int32.Parse(context.Request.Properties["maincategory"]?.ToString()?.Trim()),
									language = language,
									timeofday = timeofday,
									totaltimemetainfo = totaltimemetainfo,
									timedurationinfo = timedurationinfo,
									location = item.Select(Selectors.XPath(locationfilter))?.Value,
									startdate = item.Select(Selectors.XPath(startdatefilter))?.Value,
									producttype = producttype,
									productprovideruri = productprovideruri,
									rating = productrating,
									ratingcount = productratingcount,
									//
									//productprovidername = productprovidername,
									//
									maxparticipants = maxparticipants,
									maincategory = maincategory,
									subcategory = subcategory,
									subcategory1 = subcategory1,
									created_at = DateTime.Now,
									visited_at = DateTime.Now,
									description = description

								});
							}
							
						}
						else
						{
							results.Add(new ProductEntity
							{
								url = url,
								product_title = title,
								trainingid = trainingid,
								price = price,
								vatinfo = vatinfo,
								pricemetainfo = pricemetainfo,
								vatmetainfo = vatmetainfo,
								//maincategory = Int32.Parse(context.Request.Properties["maincategory"]?.ToString()?.Trim()),
								language = language,
								timeofday = timeofday,
								totaltimemetainfo = totaltimemetainfo,
								timedurationinfo = timedurationinfo,
								producttype = producttype,
								productprovideruri = productprovideruri,
								rating = productrating,
								ratingcount = productratingcount,
								//
								//productprovidername = productprovidername,
								//
								maxparticipants = maxparticipants,
								maincategory = maincategory,
								subcategory = subcategory,
								subcategory1 = subcategory1,
								created_at = DateTime.Now,
								visited_at = DateTime.Now,
								description = description

							});




						}

						AddParsedResult(context, results);

						//*[@class='pagination__button button--link']"

						//var pagingurl = context.Selectable.SelectList(Selectors.XPath(pagingfilter))?.Value;
						
							

					}
					catch (Exception e)
					{
						
						Logger.LogError($"handle XPATH selection failed failed: {e}");
					}


					
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
			public string product_title { get; set; }
			public string productprovideruri { get; set; }
			public string productprovidername  { get; set; }



			[StringLength(8192)]
			public string description { get; set; }
			public string price { get; set; }

			public string priceinfo { get; set; }

			public string pricemetainfo { get; set; }

			public string vatinfo { get; set; }
			public string vatmetainfo { get; set; }
			public string rating { get; set; }
			public string ratingcount { get; set; }
			public string language { get; set; }
			public string timeofday { get; set; }

			public string totaltimemetainfo { get; set; }

			public string location { get; set; }

			public string startdate { get; set; }


			public string timedurationinfo { get; set; }
			public string producttype { get; set; }
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
