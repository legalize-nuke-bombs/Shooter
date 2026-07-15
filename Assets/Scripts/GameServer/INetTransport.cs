using System;

public interface INetTransport
{
    event Action<int, string> ClientConnected;
    event Action<int, string> MessageReceived;
    event Action<int> ClientDisconnected;

    void Start(int port);
    void Send(int connectionId, string message);
    void Kick(int connectionId);
    void Stop();
    void Poll();
}
