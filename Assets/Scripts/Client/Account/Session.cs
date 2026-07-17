namespace Shooter.Client.Account
{
    public static class Session
    {
        public static string Username = "";
        public static string DisplayName = "";
        public static long UserId = -1;
        public static string Token = "";

        public static string ServerAddress = "";
        public static int GamePort = 9090;

        public static string HttpBase => "http://" + ServerAddress;
        public static string Host => ServerAddress.Contains(":") ? ServerAddress.Substring(0, ServerAddress.IndexOf(':')) : ServerAddress;
        public static string WsUrl => "ws://" + Host + ":" + GamePort + "/ws?token=" + Token;
    }
}
