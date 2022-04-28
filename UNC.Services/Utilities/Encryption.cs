using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Serilog;
using UNC.Services.Interfaces.Response;

namespace UNC.Services.Utilities
{
    public class Encryption : ServiceBase
    {

        public Encryption(ILogger logger) : base(logger)
        {
        }


        /// <summary>
        /// Using public key (Look for 'PublicKey' AppDomain 'Environment')
        /// Provide a value to Encrypt
        /// </summary>
        /// <param name="publicKey"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public IResponse EncryptText(string publicKey, string value)
        {
            try
            {
                LogBeginRequest();

                var request = GetRsaParam(publicKey);

                if (request is IErrorResponse) return request;

                var pubKey = ((ITypedResponse<RSAParameters>)request).Entity;

                var csp = new RSACryptoServiceProvider(2048);

                csp.ImportParameters(pubKey);

                var bytesPlainTextData = Encoding.Unicode.GetBytes(value);

                var bytesCypherText = csp.Encrypt(bytesPlainTextData, false);

                var cypherText = Convert.ToBase64String(bytesCypherText);

                return TypedResponse(cypherText);

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

        public IResponse DecryptText(string privateKey, string value)
        {
            try
            {
                LogBeginRequest();

                var request = GetRsaParam(privateKey);

                if (request is IErrorResponse) return request;

                var privKey = ((ITypedResponse<RSAParameters>)request).Entity;

                var csp = new RSACryptoServiceProvider(2048);
                csp.ImportParameters(privKey);

                var bytesCypherText = Convert.FromBase64String(value);
                var bytesPlainTextData = csp.Decrypt(bytesCypherText, false);
                
                var cypherText = Encoding.Unicode.GetString(bytesPlainTextData);

                return TypedResponse(cypherText);

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
        /// Generate public/private key
        /// Expect ICollectionResponse KeyValuePair string,string 
        /// </summary>
        /// <returns></returns>
        public IResponse GeneratePublicPrivateKey()
        {
            try
            {
                LogBeginRequest();

                var csp = new RSACryptoServiceProvider(2048);
                var rawPrivateKeyRequest = GetRawPrivateKey(csp);
                var rawPublicKeyRequest = GetRawPublicKey(csp);

                if (rawPrivateKeyRequest is IErrorResponse || rawPublicKeyRequest is IErrorResponse)
                {
                    if (rawPrivateKeyRequest is IErrorResponse err1) return err1;
                    
                    return rawPublicKeyRequest;
                }

                
                var pubKey = ((ITypedResponse<string>)rawPublicKeyRequest).Entity;
                var privKey = ((ITypedResponse<string>)rawPrivateKeyRequest).Entity;

                var list = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("Public", pubKey),
                    new KeyValuePair<string, string>("Private", privKey)
                };

                return CollectionResponse(list);

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


        private IResponse GetRsaParam(string rawKey)
        {
            try
            {
                LogBeginRequest();

                var sr = new StringReader(rawKey);

                var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));

                var deserialized = xs.Deserialize(sr);

                if (deserialized is null)
                {
                    return LogError("Failed to Deserialize key");
                }

                var key = (RSAParameters)deserialized;

                return TypedResponse(key);

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

        private IResponse GetRawPrivateKey(RSACryptoServiceProvider csp)
        {
            try
            {
                LogBeginRequest();

                using var sw = new StringWriter();
                
                var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));
                
                xs.Serialize(sw, csp.ExportParameters(true));
                
                return TypedResponse(sw.ToString());
                

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

        private IResponse GetRawPublicKey(RSACryptoServiceProvider csp)
        {
            try
            {
                LogBeginRequest();
                
                var sw = new StringWriter();
                
                var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));
                
                xs.Serialize(sw, csp.ExportParameters(false));
                
                return TypedResponse(sw.ToString());
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
    }
}
