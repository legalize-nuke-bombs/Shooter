using System;
using System.Text;
using UnityEngine;
using Shooter.GameServer;

namespace Shooter.Menu
{
    public static class ClientJwt
    {
        public static long ExtractUserId(string token)
        {
            try
            {
                string[] parts = token.Split('.');
                string payload = parts[1].Replace('-', '+').Replace('_', '/');
                switch (payload.Length % 4) { case 2: payload += "=="; break; case 3: payload += "="; break; }
                var claims = JsonUtility.FromJson<JwtClaims>(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
                return long.Parse(claims.sub.Split(':')[0]);
            }
            catch { return -1; }
        }
    }
}
