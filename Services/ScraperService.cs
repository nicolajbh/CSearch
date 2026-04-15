using CSearch.Domain.Interface;

namespace CSearch.Services;

public class ScraperService
{
    private readonly HttpClient _client;
    private readonly HtmlParserService _parser;
    private int _totalProductsFound = 0;

    public ScraperService(HttpClient client, HtmlParserService parser)
    {
        _client = client;
        _parser = parser;
    }

    public Task<List<IProduct>> Scrape(
        IScrapeJob job,
        int concurrency,
        CancellationToken cancellationToken = default
    )
    {
        int maxPageNum = FindMaxPageNum(job, cancellationToken).GetAwaiter().GetResult();
        Console.WriteLine($"\nSetup Complete. Starting Scraper with {maxPageNum} pages\n");

        Queue<string> urlQueue = new Queue<string>();
        for (int i = 1; i <= maxPageNum; i++)
        {
            urlQueue.Enqueue($"{job.BaseUrl}{job.QueryParams}&page={i}");
        }

        var allProducts = new List<IProduct>();
        var queueLock = new object();
        var resultLock = new object();
        List<Thread> workers = new List<Thread>();

        var tcs = new TaskCompletionSource<List<IProduct>>();
        Thread manager = new Thread(() =>
        {
            try
            {
                for (int i = 0; i < concurrency; i++)
                {
                    Thread worker = new Thread(() =>
                    {
                        while (true)
                        {
                            if (cancellationToken.IsCancellationRequested) break;

                            string? currentUrl = null;
                            lock (queueLock)
                            {
                                if (urlQueue.Count > 0)
                                    currentUrl = urlQueue.Dequeue();
                            }

                            if (currentUrl == null) break;

                            ProcessUrl(currentUrl, job, allProducts, resultLock, cancellationToken);
                        }
                    });

                    worker.Name = $"Scraper-Worker-{i}";
                    workers.Add(worker);
                    worker.Start();
                }

                foreach (var worker in workers)
                {
                    worker.Join();
                }

                tcs.SetResult(allProducts);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        manager.Start();
        return tcs.Task;
    }

    private void ProcessUrl(
        string url,
        IScrapeJob job,
        List<IProduct> allProducts,
        object resultLock,
        CancellationToken cancellationToken
    )
    {
        int threadId = Environment.CurrentManagedThreadId;
        try
        {
            Console.WriteLine($"[Thread {threadId}] FETCHING: {url}");
            var response = _client.GetAsync(url, cancellationToken).GetAwaiter().GetResult();
            var html = response.Content.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();

            var products = _parser.ParseProducts(html, job);
            lock (resultLock)
            {
                allProducts.AddRange(products);
            }

            Interlocked.Add(ref _totalProductsFound, products.Count);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Thread {threadId}] ERROR: {ex.Message}");
        }
        finally
        {
            Thread.Sleep(Random.Shared.Next(500, 1000));
        }
    }

    private async Task<int> FindMaxPageNum(IScrapeJob job, CancellationToken cancellationToken)
    {
        int low = 1, high = 1000, result = 1;
        while (low <= high)
        {
            if (cancellationToken.IsCancellationRequested) break;
            int mid = low + (high - low) / 2;
            var response = await _client.GetAsync($"{job.BaseUrl}{job.QueryParams}&page={mid}", cancellationToken);
            var html = await response.Content.ReadAsStringAsync(cancellationToken);
            var count = _parser.CountProducts(html, job);
            if (count == 0) high = mid - 1;
            else { result = mid; if (count < 16) break; else low = mid + 1; }
        }
        return result;
    }
}
