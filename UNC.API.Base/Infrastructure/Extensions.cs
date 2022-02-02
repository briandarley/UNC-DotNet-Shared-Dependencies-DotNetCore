using System;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using UNC.Extensions.General;
using UNC.Services.Interfaces.Response;
using UNC.Services.Responses;

namespace UNC.API.Base.Infrastructure
{
    public static class Extensions
    {

       



        public static ActionResult ToActionResult(this IResponse value)
        {

            //convert to json to avoid serialization issues fom the raw object
            var raw = value.ToJson();
            if (value is IErrorResponse err)
            {
                if (value is NotFoundResponse)
                {
                    return new NotFoundResult();
                }
                if (value is IExceptionResponse exceptionResponse)
                {
                    var message = exceptionResponse.Message;
                    
                    if (message.IsEmpty() || exceptionResponse.Exception != null)
                    {
                        message = exceptionResponse.Exception.Message;
                    }
                    
                    return new BadRequestObjectResult(message);
                    //return new  BadRequestObjectResult("Unexpected error occurred");
                }
                return new BadRequestObjectResult(err);
            }


            if (value is IEntityResponse)
            {

                var entity = JObject.Parse(raw).GetValue("Entity");
                return new JsonResult(entity);
            }

            if (value is ICollectionResponse)
            {
                var entities = JObject.Parse(raw).GetValue("Entities");

                return new JsonResult(entities);
            }



            if (value is SuccessResponse successResponse)
            {
                if (successResponse.Message.IsEmpty())
                {
                    return new OkResult();
                }

            }
            return new OkObjectResult(value);

        }
        public static string UserId(this ClaimsPrincipal user)
        {
            if (!user.Claims.Any()) return "Anonymous";
            var id = user.Claims.First(c => c.Type == "sub").Value;
            return id;
        }

        public static void RegisterSwaggerStartup(this IServiceCollection services)
        {
            services.AddSwaggerGen();

        }

        public static void RegisterApiVersioning(this IServiceCollection services)
        {
            //TODO read more about this
            //API versioning service 
            services.AddApiVersioning(
                o =>
                {
                    //o.Conventions.Controller<UserController>().HasApiVersion(1, 0);
                    o.AssumeDefaultVersionWhenUnspecified = true;
                    o.ReportApiVersions = true;
                    o.DefaultApiVersion = new ApiVersion(1, 0);
                    o.ApiVersionReader = new UrlSegmentApiVersionReader();
                }
            );
            // format code as "'v'major[.minor][-status]"
            services.AddVersionedApiExplorer(
                options =>
                {
                    options.GroupNameFormat = "'v'VVV";
                    //versioning by url segment
                    options.SubstituteApiVersionInUrl = true;
                });

        }

        public static void RegisterSqlDbContext<T>(this IServiceCollection services, string connectionString) where T : DbContext
        {
            services.AddDbContext<T>(options => options.UseSqlServer(connectionString, opts => opts.EnableRetryOnFailure()));
        }

        public static void RegisterInMemoryDbContext<T>(this IServiceCollection services, string connectionString) where T : DbContext
        {
            services.AddDbContext<T>(options => options.UseInMemoryDatabase(connectionString));
        }
        /// <summary>
        /// Allows for client to API communication using IdentityServer 'OpenId'
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        public static void RegisterIdentityServerAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            if (configuration.GetSection("IdentityServer") is null)
            {
                throw new Exception("RegisterIdentityServerAuthentication requires configuration section 'IdentityServer'");
            }
            //Authentication:IdentityServer4 - full version
            //JWT API authentication service
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddIdentityServerAuthentication(options =>
                {
                    options.Authority = configuration["IdentityServer:Authority"];
                    options.RequireHttpsMetadata = bool.Parse(configuration["IdentityServer:RequireHttpsMetadata"]);
                    options.ApiName = configuration["IdentityServer:ApiName"];

                });
            //.AddJwtBearer(options =>
            //    {
            //        options.TokenValidationParameters = new TokenValidationParameters
            //        {
            //            ValidateIssuer = true,
            //            ValidateAudience = true,
            //            ValidateLifetime = true,
            //            ValidateIssuerSigningKey = true,
            //            ValidIssuer = configuration["IdentityServer:Issuer"],
            //            ValidAudience = configuration["IdentityServer:Issuer"],
            //            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["IdentityServer:Key"]))
            //        };
            //    }
            //);
        }
        public static void RegisterIdentityServerJwtBearer(this IServiceCollection services, IConfiguration configuration)
        {
            if (configuration.GetSection("IdentityServer") is null)
            {
                throw new Exception("RegisterIdentityServerJwtBearer requires configuration section 'IdentityServer'");
            }
            //Authentication:IdentityServer4 - full version
            //JWT API authentication service
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                    {
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            ValidIssuer = configuration["IdentityServer:Issuer"],
                            ValidAudience = configuration["IdentityServer:Issuer"],
                            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["IdentityServer:Key"]))
                        };
                    }
                );

        }
        /// <summary>
        /// Register CORS for
        /// localhost, its-idmtst-web, its-idmuat-web, selfservice
        /// </summary>
        /// <param name="services"></param>
        public static void RegisterDefaultCors(this IServiceCollection services)
        {
            services.AddCors(options =>

                options.AddPolicy("CORS", builder =>
                {
                    builder

                        .WithOrigins(
                            "http://localhost",
                            "http://localhost:8080",
                            "http://localhost:8081",
                            "https://its-idmtst-web.adtest.unc.edu",
                            "https://its-idmuat-web.ad.unc.edu",
                            "https://selfservice.unc.edu")
                        .AllowAnyMethod()
                        .AllowCredentials()
                        .AllowAnyHeader();

                }));
        }

        public static void RegisterDefaultMvcSerialization(this IServiceCollection services)
        {
            services.AddMvc(option => option.EnableEndpointRouting = false)
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                });

        }


    }
}
