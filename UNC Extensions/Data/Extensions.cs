using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace UNC.Extensions.Data
{
    public static class Extensions
    {
        /// <summary>
        /// Supports comma delimited list of columns to sort on. Prefix the secondary sort with (+) ascending, or (-) descending
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static IOrderedQueryable<T> OrderBy<T>(this IQueryable<T> source, string propertyName)
        {
            if (!propertyName.Contains(","))
            {
                return source.OrderBy(ToLambda<T>(propertyName));
            }

            var properties = propertyName.Split(",").ToList();
            IOrderedQueryable<T> order = null;

            foreach (var property in properties)
            {
                if (order is null)
                {
                    if (property.StartsWith("-") || property.StartsWith("+"))
                    {
                        order = source.OrderBy(ToLambda<T>(property.Trim()[1..]));
                        
                    }
                    else
                    {
                        order = source.OrderBy(ToLambda<T>(property.Trim()));
                    }
                }
                else
                {
                    if (property.StartsWith("+"))
                    {
                        order = order.ThenBy(ToLambda<T>(property.Trim()[1..]));
                    }
                    else if (property.StartsWith("-"))
                    {
                        order = order.ThenByDescending(ToLambda<T>(property.Trim()[1..]));
                    }
                    else
                    {
                        order = order.ThenBy(ToLambda<T>(property.Trim()));
                    }
                }
                
            }

            return order;


        }
        /// <summary>
        /// Supports comma delimited list of columns to sort on. Prefix the secondary sort with (+) ascending, or (-) descending
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static IOrderedQueryable<T> OrderByDescending<T>(this IQueryable<T> source, string propertyName)
        {
            if (!propertyName.Contains(","))
            {
                return source.OrderByDescending(ToLambda<T>(propertyName));
            }

            var properties = propertyName.Split(",").ToList();
            IOrderedQueryable<T> order = null;

            foreach (var property in properties)
            {
                if (order is null)
                {
                    if (property.StartsWith("-") || property.StartsWith("+"))
                    {
                        order = source.OrderByDescending(ToLambda<T>(property.Trim()[1..]));

                    }
                    else
                    {
                        order = source.OrderByDescending(ToLambda<T>(property.Trim()));
                    }
                }
                else
                {
                    if (property.StartsWith("+"))
                    {
                        order = order.ThenBy(ToLambda<T>(property.Trim()[1..]));
                    }
                    else if (property.StartsWith("-"))
                    {
                        order = order.ThenByDescending(ToLambda<T>(property.Trim()[1..]));
                    }
                    else
                    {
                        order = order.ThenBy(ToLambda<T>(property.Trim()));
                    }
                }
                
            }

            return order;

        }

        public static IOrderedQueryable<T> ThenBy<T>(this IOrderedQueryable<T> source, string propertyName)
        {
            return source.ThenBy(ToLambda<T>(propertyName));
        }

        public static IOrderedQueryable<T> ThenByDescending<T>(this IOrderedQueryable<T> source, string propertyName)
        {
            return source.ThenByDescending(ToLambda<T>(propertyName));
        }

        private static Expression<Func<T, object>> ToLambda<T>(string propertyName)
        {
            var parameter = Expression.Parameter(typeof(T));
            var property = Expression.Property(parameter, propertyName);
            var propAsObject = Expression.Convert(property, typeof(object));

            return Expression.Lambda<Func<T, object>>(propAsObject, parameter);
        }

        public static async Task TruncateTable<T>(this DbContext dbContext) where T : class
        {
            var annotations = dbContext.Model.FindEntityType(typeof(T)).GetAnnotations().ToList();

            
            var tableName = annotations.First(c => c.Name == "Relational:TableName").Value.ToString();
            var schemaAnnotation = annotations.FirstOrDefault(c => c.Name == "Relational:Schema")?.Value;
            var schema = "dbo";
            if (schemaAnnotation != null)
            {
                schema = schemaAnnotation.ToString();
            }
            
            await dbContext.Database.ExecuteSqlRawAsync($"truncate table {schema}.{tableName}");

        }


    }
}
