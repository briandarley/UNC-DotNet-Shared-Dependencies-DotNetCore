using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

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


        public static bool JsonContainsProperty(this string json, string property)
        {
            try
            {
                if (!json.IsJson()) return false;

                var token = JObject.Parse(json);

                var entity = token.GetValue(property, StringComparison.OrdinalIgnoreCase)?.Value<object>();

                return entity != null;

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

        public static T JsonPropertyValue<T>(this string json, string property)
        {
            if (!json.IsJson())
            {
                throw new ArgumentException("json is not properly formatted");
            }

            var token = JObject.Parse(json);
            JToken tokenValue = token.GetValue(property, StringComparison.OrdinalIgnoreCase);
            if (tokenValue is null)
            {
                return default;
            }
            var entity = tokenValue.Value<T>();

            return entity;

        }

        public static string JsonAppendProperty(this string json, string property, string value)
        {
            if (!json.IsJson())
            {
                throw new ArgumentException("json is not properly formatted");
            };

            var token = JObject.Parse(json);
            token.Add(property, value);

            return token.ToString(Newtonsoft.Json.Formatting.None);

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
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
        });
        }

        private class InterfaceContractResolver : DefaultContractResolver
        {
            private readonly Type _InterfaceType;
            public InterfaceContractResolver(Type InterfaceType)
            {
                _InterfaceType = InterfaceType;
            }

            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                //IList<JsonProperty> properties = base.CreateProperties(type, memberSerialization);
                IList<JsonProperty> properties = base.CreateProperties(_InterfaceType, memberSerialization);
                return properties;
            }
        }

        public static string ToJson<T>(this T value, bool shallow) where T : class
        {
            //we have to use Newtonsoft here because JsonSerializer doesn't support reference loop handling
            if (value == null)
            {
                return string.Empty;
            }

            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects
            };
            if (shallow)
            {
                settings.ContractResolver = new InterfaceContractResolver(typeof(T));
            }
            return JsonConvert.SerializeObject(value, Formatting.None, settings);
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
