using System;
// using System.Collections.Generic;
using System.IO;
// using System.Linq;
// using System.Text;
// using System.Threading.Tasks;
using log4net.Core;
using log4net.Layout.Pattern;

namespace IAGrim.Utilities {
    /// <summary>
    /// Masks the windows username from logs
    /// </summary>
    class LoggerUsernameConverter : PatternLayoutConverter {
        private static readonly string? HomeDirectory = Environment.GetEnvironmentVariable("HOME");
        private static readonly string? Username = Environment.UserName;

        protected override void Convert(TextWriter writer, LoggingEvent loggingEvent) {
            string message = loggingEvent.RenderedMessage ?? string.Empty;
            if (!string.IsNullOrEmpty(HomeDirectory)) {
                message = message.Replace( HomeDirectory, "$HOME");
            }
            if (!string.IsNullOrEmpty(Username)) {
                message = message.Replace($"/{Username}/", "/:filtered/");
            }
            writer.Write(message);
        }
    }
}
