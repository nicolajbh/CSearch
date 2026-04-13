using CSearch.Domain.Interface;
using CSearch.Domain.Model;

using HtmlAgilityPack;

namespace CSearch;

public class HtmlParserService
{
    List<IProduct> ParseProducts(string html, IScrapeJob job)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var cards = doc.DocumentNode.SelectNodes(job.CardSelector)?.ToList() ?? [];
        var products = new List<IProduct>();

        foreach (var card in cards)
        {
            var name = card.SelectSingleNode(job.NameSelector)?.InnerText.Trim() ?? "";
            var price = card.SelectSingleNode(job.PriceSelector)?.InnerText.Trim() ?? "";
            var href = card.SelectSingleNode(".//a")?.GetAttributeValue("href", "") ?? "";
            var url = $"{job.BaseUrl}{href}";

            products.Add(new Product(job.SiteName, name, price, url));
        }

        return products;
    }

    public int CountProducts(string html, IScrapeJob job)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        return doc.DocumentNode.SelectNodes(job.CardSelector)?.Count ?? 0;
    }

    public static string ParseSpec(HtmlNode span)
    {
        var clone = span.CloneNode(deep: true);
        clone.SelectSingleNode(".//span")?.Remove();
        return clone.InnerText.Trim().TrimEnd('|').Trim();
    }
}
