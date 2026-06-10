using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;

namespace Test {
    class Program {
        static async Task Main() {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
            
            // Search for Boarskin Subligar
            var searchBoar = await client.GetStringAsync("https://xivapi.com/search?indexes=item&string=Boarskin%20Subligar");
            var searchDark = await client.GetStringAsync("https://xivapi.com/search?indexes=item&string=Darklight%20Subligar");
            
            var boarDoc = JsonDocument.Parse(searchBoar).RootElement;
            var darkDoc = JsonDocument.Parse(searchDark).RootElement;
            
            var boarId = boarDoc.GetProperty("Results")[0].GetProperty("ID").GetUInt32();
            var darkId = darkDoc.GetProperty("Results")[0].GetProperty("ID").GetUInt32();
            
            var boarRes = await client.GetStringAsync($"https://xivapi.com/Item/{boarId}");
            var darkRes = await client.GetStringAsync($"https://xivapi.com/Item/{darkId}");
            
            var b = JsonDocument.Parse(boarRes).RootElement;
            var d = JsonDocument.Parse(darkRes).RootElement;
            
            Console.WriteLine($"Boarskin Subligar (ID {boarId}): ModelMain={b.GetProperty("ModelMain").GetRawText()}");
            Console.WriteLine($"Darklight Subligar (ID {darkId}): ModelMain={d.GetProperty("ModelMain").GetRawText()}");
        }
    }
}
