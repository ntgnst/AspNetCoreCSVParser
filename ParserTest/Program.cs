using AspNetCoreCSVParser;
using System;
using System.Linq;

namespace ParserTest
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CsvContext csvContext = new CsvContext();
            var test = csvContext.Read<TestModel>($"C:\\TestCsv.csv",new CsvFileDescription() { SeparatorChar = ','}).ToList();
            test.ForEach(f => {
                Console.WriteLine($"ID : {f.ID} - Name : {f.Name} - Surname : {f.Surname} - Gsm : {f.Gsm}");
            });
            Console.ReadKey();
        }
    }
}
