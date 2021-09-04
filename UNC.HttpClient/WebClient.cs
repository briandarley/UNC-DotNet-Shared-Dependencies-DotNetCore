using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Serilog;
using UNC.HttpClient.EventHandlers;
using UNC.HttpClient.Exceptions;
using UNC.HttpClient.Extensions;
using UNC.HttpClient.Interfaces;
using UNC.HttpClient.Models;
using UNC.Services;
using UNC.Services.Constants;
using UNC.Services.Models;
using UNC.Services.Responses;

namespace UNC.HttpClient
{
    public class WebClient : ServiceBase, IWebClient
    {
        public TokenResponse AuthResponse { get; set; }
        private readonly string _clientId;
        private readonly string _applicationName;
        private readonly IAuthSettings _authSettings;
        private readonly IPrincipal _principal;

        public EventHandler<TokenResponse> TokenRefreshed { get; set; }
        public EventHandler<WebRequestException> WebRequestErrorHandler { get; set; }

        public bool PreventLogging { get; set; }
        private string _baseAddress;
        public int Timeout { get; set; }
        public string BaseAddress
        {
            get
            {
                if (!_baseAddress.EndsWith("/"))
                {
                    return _baseAddress + "/";
                }

                return _baseAddress;
            }
            set
            {
                if (!IsValidUrl(value))
                {
                    throw new ArgumentException("Invalid URL, unable to resolve path");
                }

                _baseAddress = value;
            }

        }
        private Lazy<Func<System.Net.Http.HttpClient>> LazyClient { get; set; }
        public bool DefaultEnsureSuccessStatusCode { get; set; }

        public WebClient(
            ILogger logger, 
            string clientId = "", 
            string applicationName = "",
            IAuthSettings authSettings = null,
            IPrincipal principal = null, 
            RequestHeader requestHeader = null) : base(logger, requestHeader)
        {
            
            _clientId = clientId;
            _applicationName = applicationName;
            _authSettings = authSettings;
            _principal = principal;

            InitializeEndPoint();
        }

        

        private void InitializeEndPoint()
        {

            DefaultEnsureSuccessStatusCode = true;
            Timeout = 1;

            //Ignore Cert Errors
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            //Not trying to get cute, we can't pass the base address in most cases so we use the lazy instantiation to get it when we perform an actual request. 
            LazyClient = new Lazy<Func<System.Net.Http.HttpClient>>(() =>
            {

                System.Net.Http.HttpClient GetClient()
                {
                    var client = new System.Net.Http.HttpClient
                    {
                        Timeout = TimeSpan.FromHours(Timeout),
                        BaseAddress = new Uri(BaseAddress),
                    };

                    if (!string.IsNullOrEmpty(_clientId))
                    {
                        client.DefaultRequestHeaders.Add(RequestHeaders.CLIENT_ID, _clientId);
                    }
                    if (!string.IsNullOrEmpty(_clientId))
                    {
                        client.DefaultRequestHeaders.Add(RequestHeaders.APPLICATION_NAME, _applicationName);
                    }
                    if (_principal != null && _principal.Identity.IsAuthenticated)
                    {
                        client.DefaultRequestHeaders.Add(RequestHeaders.AUTH_USER, _principal.Identity.Name);
                    }

                    if (_authSettings != null)
                    {
                        SetAuthToken();

                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AuthResponse.access_token);
                    }




                    return client;
                }


                return GetClient;
            });
        }

