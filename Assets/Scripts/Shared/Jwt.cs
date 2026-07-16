using System;
using System.Text;
using UnityEngine;

namespace Shooter.Shared
{
    [Serializable]
    public class JwtClaims
    {
        public string sub;
        public long iat;
        public long exp;
    }

    public static class Jwt
    {
        public static bool TryParsePayload(string token, out JwtClaims claims)
        {
            claims = null;

            string[] parts = token.Split('.');
            if (parts.Length != 3) return false;

            string payloadJson;
            try { payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1])); }
            catch { return false; }

            JwtClaims parsed = JsonUtility.FromJson<JwtClaims>(payloadJson);
            if (parsed == null || string.IsNullOrEmpty(parsed.sub)) return false;

            claims = parsed;
            return true;
        }

        public static bool TryGetUserId(string token, out long userId)
        {
            userId = -1;
            if (!TryParsePayload(token, out JwtClaims claims)) return false;
            if (!long.TryParse(claims.sub.Split(':')[0], out userId))
            {
                userId = -1;
                return false;
            }
            return true;
        }

        public static byte[] Base64UrlDecode(string input)
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
