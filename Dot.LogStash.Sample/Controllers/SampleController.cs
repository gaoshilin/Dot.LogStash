using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Dot.LogStash.Sample.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SampleController : ControllerBase
    {
        private readonly ILogger _logger;

        public SampleController(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger("ElasticSearchLogger");
        }

        [HttpGet]
        public string Get(string name)
        {
            var requestTimespan = DateTime.Now;
            var response = $"Hello {name}!";

            /* 自定义日志类，字段需要和 template 的定义一致，如：
             * "properties": {
                 "@timestamp": { "type": "date", "format": "yyyy-MM-dd HH:mm:ss" },
                 "Request": { "type": "text" },
                 "Response": { "type": "text" },
                 "Timespan": { "type": "double" }
               }
             */
            var traceLog = new TraceLog();
            traceLog.Request = name;
            traceLog.Response = response;
            traceLog.Timespan = (DateTime.Now - requestTimespan).TotalMilliseconds;

            _logger.LogInformation(JsonConvert.SerializeObject(traceLog));

            return response;
        }
    }

    public class TraceLog
    {
        public string Request { get; set; }
        public string Response { get; set; }
        public double Timespan { get; set; }
    }
}