using System;
using System.Net.Http;

class Program {
    static void Main() {
        var client = new HttpClient();
        var code = client.GetStringAsync("https://raw.githubusercontent.com/aers/FFXIVClientStructs/main/FFXIVClientStructs/FFXIV/Client/Game/MirageManager.cs").Result;
        var lines = code.Split('\n');
        for (int i = 0; i < lines.Length; i++) {
            if (lines[i].Contains("PrismBox")) {
                int start = Math.Max(0, i - 2);
                int end = Math.Min(lines.Length - 1, i + 5);
                Console.WriteLine($"--- Line {i} ---");
                for (int j = start; j <= end; j++) {
                    Console.WriteLine(lines[j]);
                }
            }
        }
    }
}
