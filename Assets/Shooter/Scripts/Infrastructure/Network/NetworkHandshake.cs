using System;
using System.Text;
using Newtonsoft.Json;

namespace Shooter.Infrastructure
{
    public static class NetworkHandshake
    {
        private const string SignatureContext = "join";

        private struct Payload
        {
            public string Name { get; set; }
            public string PublicKey { get; set; }
            public byte[] Signature { get; set; }
        }

        public static byte[] EncodeConnectionData(string playerName, Account account, string certificate)
        {
            Payload payload = new()
            {
                Name = playerName,
                PublicKey = account.PublicKey,
                Signature = account.SignData(SignatureContext, GetChallengeBytes(certificate))
            };
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload));
        }

        public static bool TryDecodeConnectionData(byte[] connectionData, string certificate, out string playerName, out string publicKey)
        {
            playerName = null;
            publicKey = null;
            if (connectionData == null || connectionData.Length == 0) return false;

            try
            {
                Payload payload = JsonConvert.DeserializeObject<Payload>(Encoding.UTF8.GetString(connectionData));
                if (string.IsNullOrEmpty(payload.PublicKey) || payload.Signature == null) return false;
                if (!Account.IsSignatureValid(payload.PublicKey, SignatureContext, GetChallengeBytes(certificate), payload.Signature)) return false;

                playerName = payload.Name ?? "";
                publicKey = payload.PublicKey;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static byte[] GetChallengeBytes(string certificate)
        {
            return Encoding.UTF8.GetBytes(certificate);
        }
    }
}
