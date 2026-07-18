namespace Shooter.Client.Menu
{
    public class ConfigFile { public string ServerAddress { get; set; } }
    public class TokenResponse { public string Token { get; set; } }
    public class ProblemResponse { public string Code { get; set; } public string Detail { get; set; } }
    public class ServerInfoResponse { public string Name { get; set; } public int Major { get; set; } public int Minor { get; set; } public int Patch { get; set; } }
    public class UserDto { public long Id { get; set; } public string DisplayName { get; set; } }
    public class PlayerDto { public long Id { get; set; } public UserDto User { get; set; } public string Role { get; set; } public long MemberSince { get; set; } }
    public class WorldDto { public string Id { get; set; } public string Name { get; set; } public long CreatedAt { get; set; } public string JoinPolicy { get; set; } public PlayerDto[] Players { get; set; } }
    public class WorldsWrap { public WorldDto[] Items { get; set; } }
    public class LoginRequest { public string Username { get; set; } public string Password { get; set; } }
    public class RegisterRequest { public string Username { get; set; } public string DisplayName { get; set; } public string Password { get; set; } }
    public class CreateWorldRequest { public string Name { get; set; } public string JoinPolicy { get; set; } }
}
