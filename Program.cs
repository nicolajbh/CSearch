using HtmlAgilityPack;

internal class Program
{
  private static readonly HttpClient client = new HttpClient();

  static async Task Main(string[] args)
  {
    int pageNum = 1;
    string baseUrl = $"https://www.refurbed.dk/search-results/?page={pageNum}&tile_type=electronics&page_type=category&category=2&sort_by=score";

    // Find max page num 
    int maxPageNum = await FindMaxPageNum();
    Console.WriteLine($"Max page num: {maxPageNum}");

    // List of urls to scrape

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
    Console.WriteLine($"Scraping page: {pageNum}");
    var html = await FetchHtml(url);
    var doc = new HtmlDocument();
    doc.LoadHtml(html);
    return doc.DocumentNode.SelectNodes("//article")?.Count ?? 0;
  }
}
