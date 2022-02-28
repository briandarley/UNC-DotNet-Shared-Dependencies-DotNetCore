using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;


namespace UNC.Extensions.General
{
    public static class Extensions
    {
        public static bool IsNumeric(this string value)
        {
            if (value is null) return false;
            return Regex.IsMatch(value, "^[0-9]+$");
        }
        public static bool IsEmpty(this string value)
        {
            if (value is null) return true;
            if (string.IsNullOrEmpty(value)) return true;
            return value.Trim() == string.Empty;
        }
        public static bool IsEmpty(this DateTime value)
        {
            return value == DateTime.MaxValue || value == DateTime.MinValue;
        }
        public static bool HasValue(this DateTime value)
        {
            return !value.IsEmpty();
        }
        public static bool HasValue(this string value)
        {
            return !value.IsEmpty();
        }
        public static bool ContainsIgnoreCase(this string value, string to)
        {
            if (value == to) return true;
            if (value.IsNullOrEmpty()) return false;
            if (to.IsNullOrEmpty()) return false;

            return value.Contains(to, StringComparison.CurrentCultureIgnoreCase);
        }
        public static bool EqualsIgnoreCase(this string value, string to)
        {
            if (value == to) return true;
            if (value.IsNullOrEmpty()) return false;
            if (to.IsNullOrEmpty()) return false;


            return StringComparer.OrdinalIgnoreCase.Equals(value, to);
        }
        public static bool EndsWithIgnoreCase(this string value, string to)
        {
            if (value == to) return true;
            if (value.IsNullOrEmpty()) return false;
            if (to.IsNullOrEmpty()) return false;

            return value.EndsWith(to, StringComparison.CurrentCultureIgnoreCase);

        }
        public static bool StartsWithIgnoreCase(this string value, string to)
        {
            if (value == to) return true;
            if (value.IsNullOrEmpty()) return false;
            if (to.IsNullOrEmpty()) return false;

            return value.StartsWith(to, StringComparison.CurrentCultureIgnoreCase);

        }

        public static bool IsEmptyOrNull<T>(this IEnumerable<T> value)
        {
            if (value is null) return true;
            return !value.Any();
        }

        public static bool ToBool(this string value)
        {
            var trueValues = new[] { "Y", "YES", "T", "TRUE", "1" };
            var result = trueValues.Contains(value.ToUpper());
            return result;

        }

        public static bool IsGuid(this string value)
        {
            if (value.IsNullOrEmpty()) return false;
            const string pattern = @"^(\{{0,1}([0-9a-fA-F]){8}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){12}\}{0,1})$";
            return Regex.IsMatch(value, pattern);

        }



        public static bool IsValidProxyAddress(this string value)
        {
            try
            {
                if (value.IsNullOrEmpty()) return false;
                if (!value.StartsWithIgnoreCase("smtp:") && Regex.IsMatch(value, @"^[a-zA-Z0-9]+\:"))
                {
                    //assume it's valid until you learn how to validate these
                    return true;
                }

                return value.Substring(5).IsEmail();

            }
            catch
            {
                return false;
            }
        }



        public static bool IsEmail(this string value)
        {
            try
            {
                if (value.IsNullOrEmpty()) return false;
                var mailAddress = new System.Net.Mail.MailAddress(value);
                return mailAddress.Address == value;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Cleans the phone number
        /// Expected formats +1 (919) 555-1212
        /// (919) 555-1212
        /// 555-1212
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToCleanPhone(this string value)
        {
            if (value.IsNullOrEmpty()) return string.Empty;
            var cleanNumber = Regex.Replace(value, "[^0-9]", "");
            if (cleanNumber.Length == 11)
            {
                return Regex.Replace(cleanNumber.Substring(1), "([0-9]{3})([0-9]{3})([0-9]{4})", "($1)$2-$3");
            }

            if (cleanNumber.Length == 10)
            {
                return Regex.Replace(cleanNumber, "([0-9]{3})([0-9]{3})([0-9]{4})", "($1)$2-$3");
            }

            if (cleanNumber.Length == 7)
            {
                return Regex.Replace(cleanNumber, "([0-9]{3})([0-9]{4})", "($1)$2-$3");
            }

            return cleanNumber;

        }

        public static bool IsDistinguishedName(this string value)
        {
            if (value.IsNullOrEmpty()) return false;
            return Regex.Match(value, "^(?:(?<cn>CN=(?<name>[^,]*)),)?(?:(?<path>(?:(?:CN|OU)=[^,]+,?)+),)?(?<domain>(?:DC=[^,]+,?)+)$").Success;
        }

        public static bool IsSamAccountName(this string value)
        {
            if (value.IsNullOrEmpty()) return false;
            if (value.IsEmail()) return false;
            return Regex.Match(value, @"^[^""\[\]:;\|=\+\*\?<>\/\\. ][^""\[\]:;\|=\+\*\?<>\/\\\n\r\t]{0,40}[^""\[\]:;\|=\+\*\?<>\/\\ \n\r\t]$").Success;
        }

        public static bool IsNullOrEmpty<T>(this IEnumerable<T> value)
        {
            if (value is null) return true;
            return !value.Any();
        }

        public static T Copy<T>(this object item)
        {
            if (item is null)
            {
                return default;
            }

            var formatter = new BinaryFormatter();
            var stream = new MemoryStream();

            formatter.Serialize(stream, item);
            stream.Seek(0, SeekOrigin.Begin);

            var result = (T)formatter.Deserialize(stream);

            stream.Close();

            return result;

        }

        /// <summary>
        /// Returns the description attribute or the string name of the enum whichever is not null
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string EnumToDescription(this Enum value)
        {
            //Sometimes a camelcase notation won't suffice, use the description attribute to handle those cases
            var description = value
                .GetType()
                .GetField(value.ToString())
                .GetCustomAttribute<DescriptionAttribute>();

            if (description is null)
            {
                return value.ToString();
            }

            return description.Description;
        }

        public static string ToEmailFromSmtpAddress(this string value)
        {
            if (value.IsEmail()) return value;

            value = value.Trim();
            if (value.IsEmptyOrNull()) return string.Empty;


            if (!value.StartsWithIgnoreCase("smtp:")) return string.Empty;

            value = value.Substring(5);

            if (value.IsEmail()) return value;

            return string.Empty;

        }

        public static IEnumerable<FieldInfo> GetConstants(this Type type)
        {
            var fieldInfos = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

            return fieldInfos.Where(fi => fi.IsLiteral && !fi.IsInitOnly);
        }

        public static IEnumerable<T> GetConstantsValues<T>(this Type type) where T : class
        {
            var fieldInfos = GetConstants(type);

            return fieldInfos.Select(fi => fi.GetRawConstantValue() as T);
        }

        /// <summary>
        /// Remove accents from names
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string RemoveDiacritics(this string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }


    }
}
