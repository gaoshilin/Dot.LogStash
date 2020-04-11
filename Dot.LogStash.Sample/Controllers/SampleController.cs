using System;
using System.Collections.Generic;
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

            var dic = new Dictionary<string, object>();
            dic.Add("Request", name);
            dic.Add("Response", response);
            dic.Add("Timespan", (DateTime.Now - requestTimespan).TotalMilliseconds);
            _logger.LogInformation(JsonConvert.SerializeObject(dic));

            return response;
        }
    }
}