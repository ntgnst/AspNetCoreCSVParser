using AspNetCoreCSVParser;
using System.Collections.Generic;


namespace ParserTest
{
    public class TestModel : ICsvRow
    {
        public TestModel()
        {
            DynamicResourceList = new List<DynamicResource>();
        }
        public int ID { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public long Gsm { get; set; }
        public List<DynamicResource> DynamicResourceList { get; set; }
    }
}
