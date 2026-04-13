namespace CSearch.Domain.Interface;

public interface IProduct
{
    string Site { get; }
    string Name { get; }
    string Price { get; }
    string CPU { get; }
    string RAM { get; }
    string Storage { get; }
    string Url { get; }
}
