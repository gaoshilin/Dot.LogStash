using System.Collections.Generic;

namespace Dot.LogStash
{
    public interface IElasticAppenderFilter 
    {
        void PrepareConfiguration(IElasticsearchClient client);
        void PrepareEvent(Dictionary<string, object> logEvent);
    }
}