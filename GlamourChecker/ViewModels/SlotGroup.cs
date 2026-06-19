using System.Collections.Generic;
using System.Linq;

namespace GlamourChecker.ViewModels;

public class SlotGroup<T>
{
    public string Name { get; set; } = string.Empty;
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
}
