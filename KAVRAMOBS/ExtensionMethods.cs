using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace KAVRAMOBS
{
    namespace ExtensionMethods
    {
        public static class HttpWebResponseExtensions
        {
            public static string GetResponseAsString(this HttpWebResponse response)
            {
                if (response == null) return "";

                using (var stream = new StreamReader(response.GetResponseStream()))
                {
                    return stream.ReadToEnd();
                }
            }
        }
        public static class CookieContainerExtensions
        {
            public static List<Cookie> List(this CookieContainer container)
            {
                var cookies = new List<Cookie>();

                var table = (Hashtable)container.GetType().InvokeMember("m_domainTable",
                                                                        BindingFlags.NonPublic |
                                                                        BindingFlags.GetField |
                                                                        BindingFlags.Instance,
                                                                        null,
                                                                        container,
                                                                        new object[] { });

                foreach (object key in table.Keys)
                {
                    if (!(key is string domain))
                    {
                        continue;
                    }

                    if (domain.StartsWith("."))
                    {
                        domain = domain.Substring(1);
                    }

                    var httpAddress = $"http://{domain}/";
                    var httpsAddress = $"https://{domain}/";

                    if (Uri.TryCreate(httpAddress, UriKind.RelativeOrAbsolute, out var httpUri))
                    {
                        cookies.AddRange(container.GetCookies(httpUri).Cast<Cookie>());
                    }

                    if (!Uri.TryCreate(httpsAddress, UriKind.RelativeOrAbsolute, out var httpsUri)) continue;
                    cookies.AddRange(container.GetCookies(httpsUri).Cast<Cookie>());
                }

                return cookies;
            }
        }
    }
}
