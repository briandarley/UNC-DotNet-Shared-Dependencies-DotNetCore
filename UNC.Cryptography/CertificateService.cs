using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using Serilog;
using UNC.Cryptography.Types;
using UNC.Services;
using UNC.Services.Interfaces.Response;

namespace UNC.Cryptography
{
    public class CertificateService : ServiceBase
    {
        public CertificateService(ILogger logger) : base(logger)
        {
        }


        /// <summary>
        /// Returns TypedResponse of <see cref="X509Certificate2">X509Certificate2</see>
        /// given <see cref="string">password</see> and <see cref="KeyTypes">keyType</see>
        /// IdentityServer will require both an RSA and ECDSA certificate, call <see cref="ConvertCertToBinary"/> to store content to file
        /// and use as a source when calling IdentityServer initialization process
        /// </summary>
        /// <param name="password"></param>
        /// <param name="keyType"></param>
        /// <returns></returns>
        public IResponse CreateX509Certificate(string password, KeyTypes keyType)
        {
            try
            {
                LogBeginRequest();

                
                var rand = new SecureRandom();

                //Generate a key pair
                var keyGen = new RsaKeyPairGenerator();
                keyGen.Init(new KeyGenerationParameters(rand, 1024));

                var keys = GetKeys(keyType);

                var signatureFactory = keyType == KeyTypes.ECDSA 
                    ? new Asn1SignatureFactory("SHA256WITHECDSA", keys.Private, rand) 
                    : new Asn1SignatureFactory("SHA512WITHRSA", keys.Private, rand);

                var guid = Guid.NewGuid();

                //Generate a certificate
                var dn = new X509Name("CN=" + guid);
                var certGen = new X509V3CertificateGenerator();
                certGen.SetIssuerDN(dn);
                certGen.SetSerialNumber(new BigInteger(1, guid.ToByteArray()));


                certGen.SetSubjectDN(dn);
                certGen.SetPublicKey(keys.Public);
                certGen.SetNotBefore(DateTime.UtcNow);
                certGen.SetNotAfter(DateTime.UtcNow.AddYears(2));

                var bcCert = certGen.Generate(signatureFactory);

                
                using var p12Stream = new MemoryStream();
                
                var p12 = new Pkcs12Store();

                p12.SetKeyEntry("identity-management", new AsymmetricKeyEntry(keys.Private), new X509CertificateEntry[] { new X509CertificateEntry(bcCert) });
                p12.Save(p12Stream, password.ToCharArray(), rand);

                
                return TypedResponse(new X509Certificate2(p12Stream.ToArray(), password, X509KeyStorageFlags.DefaultKeySet | X509KeyStorageFlags.Exportable));

            }
            catch (Exception ex)
            {
                return LogException(ex, false);
            }
            finally
            {
                LogEndRequest();
            }
        }

        /// <summary>
        /// Given <see cref="X509Certificate2">X509Certificate2</see> and <see cref="string">password</see> convert contents of certificate to binary representation. 
        /// Returns TypedResponse of byte[]
        /// </summary>
        /// <param name="cert"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public IResponse ConvertCertToBinary(X509Certificate2 cert, string password)
        {
            try
            {
                LogBeginRequest();

                var bytes = cert.Export(X509ContentType.Pkcs12, password);

                return TypedResponse(bytes);

            }
            catch (Exception ex)
            {
                return LogException(ex, false);
            }
            finally
            {
                LogEndRequest();
            }
        }

        /// <summary>
        /// Returns a basic RSAParameters, initially used in IdentityServer but replaced by CreateX509Certificate
        /// </summary>
        /// <returns></returns>
        public IResponse GetRsaParameters()
        {
            try
            {
                LogBeginRequest();

                var newKeys = GetKeys(KeyTypes.RSA);
                
                var rpckp = (RsaPrivateCrtKeyParameters)newKeys.Private;

                
                var rsaParameters = new System.Security.Cryptography.RSAParameters
                {
                    
                    Modulus = rpckp.Modulus.ToByteArrayUnsigned(),
                    P = rpckp.P.ToByteArrayUnsigned(),
                    Q = rpckp.Q.ToByteArrayUnsigned(),
                    DP = rpckp.DP.ToByteArrayUnsigned(),
                    DQ = rpckp.DQ.ToByteArrayUnsigned(),
                    InverseQ = rpckp.QInv.ToByteArrayUnsigned(),
                    D = rpckp.Exponent.ToByteArrayUnsigned(),
                    Exponent = rpckp.PublicExponent.ToByteArrayUnsigned()
                };

                return TypedResponse(rsaParameters);
            }
            catch (Exception ex)
            {
                return LogException(ex, false);
            }
            finally
            {
                LogEndRequest();
            }
        }

        private static AsymmetricCipherKeyPair GetKeys(KeyTypes keyType)
        {


            if (keyType == KeyTypes.RSA)
            {
                RsaKeyPairGenerator r = new RsaKeyPairGenerator();
                r.Init(new Org.BouncyCastle.Crypto.KeyGenerationParameters(new Org.BouncyCastle.Security.SecureRandom(), 2048));

                AsymmetricCipherKeyPair keys = r.GenerateKeyPair();

                return keys;
            }
            else
            {
                var gen = new ECKeyPairGenerator();
                var keyGenerationParameters = new KeyGenerationParameters(new Org.BouncyCastle.Security.SecureRandom(), 256);

                //var ecp = Org.BouncyCastle.Asn1.Sec.SecNamedCurves.GetByName("secp384r1");
                //gen.Init(new ECKeyGenerationParameters(new ECDomainParameters(ecp.Curve, ecp.G, ecp.N, ecp.H, ecp.GetSeed()), new SecureRandom()));

                gen.Init(keyGenerationParameters);

                return gen.GenerateKeyPair();
            }

        }
    }
}