        // ReSharper disable once MemberCanBePrivate.Global
        //public static TokenResponse AuthResponse { get; set; }
        private static readonly object _lock = new object();
        private void SetAuthToken()
        {
            if (AuthResponse != null && AuthResponse.EmpireDateTime > DateTime.Now.AddMilliseconds(100))
            {
                return;
            }
            lock (_lock)
            {
                var request = GetAuthToken();
                request.Wait();
                AuthResponse = request.Result;
                TokenRefreshed?.Invoke(this, AuthResponse);
            }
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public async Task<TokenResponse> GetAuthToken()
        {
            try
            {
                LogBeginRequest();
                var identityServer = _authSettings.IdentityServer;
                if (identityServer.EndsWith("/"))
                {
                    identityServer = identityServer.Substring(0, identityServer.Length - 1);
                }
                var client = new System.Net.Http.HttpClient
                {
                    BaseAddress = new Uri($"{identityServer}/connect/token")
                };



                var message = new HttpRequestMessage();
                message.Headers.Accept.Clear();
                message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));


                var content = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("grant_type", "client_credentials"),
                    new KeyValuePair<string, string>("scope", _authSettings.Scope),
                    new KeyValuePair<string, string>("client_id", _authSettings.ClientId),
                    new KeyValuePair<string, string>("client_secret", _authSettings.ClientSecret),

                });
                message.Content = content;
                var request = await client.PostAsync("", message.Content);


                var responseContent = await request.Content.ReadAsStringAsync();


                var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseContent);

                return tokenResponse;
            }
            catch (Exception ex)
            {
                LogException(ex, true);
                return null;
            }
            finally
            {
                LogEndRequest();
            }
        }
        public async Task<bool> Put(string path = "", object entity = null, bool putByQueryParameter = false)
        {

            var fullPath = string.Empty;
            
            try
            {
                LogBeginRequest();

                

                var content = SerializedObject(entity);

                HttpResponseMessage response;

                if (LazyClient is null)
                {
                    throw new Exception("LazyClient not initialized");
                }

                var client = LazyClient.Value();

                if (entity != null)
                {

                    if (putByQueryParameter)
                    {
                        fullPath = $"{BaseAddress}{path}?model={content}";

                        LogWebRequestPath(fullPath);


                        response = await client.PutAsync(fullPath, null);
                    }
                    else
                    {
                        fullPath = $"{BaseAddress}{path}";

                        LogWebRequestPath(fullPath);

                        response = await client.PutAsync(fullPath, new StringContent(content, Encoding.UTF8, "application/json"));
                    }
                }
                else
                {
                    fullPath = $"{BaseAddress}{path}";

                    LogWebRequestPath(fullPath);

                    response = await client.PutAsync(fullPath, null);
                }

                if (DefaultEnsureSuccessStatusCode)
                {
                    response.EnsureSuccessStatusCode();
                }
                return response.IsSuccessStatusCode;




            }
            catch (Exception ex) when (ex.InnerException != null && ex.InnerException.Message.Contains("A connection with the server could not be established"))
            {
                var args = new WebRequestException
                {
                    Path = fullPath,
                    Entity = entity,
                    Exception = ex,
                    Action = "PUT"
                };

                WebRequestErrorHandler?.Invoke(this, args);


                if (!PreventLogging || args.LogError)
                {
                    LogWarning($"URL: {fullPath}");
                    LogPayloadRequest(entity);

                    LogException(ex, true, $"Failed to connect to endpoint {path}, make sure service is running.");
                }

                if (!args.ExceptionAcknowledged)
                {
                    throw;
                }

                return false;
            }
            catch (Exception ex)
            {
                var args = new WebRequestException
                {
                    Path = fullPath,
                    Entity = entity,
                    Exception = ex,
                    Action = "PUT"
                };

                WebRequestErrorHandler?.Invoke(this, args);

                var message = $"Failed to PUT to endpoint {fullPath}.";
                if (!PreventLogging || args.LogError)
                {
                    LogPayloadRequest(entity);
                    LogException(ex, false, message);

                }
                if (!args.ExceptionAcknowledged)
                {
                    throw new RequestException(fullPath, message, ex);
                }

                return false;
            }
            finally
            {
                LogEndRequest();
            }



        }





        public async Task<T> Put<T>(string path = "", object entity = null, bool putByQueryParameter = false)
        {
            var fullPath = BaseAddress + path;
            
            try
            {

                LogBeginRequest();

                

                HttpResponseMessage response;

                if (LazyClient is null)
                {
                    throw new Exception("LazyClient not initialized");
                }

                var client = LazyClient.Value();

                if (entity == null)
                {
                    LogWebRequestPath(fullPath);
                    response = await client.PutAsync(fullPath, null);
                }
                else
                {
                    var content = SerializedObject(entity);

                    if (putByQueryParameter)
                    {
                        fullPath += $"?model={content}";

                        LogWebRequestPath(fullPath);

                        response = await client.PutAsync(fullPath, null);
                    }
                    else
                    {
                        LogWebRequestPath(fullPath);
                        response = await client.PutAsync(fullPath, new StringContent(content, Encoding.UTF8, "application/json"));
                    }
                }

                if (DefaultEnsureSuccessStatusCode)
                    response.EnsureSuccessStatusCode();


                if (response.IsSuccessStatusCode)
                {

                    T responseEntity = await response.Content.ReadAsAsync<T>();
                    return responseEntity;
                }

                return default(T);

            }
            catch (Exception ex) when (ex.InnerException != null && ex.InnerException.Message.Contains("A connection with the server could not be established"))
            {
                var args = new WebRequestException
                {
                    Path = fullPath,
                    Entity = entity,
                    Exception = ex,
                    Action = "PUT"
                };

                WebRequestErrorHandler?.Invoke(this, args);


                if (!PreventLogging || args.LogError)
                {
                    LogWarning($"URL: {fullPath}");
                    LogPayloadRequest(entity);
                    LogException(ex, true, $"Failed to connect to endpoint {path}, make sure service is running.");
                }

                if (!args.ExceptionAcknowledged)
                {
                    throw;
                }

                return default(T);

            }
            catch (Exception ex)
            {

                var args = new WebRequestException
                {
                    Path = fullPath,
                    Entity = entity,
                    Exception = ex,
                    Action = "PUT"
                };

                WebRequestErrorHandler?.Invoke(this, args);

                var message = $"Failed to PUT to endpoint {fullPath}.";
                if (!PreventLogging || args.LogError)
                {
                    LogPayloadRequest(entity);
                    LogException(ex, false, message);

                }
                if (!args.ExceptionAcknowledged)
                {
                    throw new RequestException(fullPath, message, ex);
                }

                return default(T);
            }
            finally
            {
                LogEndRequest();
            }



        }

        public async Task<bool> Post(string path = "", object entity = null, bool putByQueryParameter = false)
        {

            var fullPath = BaseAddress + path;
            
            try
            {
                LogBeginRequest();

                
                var content = SerializedObject(entity);

                HttpResponseMessage response;

                if (LazyClient is null)
                {
                    throw new Exception("LazyClient not initialized");
                }

                var client = LazyClient.Value();

                if (entity != null)
                {

                    if (putByQueryParameter)
                    {
                        fullPath = $"{BaseAddress}{path}?model={content}";

                        LogWebRequestPath(fullPath);

                        response = await client.PostAsync(fullPath, null);
                    }
                    else
                    {
                        fullPath = $"{BaseAddress}{path}";

                        LogWebRequestPath(fullPath);

                        response = await client.PostAsync(fullPath, new StringContent(content, Encoding.UTF8, "application/json"));
                    }
                }
                else
                {
                    fullPath = $"{BaseAddress}{path}";

                    LogWebRequestPath(fullPath);

                    response = await client.PostAsync(fullPath, null);
                }

                if (DefaultEnsureSuccessStatusCode)
                {
                    response.EnsureSuccessStatusCode();
                }

                return response.IsSuccessStatusCode;




            }
            catch (Exception ex) when (ex.InnerException != null && ex.InnerException.Message.Contains("A connection with the server could not be established"))
            {
                var args = new WebRequestException
                {
                    Path = fullPath,
                    Entity = entity,
                    Exception = ex,
                    Action = "POST"
                };

                WebRequestErrorHandler?.Invoke(this, args);


                if (!PreventLogging || args.LogError)
                {
                    LogWarning($"URL: {fullPath}");
                    LogPayloadRequest(entity);
                    LogException(ex, true, $"Failed to connect to endpoint {path}, make sure service is running.");
                }

                if (!args.ExceptionAcknowledged)
                {
                    throw;
                }

                return false;

            }
            catch (Exception ex)
            {

                var args = new WebRequestException
                {
                    Path = fullPath,
                    Entity = entity,
                    Exception = ex,
                    Action = "POST"
                };

                WebRequestErrorHandler?.Invoke(this, args);

                var message = $"Failed to POST to endpoint {fullPath}.";
                if (!PreventLogging || args.LogError)
                {
                    LogPayloadRequest(entity);
                    LogException(ex, false, message);

                }
                if (!args.ExceptionAcknowledged)
                {
                    throw new RequestException(fullPath, message, ex);
                }

                return false;
            }
            finally
            {
                LogEndRequest();
            }



        }

        public async Task<T> Post<T>(string path = "", object entity = null, bool postByQueryParameter = false)
        {

            var fullPath = BaseAddress + path;
            
            try
            {
                LogBeginRequest();

                

                HttpResponseMessage response;

                if (LazyClient is null)
                {
                    throw new Exception("LazyClient not initialized");
                }

                var client = LazyClient.Value();

                if (entity == null)
                {
                    LogWebRequestPath(fullPath);
                    response = await client.PostAsync(fullPath, null);
                }
                else
                {
                    var content = SerializedObject(entity);

                    if (postByQueryParameter)
                    {
                        fullPath += $"?model={content}";
                        LogWebRequestPath(fullPath);
                        response = await client.PostAsync(fullPath, null);
                    }
                    else
                    {
                        LogWebRequestPath(fullPath);
                        response = await client.PostAsync(fullPath, new StringContent(content, Encoding.UTF8, "application/json"));
                    }
                }

                if (DefaultEnsureSuccessStatusCode)
                {
                    response.EnsureSuccessStatusCode();
                }

                if (response.IsSuccessStatusCode)
                {
                    T responseEntity = await response.Content.ReadAsAsync<T>();
                    return responseEntity;
                }

                return default(T);

            }
            catch (Exception ex) when (ex.InnerException != null && ex.InnerException.Message.Contains("A connection with the server could not be established"))
            {
                var args = new WebRequestException
                {
                    Path = fullPath,
                    Entity = entity,
                    Exception = ex,
                    Action = "POST"
                };

                WebRequestErrorHandler?.Invoke(this, args);


                if (!PreventLogging || args.LogError)
                {
                    LogWarning($"URL: {fullPath}");
                    LogPayloadRequest(entity);
                    LogException(ex, true, $"Failed to connect to endpoint {path}, make sure service is running.");
                }

                if (!args.ExceptionAcknowledged)
                {
                    throw;
                }

                return default(T);

            }
            catch (Exception ex)
            {

                var args = new WebRequestException
                {
                    Path = fullPath,
                    Entity = entity,
                    Exception = ex,
                    Action = "POST"
                };

                WebRequestErrorHandler?.Invoke(this, args);

                var message = $"Failed to POST to endpoint {fullPath}.";
                if (!PreventLogging || args.LogError)
                {
                    LogPayloadRequest(entity);
                    LogException(ex, false, message);

                }
                if (!args.ExceptionAcknowledged)
                {
                    throw new RequestException(fullPath, message, ex);
                }

                return default(T);
            }
            finally
            {
                LogEndRequest();
            }


        }

        public async Task<T> GetEntity<T>(string path = "")
        {
            var fullPath = BaseAddress + path;
            
            try
            {
                LogBeginRequest();

                if (LazyClient is null)
                {
                    throw new Exception("LazyClient not initialized");
                }

                var client = LazyClient.Value();

                LogWebRequestPath(fullPath);
                var response = await client.GetAsync(fullPath);

                if (response.StatusCode == HttpStatusCode.NotFound
                    && typeof(T).IsGenericType
                    && typeof(T).GetGenericTypeDefinition() == typeof(PagedResponse<>))
                {
                    return (T)Activator.CreateInstance(typeof(T));

                }

                if (DefaultEnsureSuccessStatusCode)
                {
                    response.EnsureSuccessStatusCode();
                }

                if (response.IsSuccessStatusCode)
                {
                    var rawResponse = await response.Content.ReadAsStringAsync();

                    return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(rawResponse);

                }



            }
            catch (HttpRequestException ex) when (ex.Message.Contains("502"))
            {
                if (!PreventLogging)
                {
                    LogInfo("502 Occurred, retrying request");

                }
                LogWebRequestPath(fullPath);

                if (LazyClient is null)
                {
                    throw new Exception("LazyClient not initialized");
                }

                var client = LazyClient.Value();

                var response = await client.GetAsync(fullPath);

                response.EnsureSuccessStatusCode();

                var serializedObject = await response.Content.ReadAsStringAsync();

                return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(serializedObject);

            }
            catch (HttpRequestException ex) when (ex.Message.Contains("404") || ex.InnerException != null && ex.InnerException.Message.Contains("404"))
            {
                var message = $"Failed to GET endpoint {path}. Details {ex.Message}";
                LogDebug($"URL: {BaseAddress}{path}");
                LogDebug(message);

            }
            catch (Exception ex) when (ex.InnerException != null && ex.InnerException.Message.Contains("A connection with the server could not be established"))
            {
                var args = new WebRequestException
                {
                    Path = fullPath,
                    Exception = ex,
                    Action = "GET"
                };

                WebRequestErrorHandler?.Invoke(this, args);

                LogWebRequestPath(fullPath);

                if (!PreventLogging || args.LogError)
                {

                    LogException(ex, true, $"Failed to connect to endpoint {path}, make sure service is running.");
                }

                if (!args.ExceptionAcknowledged)
                {
                    throw;
                }

                return default(T);

            }
            catch (Exception ex)
            {
                LogWebRequestPath(fullPath);
                var args = new WebRequestException
                {
                    Path = fullPath,
                    Exception = ex,
                    Action = "GET"
                };

                WebRequestErrorHandler?.Invoke(this, args);

                var message = $"Failed to GET to endpoint {fullPath}.";
                if (!PreventLogging || args.LogError)
                {
                    LogException(ex, false, message);

                }
                if (!args.ExceptionAcknowledged)
                {
                    throw new RequestException(fullPath, message, ex);
                }

                return default(T);
            }

            finally
            {
                LogEndRequest();
            }



            return default(T);

        }
        public async Task<string> GetRaw(string path = "")
        {
            var fullPath = BaseAddress + path;
            
            try
            {
                LogBeginRequest();

                
                if (LazyClient is null)
                {
                    throw new Exception("LazyClient not initialized");
                }

                var client = LazyClient.Value();

                LogWebRequestPath(fullPath);
                var response = await client.GetAsync(fullPath);

                if (DefaultEnsureSuccessStatusCode)
                {
                    response.EnsureSuccessStatusCode();
                }

                if (response.IsSuccessStatusCode)
                {
                    var rawResponse = await response.Content.ReadAsStringAsync();
                    return rawResponse;
                }


            }
            catch (HttpRequestException ex) when (ex.Message.Contains("502"))
            {
                if (!PreventLogging)
                {
                    LogInfo("502 Occurred, retrying request");
                    LogInfo($"URL: {BaseAddress}{path}");
                }

                if (LazyClient is null)
                {
                    throw new Exception("LazyClient not initialized");
                }

                var client = LazyClient.Value();

                var response = await client.GetAsync(fullPath);

                response.EnsureSuccessStatusCode();

                var stringResponse = await response.Content.ReadAsStringAsync();

                return stringResponse;

            }
            catch (Exception ex) when (ex.InnerException != null && ex.InnerException.Message.Contains("A connection with the server could not be established"))
            {
                var args = new WebRequestException
                {
                    Path = fullPath,
                    Exception = ex,
                    Action = "GET"
                };

                WebRequestErrorHandler?.Invoke(this, args);

                LogWebRequestPath(fullPath);

                if (!PreventLogging || args.LogError)
                {

                    LogException(ex, true, $"Failed to connect to endpoint {path}, make sure service is running.");
                }

                if (!args.ExceptionAcknowledged)
                {
                    throw;
                }

                return string.Empty;

            }
            catch (Exception ex)
            {
                LogWebRequestPath(fullPath);
                var args = new WebRequestException
                {
                    Path = fullPath,
                    Exception = ex,
                    Action = "GET"
                };

                WebRequestErrorHandler?.Invoke(this, args);

                var message = $"Failed to GET to endpoint {fullPath}.";
                if (!PreventLogging || args.LogError)
                {
                    LogException(ex, false, message);

                }
                if (!args.ExceptionAcknowledged)
                {
                    throw new RequestException(fullPath, message, ex);
                }

                return string.Empty;
            }

            finally
            {
                LogEndRequest();
            }


            return string.Empty;

        }



        public async Task<bool> EnsureSuccessStatusCode(string path = "")
        {
            var fullPath = BaseAddress + path;
            
            try
            {
                LogBeginRequest();

                
                if (LazyClient is null)
                {
                    throw new Exception("LazyClient not initialized");
                }

                var client = LazyClient.Value();

                LogWebRequestPath(fullPath);

                var response = await client.GetAsync(fullPath);

                if (DefaultEnsureSuccessStatusCode)
                {
                    response.EnsureSuccessStatusCode();
                }

                return true;


            }
            catch (Exception ex)
            {
                LogWebRequestPath(fullPath);
                LogException(ex, false, $"Failed to post to endpoint {path}.");


            }
            finally
            {

                LogEndRequest();
            }

            return false;

        }

        public async Task<bool> DeleteEntity(string path = "")
        {
            var fullPath = BaseAddress + path;
            
            try
            {
                LogBeginRequest();

                

                fullPath = BaseAddress + path;

                if (LazyClient is null)
                {
                    throw new Exception("LazyClient not initialized");
                }

                using (var client = LazyClient.Value())
                {

                    LogWebRequestPath(fullPath);

                    var response = await client.DeleteAsync(fullPath);

                    if (DefaultEnsureSuccessStatusCode)
                    {
                        response.EnsureSuccessStatusCode();
                    }
                    return response.IsSuccessStatusCode;
                }
            }
            catch (Exception ex) when (ex.InnerException != null && ex.InnerException.Message.Contains("A connection with the server could not be established"))
            {
                var args = new WebRequestException
                {
                    Path = fullPath,
                    Exception = ex,
                    Action = "DELETE"
                };

                WebRequestErrorHandler?.Invoke(this, args);


                if (!PreventLogging || args.LogError)
                {
                    LogWarning($"URL: {fullPath}");
                    LogException(ex, true, $"Failed to connect to endpoint {path}, make sure service is running.");
                }

                if (!args.ExceptionAcknowledged)
                {
                    throw;
                }

                return false;
            }
            catch (Exception ex)
            {
                var args = new WebRequestException
                {
                    Path = fullPath,
                    Exception = ex,
                    Action = "DELETE"
                };

                WebRequestErrorHandler?.Invoke(this, args);

                var message = $"Failed to DELETE to endpoint {fullPath}.";
                if (!PreventLogging || args.LogError)
                {
                    LogException(ex, false, message);

                }
                if (!args.ExceptionAcknowledged)
                {
                    throw new RequestException(fullPath, message, ex);
                }

                return false;
            }
            finally
            {
                LogEndRequest();
            }



        }



        private string SerializedObject(object entity)
        {
            //System.Text.Json does not support reference loop handling ... wtf .. we must rely on NewtonSoft to handle this then. so much for ridding ourselves of dependencies..
            return Newtonsoft.Json.JsonConvert.SerializeObject(entity, Newtonsoft.Json.Formatting.None, new Newtonsoft.Json.JsonSerializerSettings { ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore });

        }

        private void LogPayloadRequest(object entity)
        {
            try
            {
                if (entity == null) return;

                var contents = SerializedObject(entity);
                if (!PreventLogging)
                    LogWarning($"Message Payload: {contents}");

            }
            catch
            {
                // ignored
            }
        }

        private bool IsValidUrl(string uriName)
        {
            return Uri.TryCreate(uriName, UriKind.Absolute, out var uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        protected override void LogEndRequest(TimeSpan? elapsed = null, string callerName = "", string sourcePath = "", int sourceLineNumber = 0)
        {
            if (!PreventLogging)
            {
                base.LogEndRequest(elapsed, callerName, sourcePath, sourceLineNumber);
            }
        }

        protected override void LogBeginRequest(string callerName = "", string sourcePath = "", int sourceLineNumber = 0)
        {
            if (!PreventLogging)
            {
                base.LogBeginRequest(callerName, sourcePath, sourceLineNumber);
            }
        }

        private void LogWebRequestPath(string fullPath)
        {
            if (!PreventLogging)
            {
                LogInfo($"URL: {fullPath}");
            }
        }

        public async Task<bool> Patch(string path = "", object entity = null, bool putByQueryParameter = false)
        {

            var fullPath = string.Empty;

            
            try
            {
                LogBeginRequest();

                
                var content = SerializedObject(entity);

                HttpResponseMessage response;

                if (LazyClient is null)
                {
                    throw new Exception("LazyClient not initialized");
                }

                var client = LazyClient.Value();


                if (entity != null)
                {

                    if (putByQueryParameter)
                    {
                        fullPath = $"{BaseAddress}{path}?model={content}";

                        LogWebRequestPath(fullPath);


                        response = await client.PatchAsync(fullPath + $"?model={content}", null);

                    }
                    else
                    {
                        fullPath = $"{BaseAddress}{path}";

                        LogWebRequestPath(fullPath);

                        response = await client.PatchAsync(fullPath, new StringContent(content, Encoding.UTF8, "application/json"));
                    }
                }
                else
                {
                    fullPath = $"{BaseAddress}{path}";

                    LogWebRequestPath(fullPath);


                    response = await client.PatchAsync(fullPath, null);
                }

                if (DefaultEnsureSuccessStatusCode)
                {
                    response.EnsureSuccessStatusCode();
                }

                return response.IsSuccessStatusCode;




            }
            catch (Exception ex) when (ex.InnerException != null && ex.InnerException.Message.Contains("A connection with the server could not be established"))
            {
                var args = new WebRequestException
                {
                    Path = fullPath,
                    Entity = entity,
                    Exception = ex,
                    Action = "PATCH"
                };

                WebRequestErrorHandler?.Invoke(this, args);


                if (!PreventLogging || args.LogError)
                {
                    LogWarning($"URL: {fullPath}");
                    LogPayloadRequest(entity);
                    LogException(ex, true, $"Failed to connect to endpoint {path}, make sure service is running.");
                }

                if (!args.ExceptionAcknowledged)
                {
                    throw;
                }

                return false;

            }
            catch (Exception ex)
            {

                var args = new WebRequestException
                {
                    Path = fullPath,
                    Entity = entity,
                    Exception = ex,
                    Action = "PATCH"
                };

                WebRequestErrorHandler?.Invoke(this, args);

                var message = $"Failed to PATCH to endpoint {fullPath}.";
                if (!PreventLogging || args.LogError)
                {
                    LogPayloadRequest(entity);
                    LogException(ex, false, message);

                }
                if (!args.ExceptionAcknowledged)
                {
                    throw new RequestException(fullPath, message, ex);
                }

                return false;
            }
            finally
            {
                LogEndRequest();
            }



        }

        public async Task<T> Patch<T>(string path = "", object entity = null, bool putByQueryParameter = false)
        {
            var fullPath = BaseAddress + path;
            
            

            try
            {
                LogBeginRequest();
                
                

                HttpResponseMessage response;

                if (LazyClient is null)
                {
                    throw new Exception("LazyClient not initialized");
                }

                var client = LazyClient.Value();


                if (entity == null)
                {
                    LogWebRequestPath(fullPath);

                    response = await client.PatchAsync(fullPath, null);
                }
                else
                {
                    var content = SerializedObject(entity);

                    if (putByQueryParameter)
                    {
                        fullPath += $"?model={content}";

                        LogWebRequestPath(fullPath);

                        response = await client.PatchAsync(fullPath, null);
                    }
                    else
                    {
                        LogWebRequestPath(fullPath);

                        response = await client.PatchAsync(fullPath, new StringContent(content, Encoding.UTF8, "application/json"));
                    }
                }

                if (DefaultEnsureSuccessStatusCode)
                    response.EnsureSuccessStatusCode();


                if (response.IsSuccessStatusCode)
                {

                    T responseEntity = await response.Content.ReadAsAsync<T>();
                    return responseEntity;
                }

                return default(T);

            }
            catch (Exception ex) when (ex.InnerException != null && ex.InnerException.Message.Contains("A connection with the server could not be established"))
            {
                var args = new WebRequestException
                {
                    Path = fullPath,
                    Entity = entity,
                    Exception = ex,
                    Action = "PATCH"
                };

                WebRequestErrorHandler?.Invoke(this, args);


                if (!PreventLogging || args.LogError)
                {
                    LogWarning($"URL: {fullPath}");
                    LogPayloadRequest(entity);
                    LogException(ex, true, $"Failed to connect to endpoint {path}, make sure service is running.");
                }

                if (!args.ExceptionAcknowledged)
                {
                    throw;
                }

                return default(T);

            }
            catch (Exception ex)
            {

                var args = new WebRequestException
                {
                    Path = fullPath,
                    Entity = entity,
                    Exception = ex,
                    Action = "PATCH"
                };

                WebRequestErrorHandler?.Invoke(this, args);

                var message = $"Failed to PATCH to endpoint {fullPath}.";
                if (!PreventLogging || args.LogError)
                {
                    LogPayloadRequest(entity);
                    LogException(ex, false, message);

                }
                if (!args.ExceptionAcknowledged)
                {
                    throw new RequestException(fullPath, message, ex);
                }

                return default(T);
            }
            finally
            {
                LogEndRequest();
            }




        }

        public override string ToString()
        {
            return _authSettings.ToString();
        }
    }

    
}
