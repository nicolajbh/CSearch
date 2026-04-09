namespace CSearch.Domain.Interface;

public interface IScrapeJob
{
    string SiteName { get; }
    string BaseUrl { get; }
    string QueryParams { get; }
    string CardSelector { get; }
    string NameSelector { get; }
    string PriceSelector { get; }
}
