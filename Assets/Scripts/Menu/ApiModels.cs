using System;

namespace Shooter.Menu
{
    [Serializable] public class ConfigFile { public string serverAddress; }
    [Serializable] public class TokenResponse { public string token; }
    [Serializable] public class ProblemResponse { public string code; public string detail; }
    [Serializable] public class ServerInfoResponse { public string name; public int major; public int minor; public int patch; }
    [Serializable] public class UserDto { public long id; public string displayName; }
    [Serializable] public class PlayerDto { public long id; public UserDto user; public string role; public long memberSince; }
    [Serializable] public class WorldDto { public string id; public string name; public long createdAt; public string joinPolicy; public PlayerDto[] players; }
    [Serializable] public class WorldsWrap { public WorldDto[] items; }
    [Serializable] public class LoginRequest { public string username; public string password; }
    [Serializable] public class RegisterRequest { public string username; public string displayName; public string password; }
    [Serializable] public class CreateWorldRequest { public string name; public string joinPolicy; }
}
