using CSearch.Domain.Interface;

namespace CSearch.Domain.Model;

public record Product : IProduct
{
  public string Site { get; }
  public string Name { get; }
  public string Price { get; }
  public string Url { get; }

  public Product(string site, string name, string price, string url)
  {
    Site = site;
    Name = name;
    Price = price;
    Url = url;
  }
}
