using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UNC.Extensions.General
{
    public static class CriteriaExtensions
    {
        public static string ToQueryParams<T>(this T value) where T : class
        {
            var json = JsonConvert.SerializeObject(value);
            var dictionary = JsonConvert.DeserializeObject<Dictionary<string, Object>>(json);
            var sb = new StringBuilder();
            foreach (var entry in dictionary
                .Where(c => c.Value != null)
                .Where(c => !(c.Value is JArray))
                .Where(c => c.Value.ToString().Length > 0))
            {
                sb.Append($"{entry.Key}={System.Web.HttpUtility.UrlEncode(entry.Value.ToString())}&");
            }

            foreach (var entry in dictionary.Where(c => c.Value is JArray))
            {
                var list = (JArray)entry.Value;
                foreach (var jToken in list)
                {
                    var item = (JValue)jToken;
                    sb.Append($"{entry.Key}={System.Web.HttpUtility.UrlEncode(item.Value.ToString())}&");
                }
            }


            return Regex.Replace(sb.ToString(), "\\&$", "");
        }
    }
}
