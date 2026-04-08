internal class Program
{
  private static readonly HttpClient client = new HttpClient();
  static async Task Main(string[] args)
  {
    string baseUrl = "https://www.refurbed.dk/";
    string response = await client.GetStringAsync(baseUrl);
    Console.WriteLine("Response received");
    Console.WriteLine(response);
  }
}
