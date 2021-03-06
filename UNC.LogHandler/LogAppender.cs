using System;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog.Core;

using Serilog.Events;
using UNC.Extensions.General;
using UNC.LogHandler.Models;

namespace UNC.LogHandler
{
    public class LogAppender : ILogEventSink, IDisposable
    {


        private readonly Func<IHttpContextAccessor> _httpContextAccessor;
        private readonly LogHttpClient _logHttpClient;
        private readonly IPrincipal _principal;
        

        public LogAppender(LogHttpClient logHttpClient, IPrincipal principal, Func<IHttpContextAccessor> httpContextAccessor)
        {

            _logHttpClient = logHttpClient;
            _principal = principal;
            _httpContextAccessor = httpContextAccessor;
        }


        public void Emit(LogEvent logEvent)
        {
            //now have thread id!!
            var logProperties = logEvent.Properties;
            var threadId = logProperties.SingleOrDefault(c => c.Key == "ThreadId").Value?.ToString();

            var messageText = logEvent.MessageTemplate.Text;

            if (!messageText.IsJson())
            {
                var jsonMessage = new
                {
                    Message = messageText
                };

                messageText = jsonMessage.ToJson();
            }

            var jobject = JObject.Parse(messageText);
            jobject["ThreadId"] = threadId;
            var appName = logProperties.SingleOrDefault(c => c.Key == "Application").Value?.ToString();
            appName = Regex.Replace(appName ?? "", "^\"|\"$", "");
            jobject["Application"] = appName;
            jobject["Level"] = logEvent.Level.ToString();


            var authUser = _principal.UserName();

            var appSource = logProperties.SingleOrDefault(c => c.Key == "AppSource").Value?.ToString();

            if (_httpContextAccessor() != null)
            {
                var context = _httpContextAccessor().HttpContext;

                if (context?.Request != null)
                {
                    var headerList = context.Request.Headers.ToList();
                    //Todo, we may want to use client id to discern which client made the request (Test console app, Production App) ClientId is set in the configuration file
                    //var clientId = headerList.SingleOrDefault(c => c.Key == "CLIENT_ID").Value;
                    appSource = headerList.SingleOrDefault(c => c.Key == "APPLICATION_NAME").Value;
                    if (appSource != null)
                    {
                        authUser = headerList.SingleOrDefault(c => c.Key == "AUTH_USER").Value;
                    }
                    

                }

            }




            //AppSource and Service account can both be found in the header of the request

            if (appSource != "null" && appSource != "\"\"")
            {
                jobject["AppSource"] = appSource;
            }

            if (authUser != "null")
            {
                jobject["AuthUser"] = authUser;
            }

            var serviceAccount = logProperties.SingleOrDefault(c => c.Key == "ServiceAccount").Value?.ToString();
            if (serviceAccount != "null" && serviceAccount != "\"\"")
            {
                jobject.Add("ServiceAccount", serviceAccount);
            }


            var json = jobject.ToString(Formatting.None);

            var uri = new Uri(_logHttpClient.HttpClient.BaseAddress, "logs");
            var message = new StringContent(json, Encoding.UTF8, "application/json");
            //_logHttpClient.HttpClient.PostAsync(uri, message).GetAwaiter().GetResult();
            //Fire and forget
            //_httpClient.PostAsync(uri, message).Wait();
            Task.Factory.StartNew(async () => { await _logHttpClient.HttpClient.PostAsync(uri, message); });

        }





        private bool _disposed;


        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
        }


    }
}
