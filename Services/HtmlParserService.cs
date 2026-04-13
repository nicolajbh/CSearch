using CSearch.Domain.Interface;
using CSearch.Domain.Model;

using HtmlAgilityPack;

namespace CSearch.Services;

public class HtmlParserService
{
    public List<IProduct> ParseProducts(string html, IScrapeJob job)
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
            var specs = ParseSpecs(card, job);

            products.Add(
                new Product(
                    job.SiteName,
                    name,
                    price,
                    ram: specs["ram"] ?? "",
                    storage: specs["storage"] ?? "",
                    url
                )
            );
        }

        return products;
    }

    private Dictionary<string, string?> ParseSpecs(HtmlNode card, IScrapeJob job)
    {
        var result = new Dictionary<string, string?>();
        var container = card.SelectSingleNode(job.SpecsContainerSelector);
        if (container == null)
            return result;

        foreach (var span in container.ChildNodes.Where(n => n.Name == "span"))
        {
            var spec = ParseSpec(span);
            if (spec.StartsWith("RAM"))
                result["ram"] = spec.Replace("RAM", "").Trim();
            else if (spec.Contains("Hukommelsesplads"))
                result["storage"] = spec.Replace("Hukommelsesplads", "").Trim();
        }

        return result;
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
