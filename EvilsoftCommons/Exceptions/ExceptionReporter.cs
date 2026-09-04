#if !DEBUG
#define RELEASE
#endif

// Just leave it on for now
//#define RELEASE


using log4net;
using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text;

namespace EvilsoftCommons.Exceptions {

    public class ExceptionReporter {
        static readonly ILog Logger = LogManager.GetLogger(typeof(ExceptionReporter));

        public static string? Uuid { protected get; set; }

        public static string? UrlStats {
            get;
            set;
        }


        public static void EnableLogUnhandledOnThread() {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(MyHandler);
        }

        private static void MyHandler(object sender, UnhandledExceptionEventArgs args) {
            Exception e = (Exception)args.ExceptionObject;
            Logger.Fatal(e.Message);
            Logger.Fatal(e.StackTrace);
        }


        public static void ReportUsage() {
#if !DEBUG
            try {
                var versionString = VersionString;
                string postData = string.Format("version={0}&uuid={1}", Uri.EscapeDataString(versionString), Uuid);
                HttpWebRequest httpWReq = (HttpWebRequest) WebRequest.Create(UrlStats);
                Encoding encoding = new UTF8Encoding();
                byte[] data = encoding.GetBytes(postData);

                httpWReq.Method = "POST";
                httpWReq.ContentType = "application/x-www-form-urlencoded";
                httpWReq.ContentLength = data.Length;

                using (Stream stream = httpWReq.GetRequestStream()) {
                    stream.Write(data, 0, data.Length);
                }

                // threshold
                using (HttpWebResponse response = (HttpWebResponse) httpWReq.GetResponse()) {
                    if (response.StatusCode != HttpStatusCode.OK) {
                        Logger.Warn("Failed to send anonymous usage statistics to developer.");
                        return;
                    }

                    string responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

                    Logger.Info("Sent anonymous usage statistics to developer.");
                }
            }
            catch (Exception ex) {
                Logger.Fatal(ex.Message);
                Logger.Fatal(ex.StackTrace);
            }
#endif
        }

        public static string VersionString {
            get {
                try {
                    return Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "1.1.2.2";
                }
                catch (Exception ex) {
                    Logger.Warn("Error getting assembly version, automatic updates may not function correctly.");
                    Logger.Warn(ex.Message);
                    Logger.Warn(ex.StackTrace);
                    return Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "1.1.2.2";
                }
            }
        }

        public static DateTime BuildDate {
            get {
                try {
                    var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
                    var metadata = assembly .GetCustomAttributes<AssemblyMetadataAttribute>() .FirstOrDefault(x => x.Key == "BuildDateTimeUtc");
                    if (metadata?.Value == null)
                        throw new InvalidOperationException("BuildDateTimeUtc assembly metadata is missing.");
                    return new DateTime(2000, 1, 1).AddDays(int.Parse(metadata.Value));
                }
                catch (Exception ex) {
                    Logger.Warn("Error getting assembly build date.");
                    Logger.Warn(ex.Message);
                    Logger.Warn(ex.StackTrace);
                    return DateTime.MinValue;
                }
            }
        }
    }
}
