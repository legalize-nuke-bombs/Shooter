using Shooter.Logging;

namespace Shooter.Client.Account
{
    public sealed class ClientSession
    {
        private const int GamePort = 9090;

        public ClientSession(string serverAddress)
        {
            ServerAddress = serverAddress;
        }

        public string ServerAddress { get; }

        public string DisplayName { get; private set; } = "";

        public long UserId { get; private set; } = -1;

        public string Token { get; private set; } = "";

        public bool LoggedIn => !string.IsNullOrEmpty(Token);

        public string HttpBase => "http://" + ServerAddress;

        public string WsUrl => "ws://" + Host + ":" + GamePort + "/ws?token=" + Token;

        public void Authorize(string token)
        {
            Token = token;
        }

        public void Identify(long userId, string displayName)
        {
            UserId = userId;
            DisplayName = displayName;
            Log.Info("Session opened for user {} '{}'", userId, displayName);
        }

        public void LogOut()
        {
            Log.Info("Session closed for user {}", UserId);
            Token = "";
            UserId = -1;
            DisplayName = "";
        }

        private string Host => ServerAddress.Contains(":") ? ServerAddress.Substring(0, ServerAddress.IndexOf(':')) : ServerAddress;
    }
}
