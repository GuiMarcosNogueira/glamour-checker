using System;
using System.Linq;
using Lumina;
using Lumina.Excel.Sheets;

public class Program {
    public static void Main() {
        var lumina = new GameData(@"D:\SquareEnix\FINAL FANTASY XIV - A Realm Reborn\game\sqpack");
        var sheet = lumina.GetExcelSheet<Item>();
        foreach (var item in sheet.Where(x => x.Name.ToString().Contains("Acolyte's Attire"))) {
            Console.WriteLine($"Item: {item.RowId} {item.Name}");
        }
        var mirageSheet = lumina.GetExcelSheet<MirageStoreSetItem>();
        foreach (var m in mirageSheet) {
            // How do we get the name of the MirageStoreSetItem?
            // Maybe it doesn't have a name, but it has item components.
            if (m.Body.RowId == 3035) { // Acolyte's Robe
                Console.WriteLine($"MirageStoreSetItem: {m.RowId}");
            }
        }
    }
}
