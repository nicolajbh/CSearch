using System.Text;
using CSearch.Domain.Interface;
using CSearch.Domain.Model;

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

    public static List<IProduct> LoadProducts()
    {
        if (!File.Exists(_outputPath))
            return [];

        var products = new List<IProduct>();

        foreach (var line in File.ReadAllLines(_outputPath, Encoding.UTF8).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var cols = ParseCsvLine(line);
            if (cols.Length < 6)
                continue;

            products.Add(
                new Product(
                    site: cols[0],
                    name: cols[1],
                    price: cols[2],
                    ram: cols[3],
                    storage: cols[4],
                    url: cols[5]
                )
            );
        }

        return products;
    }

    private static string[] ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (inQuotes)
            {
                if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else if (c == '"')
                {
                    inQuotes = false;
                }
                else
                {
                    sb.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    fields.Add(sb.ToString());
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }
        }

        fields.Add(sb.ToString());
        return fields.ToArray();
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
