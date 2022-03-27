using System;
using System.IO;
using System.Security.Cryptography;
using Serilog;
using UNC.Services;

namespace UNC.Cryptography
{
    public class PublicPrivateKeyService:ServiceBase
    {
        private readonly int _dwKeySize;
        private RSACryptoServiceProvider _csp;

        
        internal PublicPrivateKeyService(ILogger logger, int dwKeySize) : base(logger)
        {
            _dwKeySize = dwKeySize;

            _csp = GetCsp(_dwKeySize);
        }

        

        public string GetPrivateKey()
        {
            try
            {
                LogBeginRequest();
                
                var key = GetRawPrivateKey(_csp);

                return key;
            }
            catch (Exception ex)
            {
                LogException(ex, true);
                return default;
            }
            finally
            {
                LogEndRequest();
            }
        }

        public string GetPublicKey()
        {
            try
            {
                LogBeginRequest();

                var key = GetRawPublicKey(_csp);

                return key;
            }
            catch (Exception ex)
            {
                LogException(ex, true);
                return default;
            }
            finally
            {
                LogEndRequest();
            }
        }

        public string EncryptText(string publicKey, string plainText)
        {
            try
            {
                LogBeginRequest();

                var pubKey = GetRsaParam(publicKey);
                var csp = new RSACryptoServiceProvider(_dwKeySize);
                csp.ImportParameters(pubKey);

                //for encryption, always handle bytes...
                var bytesPlainTextData = System.Text.Encoding.Unicode.GetBytes(plainText);

                //apply pkcs#1.5 padding and encrypt our data 
                var bytesCypherText = csp.Encrypt(bytesPlainTextData, false);

                //we might want a string representation of our cypher text... base64 will do
                var cypherText = Convert.ToBase64String(bytesCypherText);
                return cypherText;


            }
            catch (Exception ex)
            {
                LogException(ex, true);

                return default;
            }
            finally
            {
                LogEndRequest();
            }
        }

        public string DecryptText(string privateKey, string encryptedText)
        {
            try
            {
                LogBeginRequest();

                var bytesCypherText = Convert.FromBase64String(encryptedText);

                var privKey = GetRsaParam(privateKey);

                //we want to decrypt, therefore we need a csp and load our private key
                _csp.ImportParameters(privKey);

                //decrypt and strip pkcs#1.5 padding
                var bytesPlainTextData = _csp.Decrypt(bytesCypherText, false);

                //get our original plainText back...
                return System.Text.Encoding.Unicode.GetString(bytesPlainTextData);

            }
            catch (Exception ex)
            {
                LogException(ex, true);
                return default;
            }
            finally
            {
                LogEndRequest();
            }
        }


        RSAParameters GetRsaParam(string rawKey)
        {
            
            using var sr = new StringReader(rawKey);
            //we need a deserializer
            var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));
            //get the object back from the stream
            var key = (RSAParameters)xs.Deserialize(sr);
            
            return key;
        }

        private string GetRawPublicKey(RSACryptoServiceProvider csp)
        {
            try
            {
                LogBeginRequest();

                var pubKey = csp.ExportParameters(false);
                
                //we need some buffer
                var sw = new StringWriter();
                //we need a serializer
                var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));
                //serialize the key into the stream
                xs.Serialize(sw, pubKey);
                //get the string from the stream
                var pubKeyString = sw.ToString();
                return pubKeyString;

            }
            catch (Exception ex)
            {
                LogException(ex, true);
                return default;
            }
            finally
            {
                LogEndRequest();
            }
        }

        private string  GetRawPrivateKey(RSACryptoServiceProvider csp)
        {
            try
            {
                LogBeginRequest();

                //how to get the private key
                var privateKey = csp.ExportParameters(true);

                //we need some buffer
                using var sw = new StringWriter();
                //we need a serializer
                var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));
                //serialize the key into the stream
                xs.Serialize(sw, privateKey);
                
                //get the string from the stream
                var privateKeyString = sw.ToString();
                
                return privateKeyString;

            }
            catch (Exception ex)
            {
                LogException(ex, true);
                return default;
            }
            finally
            {
                LogEndRequest();
            }
        }

        private RSACryptoServiceProvider GetCsp(int dwKeySize)
        {
            try
            {
                LogBeginRequest();

                return new RSACryptoServiceProvider(dwKeySize);
            }
            catch (Exception ex)
            {
                LogException(ex, true);
                return default;
            }
            finally
            {
                LogEndRequest();
            }
        }



    }
}
