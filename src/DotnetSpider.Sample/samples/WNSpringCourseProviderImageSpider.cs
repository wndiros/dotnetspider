using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Security.Policy;
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
using static DotnetSpider.Sample.samples.WNSpringCourseProviderSpider;
using static DotnetSpider.Sample.samples.WNSpringEntityCatSpider;

namespace DotnetSpider.Sample.samples
{
	public class WNSpringCourseProviderImageSpider : Spider
	{
		public static async Task RunAsync()
		{
			var builder = Builder.CreateDefaultBuilder<WNSpringCourseProviderImageSpider>(options =>
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
			var builder = Builder.CreateDefaultBuilder<WNSpringCourseProviderSpider>(options =>
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

		public WNSpringCourseProviderImageSpider(IOptions<SpiderOptions> options, DependenceServices services,
			ILogger<Spider> logger) : base(
			options, services, logger)
		{
		}

		protected override async Task InitializeAsync(CancellationToken stoppingToken = default)
		{
			WNSpringCatEntityStorage wnEntityStorage = (WNSpringCatEntityStorage)GetDefaultStorage();

			AddDataFlow(new ProviderImageParser(wnEntityStorage));			
			AddDataFlow(GetDefaultStorage());
			//AddDataFlow(new WNSpringImageStorage());

			IEnumerable<dynamic> results = await wnEntityStorage.GetData("SELECT DISTINCT productprovideruri FROM Product where productprovideruri IS NOT NULL");

			var i = 0;
			foreach (var row in results)
			{

				if (row.productprovideruri != null)
				{
					await AddRequestsAsync(
					new Request(row.productprovideruri, new Dictionary<string, object> { { "website", "Springest" } })
					//new Request(row.url)
					{ Timeout = 10000 });


				}
				
				i += 1;
				if (i == 5)
				{
					break;
				}

			}
		}


		protected class ProviderImageParser : DataParser<ProviderEntity>
		{
			private readonly IDataFlow _wnEntityStorage;
			private readonly HashSet<string> _imageExtensions;
			public string[] ImageExtensions { get; set; } = { "jpeg", "gif", "jpg", "bmp", "png", "ico", "svg" };

			public ProviderImageParser(IDataFlow entityStorage)
			{
				//Was planed for querying if a specific record exists
				//Not necessary as unique records can be enforced by an unique index in the DB table
				//the definition of the index can be done in the custom entity Class
				_wnEntityStorage = entityStorage;
				_imageExtensions = new HashSet<string>(ImageExtensions);
				//_wnEntityStorage.
			}
			public override Task InitializeAsync()
			{

				return Task.CompletedTask;

			}

			protected override Task ParseAsync(DataFlowContext context)
			{

				List<ProviderImageEntity> results = new List<ProviderImageEntity>();
				var typeName = typeof(ProviderImageEntity).FullName;
				var count = 1;

				//var mainblock = context.Selectable.SelectList(Selectors.XPath(".//li[@class='category-list__item']"));
				//string urlfilter = "//a[@class='category-list__title-link']/@href";

				

				string providerimagefilter = "//*[@class='hero__image']/@src";
				var providerimageurl = context.Selectable.Select(Selectors.XPath(providerimagefilter))?.Value;

				//new Request(providerimageurl);
				//var request = context.CreateNewRequest(new Uri(providerimageurl));
				//context.AddFollowRequests(request);


				//
				// Check if we have an ImageUrl on the page
				// if yes, create a new request and store the provider uri for later reference
				//

				if (providerimageurl != null)
				{
					var Request = context.CreateNewRequest(new Uri(providerimageurl));
					Request.Properties.Add("url", context.Request.RequestUri);
					context.AddFollowRequests(Request);
					return Task.CompletedTask;
				}

			
				//
				// if we have an image extension then we have a successfull
				// image download, let's store the data!
				//
				var fileName = context.Request.RequestUri.AbsolutePath;
				if (!_imageExtensions.Any(x => fileName.EndsWith(x)))
				{
					
						return Task.CompletedTask;
				}

			

				results.Add(new ProviderImageEntity
					{
						url = context.Request.Properties["url"].ToString(),
						//providername = providername,
						//streetaddress = streetaddress,
						//locality = locality,
						//postalcode = postalcode,
						//reviewscore = reviewscore,
						//reviewtext = reviewtext,
						imagedata = context.Response.Content.Bytes,
						created_at = DateTime.Now,
						visited_at = DateTime.Now
					});

				AddParsedResult(context, results);

				return Task.CompletedTask;
			}

		}

		protected override SpiderId GenerateSpiderId()
		{
			return new(ObjectId.CreateId().ToString(), "Provider");
		}

		


		/// <summary>
		/// docker run --name mariadbDSpiderDemo -p 3306:3306 -volume -v mariadbtest:/var/lib/mysql -e MYSQL_ROOT_PASSWORD=clamawu! -d mariadb
		/// </summary>

		[Schema("Springest", "ProviderImage")]
		public class ProviderImageEntity : EntityBase<ProviderImageEntity>
		{

			//
			/// <summary>
			/// Category Data			
			/// </summary>
			///

			public int Id { get; set; }

			public string url { get; set; }

			//public string providername { get; set; }

			//public string locality { get; set; }

			public byte[] imagedata { get; set; }

			//[StringLength(8192)]
			//public string description { get; set; }

			
			public DateTime created_at { get; set; }

			
			public DateTime visited_at { get; set; }

			
		}

		
	}
}
