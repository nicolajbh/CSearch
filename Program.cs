using CSearch.Domain.Model;
using CSearch.Infrastructure.Data;
using CSearch.Services;

internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("CSearch");
        Console.WriteLine("=======");
        Console.WriteLine("1. Scrape refurbed.dk");
        Console.WriteLine("2. Search products.csv");
        Console.Write("\nChoose [1/2]: ");

        var choice = Console.ReadLine()?.Trim();

        if (choice == "2")
        {
            var search = new SearchService();
            search.Run();
            return;
        }

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
            specsContainerSelector: ".//div[contains(@class, 'line-clamp-3')]"
        );

        var client = new HttpClient();
        var parser = new HtmlParserService();
        var scraper = new ScraperService(client, parser);

        var products = scraper.Scrape(job, concurrency: 5, cts.Token);
        Console.WriteLine($"Finished scraping. Found {products.Count} products.");
        FileManager.SaveProducts(products);
    }
}
