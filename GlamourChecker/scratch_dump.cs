using System;
using System.Reflection;
using Lumina.Excel.Sheets;

class Program
{
    static void Main()
    {
        Console.WriteLine("Item Properties:");
        foreach (var p in typeof(Item).GetProperties())
        {
            if (p.Name.Contains("BaseParam"))
                Console.WriteLine($"{p.Name} : {p.PropertyType.Name}");
        }

        Console.WriteLine("\nClassJob Properties:");
        foreach (var p in typeof(ClassJob).GetProperties())
        {
            if (p.Name.Contains("Modifier") || p.Name.Contains("Role"))
                Console.WriteLine($"{p.Name} : {p.PropertyType.Name}");
        }
    }
}
