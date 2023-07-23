using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection.Emit;
using System.Reflection.Metadata;
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
using static DotnetSpider.Sample.samples.WNSpringEntityCatSpider;

namespace DotnetSpider.Sample.samples
{
	public class WNSpringCourseProviderSpider : Spider
	{
		public static async Task RunAsync()
		{
			var builder = Builder.CreateDefaultBuilder<WNSpringCourseProviderSpider>(options =>
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

		public WNSpringCourseProviderSpider(IOptions<SpiderOptions> options, DependenceServices services,
			ILogger<Spider> logger) : base(
			options, services, logger)
		{
		}

		protected override async Task InitializeAsync(CancellationToken stoppingToken = default)
		{
			WNSpringCatEntityStorage wnEntityStorage = (WNSpringCatEntityStorage)GetDefaultStorage();

			AddDataFlow(new ProviderParser(wnEntityStorage));			
			AddDataFlow(GetDefaultStorage());
			//AddDataFlow(new ImageStorage());

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
				if (i == 2)
				{
					break;
				}

			}
		}


		protected class ProviderParser : DataParser<ProviderEntity>
		{
			private readonly IDataFlow _wnEntityStorage;

			public ProviderParser(IDataFlow entityStorage)
			{
				//Was planed for querying if a specific record exists
				//Not necessary as unique records can be enforced by an unique index in the DB table
				//the definition of the index can be done in the custom entity Class
				_wnEntityStorage = entityStorage;
				//_wnEntityStorage.
			}
			public override Task InitializeAsync()
			{

				return Task.CompletedTask;

			}

			protected override Task ParseAsync(DataFlowContext context)
			{

				List<ProviderEntity> results = new List<ProviderEntity>();
				var typeName = typeof(ProviderEntity).FullName;
				var count = 1;

				//var mainblock = context.Selectable.SelectList(Selectors.XPath(".//li[@class='category-list__item']"));
				//string urlfilter = "//a[@class='category-list__title-link']/@href";

				string providernamefilter = "//*[@class='content__title page__title']/text()";
				string streetaddresfilter = "//*[@class='street-address']/span/text()";
				string localityfilter = "//*[@class='locality']/text()";
				string postalcodefilter = "//*[@class='postal-code']/text()";
				string reviewscorefilter = "(//*[@class='review-score__square--large']/text())[1]";
				string reviewtextfilter = "//*[@class='profile__description']/p/text()";




				var url = context.Request.RequestUri.AbsoluteUri;
				var providername = context.Selectable.Select(Selectors.XPath(providernamefilter))?.Value;
				var streetaddress = context.Selectable.Select(Selectors.XPath(streetaddresfilter))?.Value;
				var locality = context.Selectable.Select(Selectors.XPath(localityfilter))?.Value;
				var postalcode = context.Selectable.Select(Selectors.XPath(postalcodefilter))?.Value;
				var reviewscore = context.Selectable.Select(Selectors.XPath(reviewscorefilter))?.Value;
				var reviewtext = context.Selectable.Select(Selectors.XPath(reviewtextfilter))?.Value;

				string providerimagefilter = "//*[@class='product__provider']/a/img";
				//string providerimagefilter = "//*[@class='hero__image']/@src";
				var providerimage = context.Selectable.Select(Selectors.XPath(providerimagefilter))?.Value;


				results.Add(new ProviderEntity
				{
					url = url,
					providername = providername,
					streetaddress = streetaddress,
					locality = locality,
					postalcode = postalcode,
					reviewscore = reviewscore,
					reviewtext = reviewtext,
					//imagedata = providerimage,
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

		[Schema("Springest", "Provider")]
		//[EntitySelector(Expression = "//*[@role='main']", Type = SelectorType.XPath)]		
		//[GlobalValueSelector(Expression = "//a[@class='category-list__title-link']/@href", Name = "URL", Type = SelectorType.XPath)]
		//[GlobalValueSelector(Expression = ".//a[@class='category-list__title-link']//text()", Name = "Title", Type = SelectorType.XPath)]
		//[FollowRequestSelector(Expressions = new[] { "//a[@class='category-list__title-link']/@href" })]
		public class ProviderEntity : EntityBase<ProviderEntity>
		{

			//
			/// <summary>
			/// Category Data			
			/// </summary>
			///

			public int Id { get; set; }

			public string url { get; set; }

			public string providername { get; set; }

			public string streetaddress { get; set; }

			public string locality { get; set; }

			
			public string postalcode { get; set; }

			public string reviewscore { get; set; }

			
			[StringLength(8192)]
			public string reviewtext { get; set; }

			public byte[] imagedata { get; set; }

			public DateTime created_at { get; set; }

			
			public DateTime visited_at { get; set; }

			
		}
	}
}
