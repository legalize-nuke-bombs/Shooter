using System;
using System.Security.Cryptography;
using System.Text;
using Shooter.Shared;

namespace Shooter.GameServer
{
    public static class JwtVerifier
    {
        public static bool TryVerify(string token, byte[] secret, out JwtClaims claims)
        {
            claims = null;

            string[] parts = token.Split('.');
            if (parts.Length != 3) return false;

            byte[] expectedSignature;
            using (var hmac = new HMACSHA256(secret))
                expectedSignature = hmac.ComputeHash(Encoding.ASCII.GetBytes(parts[0] + "." + parts[1]));

            byte[] actualSignature;
            try { actualSignature = Jwt.Base64UrlDecode(parts[2]); }
            catch { return false; }

            if (!CryptographicOperations.FixedTimeEquals(expectedSignature, actualSignature)) return false;

            if (!Jwt.TryParsePayload(token, out JwtClaims parsed)) return false;

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (parsed.exp != 0 && parsed.exp < now) return false;

            claims = parsed;
            return true;
        }
    }
}
