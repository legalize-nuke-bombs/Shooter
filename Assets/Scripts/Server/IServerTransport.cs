using System;

namespace Shooter.Server
{
    public interface IServerTransport
    {
        event Action<int, string> ClientConnected;
        event Action<int, string> MessageReceived;
        event Action<int> ClientDisconnected;
        event Action<string> HookReceived;

        Func<string, bool> HookAuthorizer { get; set; }

        void Start(int port);
        void Send(int connectionId, string message);
        void Kick(int connectionId);
        void Stop();
        void Poll();
    }
}
