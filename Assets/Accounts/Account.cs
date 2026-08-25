using System;
using System.Text;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;

namespace Shooter.Accounts
{
    public class Account
    {
        private const int KeyBits = 4096;
        private const string Algorithm = "SHA256withRSA";

        private readonly AsymmetricCipherKeyPair keys;

        private Account(AsymmetricCipherKeyPair keys)
        {
            this.keys = keys;
        }

        public static Account Generate()
        {
            var generator = new RsaKeyPairGenerator();
            generator.Init(new KeyGenerationParameters(new SecureRandom(), KeyBits));
            return new Account(generator.GenerateKeyPair());
        }

        public static Account FromKey(string secret)
        {
            var key = (RsaPrivateCrtKeyParameters)PrivateKeyFactory.CreateKey(Convert.FromBase64String(secret));
            var pub = new RsaKeyParameters(false, key.Modulus, key.PublicExponent);
            return new Account(new AsymmetricCipherKeyPair(pub, key));
        }

        public string Key => Convert.ToBase64String(
            PrivateKeyInfoFactory.CreatePrivateKeyInfo(keys.Private).GetDerEncoded());

        public string Public => Convert.ToBase64String(
            SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(keys.Public).GetDerEncoded());

        public byte[] Sign(string context, byte[] data)
        {
            byte[] message = Framed(context, data);
            ISigner signer = SignerUtilities.GetSigner(Algorithm);
            signer.Init(true, keys.Private);
            signer.BlockUpdate(message, 0, message.Length);
            return signer.GenerateSignature();
        }

        public static bool Verify(string publicKey, string context, byte[] data, byte[] signature)
        {
            AsymmetricKeyParameter pub = PublicKeyFactory.CreateKey(Convert.FromBase64String(publicKey));
            byte[] message = Framed(context, data);
            ISigner verifier = SignerUtilities.GetSigner(Algorithm);
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
