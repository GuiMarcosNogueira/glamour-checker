using Xunit;
using GlamourChecker.Core;

namespace GlamourChecker.Tests
{
    public class VisualDictionaryDebug
    {
        [Fact]
        public void TestVisualGroup()
        {
            var scanner = new ModelScanner(id => new ItemModelData { ModelMain = 1, EquipSlotCategory = 1, DyeCount = 1 });
            var visualGroupWarlock = scanner.GetSharedModelId(3658);
            var visualGroupGoatskin = scanner.GetSharedModelId(3675);

            System.Console.WriteLine($"Warlock: {visualGroupWarlock}");
            System.Console.WriteLine($"Goatskin: {visualGroupGoatskin}");

            Assert.Equal(visualGroupGoatskin, visualGroupWarlock);
        }
    }
}
