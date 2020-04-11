using System.Collections.Generic;
using Dot.LogStash.Configuration;

namespace Dot.LogStash
{
    public class InnerBulkOperation 
    {
        public string IndexName { get; set; }
        public string IndexType { get; set; }
        public object Document { get; set; }
        public Dictionary<string, string> IndexOperationParams { get; set; }

        public InnerBulkOperation()
        {
        }
    }
}