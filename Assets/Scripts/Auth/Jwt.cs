using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Shooter.Auth
{
    public static class Jwt
    {
        [Serializable]
        private class JwtClaims
        {
            public string sub;
            public long exp;
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

            JwtClaims claims = JsonUtility.FromJson<JwtClaims>(payloadJson);
            if (claims == null || string.IsNullOrEmpty(claims.sub)) return false;

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (claims.exp != 0 && claims.exp < now) return false;

            subject = claims.sub;
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
