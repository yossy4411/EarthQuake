namespace EarthQuake.Core.EarthQuakes.P2PQuake.Client;

public interface IPeerConnection
{
    public int PeerId { get; set; }
    public PeerType Type { get; }

    public enum PeerType : byte
    {
        Server = 1,
        Client = 2,
    }

    public P2PEventArgs CreateEvent(string e) => new P2PeerEventArgs(e, PeerId);

    protected void OnReceived(object? sender, P2PEventArgs e)
    {
    }

    protected void OnReceived(string e) => OnReceived(this, CreateEvent(e));

    protected void OnSent(object? sender, P2PEventArgs e)
    {
    }

    protected void OnSent(string e) => OnSent(this, CreateEvent(e));

    protected void OnError(Exception message)
    {
    }

    public Task Send(string message);
    public Task<string> Read();
    public event EventHandler<P2PEventArgs>? OnMessageReceived;
    public event EventHandler<P2PEventArgs>? OnMessageSent;
    public event EventHandler<ErrorEventArgs>? OnErrorOccured;
}