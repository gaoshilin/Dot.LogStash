using RestSharp;

namespace Dot.LogStash.Authentication
{
    public class RequestData
    {
        public RestRequest RestRequest { get; set; }

        public string Url { get; set; }

        public string RequestString { get; set; }
    }
}
