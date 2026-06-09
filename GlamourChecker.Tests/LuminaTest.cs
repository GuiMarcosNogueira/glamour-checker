using System;
using System.IO;
using System.Linq;
using Xunit;
using Lumina;
namespace GlamourChecker.Tests {
    public class LuminaTest {
        [Fact]
        public void DumpMirageStoreSetItemProperties() {
            var props = typeof(Lumina.Excel.Sheets.MirageStoreSetItem).GetProperties();
            foreach(var p in props) {
                Console.WriteLine($"{p.PropertyType.Name} {p.Name}");
            }
        }
    }
}
