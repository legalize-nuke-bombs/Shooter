using System;
using System.IO;

namespace Shooter.Infrastructure
{
    public static class NetworkInvite
    {
        private const byte FormatVersion = 1;

        public static string EncodeInviteCode(string address, ushort port, string certificate)
        {
            using MemoryStream stream = new();
            using (BinaryWriter writer = new(stream))
            {
                writer.Write(FormatVersion);
                writer.Write(address);
                writer.Write(port);
                writer.Write(Account.DecodeCertificatePem(certificate));
            }

            return Convert.ToBase64String(stream.ToArray());
        }

        public static bool TryDecodeInviteCode(string inviteCode, out string address, out ushort port, out string certificate)
        {
            address = null;
            port = 0;
            certificate = null;

            try
            {
                using BinaryReader reader = new(new MemoryStream(Convert.FromBase64String(inviteCode.Trim())));
                if (reader.ReadByte() != FormatVersion) return false;

                string decodedAddress = reader.ReadString();
                ushort decodedPort = reader.ReadUInt16();
                byte[] certificateDer = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
                if (string.IsNullOrEmpty(decodedAddress) || certificateDer.Length == 0) return false;

                address = decodedAddress;
                port = decodedPort;
                certificate = Account.EncodeCertificatePem(certificateDer);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
