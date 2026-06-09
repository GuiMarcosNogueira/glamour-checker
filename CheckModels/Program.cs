using System;
using System.Linq;
using Lumina;
using Lumina.Excel.Sheets;

class Program {
    static void Main() {
        var lumina = new GameData(@"D:\Jogos\Steam\steamapps\common\FINAL FANTASY XIV Online\game\sqpack");
        var itemSheet = lumina.GetExcelSheet<Item>();
        
        var items = itemSheet.Where(x => x.ModelMain == 327682).ToList();
        Console.WriteLine($"Items with ModelMain == 327682 (Aiming bracelet model):");
        foreach(var item in items) {
            Console.WriteLine($"{item.RowId} {item.Name.ExtractText()}");
        }
        
        var items2 = itemSheet.Where(x => x.ModelMain == 65538).ToList();
        Console.WriteLine($"\nItems with ModelMain == 65538 (Striking bracelet model):");
        foreach(var item in items2) {
            Console.WriteLine($"{item.RowId} {item.Name.ExtractText()}");
        }
    }
}
