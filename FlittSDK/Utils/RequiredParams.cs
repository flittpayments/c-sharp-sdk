using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using FlittSDK.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Formatting = Newtonsoft.Json.Formatting;

namespace FlittSDK.Utils
{
#pragma warning disable CS0618 // Legacy Config/XML overloads are retained for binary compatibility.
    /// <summary>
    /// Class to getting params
    /// </summary>
    internal static class RequiredParams
    {
        /// <summary>
        /// Convert Response By Content Type
        /// </summary>
        /// <param name="response"></param>
        /// <param name="isRoot"></param>
        /// <param name="type"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T ConvertResponseByContentType<T>(string response, bool isRoot, string type = null)
        {
            T data;
            if (type == null)
            {
                type = LegacyConfigClientFactory.GetContentType();
            }

            switch (type)
            {
                case "xml":
                    data = XmlFormatter.ConvertFromXml<T>(response);
                    break;
                case "form":
                    data = QueryParameters.ConvertFromQuery<T>(response);
                    break;
                default:
                    data = JsonFormatter.ConvertFromJson<T>(response, isRoot);
                    break;
            }

            return data;
        }

        /// <summary>
        /// Get V2 params
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="isCredit"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [Obsolete("This overload reads legacy static credentials. Use the overload with secretKey.")]
        public static string GetParamsV2<T>(T obj, bool isCredit)
        {
            return GetParamsV2(obj, isCredit, LegacyConfigClientFactory.GetSecretKey(isCredit));
        }

        public static string GetParamsV2<T>(T obj, bool isCredit, string secretKey)
        {
            RequestV2 data = new RequestV2();
            var order = JObject.FromObject(obj);
            order.Property("signature")?.Remove();
            order.Property("version")?.Remove();
            var payload = new JObject(new JProperty("order", order)).ToString(Formatting.None);
            data.data = Signature.Base64Encode(payload);
            data.signature = Signature.GetRequestSignatureV2(data.data, isCredit, secretKey);
            data.version = "2.0";
            return JsonFormatter.ConvertToJson(data);
        }

        /// <summary>
        /// Convert Request By Content Type
        /// </summary>
        /// <param name="obj"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static string ConvertRequestByContentType<T>(T obj)
        {
            return ConvertRequestByContentType(obj, LegacyConfigClientFactory.GetContentType());
        }

        public static string ConvertRequestByContentType<T>(T obj, string contentType)
        {
            string data;
            switch (contentType ?? LegacyConfigClientFactory.GetContentType())
            {
                case "xml":
                    data = XmlFormatter.ConvertToXml(obj);
                    break;
                case "form":
                    data = QueryParameters.ConvertToQuery(obj);
                    break;
                default:
                    data = JsonFormatter.ConvertToJson(obj);
                    break;
            }

            return data;
        }

        /// <summary>
        /// Get Signature params
        /// </summary>
        /// <param name="postObj"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetHashProperties(object postObj)
        {
            return GetHashProperties(postObj, LegacyConfigClientFactory.GetContentType());
        }

        public static IEnumerable<string> GetHashProperties(object postObj, string contentType)
        {
            Type tModelType = postObj.GetType();
            PropertyInfo[] arrayProperty = tModelType.GetProperties();
            var hashKeys = arrayProperty
                .Where(o => o.Name != "signature" &&
                            o.Name != "response_signature_string" &&
                            o.GetValue(postObj) != null &&
                            o.GetValue(postObj).ToString() != ""
                )
                .OrderBy(o => o.Name)
                .ToList()
                .Select(o =>
                    o.GetGetMethod().Invoke(postObj, null).GetType() != typeof(string) &&
                    o.GetGetMethod().Invoke(postObj, null).GetType() != typeof(int)
                        ? SetAsString(o.GetGetMethod().Invoke(postObj, null), contentType)
                        : o.GetGetMethod().Invoke(postObj, null).ToString());

            return hashKeys;
        }

        /// <summary>
        /// Setting parameter as content string
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private static string SetAsString(object obj, string contentType = null)
        {
            string data;
            switch (contentType ?? LegacyConfigClientFactory.GetContentType())
            {
                case "xml":
                    data = XmlFormatter.ConvertToXmlSimple(obj);
                    break;
                case "form":
                    data = QueryParameters.ConvertToQuerySimple(obj);
                    break;
                default:
                    data = JsonConvert.SerializeObject(obj, Formatting.None);
                    break;
            }

            return data;
        }
    }
#pragma warning restore CS0618
}
