using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Cache;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace KAVRAMOBS
{
    public static class Http
    {
        public static HttpWebResponse MakeRequest(string url, string body, string method, string contentType, string cookies, CookieContainer cookiesContainer = null, Dictionary<string, string> extraHeaders = null, WebProxy proxy = null, string referer = "")
        {
            HttpRequestCachePolicy policy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
            HttpWebRequest.DefaultCachePolicy = policy;
            var request = (HttpWebRequest)WebRequest.Create(url);
            HttpRequestCachePolicy noCachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
            request.CachePolicy = noCachePolicy;
            var data = Encoding.UTF8.GetBytes(body);

            request.Method = method;
            request.ContentType = contentType;
            request.ContentLength = data.Length;
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/105.0.0.0 Safari/537.36";
            request.Headers["sec-ch-ua"] = "\"Google Chrome\";v=\"105\", \"Not)A;Brand\";v=\"8\", \"Chromium\";v=\"105\"";
            request.Headers["sec-ch-ua-mobile"] = "?0";
            request.Headers["sec-ch-ua-platform"] = "?0";
            request.Headers["sec-fetch-dest"] = "empty";
            request.Headers["sec-fetch-mode"] = "cors";
            request.Headers["sec-fetch-site"] = "same-site";
            request.Referer = referer;

            if (proxy != null)
                request.Proxy = proxy;

            if (extraHeaders != null)
            {
                foreach (KeyValuePair<string, string> headers in extraHeaders)
                {
                    request.Headers[headers.Key] = headers.Value;
                }
            }

            if (cookiesContainer == null)
            {
                request.Headers[HttpRequestHeader.Cookie] = cookies;
            }
            else
            {
                request.CookieContainer = cookiesContainer;
            }

            if (method != "GET")
            {
                using (Stream stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }
            }

            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                return response;
            }
            catch (WebException ex)
            {
                return (HttpWebResponse)ex.Response;
            }
        }
    }
}
