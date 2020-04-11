namespace Dot.LogStash.Configuration
{
    public interface IServerData
    {
        string Address { get; set; }
        int Port { get; set; }
        string Path { get; set; }
    }
}