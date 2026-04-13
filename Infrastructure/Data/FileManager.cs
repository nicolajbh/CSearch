using System.Text;

using CSearch.Domain.Interface;

namespace CSearch.Infrastructure.Data;

public static class FileManager
{
    private static readonly string _outputPath = "products.csv";

    public static void SaveProducts(List<IProduct> products)
    {
        var lines = new List<string>();
        lines.Add("Site,Name,Price,RAM,Storage,Url");

        foreach (var p in products)
            lines.Add(
                $"{Escape(p.Site)},{Escape(p.Name)},{Escape(p.Price)},{Escape(p.RAM)},{Escape(p.Storage)},{Escape(p.Url)}"
            );

        File.WriteAllLines(_outputPath, lines, Encoding.UTF8);
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";
        return value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }
}
