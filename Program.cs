using HtmlAgilityPack;

internal class Program
{
    private static readonly HttpClient client = new HttpClient();
    private static int totalProductsFound = 0;
    private static readonly object counterLock = new object();

    static async Task Main(string[] args)
    {
        int maxPageNum = await FindMaxPageNum();
        Console.WriteLine("Found 72 pages");
        Console.WriteLine($"\n--- Setup Complete. Starting Scraper with {maxPageNum} pages ---\n");

        Queue<string> urlQueue = new Queue<string>();
        for (int i = 1; i <= maxPageNum; i++)
        {
            urlQueue.Enqueue($"https://www.refurbed.dk/search-results/?page={i}&tile_type=electronics&page_type=category&category=2&sort_by=score");
        }

        // Semaphore to limit concurrency to 5
        SemaphoreSlim semaphore = new SemaphoreSlim(5);
        List<Task> workers = new List<Task>();

        while (true)
        {
            string? currentUrl = null;
            lock (urlQueue)
            {
                if (urlQueue.Count > 0) currentUrl = urlQueue.Dequeue();
            }

            if (currentUrl == null) break;

            await semaphore.WaitAsync();

            var task = Task.Run(async () =>
            {
                int threadId = Environment.CurrentManagedThreadId;
                try
                {
                    Console.WriteLine($"[Thread {threadId}] STARTING: {currentUrl}");

                    var html = await FetchHtml(currentUrl);
                    var doc = new HtmlDocument();
                    doc.LoadHtml(html);

                    var products = doc.DocumentNode.SelectNodes("//article")?.ToList() ?? [];
                    string BaseUrl = "https://www.refurbed.dk";
                    foreach (var product in products)
                    {
                        var title = product.SelectSingleNode(".//h3")?.InnerText.Trim() ?? "";
                        var href = product.SelectSingleNode(".//a")?.GetAttributeValue("href", "") ?? "";
                        var url = $"{BaseUrl}{href}";

                        var price = product
                            .SelectSingleNode(".//div[contains(@class, 'text-emphasize-03')]")
                            ?.InnerText.Trim() ?? "";

                        var originalPriceNode = product.SelectSingleNode(".//del");
                        var originalPrice = originalPriceNode?.InnerText.Trim();

                        var specsDiv = product.SelectSingleNode(".//div[contains(@class, 'line-clamp-3')]");
                        var specMap = new Dictionary<string, string?>();

                        if (specsDiv != null)
                        {
                            foreach (var span in specsDiv.ChildNodes.Where(n => n.Name == "span"))
                            {
                                var spec = ParseSpec(span);

                                if (spec.StartsWith("RAM"))
                                    specMap["ram"] = spec.Replace("RAM", "").Trim();
                                else if (spec.Contains("Hukommelsesplads"))
                                    specMap["storage"] = spec.Replace("Hukommelsesplads", "").Trim();
                            }
                        }
                        Console.WriteLine($"[THREAD {threadId}] Title: {title}. Price: {price}.");
                    }

                    lock (counterLock)
                    {
                        totalProductsFound += products.Count;
                    }

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
        Console.WriteLine($"Total Products Scraped: {totalProductsFound}");
    }

    static string ParseSpec(HtmlNode span)
    {
        var clone = span.CloneNode(deep: true);
        var variants = clone.SelectSingleNode(".//span");
        variants?.Remove();
        return clone.InnerText.Trim().TrimEnd('|').Trim();
    }

    static async Task<int> FindMaxPageNum()
    {
        int low = 1, high = 1000;
        int result = 1;

        while (low <= high)
        {
            int mid = low + (high - low) / 2;
            var count = await GetProductCount(mid);
            Console.WriteLine($"Found {count} products");

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
    static async Task<string> FetchHtml(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        var response = await client.SendAsync(request);
        return await response.Content.ReadAsStringAsync();
    }

    static async Task<int> GetProductCount(int pageNum)
    {
        var url = $"https://www.refurbed.dk/search-results/?page={pageNum}&tile_type=electronics&page_type=category&category=2&sort_by=score";
        var html = await FetchHtml(url);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        return doc.DocumentNode.SelectNodes("//article")?.Count ?? 0;
    }
}
