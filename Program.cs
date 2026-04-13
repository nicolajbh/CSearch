using CSearch.Domain.Model;
using CSearch.Infrastructure.Data;
using CSearch.Services;

internal class Program
{
    static async Task Main(string[] args)
    {
        using var cts = new CancellationTokenSource();

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            Console.WriteLine("Shutting down...");
            cts.Cancel();
        };

        var job = new ScrapeJob(
            siteName: "refurbed.dk",
            baseUrl: "https://www.refurbed.dk",
            queryParams: "/search-results/?tile_type=electronics&page_type=category&category=2&sort_by=score",
            cardSelector: "//article",
            nameSelector: ".//h3",
            priceSelector: ".//div[contains(@class, 'text-emphasize-03')]",
            specsContainerSelector: ".//div[contains(@class, 'line-clamp-3')]",
            specKeywords: new Dictionary<string, string>
            {
                { "ram", "RAM" },
                { "storage", "Hukommelsesplads" },
            }
        );

        var client = new HttpClient();
        var parser = new HtmlParserService();
        var scraper = new ScraperService(client, parser);

        var products = await scraper.Scrape(job, concurrency: 5, cts.Token);
        Console.WriteLine($"Finished scraping. Found {products.Count} products.");
        FileManager.SaveProducts(products);
    }
}
