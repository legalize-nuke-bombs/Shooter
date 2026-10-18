using System;
using System.IO;
using System.Text;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;

namespace Shooter.Infrastructure
{
    public class Account
    {
        public const string CommonName = "shooter-host";
        private const int KeyBits = 4096;
        private const string SignatureAlgorithm = "SHA256withRSA";
        private static readonly DateTime NotBefore = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime NotAfter = new(2100, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private readonly AsymmetricCipherKeyPair keys;

        private string certificate;
        private string privateKeyPem;

        private Account(AsymmetricCipherKeyPair keys)
        {
            this.keys = keys;
        }

        public static Account Generate()
        {
            RsaKeyPairGenerator generator = new();
            generator.Init(new KeyGenerationParameters(new SecureRandom(), KeyBits));
            return new Account(generator.GenerateKeyPair());
        }

        public static Account FromPrivateKey(string secret)
        {
            RsaPrivateCrtKeyParameters privateKey = (RsaPrivateCrtKeyParameters)PrivateKeyFactory.CreateKey(Convert.FromBase64String(secret));
            RsaKeyParameters publicKey = new(false, privateKey.Modulus, privateKey.PublicExponent);
            return new Account(new AsymmetricCipherKeyPair(publicKey, privateKey));
        }

        public string PrivateKey => Convert.ToBase64String(PrivateKeyInfoFactory.CreatePrivateKeyInfo(keys.Private).GetDerEncoded());

        public string PublicKey => Convert.ToBase64String(SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(keys.Public).GetDerEncoded());

        public string Certificate => certificate ??= BuildCertificate();

        public string PrivateKeyPem => privateKeyPem ??= Pem(keys.Private);

        public byte[] Sign(string context, byte[] data)
        {
            byte[] message = BoundToContext(context, data);
            ISigner signer = SignerUtilities.GetSigner(SignatureAlgorithm);
            signer.Init(true, keys.Private);
            signer.BlockUpdate(message, 0, message.Length);
            return signer.GenerateSignature();
        }

        public static bool Verify(string publicKey, string context, byte[] data, byte[] signature)
        {
            AsymmetricKeyParameter verifyingKey = PublicKeyFactory.CreateKey(Convert.FromBase64String(publicKey));
            byte[] message = BoundToContext(context, data);
            ISigner verifier = SignerUtilities.GetSigner(SignatureAlgorithm);
            verifier.Init(false, verifyingKey);
            verifier.BlockUpdate(message, 0, message.Length);
            return verifier.VerifySignature(signature);
        }

        private static byte[] BoundToContext(string context, byte[] data)
        {
            byte[] tag = Encoding.UTF8.GetBytes(context);
            byte[] message = new byte[tag.Length + 1 + data.Length];
            Buffer.BlockCopy(tag, 0, message, 0, tag.Length);
            message[tag.Length] = 0x00;
            Buffer.BlockCopy(data, 0, message, tag.Length + 1, data.Length);
            return message;
        }

        private string BuildCertificate()
        {
            X509Name name = new("CN=" + CommonName);
            X509V3CertificateGenerator generator = new();
            generator.SetSerialNumber(BigInteger.One);
            generator.SetIssuerDN(name);
            generator.SetSubjectDN(name);
            generator.SetNotBefore(NotBefore);
            generator.SetNotAfter(NotAfter);
            generator.SetPublicKey(keys.Public);
            Asn1SignatureFactory signature = new(SignatureAlgorithm, keys.Private);
            return Pem(generator.Generate(signature));
        }

        private static string Pem(object subject)
        {
            using StringWriter writer = new();
            new PemWriter(writer).WriteObject(subject);
            return writer.ToString();
        }
    }
}
