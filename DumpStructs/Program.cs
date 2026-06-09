using System;
using System.Reflection;
using Lumina.Excel.Sheets;

class Program {
    static void Main() {
        var t = typeof(Item);
        foreach (var p in t.GetProperties()) {
            Console.WriteLine($"{p.Name} : {p.PropertyType}");
        }
    }
}
