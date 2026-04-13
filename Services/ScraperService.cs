using CSearch.Domain.Interface;

namespace CSearch.Services;

public class ScraperService
{
    private static readonly HttpClient _client;
    private readonly HtmlParserService _parser;
    private static int _totalProductsFound = 0;

    public ScraperService(HttpClient client, HtmlParserService parser)
    {
        _client = client;
        _parser = parser;
    }

    public async Task<IEnumerable<IProduct>> Scrape(
        IScrapeJob job,
        int concurrency,
        CancellationToken cancellationToken = default
    )
    {
        int maxPageNum = await FindMaxPageNum(job, cancellationToken);
        Console.WriteLine($"\nSetup Complete. Starting Scraper with {maxPageNum} pages\n");

        Queue<string> urlQueue = new Queue<string>();
        for (int i = 1; i <= maxPageNum; i++)
        {
            urlQueue.Enqueue($"{job.BaseUrl}{job.QueryParams}&page={i}");
        }

        var allProducts = new List<IProduct>();
        var queueLock = new object();
        var resultLock = new object();

        SemaphoreSlim semaphore = new SemaphoreSlim(concurrency);
        List<Task> workers = new List<Task>();

        while (true)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            string? currentUrl = null;

            lock (queueLock)
            {
                if (urlQueue.Count > 0)
                    currentUrl = urlQueue.Dequeue();
            }

            if (currentUrl == null)
                break;

            await semaphore.WaitAsync();

            var task = Task.Run(async () =>
            {
                int threadId = Environment.CurrentManagedThreadId;
                try
                {
                    Console.WriteLine($"[Thread {threadId}] STARTING: {currentUrl}");

                    var html = await FetchHtml(currentUrl, cancellationToken);
                    var products = _parser.ParseProducts(html, job);

                    foreach (var product in products)
                    {
                        Console.WriteLine($"[Thread {threadId}] {product.Name} - {product.Price}");
                    }

                    lock (resultLock)
                        allProducts.AddRange(products);
                    Interlocked.Add(ref _totalProductsFound, products.Count);

                    // forsinkelse fordi ???
                    // await Task.Delay(1000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Thread {threadId}] ERROR: {ex.Message}");
                }
                finally
                {
                    semaphore.Release();
                }
            });
            workers.Add(task);
        }
        await Task.WhenAll(workers);
        Console.WriteLine($"\n--- SCRAPING COMPLETE ---");
        Console.WriteLine($"Total Products Scraped: {_totalProductsFound}");
    }

    private async Task<int> FindMaxPageNum(IScrapeJob job, CancellationToken cancellationToken)
    {
        int low = 1,
            high = 1000,
            result = 1;

        while (low <= high)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int mid = low + (high - low) / 2;
            var url = $"{job.BaseUrl}{job.QueryParams}&page={mid}";
            var html = await FetchHtml(url, cancellationToken);

            var count = _parser.CountProducts(html, job);
            Console.WriteLine($"Page {mid}: {count} products");

            if (count == 0)
            {
                // Page is empty, max is somewhere below
                high = mid - 1;
            }
            else
            {
                // Page has products, so this is a valid page
                result = mid;
                if (count < 16)
                    break; // Partial page = last page, stop early
                else
                    low = mid + 1;
            }
        }

        return result;
    }

    static async Task<string> FetchHtml(string url, CancellationToken cancellationToken)
    {
        var response = await _client.SendAsync(
            new HttpRequestMessage(HttpMethod.Get, url),
            cancellationToken
        );
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}
