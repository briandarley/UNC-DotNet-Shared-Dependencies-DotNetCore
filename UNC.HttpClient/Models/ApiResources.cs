using System;
using System.Collections.Generic;
using System.Linq;
using UNC.HttpClient.Interfaces;

namespace UNC.HttpClient.Models
{
    public class ApiResources : IApiResources
    {
        public List<ApiResource> Resources { get; set; }

        public ApiResources()
        {
            Resources = new List<ApiResource>();
        }

        public string GetAddress(string resourceName)
        {
            if (Resources is null) return string.Empty;

            return Resources.SingleOrDefault(c => string.Equals(c.Name, resourceName, StringComparison.CurrentCultureIgnoreCase))?.Address;
        
        }

        


    }
}