using System;
using System.Collections.Generic;
using log4net.Core;
using Newtonsoft.Json;

namespace Dot.LogStash.LogEventFactory
{
    public class SimpleLogEventFactory : ILogEventFactory
    {
        public void Configure(ILogEventFactoryParams factoryParams)
        {
        }

        public virtual Dictionary<string, object> CreateLogEvent(LoggingEvent loggingEvent)
        {
            if (loggingEvent == null)
            {
                throw new ArgumentNullException("loggingEvent");
            }

            var resultDictionary = new Dictionary<string, object>();

            ParseBasicFields(loggingEvent, resultDictionary);

            ParseMessage(loggingEvent, resultDictionary);

            return resultDictionary;
        }

        protected void ParseBasicFields(LoggingEvent loggingEvent, Dictionary<string, object> resultDictionary)
        {
            resultDictionary["@timestamp"] = loggingEvent.TimeStamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
            //resultDictionary["@timestamp"] = loggingEvent.TimeStamp.ToUniversalTime().ToString("O");
            resultDictionary["LoggerName"] = loggingEvent.LoggerName;
            resultDictionary["HostName"] = Environment.MachineName;
        }

        protected void ParseMessage(LoggingEvent loggingEvent, Dictionary<string, object> resultDictionary)
        {
            resultDictionary["Message"] = loggingEvent.RenderedMessage;

            // Added special handling of the MessageObject since it may be an exception. 
            // Exception Types require specialized serialization to prevent serialization exceptions.
            if (loggingEvent.MessageObject != null && !(loggingEvent.MessageObject is string))
            {
                // Write by this way: log.Info(object obj);
                if (loggingEvent.MessageObject is Exception)
                {
                    resultDictionary["MessageObject"] = JsonSerializableException.Create(loggingEvent.MessageObject as Exception);
                }
                else 
                {
                    resultDictionary["MessageObject"] = loggingEvent.MessageObject;
                }

                resultDictionary["Message"] = JsonConvert.SerializeObject(resultDictionary["MessageObject"]);
            }
        }
    }
}
