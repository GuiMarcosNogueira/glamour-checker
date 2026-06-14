using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Moq;
using GlamourChecker.Core;

namespace GlamourChecker.Tests
{
    public class DuplicatesTest
    {
        [Fact]
        public void TestDuplicates()
        {
            var config = new Configuration();
            // Ascetic's Tights ID=3315. Velveteen Tights ID=3324.
            // Let's assume they have the same SharedModelId = 999.
            config.DresserItemsBySharedModel = new Dictionary<ulong, List<uint>> {
                { 999, new List<uint> { 3315, 3324 } }
            };

            var scannerMock = new Mock<ModelScanner>((Func<uint, ItemModelData?>)null!);
            scannerMock.Setup(m => m.GetModelId(3315)).Returns(100);
            scannerMock.Setup(m => m.GetModelId(3324)).Returns(100); // Or different, doesn't matter if dyeable
            scannerMock.Setup(m => m.GetSharedModelId(3315)).Returns(999);
            scannerMock.Setup(m => m.GetSharedModelId(3324)).Returns(999);
            scannerMock.Setup(m => m.IsDyeable(3315)).Returns(false); // Ascetic
            scannerMock.Setup(m => m.IsDyeable(3324)).Returns(true); // Velveteen

            var memoryMock = new Mock<IGameMemoryProvider>();

            var watcher = new InventoryWatcher(scannerMock.Object, config, memoryMock.Object);

            var dups = watcher.GetDuplicates();

            Assert.Single(dups);
            Assert.Equal(2, dups[0].ItemIds.Count);
        }
    }
}
