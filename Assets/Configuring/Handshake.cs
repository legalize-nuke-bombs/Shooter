using System;
using System.Text;
using Newtonsoft.Json;
using Shooter.Accounts;

namespace Shooter.Configuring
{
    public static class Handshake
    {
        private const string Context = "join";

        private struct Payload
        {
            public string Name { get; set; }
            public string PublicKey { get; set; }
            public byte[] Signature { get; set; }
        }

        public static byte[] Encode(string name, Account account, string certificate)
        {
            var payload = new Payload
            {
                Name = name,
                PublicKey = account.Public,
                Signature = account.Sign(Context, Challenge(certificate))
            };
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload));
        }

        public static bool TryDecode(byte[] data, string certificate, out string name, out string publicKey)
        {
            name = null;
            publicKey = null;
            if (data == null || data.Length == 0) return false;

            try
            {
                var payload = JsonConvert.DeserializeObject<Payload>(Encoding.UTF8.GetString(data));
                if (string.IsNullOrEmpty(payload.PublicKey) || payload.Signature == null) return false;
                if (!Account.Verify(payload.PublicKey, Context, Challenge(certificate), payload.Signature)) return false;

                name = payload.Name ?? "";
                publicKey = payload.PublicKey;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static byte[] Challenge(string certificate)
        {
            return Encoding.UTF8.GetBytes(certificate);
        }
    }
}
