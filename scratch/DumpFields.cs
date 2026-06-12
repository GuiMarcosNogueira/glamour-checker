using System;
using System.Reflection;
using System.Runtime.InteropServices;

public class Program {
    public static void Main() {
        try {
            var assembly = Assembly.LoadFrom(@"C:\Users\Illidan\AppData\Roaming\XIVLauncher\addon\Hooks\dev\FFXIVClientStructs.dll");
            var type = assembly.GetType("FFXIVClientStructs.FFXIV.Client.Game.MirageManager");
            if (type != null) {
                Console.WriteLine("--- MirageManager Fields ---");
                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)) {
                    // Try to get FieldOffset
                    var offsetAttr = field.GetCustomAttribute<FieldOffsetAttribute>();
                    string offsetStr = offsetAttr != null ? $"[Offset: 0x{offsetAttr.Value:X}]" : "";
                    Console.WriteLine($"{field.Name}: {field.FieldType.Name} {offsetStr}");
                }
            } else {
                Console.WriteLine("Type not found.");
            }
        } catch (Exception e) {
            Console.WriteLine(e);
        }
    }
}
