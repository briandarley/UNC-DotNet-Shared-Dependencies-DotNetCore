using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Security.Principal;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using UNC.LogHandler.Extensions;
using UNC.LogHandler.Models;
using UNC.Services.Configurations;
using UNC.Services.Interfaces;
using UNC.Services.Interfaces.Response;
using UNC.Services.Models;

namespace UNC.Services.Infrastructure
{
    [Flags]
    public enum LogTypes
    {
        ConsoleLogging = 1,
        RemoteApiLogging = 2,
        FileLogging = 4
    }


    public static class Extensions
    {
        public static void RegisterLogHttpClient(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton(cfg =>
            {
                var loggingEndPoint = configuration.GetSection("Endpoints").GetChildren().SingleOrDefault(c => c.Key == "LOGGING")?.Value;
                var application = configuration.GetSection("Application").Value;
                var result = new LogSetting { EndPoint = loggingEndPoint, Application = application };
                return result;

            });
            services.AddTransient(cfg =>
            {
                var logSetting = cfg.GetService<LogSetting>();


                HttpClient GetClient()
                {
                    var client = new System.Net.Http.HttpClient
                    {
                        Timeout = TimeSpan.FromHours(1),
                        BaseAddress = new Uri(logSetting.EndPoint),
                    };

                    if (!string.IsNullOrEmpty(logSetting.Application))
                    {
                        client.DefaultRequestHeaders.Add(Constants.RequestHeaders.APPLICATION_NAME, logSetting.Application);
                    }


                    var principal = cfg.GetRequiredService<IPrincipal>();
                    if (principal != null && principal.Identity.IsAuthenticated)
                    {
                        client.DefaultRequestHeaders.Add(Constants.RequestHeaders.AUTH_USER, principal.Identity.Name);
                    }


                    return client;
                }



                var logHttpClient = new LogHttpClient(GetClient());
                return logHttpClient;

            });
        }

        /// <summary>
        /// Load any 'Endpoints' configuration from configuration files
        /// Registers <see cref="IApiEndPoints"/> with many <see cref="EndPointAddress"/>
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        public static void RegisterApiEndPoints(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IApiEndPoints>(cfg =>
            {
                var resourceEndpoints = configuration
                    .GetSection("Endpoints")
                    .GetChildren()
                    .Select(c => new EndPointAddress { Address = c.Value, Name = c.Key })
                    .ToList();

                var response = new ApiEndPoints(resourceEndpoints);

                return response;

            });

        }

        /// <summary>
        /// Registers custom SeriLog Sink <see cref="LogApiSink"/>
        /// Attempts to load from DI
        /// <see cref="IPrincipal"/>,
        /// <see cref="IHttpContextAccessor"/> (To retrieve AppSource, ServiceAccount from headers)
        /// So that LogAPI can register events
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <param name="logType"></param>
        public static void RegisterCustomLogging(this IServiceCollection services, IConfiguration configuration, LogTypes logType)
        {
            services.AddSingleton(cfg =>
            {
                //AuthUser => Principal?.Identity?.Name

                var appName = configuration.GetValue<string>("Application");

                var filePath = configuration.GetValue<string>("Serilog:LogFilePath");
                filePath = Path.Combine(filePath, appName);
                filePath = filePath + $@"\{appName}.log";

                var logEventLevel = configuration.GetValue<LogEventLevel>("Serilog:MinimumLevel");
                var outputTemplate = "===> {Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] {Message}{NewLine}{Exception}";

                //string GetPrincipalIdentityName()
                //{
                //    var principal = cfg.GetService<IPrincipal>();
                //    return principal.Identity.Name;
                //}
                IHttpContextAccessor GetHttpContextAccessor()
                {

                    var httpContext = cfg.GetService<IHttpContextAccessor>();
                    return httpContext;
                }

                var httpContext = cfg.GetService<IHttpContextAccessor>();
                string appSource = "", serviceAccount = "";
                if (httpContext != null)
                {
                    if (httpContext.HttpContext is null)
                    {
                        throw new Exception("httpContext.HttpContext is null, are you sure you need to register custom logging?");
                    }
                    appSource = httpContext.HttpContext.Request.Headers["AppSource"].FirstOrDefault();
                    serviceAccount = httpContext.HttpContext.Request.Headers["ServiceAccount"].FirstOrDefault();
                }

                var loggerConfiguration = new LoggerConfiguration()
                    .ReadFrom.Configuration(configuration)
                    .Enrich.WithThreadId()

                    .Enrich.WithProperty("Application", appName)
                    //.Enrich.WithProperty("AuthUser", principal.Identity.Name)
                    .Enrich.WithProperty("AppSource", appSource)
                    .Enrich.WithProperty("ServiceAccount", serviceAccount)
                    .Enrich.FromLogContext();


                if (logType.HasFlag(LogTypes.RemoteApiLogging))
                {
                    var logHttpClient = cfg.GetRequiredService<LogHttpClient>();
                    var principal = cfg.GetRequiredService<IPrincipal>();

                    loggerConfiguration.WriteTo.Conditional(evt => evt.Level >= logEventLevel, wt => wt.LogAppender(principal, logHttpClient, GetHttpContextAccessor));
                }

                if (logType.HasFlag(LogTypes.FileLogging))
                {
                    loggerConfiguration.WriteTo.File(filePath,
                        logEventLevel,
                        fileSizeLimitBytes: 5000000,
                        rollOnFileSizeLimit: true,
                        retainedFileCountLimit: 10,
                        outputTemplate: outputTemplate);
                }

                if (logType.HasFlag(LogTypes.ConsoleLogging))
                {
                    loggerConfiguration.WriteTo.Console(logEventLevel);
                }



                Log.Logger = loggerConfiguration.CreateLogger();

                AppDomain.CurrentDomain.ProcessExit += (s, e) =>
                {
                    Log.CloseAndFlush();

                };

                return Log.Logger;



            });

        }

