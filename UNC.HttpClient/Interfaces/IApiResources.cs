using System.Collections.Generic;
using UNC.HttpClient.Models;

namespace UNC.HttpClient.Interfaces
{
    public interface IApiResources
    {
        List<ApiResource> Resources { get; set; }
        string GetAddress(string resourceName);
    }
}