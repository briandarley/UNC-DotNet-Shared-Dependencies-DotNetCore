using System;

namespace UNC.Services.Attributes
{
    /// <summary>
    /// Use this attribute to remove parameter from Swagger Docs
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class SwaggerExcludeAttribute : Attribute
    {
    }
}
