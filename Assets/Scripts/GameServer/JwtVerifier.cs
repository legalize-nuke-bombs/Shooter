using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

[Serializable]
public class JwtClaims
{
    public string sub;
    public string name;
    public string worldId;
    public long iat;
    public long exp;
}

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
        try { actualSignature = Base64UrlDecode(parts[2]); }
        catch { return false; }

        if (!CryptographicOperations.FixedTimeEquals(expectedSignature, actualSignature)) return false;

        string payloadJson;
        try { payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1])); }
        catch { return false; }

        JwtClaims parsed = JsonUtility.FromJson<JwtClaims>(payloadJson);
        if (parsed == null || string.IsNullOrEmpty(parsed.sub)) return false;

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (parsed.exp != 0 && parsed.exp < now) return false;

        claims = parsed;
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
