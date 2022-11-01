using KAVRAMOBS.ExtensionMethods;
using System;
using System.Net;
using System.Web;
using KAVRAMOBS.Properties;
using System.Text.Json.Nodes;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Reflection;

namespace KAVRAMOBS
{
    public static class Config
    {
        public static readonly string ConfigFilePath = Path.Combine(Path.GetDirectoryName(AppContext.BaseDirectory)!, "config.json");

        public static JsonArray DeviceTokens = new JsonArray();
        public static string Username = "";
        public static string Password = "";
        public static int RefreshInterval = 3600;

        public static void InitConfig()
        {
            var configJson = new JsonObject();

            var jsonArray = new JsonArray();
            var stringCurrent = "";


            Console.WriteLine("Get device token from \"https://test-6d179.web.app/\", don't forget to allow notifications!");
            Console.WriteLine("You can add multiple device tokens, so you can add your mobile device and PC simultaneously.");
            Console.WriteLine("QR-Code for mobile devices: \n");
            Console.WriteLine(Resources.qrCode);
            Console.WriteLine("");

            Thread.Sleep(3000);

            Process.Start(new ProcessStartInfo("https://test-6d179.web.app/") { UseShellExecute = true });

            do
            {
                Console.Write("Device token(s) [keep blank for exit]: ");

                stringCurrent = Console.ReadLine();

                if (stringCurrent?.Length <= 0) break;

                jsonArray.Add(stringCurrent);
            } while (true);

            configJson["device_tokens"] = jsonArray;
            Console.Write("Username: ");
            configJson["username"] = Console.ReadLine();
            Console.Write("Password: ");
            configJson["password"] = Console.ReadLine();

            File.WriteAllText(ConfigFilePath, configJson.ToJsonString());

            DeviceTokens = jsonArray;

            Program.Notify("Kurulum tamamlandı!", "Artık mail bildirimlerini alabileceksin!");

            LoadConfig();
        }

        public static void LoadConfig()
        {
            if (!File.Exists(ConfigFilePath))
                InitConfig();

            var configJson = JsonNode.Parse(File.ReadAllText(ConfigFilePath));
            DeviceTokens = configJson!["device_tokens"]!.AsArray();
            Username = configJson!["username"]!.GetValue<string>();
            Password = configJson!["password"]!.GetValue<string>();
        }
    }

    public static class Program
    {
        static CookieContainer container = new CookieContainer();
        static string passwordEncrypted = "";
        public static string GeneratePassword(string pass)
        {
            var engine = new ScriptEngine("jscript");
            var ret = (string)engine.Parse(Resources.script).CallMethod("strnew", pass);
            engine.Dispose();
            return ret;
        }

        public static int GetCaptchaResult()
        {
            var FirstDigit = new Dictionary<long, int>
            {
                { 11234, 10 },
                { 19264, 20 },
                { 20584, 30 },
                { 9034 , 40 },
                { 19486, 50 },
                { 28726, 60 },
                { 29716, 70 },
                { 24018, 80 },
                { 50064, 90 },
                { 31500, 100 }
            };

            var SecondDigit = new Dictionary<long, int>
            {
                { 1823 , 0 },
                { 13462, 1 },
                { 15636, 2 },
                { 13984, 3 },
                { 35764, 4 },
                { 16956, 5 },
                { 18386, 6 },
                { 17066, 7 },
                { 23334, 8 },
                { 22564, 9 }
            };

            Http.MakeRequest(Resources.captchaUrl, "", "GET", "text/html; charset=utf-8", "", container);
            var a = Http.MakeRequest($"{Resources.captchaSoundUrl}a.aspx", "", "GET", "text/html; charset=utf-8", "", container);
            var b = Http.MakeRequest($"{Resources.captchaSoundUrl}b.aspx", "", "GET", "text/html; charset=utf-8", "", container);

            var result = FirstDigit[a.ContentLength] + SecondDigit[b.ContentLength];
            return result;
        }

        static void Login(string username, string password)
        {
            Http.MakeRequest(Resources.loginUrl, "", "GET", "text/html; charset=utf-8", "", container);

            if (passwordEncrypted.Length <= 0)
                passwordEncrypted = GeneratePassword(password);

            var captcha = GetCaptchaResult();

            var encodedString =
                $"__LASTFOCUS=" +
                $"&__EVENTTARGET=btnLogin" +
                $"&__EVENTARGUMENT=" +
                $"&__VIEWSTATE=" +
                $"&__VIEWSTATEGENERATOR=7D22E5E0" +
                $"&__SCROLLPOSITIONX=0" +
                $"&__SCROLLPOSITIONY=0" +
                $"&txtParamT01={username}" +
                $"&txtParamT02=" +
                $"&txtSecCode={captcha}" +
                $"&txtParamT1={HttpUtility.UrlEncode(passwordEncrypted)}";

            var result = Http.MakeRequest(Resources.loginUrl, encodedString, "POST", "application/x-www-form-urlencoded", "", container);

            if (result.ResponseUri.ToString().Contains("login.aspx"))
                throw new Exception("Password or username was wrong.");
        }
        public static bool Notify(string title, string body)
        {
            try
            {
                var serverKey = string.Format("key={0}", Resources.firebase_key);
                var senderId = string.Format("id={0}", Resources.firebase_sender_id);

                var jsonBody = new JsonObject
                {
                    {"registration_ids", JsonArray.Parse(Config.DeviceTokens.ToJsonString())},
                    {"data", new JsonObject {
                        {"title", title },
                        {"body", body},
                        {"data", new JsonObject { { "url", Resources.loginUrl } } },
                    }},
                };

                using (var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://fcm.googleapis.com/fcm/send"))
                {
                    httpRequest.Headers.TryAddWithoutValidation("Authorization", serverKey);
                    httpRequest.Headers.TryAddWithoutValidation("Sender", senderId);
                    httpRequest.Content = new StringContent(jsonBody.ToJsonString(), Encoding.UTF8, "application/json");

                    using (var httpClient = new HttpClient())
                    {
                        var result = httpClient.Send(httpRequest);
                        return result.IsSuccessStatusCode;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        static void SendNotification(string label, string message)
        {
            if (!Notify(label, message))
                Console.WriteLine("Notification couldn't be sent.");
        }

        static void CheckMails()
        {
            var mail = Http.MakeRequest(Resources.mailUrl, "", "GET", "text/html; charset=utf-8", "", container, null!, null!, Properties.Resources.mailReferrerUrl);
            var mailString = mail.GetResponseAsString();

            if (mailString.Contains("Yeni Mesaj Yok"))
            {
                Console.WriteLine("Yeni mesaj bulunamadı.");
            }
            else
            {
                SendNotification("OBS - Mail", "Okuldan yeni mail geldi.");
                Console.WriteLine("Yeni mesaj geldi.");
            }
        }

        static void Logout()
        {
            container = new CookieContainer();
        }

        public static void Main(string[] args)
        {
            try
            {
                Config.LoadConfig();

                while (true)
                {

                    Login(Config.Username, Config.Password);
                    CheckMails();
                    Logout();

                    Thread.Sleep(Config.RefreshInterval * 1000);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw;
            }

        }
    }
}