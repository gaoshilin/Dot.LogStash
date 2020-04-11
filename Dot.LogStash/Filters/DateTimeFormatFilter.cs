using System;
using System.Collections.Generic;
using System.Text;
using Dot.LogStash.Extensions;
using Dot.LogStash.SmartFormatters;

namespace Dot.LogStash.Filters
{
    public class DateTimeFormatFilter : IElasticAppenderFilter
    {
        private LogEventSmartFormatter _sourceKey;

        [PropertyNotEmpty]
        public string SourceKey
        {
            get { return _sourceKey; }
            set { _sourceKey = value; }
        }

        public string OriginalFormat { get; set; }
        public string NewFormat { get; set; }

        public void PrepareConfiguration(IElasticsearchClient client)
        {
        }

        public void PrepareEvent(Dictionary<string, object> logEvent)
        {
            string formattedKey = _sourceKey.Format(logEvent);
            if (logEvent.TryGetStringValue(formattedKey, out string value))
            {
                logEvent[formattedKey] = DateTime.ParseExact(value, OriginalFormat, null).ToString(NewFormat);
            }
        }
    }
}
