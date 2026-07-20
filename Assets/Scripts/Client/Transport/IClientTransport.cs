using System;

namespace Shooter.Client.Transport
{
    public interface IClientTransport
    {
        event Action Connected;
        event Action<string> MessageReceived;

        void Connect(string url);
        void Send(string message);
        void Stop();
        void Poll();
    }
}
