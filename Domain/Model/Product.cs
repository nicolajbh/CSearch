using CSearch.Domain.Interface;

namespace CSearch.Domain.Model;

public record Product : IProduct
{
    public string Site { get; }
    public string Name { get; }
    public string Price { get; }
    public string RAM { get; }
    public string Storage { get; }
    public string Url { get; }

    public Product(string site, string name, string price, string ram, string storage, string url)
    {
        Site = site;
        Name = name;
        Price = price;
        RAM = ram;
        Storage = storage;
        Url = url;
    }
}
