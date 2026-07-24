using Shooter.Auth;

namespace Shooter.Server.Sessions
{
    public sealed class HookAuthority
    {
        private const string HookSubject = "hook";

        private readonly byte[] jwtSecret;

        public HookAuthority(byte[] jwtSecret)
        {
            this.jwtSecret = jwtSecret;
        }

        public bool Allows(string token)
        {
            return Jwt.TryVerify(token, jwtSecret, out string subject) && subject == HookSubject;
        }
    }
}
