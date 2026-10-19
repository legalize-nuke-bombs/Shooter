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

        private readonly AsymmetricCipherKeyPair keyPair;

        private string certificate;
        private string privateKeyPem;

        private Account(AsymmetricCipherKeyPair keyPair)
        {
            this.keyPair = keyPair;
        }

        public static Account CreateWithGeneratedKeys()
        {
            RsaKeyPairGenerator keyPairGenerator = new();
            keyPairGenerator.Init(new KeyGenerationParameters(new SecureRandom(), KeyBits));
            return new Account(keyPairGenerator.GenerateKeyPair());
        }

        public static Account FromPrivateKey(string privateKey)
        {
            RsaPrivateCrtKeyParameters privateKeyParameters = (RsaPrivateCrtKeyParameters)PrivateKeyFactory.CreateKey(Convert.FromBase64String(privateKey));
            RsaKeyParameters publicKeyParameters = new(false, privateKeyParameters.Modulus, privateKeyParameters.PublicExponent);
            return new Account(new AsymmetricCipherKeyPair(publicKeyParameters, privateKeyParameters));
        }

        public string PrivateKey => Convert.ToBase64String(PrivateKeyInfoFactory.CreatePrivateKeyInfo(keyPair.Private).GetDerEncoded());

        public string PublicKey => Convert.ToBase64String(SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(keyPair.Public).GetDerEncoded());

        public string Certificate => certificate ??= BuildCertificate();

        public string PrivateKeyPem => privateKeyPem ??= EncodeAsPem(keyPair.Private);

        public byte[] SignData(string context, byte[] data)
        {
            byte[] message = BindToContext(context, data);
            ISigner signer = SignerUtilities.GetSigner(SignatureAlgorithm);
            signer.Init(true, keyPair.Private);
            signer.BlockUpdate(message, 0, message.Length);
            return signer.GenerateSignature();
        }

        public static bool IsSignatureValid(string publicKey, string context, byte[] data, byte[] signature)
        {
            AsymmetricKeyParameter verifyingKey = PublicKeyFactory.CreateKey(Convert.FromBase64String(publicKey));
            byte[] message = BindToContext(context, data);
            ISigner verifier = SignerUtilities.GetSigner(SignatureAlgorithm);
            verifier.Init(false, verifyingKey);
            verifier.BlockUpdate(message, 0, message.Length);
            return verifier.VerifySignature(signature);
        }

        private static byte[] BindToContext(string context, byte[] data)
        {
            byte[] contextBytes = Encoding.UTF8.GetBytes(context);
            byte[] message = new byte[contextBytes.Length + 1 + data.Length];
            Buffer.BlockCopy(contextBytes, 0, message, 0, contextBytes.Length);
            message[contextBytes.Length] = 0x00;
            Buffer.BlockCopy(data, 0, message, contextBytes.Length + 1, data.Length);
            return message;
        }

        private string BuildCertificate()
        {
            X509Name distinguishedName = new("CN=" + CommonName);
            X509V3CertificateGenerator certificateGenerator = new();
            certificateGenerator.SetSerialNumber(BigInteger.One);
            certificateGenerator.SetIssuerDN(distinguishedName);
            certificateGenerator.SetSubjectDN(distinguishedName);
            certificateGenerator.SetNotBefore(NotBefore);
            certificateGenerator.SetNotAfter(NotAfter);
            certificateGenerator.SetPublicKey(keyPair.Public);
            Asn1SignatureFactory signatureFactory = new(SignatureAlgorithm, keyPair.Private);
            return EncodeAsPem(certificateGenerator.Generate(signatureFactory));
        }

        private static string EncodeAsPem(object subject)
        {
            using StringWriter writer = new();
            new PemWriter(writer).WriteObject(subject);
            return writer.ToString();
        }
    }
}
