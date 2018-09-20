using System.Collections.Generic;

namespace AspNetCoreCSVParser
{
    public interface ICsvRow
    {
            List<DynamicResource> DynamicResourceList { get; set; }
    }
}
