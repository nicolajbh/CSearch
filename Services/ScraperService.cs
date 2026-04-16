using CSearch.Domain.Interface;

namespace CSearch.Services;

public class ScraperService
{
    private readonly HttpClient _client;
    private readonly HtmlParserService _parser;
    private int _totalProductsFound = 0;

    private readonly Queue<string> _urlQueue = new Queue<string>();
    private readonly List<IProduct> _allProducts = new List<IProduct>();
    private readonly object _lockObj = new object();

    public ScraperService(HttpClient client, HtmlParserService parser)
    {
        _client = client;
        _parser = parser;
    }

    public List<IProduct> Scrape(
        IScrapeJob job,
        int concurrency,
        CancellationToken cancellationToken = default
    )
    {
        int maxPageNum = FindMaxPageNum(job, cancellationToken);
        Console.WriteLine($"\nSetup Complete. Starting Scraper with {maxPageNum} pages\n");

        for (int i = 1; i <= maxPageNum; i++)
        {
            _urlQueue.Enqueue($"{job.BaseUrl}{job.QueryParams}&page={i}");
        }


        List<Thread> threads = new List<Thread>();
        using Semaphore semaphore = new Semaphore(concurrency, concurrency);

        while (true)
        {
            if (cancellationToken.IsCancellationRequested) break;

            string? currentUrl = null;

            lock (_lockObj)
            {
                if (_urlQueue.Count == 0) break;
                currentUrl = _urlQueue.Dequeue();
            }

            semaphore.WaitOne();

            Thread thread = new Thread(() =>
                        {
                            try
                            {
                                ProcessUrl(currentUrl, job, cancellationToken);
                            }
                            finally
                            {
                                semaphore.Release();
                            }
                        });

            threads.Add(thread);
            thread.Start();
        }

        foreach (var t in threads)
        {
            t.Join();
        }

        return _allProducts;
    }

    private void ProcessUrl(
        string url,
        IScrapeJob job,
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

            lock (_lockObj)
            {
                _allProducts.AddRange(products);
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

    private int FindMaxPageNum(IScrapeJob job, CancellationToken cancellationToken)
    {
        int low = 1, high = 1000, result = 1;
        while (low <= high)
        {
            if (cancellationToken.IsCancellationRequested) break;
            int mid = low + (high - low) / 2;

            var url = $"{job.BaseUrl}{job.QueryParams}&page={mid}";
            var response = _client.GetAsync(url, cancellationToken).GetAwaiter().GetResult();
            var html = response.Content.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();

            var count = _parser.CountProducts(html, job);
            if (count == 0)
            {
                high = mid - 1;
            }
            else
            {
                result = mid;
                if (count < 16) break;
                else low = mid + 1;
            }
        }
        return result;
    }
}
