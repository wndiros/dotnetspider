using DotnetSpider;
//using DotnetSpider.Processor;
using DotnetSpider.Scheduler;
using DotnetSpider.Downloader;
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
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DotnetSpider.Sample.samples
{
	class CategorySpider : Spider
	{
		private readonly string _connectionString;

		public CategorySpider(IDownloader downloader, IScheduler scheduler, string connectionString)
			: base(downloader, scheduler)
		{
			_connectionString = connectionString;
		}

		protected override async Task InitializeAsync(CancellationToken stoppingToken = default)
		{
			// URL der Website, die gescraped werden soll
			AddStartUrl("https://www.example.com");

			// XPath-Ausdruck, um die Hauptkategorien auf der Website zu extrahieren
			var categorySelector = new CssSelector(".category");

			// Eine Methode, die aufgerufen wird, wenn die Kategorien extrahiert wurden
			AddPageProcessor(new PageProcessor
			{
				// Extrahiert die Kategorien auf der Webseite
				Extractor = new CompositeSelectorExtractor(
					new List<ISelectorExtractor>
					{
						new DefaultSelectorExtractor(categorySelector),
						new CssSelectorExtractor(".sub-category")
					}),

				// Prüft, ob der Link zu einer der Kategorien gehört, und fügt ihn der Ergebnisliste hinzu
				HandleExtractedResults = (context, result) =>
				{
					var categoryLinks = new List<string>();
					var subcategoryLinks = new List<string>();

					// Jede Kategorie durchlaufen und die Links extrahieren
					foreach (var item in result)
					{
						if (item.SelectorType == SelectorType.Css)
						{
							var cssSelector = (CssSelector)item;
							if (cssSelector.CssPath.EndsWith(".category"))
							{
								var categoryLink = cssSelector.Selector;
								categoryLinks.Add(categoryLink);

								// Die neue Kategorie-Seite zur Verarbeitung in der Scheduler-Queue hinzufügen
								AddRequest(new Request(categoryLink));
							}
							else if (cssSelector.CssPath.EndsWith(".sub-category"))
							{
								var subcategoryLink = cssSelector.Selector;
								subcategoryLinks.Add(subcategoryLink);

								// Die neue Unterkategorie-Seite zur Verarbeitung in der Scheduler-Queue hinzufügen
								AddRequest(new Request(subcategoryLink));
							}
						}
					}

					context.AddItem("categoryLinks", categoryLinks);
					context.AddItem("subcategoryLinks", subcategoryLinks);
				}
			});

			// Eine Methode, die aufgerufen wird, wenn ein Link von der Website extrahiert wird
			AddPageProcessor(new PageProcessor
			{
				// Extrahiert alle Links auf der Webseite
				ExtractLinks = page => page.TargetUrls,

				// Prüft, ob der Link zu einer der Kategorien gehört, und fügt ihn der Ergebnisliste hinzu
				HandleLinks = (context, links) =>
				{
				var foundLinks = new List<string>();
				var categoryLinks = ((List<string>)context.GetData("categoryLinks"));
				var subcategoryLinks = ((List<string>)context.GetData("subcategoryLinks"));

				foreach (var link in links)
				{
					foreach (var categoryLink in categoryLinks)
					{
						if (link.Contains(categoryLink)) // End of Code
							{
								foundLinks.Add(link);								
							// Die neue Kategorie-Seite zur Verarbeitung in der Scheduler-Queue hinzufügen
							AddRequest(new Request(link));

								break;
							}
							foreach (var subcategoryLink in subcategoryLinks)
							{
								if (link.Contains(subcategoryLink))
								{
									foundLinks.Add(link);

									// Die neue Unterkategorie-Seite zur Verarbeitung in der Scheduler-Queue hinzufügen
									AddRequest(new Request(link));

									break;
								}
							}
						}
					}

					context.AddItem("links", foundLinks);
					return foundLinks.ToArray();
				}
			});
		}

		protected override void SpiderClosed()
		{
			// Gefundene Links ausgeben und in die MySQL-Datenbank speichern
			var links = ((List<string>)Data["links"]).Distinct();
			foreach (var link in links)
			{
				System.Console.WriteLine(link);

				// Kategorie und Unterkategorie aus dem Link extrahieren
				var category = "";
				var subcategory = "";

				// Hier können Sie verschiedene Methoden zum Extrahieren der Kategorie und Unterkategorie aus dem Link implementieren, abhängig von der Struktur der Website.
				// In diesem Beispiel wird davon ausgegangen, dass die Kategorie am Anfang des Links und die Unterkategorie am Ende des Links steht.
				var parts = link.Split('/');
				if (parts.Length > 3)
				{
					category = parts[3];
				}
				if (parts.Length > 4)
				{
					subcategory = parts.Last();
				}

				// Verbindung zur MySQL-Datenbank herstellen und die Kategorie und Unterkategorie speichern
				using (var connection = new MySqlConnection(_connectionString))
				{
					connection.Open();

					using (var command = new MySqlCommand("INSERT INTO categories (category, subcategory) VALUES (@category, @subcategory)", connection))
					{
						command.Parameters.AddWithValue("@category", category);
						command.Parameters.AddWithValue("@subcategory", subcategory);
						command.ExecuteNonQuery();
					}
				}
			}
		}
	}


}
