using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using UNC.Services.Attributes;
using UNC.API.Base.Infrastructure;
using UNC.Extensions.General;
//https://discoverdot.net/projects/swashbuckle-aspnetcore#operation-filters
namespace UNC.API.Base.Filters
{
    /// <summary>
    /// Swashbuckle generates a Swagger-flavored JSONSchema for every parameter, response and property type that's exposed by your controller actions.
    /// Once generated, it passes the schema and type through the list of configured Schema Filters.
    /// 
    /// For PUT/POST/PATCH (Requests containing a body)
    /// Properties with Exclude attribute will be removed from the body
    /// </summary>
    public class SwaggerExcludeSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (schema?.Properties == null || context.Type == null)
                return;
            
            var excludedProperties = context.Type.GetProperties()
                .Where(t => t.GetCustomAttributes().Any(r => r.GetType() == typeof(SwaggerExcludeAttribute)));
            
            foreach (var excludedProperty in excludedProperties)
            {
                var propertyToRemove = schema.Properties.FirstOrDefault(c => c.Key.EqualsIgnoreCase(excludedProperty.Name));
                if (propertyToRemove.Key.HasValue())
                {
                    schema.Properties.Remove(propertyToRemove);
                }

            }
        }
    }
    
    public class SwaggerExcludeParameterFilter : IParameterFilter
    {
        public void Apply(OpenApiParameter parameter, ParameterFilterContext context)
        {
            var type = Nullable.GetUnderlyingType(context.ApiParameterDescription.Type) ?? context.ApiParameterDescription.Type;
            if (type.IsEnum)
            {
                
                AddEnumParamSpec(parameter, type, context);
                parameter.Required = type == context.ApiParameterDescription.Type;
            }
            else if (type.IsArray || (type.IsGenericType && type.GetInterfaces().Contains(typeof(IEnumerable))))
            {
                var itemType = type.GetElementType() ?? type.GenericTypeArguments.First();
                AddEnumSpec(parameter, itemType, context);
            }
        }
        private static void AddEnumSpec(OpenApiParameter parameter, Type type, ParameterFilterContext context)
        {
            var schema = context.SchemaRepository.Schemas.GetOrAdd($"#/definitions/{type.Name}", () =>
                context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository)
            );

            if (schema.Reference == null || !type.IsEnum)
            {
                return;
            }

            parameter.Schema = schema;

            var enumNames = new OpenApiArray();
            enumNames.AddRange(Enum.GetNames(type).Select(_ => new OpenApiString(_)));
            schema.Extensions.Add("x-enumNames", enumNames);
        }

        private static void AddEnumParamSpec(OpenApiParameter parameter, Type type, ParameterFilterContext context)
        {
            var schema = context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository);
            if (schema.Reference == null)
            {
                return;
            }

            parameter.Schema = schema;

            var enumNames = new OpenApiArray();
            enumNames.AddRange(Enum.GetNames(type).Select(_ => new OpenApiString(_)));
            schema.Extensions.Add("x-enumNames", enumNames);
            
          
        }
    }
    //public class SwaggerExcludeSchemaFilter : ISchemaFilter, IParameterFilter, IOperationFilter
    /// <summary>
    /// Swashbuckle retrieves an ApiDescription, part of ASP.NET Core, for every action and uses it to generate a corresponding OpenApiOperation.
    /// Once generated, it passes the OpenApiOperation and the ApiDescription through the list of configured Operation Filters.
    /// 
    /// Remove parameters from criteria given as a query parameter,
    /// will not remove entries from body
    /// </summary>
    public class SwaggerExcludeOperationFilter : IOperationFilter
    {
        public static HashSet<OpenApiParameter> IgnoreParams = new HashSet<OpenApiParameter>();

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (context.MethodInfo is null) return;

            var parameters = context.MethodInfo.GetParameters();

            var properties = parameters
                .SelectMany(c => c.ParameterType.GetProperties()
                    .Where(d => d.GetCustomAttribute<SwaggerExcludeAttribute>() != null))
                    .ToList();
            var toRemove = new List<OpenApiParameter>();


            var remove = operation.Parameters.Where(c => properties.Any(d => d.Name.Equals(c.Name))).ToList();

            foreach (var openApiParameter in remove)
            {
                if (!IgnoreParams.Contains(openApiParameter))
                {
                    toRemove.Add(openApiParameter);
                }
            }


            if (toRemove.Any())
            {
                foreach (var parameter in toRemove)
                {
                    operation.Parameters.Remove(parameter);
                }
            }

            IgnoreParams.Clear();
        }
    }
}
