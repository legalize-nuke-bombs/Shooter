using System;
using System.Text;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Security;
using Shooter.Accounts.Mnemonics;

namespace Shooter.Accounts
{
    public class Account
    {
        private readonly Ed25519PrivateKeyParameters key;

        private Account(Ed25519PrivateKeyParameters key)
        {
            this.key = key;
        }

        public static Account Generate()
        {
            return new Account(new Ed25519PrivateKeyParameters(new SecureRandom()));
        }

        public static Account FromKey(string secret)
        {
            return new Account(new Ed25519PrivateKeyParameters(Convert.FromBase64String(secret), 0));
        }

        public static Account FromPhrase(string phrase)
        {
            return new Account(new Ed25519PrivateKeyParameters(Mnemonic.ToEntropy(phrase), 0));
        }

        public string Key => Convert.ToBase64String(key.GetEncoded());

        public string Public => Convert.ToBase64String(key.GeneratePublicKey().GetEncoded());

        public string Phrase => Mnemonic.FromEntropy(key.GetEncoded());

        public byte[] Sign(string context, byte[] data)
        {
            byte[] message = Framed(context, data);
            var signer = new Ed25519Signer();
            signer.Init(true, key);
            signer.BlockUpdate(message, 0, message.Length);
            return signer.GenerateSignature();
        }

        public static bool Verify(string publicKey, string context, byte[] data, byte[] signature)
        {
            var pub = new Ed25519PublicKeyParameters(Convert.FromBase64String(publicKey), 0);
            byte[] message = Framed(context, data);
            var verifier = new Ed25519Signer();
            verifier.Init(false, pub);
            verifier.BlockUpdate(message, 0, message.Length);
            return verifier.VerifySignature(signature);
        }

        private static byte[] Framed(string context, byte[] data)
        {
            byte[] tag = Encoding.UTF8.GetBytes(context);
            var message = new byte[tag.Length + 1 + data.Length];
            Buffer.BlockCopy(tag, 0, message, 0, tag.Length);
            message[tag.Length] = 0x00;
            Buffer.BlockCopy(data, 0, message, tag.Length + 1, data.Length);
            return message;
        }
    }
}
