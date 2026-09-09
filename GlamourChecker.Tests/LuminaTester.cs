using Xunit;
using System;
using Lumina.Excel.Sheets;
namespace GlamourChecker.Tests { public class LuminaTester { [Fact] public void TestItemProps() { var type = typeof(Item); foreach (var prop in type.GetProperties()) { if (prop.Name.Contains("Defense") || prop.Name.Contains("Damage")) { Console.WriteLine(prop.Name + " : " + prop.PropertyType); } } } } }
