using Newtonsoft.Json;

namespace Dot.LogStash
{
    internal sealed class PartialElasticResponse
    {
        [JsonProperty("errors")]
        public bool Errors { get; set; }
    }
}