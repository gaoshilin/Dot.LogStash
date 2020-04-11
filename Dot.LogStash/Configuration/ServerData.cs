namespace Dot.LogStash.Configuration
{
    public class ServerData : IServerData
    {
        public string Address { get; set; }
        public int Port { get; set; }
        public string Path { get; set; }
    }
}
