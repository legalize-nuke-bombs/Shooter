using System;
using System.Text;
using Newtonsoft.Json;

namespace Shooter.Configuring
{
    public static class Invite
    {
        private struct Payload
        {
            public string Address { get; set; }
            public ushort Port { get; set; }
            public string Certificate { get; set; }
        }

        public static string Encode(string address, ushort port, string certificate)
        {
            var payload = new Payload { Address = address, Port = port, Certificate = certificate };
            string json = JsonConvert.SerializeObject(payload);
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        }

        public static bool TryDecode(string code, out string address, out ushort port, out string certificate)
        {
            address = null;
            port = 0;
            certificate = null;

            try
            {
                string json = Encoding.UTF8.GetString(Convert.FromBase64String(code.Trim()));
                var payload = JsonConvert.DeserializeObject<Payload>(json);
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
