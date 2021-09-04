using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UNC.Extensions.General
{
    public static class JsonExtensions
    {

        public static bool IsJson(this string value)
        {
            try
            {
                if (value.IsEmptyOrNull()) return false;
                value = value.Trim();
                //Must start and stop with {} or []
                if (!(value.StartsWith("{") && value.EndsWith("}") ||
                      value.StartsWith("[") && value.EndsWith("]")))
                {
                    return false;
                }

                JToken.Parse(value);
                return true;

            }
            catch (JsonReaderException jex)
            {
                Console.WriteLine(jex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return false;
        }


        /// <summary>
        /// Read json from specified fullPath to designated Type T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fullPath"></param>
        /// <returns></returns>
        public static T FromJsonFile<T>(this string fullPath)
        {
            var serializedValue = System.IO.File.ReadAllText(fullPath);
            return JsonConvert.DeserializeObject<T>(serializedValue);

        }


        public static string ToJson<T>(this T value) where T : class
        {
            //we have to use Newtonsoft here because JsonSerializer doesn't support reference loop handling
            if (value == null)
            {
                return string.Empty;
            }
            return JsonConvert.SerializeObject(value, Formatting.None, new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects
            });
        }


        /// <summary>
        /// Take object, serialize to JSON, save to specified fullPath
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="fullPath"></param>
        public static void ToJsonFile<T>(this T value, string fullPath)
        {
            //we have to use Newtonsoft here because JsonSerializer doesn't support reference loop handling
            var serializedValue = JsonConvert.SerializeObject(value, Formatting.None, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore, PreserveReferencesHandling = PreserveReferencesHandling.Objects });

            System.IO.File.WriteAllText(fullPath, serializedValue);

        }

        public static T FromJson<T>(this string value) where T : class

        {
            var response = JsonConvert.DeserializeObject<T>(value);
            return response;
        }

    }
}
