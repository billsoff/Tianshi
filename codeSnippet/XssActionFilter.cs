using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;

using Microsoft.AspNetCore.Mvc.Filters;

namespace CCFlow.NetCore.NetCore.common
{
    public class XssActionFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            foreach (var argument in filterContext.ActionArguments)
            {
                SanitizeObjectProperties(argument.Value);
            }
        }

        private void SanitizeObjectProperties(object obj, List processedObjs = null)
        {
            if (obj == null || obj.GetType().IsValueType)
            {
                return;
            }

            if (processedObjs == null)
            {
                processedObjs = new List();
            }

            if (processedObjs.Contains(obj))
            {
                return;
            }

            processedObjs.Add(obj)

            if (obj is IList)
            {
                foreach (var inputValue in (IList)obj)
                {
                    SanitizeObjectProperties(inputValue, processedObjs);
                }

                return;
            }

            if (obj is IDictionary)
            {
                foreach (DictionaryEntry entry in (IDictionary)obj)
                {
                    SanitizeObjectProperties(entry.Value, processedObjs);
                }

                return;
            }

            var properties = obj
                .GetType()
                .GetProperties()
                .Where(p => p.CanRead);

            foreach (var property in properties)
            {
                if (property.PropertyType == typeof(string))
                {
                    if (!property.CanWrite || property.SetMethod.GetParameters().Length != 1)
                    {
                        continue;
                    }

                    string propertyValue = (string)property.GetValue(obj);

                    if (string.IsNullOrEmpty(propertyValue))
                    {
                        continue;
                    }

                    property.SetValue(obj, WebUtility.HtmlEncode(propertyValue));

                    continue;
                }

                SanitizeObjectProperties(property.GetValue(obj), processedObjs);
            }
        }
    }
}
