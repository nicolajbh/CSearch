using CSearch.Domain.Interface;

namespace CSearch.Domain.Model;

public record ScrapeJob : IScrapeJob
{
    public string SiteName { get; }
    public string BaseUrl { get; }
    public string QueryParams { get; }
    public string CardSelector { get; }
    public string NameSelector { get; }
    public string PriceSelector { get; }

    public ScrapeJob(
        string siteName,
        string baseUrl,
        string queryParams,
        string cardSelector,
        string nameSelector,
        string priceSelector
    )
    {
        SiteName = siteName;
        BaseUrl = baseUrl;
        QueryParams = queryParams;
        CardSelector = cardSelector;
        NameSelector = nameSelector;
        PriceSelector = priceSelector;
    }
}
