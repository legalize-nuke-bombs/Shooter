using System;
using System.Text;
using Newtonsoft.Json;

namespace Shooter.Infrastructure
{
    public static class NetworkInvite
    {
        private struct Payload
        {
            public string Address { get; set; }
            public ushort Port { get; set; }
            public string Certificate { get; set; }
        }

        public static string EncodeInviteCode(string address, ushort port, string certificate)
        {
            Payload payload = new() { Address = address, Port = port, Certificate = certificate };
            string json = JsonConvert.SerializeObject(payload);
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        }

        public static bool TryDecodeInviteCode(string inviteCode, out string address, out ushort port, out string certificate)
        {
            address = null;
            port = 0;
            certificate = null;

            try
            {
                string json = Encoding.UTF8.GetString(Convert.FromBase64String(inviteCode.Trim()));
                Payload payload = JsonConvert.DeserializeObject<Payload>(json);
                if (string.IsNullOrEmpty(payload.Address) || string.IsNullOrEmpty(payload.Certificate)) return false;

                address = payload.Address;
                port = payload.Port;
                certificate = payload.Certificate;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
