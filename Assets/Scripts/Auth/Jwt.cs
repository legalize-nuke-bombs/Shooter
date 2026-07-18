using System;
using System.Security.Cryptography;
using System.Text;
using Shooter.Serialization;

namespace Shooter.Auth
{
    public static class Jwt
    {
        private class JwtClaims
        {
            public string Sub { get; set; }
            public long Exp { get; set; }
        }

        public static bool TryVerify(string token, byte[] secret, out string subject)
        {
            subject = null;

            string[] parts = token.Split('.');
            if (parts.Length != 3) return false;

            byte[] expectedSignature;
            using (var hmac = new HMACSHA256(secret))
                expectedSignature = hmac.ComputeHash(Encoding.ASCII.GetBytes(parts[0] + "." + parts[1]));

            byte[] actualSignature;
            string payloadJson;
            try
            {
                actualSignature = Base64UrlDecode(parts[2]);
                payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
            }
            catch { return false; }

            if (!CryptographicOperations.FixedTimeEquals(expectedSignature, actualSignature)) return false;

            JwtClaims claims = Json.Deserialize<JwtClaims>(payloadJson);
            if (claims == null || string.IsNullOrEmpty(claims.Sub)) return false;

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (claims.Exp != 0 && claims.Exp < now) return false;

            subject = claims.Sub;
            return true;
        }

        private static byte[] Base64UrlDecode(string input)
        {
            string padded = input.Replace('-', '+').Replace('_', '/');
            switch (padded.Length % 4)
            {
                case 2: padded += "=="; break;
                case 3: padded += "="; break;
            }
            return Convert.FromBase64String(padded);
        }
    }
}
