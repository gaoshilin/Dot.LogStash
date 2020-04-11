﻿using System.Collections.Generic;
using System.Xml;
using Dot.LogStash.Extensions;
using Dot.LogStash.SmartFormatters;
using Newtonsoft.Json;

namespace Dot.LogStash.Filters
{
    public class XmlFilter : IElasticAppenderFilter
    {
        private LogEventSmartFormatter _sourceKey;
        private JsonFilter _jsonFilter;

        [PropertyNotEmpty]
        public string SourceKey
        {
            get { return _sourceKey; }
            set { _sourceKey = value; }
        }

        public bool FlattenXml { get; set; }

        public string Separator { get; set; }

        public XmlFilter()
        {
            SourceKey = "XmlRaw";
            FlattenXml = false;
            Separator = JsonFilter.DefaultSeparator;
        }

        public void PrepareConfiguration(IElasticsearchClient client)
        {
            _jsonFilter = new JsonFilter {FlattenJson = FlattenXml, SourceKey = SourceKey, Separator = Separator};
            _jsonFilter.PrepareConfiguration(client);
        }

        public void PrepareEvent(Dictionary<string, object> logEvent)
        {
            var key = _sourceKey.Format(logEvent);
            string input;
            if (!logEvent.TryGetStringValue(key, out input))
            {
                return;
            }

            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(input);
            var jsonDoc = JsonConvert.SerializeXmlNode(xmlDoc);
            logEvent[key] = jsonDoc;
            _jsonFilter.PrepareEvent(logEvent);
        }
    }
}
