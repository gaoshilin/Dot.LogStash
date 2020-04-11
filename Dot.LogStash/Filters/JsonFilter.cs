using System;
using System.Collections.Generic;
using Dot.LogStash.Extensions;
using Dot.LogStash.SmartFormatters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dot.LogStash.Filters
{
    public class JsonFilter : IElasticAppenderFilter
    {
        public static readonly string DefaultSeparator = "_";
        private static JsonSerializerSettings JsonSerializerSettings;
        private const string DefaultDateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff";
        private LogEventSmartFormatter _sourceKey;

        [PropertyNotEmpty]
        public string SourceKey
        {
            get { return _sourceKey; }
            set { _sourceKey = value; }
        }

        public bool FlattenJson { get; set; }

        public string Separator { get; set; }

        public string DateTimeFormat { get; set; }

        public JsonFilter()
        {
            SourceKey = "JsonRaw";
            FlattenJson = false;
            Separator = DefaultSeparator;
            DateTimeFormat = DefaultDateTimeFormat;

            if (JsonSerializerSettings == null)
            {
                JsonSerializerSettings = new JsonSerializerSettings
                {
                    DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,
                    DateFormatString = DateTimeFormat
                };
            }
        }

        public void PrepareConfiguration(IElasticsearchClient client)
        {
        }

        public void PrepareEvent(Dictionary<string, object> logEvent)
        {
            var key = _sourceKey.Format(logEvent);
            if (!logEvent.TryGetValue(key, out object value))
                return;

            var json = (value is string) ? (string)value : JsonConvert.SerializeObject(value, JsonSerializerSettings);
            var token = JToken.Parse(json);
            if (FlattenJson)
            {
                ScanToken(logEvent, token, "");
            }
            else
            {
                logEvent[key] = token;
            }
        }

        private void ScanToken(IDictionary<string, object> logEvent, JToken token, string prefix)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    foreach (var prop in token.Children<JProperty>())
                    {
                        ScanToken(logEvent, prop.Value, Join(prefix, prop.Name));
                    }
                    break;

                case JTokenType.Array:
                    var index = 0;
                    foreach (var child in token.Children())
                    {
                        ScanToken(logEvent, child, Join(prefix, index.ToString()));
                        index++;
                    }
                    break;
                default:
                    logEvent.ReplaceOrIgnoreNull(prefix, ((JValue)token).Value);
                    break;
            }
        }

        private string Join(string prefix, string name)
        {
            return (string.IsNullOrEmpty(prefix) ? name : string.Concat(prefix, Separator, name));
        }
    }
}
