using CSearch.Domain.Interface;
using CSearch.Domain.Model;
using CSearch.Infrastructure.Data;

namespace CSearch.Services;

public class SearchService
{
    public void Run()
    {
        var products = FileManager.LoadProducts();

        if (products.Count == 0)
        {
            Console.WriteLine("No products found. Run the scraper first to populate products.csv.");
            return;
        }

        var brands = ExtractBrands(products);

        Console.WriteLine($"Loaded {products.Count} products.\n");
        PrintHelp();

        while (true)
        {
            Console.Write("\n> ");
            var input = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(input))
                continue;

            if (
                input.Equals("exit", StringComparison.OrdinalIgnoreCase)
                || input.Equals("quit", StringComparison.OrdinalIgnoreCase)
            )
                break;

            if (input.Equals("help", StringComparison.OrdinalIgnoreCase))
            {
                PrintHelp();
                continue;
            }

            if (input.Equals("brands", StringComparison.OrdinalIgnoreCase))
            {
                PrintBrands(brands);
                continue;
            }

            var results = Filter(products, input);
            PrintResults(results, input);
        }
    }

    private List<IProduct> Filter(List<IProduct> products, string query)
    {
        var terms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return products
            .Where(p =>
            {
                var haystack = $"{p.Name} {p.RAM} {p.Storage} {p.Price}".ToLowerInvariant();
                return terms.All(t => haystack.Contains(t.ToLowerInvariant()));
            })
            .ToList();
    }

    private void PrintResults(List<IProduct> results, string query)
    {
        if (results.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"No results for \"{query}\".");
            Console.ResetColor();
            return;
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n{results.Count} result(s) for \"{query}\":\n");
        Console.ResetColor();

        Console.WriteLine($"{"#", -4} {"Name", -55} {"Price", -16} {"RAM", -10} {"Storage", -10}");
        Console.WriteLine(new string('-', 100));

        for (int i = 0; i < results.Count; i++)
        {
            var p = results[i];
            Console.WriteLine(
                $"{i + 1, -4} {Truncate(p.Name, 54), -55} {p.Price, -16} {p.RAM, -10} {p.Storage, -10}"
            );
        }

        Console.WriteLine();

        Console.Write("Enter a number to open the product URL, or press Enter to continue: ");
        var choice = Console.ReadLine()?.Trim();

        if (int.TryParse(choice, out int idx) && idx >= 1 && idx <= results.Count)
        {
            var url = results[idx - 1].Url;
            Console.WriteLine($"URL: {url}");
            TryOpenBrowser(url);
        }
    }

    private void PrintBrands(List<string> brands)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\nDetected brands:\n");
        Console.ResetColor();
        foreach (var b in brands)
            Console.WriteLine($"  {b}");
    }

    private void PrintHelp()
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("Commands:");
        Console.WriteLine(
            "  <query>   Search by any combination of words (name, RAM, storage, price)"
        );
        Console.WriteLine(
            "            Examples: \"lenovo 16 GB\"  |  \"macbook 512\"  |  \"dell 32 GB 1000\""
        );
        Console.WriteLine("  brands    List all detected manufacturers");
        Console.WriteLine("  help      Show this message");
        Console.WriteLine("  exit      Quit search mode");
        Console.ResetColor();
    }

    private List<string> ExtractBrands(List<IProduct> products)
    {
        var prefixes = new[]
        {
            "Apple",
            "Lenovo",
            "HP",
            "Dell",
            "Microsoft",
            "Fujitsu",
            "ASUS",
            "Samsung",
            "Acer",
            "Razer",
            "MSI",
            "Getac",
            "Huawei",
            "Honor",
            "Medion",
            "Toshiba",
        };

        return prefixes
            .Where(b => products.Any(p => p.Name.StartsWith(b, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(b => b)
            .ToList();
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..(maxLength - 1)] + "…";

    private static void TryOpenBrowser(string url)
    {
        try
        {
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo { FileName = url, UseShellExecute = true }
            );
        }
        catch
        {
            // silently skip if shell open isn't available
        }
    }
}
