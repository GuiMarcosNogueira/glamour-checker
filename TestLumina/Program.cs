using System;
using System.Linq;
using System.Reflection;

namespace Test {
    class Program {
        static void Main() {
            var dataManager = new Lumina.GameData(@"D:\Jogos\Steam\steamapps\common\FINAL FANTASY XIV Online\game\sqpack");
            var itemType = dataManager.GetType().Assembly.GetTypes().FirstOrDefault(t => t.Name == "UIColor" && t.Namespace.StartsWith("Lumina.Excel"));
            var getExcelSheetMethod = dataManager.GetType().GetMethod("GetExcelSheet", Type.EmptyTypes).MakeGenericMethod(itemType);
            var sheet = getExcelSheetMethod.Invoke(dataManager, null);
            var getEnumeratorMethod = sheet.GetType().GetMethod("GetEnumerator");
            var enumerator = (System.Collections.IEnumerator)getEnumeratorMethod.Invoke(sheet, null);
            int count = 0;
            while (enumerator.MoveNext() && count < 60) {
                var item = enumerator.Current;
                var rowId = itemType.GetProperty("RowId").GetValue(item);
                var uiFore = itemType.GetProperty("UIForeground").GetValue(item);
                uint fore = (uint)uiFore;
                byte r = (byte)(fore >> 24);
                byte g = (byte)(fore >> 16);
                byte b = (byte)(fore >> 8);
                Console.WriteLine($"ID: {rowId}, R:{r:X2} G:{g:X2} B:{b:X2}");
                count++;
            }
        }
    }
}