        public static void RegisterAutoMapper<T>(this IServiceCollection services) where T : class, IAutoMapperService
        {
            services.AddSingleton<IAutoMapperService, T>();


        }




        /// <summary>
        /// Compresses a string and returns a deflate compressed, Base64 encoded string.
        /// </summary>
        /// <param name="uncompressedString">String to compress</param>
        public static string Compress(this string uncompressedString)
        {
            byte[] compressedBytes;

            using (var rawStream = new MemoryStream(Encoding.UTF8.GetBytes(uncompressedString)))
            {
                using (var compressedStream = new MemoryStream())
                {
                    // setting the leaveOpen parameter to true to ensure that compressedStream will not be closed when compressorStream is disposed
                    // this allows compressorStream to close and flush its buffers to compressedStream and guarantees that compressedStream.ToArray() can be called afterward
                    // although MSDN documentation states that ToArray() can be called on a closed MemoryStream, I don't want to rely on that very odd behavior should it ever change
                    using (var compressorStream = new DeflateStream(compressedStream, CompressionLevel.Fastest, true))
                    {
                        rawStream.CopyTo(compressorStream);
                    }

                    // call compressedStream.ToArray() after the enclosing DeflateStream has closed and flushed its buffer to compressedStream
                    compressedBytes = compressedStream.ToArray();
                }
            }

            return Convert.ToBase64String(compressedBytes);
        }

        /// <summary>
        /// Decompresses a deflate compressed, Base64 encoded string and returns an uncompressed string.
        /// </summary>
        /// <param name="compressedString">String to decompress.</param>
        public static string Decompress(this string compressedString)
        {
            byte[] decompressedBytes;

            var compressedStream = new MemoryStream(Convert.FromBase64String(compressedString));

            using (var stream = new DeflateStream(compressedStream, CompressionMode.Decompress))
            {
                using (var decompressedStream = new MemoryStream())
                {
                    stream.CopyTo(decompressedStream);

                    decompressedBytes = decompressedStream.ToArray();
                }
            }

            return Encoding.UTF8.GetString(decompressedBytes);
        }

        public static T ToEntityFromResponse<T>(this IResponse value) where T : IEntity
        {
            if (value is IEntityResponse<T> entityResponse)
            {
                return entityResponse.Entity;
            }
            throw new ArgumentException("Value is not of type IEntityResponse");
        }

        public static T ToTypeFromResponse<T>(this IResponse value)
        {
            if (value is ITypedResponse<T> entityResponse)
            {
                return entityResponse.Entity;
            }
            throw new ArgumentException("Value is not of type ITypedResponse");
        }
    }
}
