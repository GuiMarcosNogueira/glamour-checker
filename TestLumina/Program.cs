using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;

namespace Test {
    class Program {
        static async Task Main() {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
            
            var asceticRes = await client.GetStringAsync("https://xivapi.com/Item/3315");
            var velvetRes = await client.GetStringAsync("https://xivapi.com/Item/3324");
            
            var a = JsonDocument.Parse(asceticRes).RootElement;
            var v = JsonDocument.Parse(velvetRes).RootElement;
            
            Console.WriteLine($"Ascetic: ModelMain={a.GetProperty("ModelMain").GetRawText()}, ModelSub={a.GetProperty("ModelSub").GetRawText()}");
            Console.WriteLine($"Velvet: ModelMain={v.GetProperty("ModelMain").GetRawText()}, ModelSub={v.GetProperty("ModelSub").GetRawText()}");
        }
    }
}
