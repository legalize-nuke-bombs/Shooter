public static class ConnectionConfig
{
    public static string Username = "";
    public static string DisplayName = "";
    public static string Token = "";
    public static string ServerAddress = "localhost:8080";
    public static string RoomCode = "";

    public static string HttpBase => "http://" + ServerAddress;
    public static string WsUrl => "ws://" + ServerAddress + "/ws?token=" + Token;
}
