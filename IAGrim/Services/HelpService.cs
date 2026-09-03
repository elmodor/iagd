using IAGrim.Backup.Cloud;
using IAGrim.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// using System.Windows.Forms;
using IAGrim.Services;

namespace IAGrim.Services {
    public class HelpService : IHelpService {
        public void ShowHelp(IHelpService.HelpType type) {
            Process.Start(new ProcessStartInfo { FileName = $"https://grimdawn.evilsoft.net/help/?q={type.ToString()}&r={DateTime.UtcNow.Ticks}", UseShellExecute = true });
        }

        public void ShowCharacterBackups() {
            // shrug..
        }

        public void SetIsFirstRun(bool v) {
            // shrug..
        }

        public void SetIsGrimParsed(bool enabled) {
            // shrug..
        }

        public static string GetUrl(IHelpService.HelpType type) {
            return $"https://grimdawn.evilsoft.net/help/?q={type.ToString()}&r={DateTime.UtcNow.Ticks}";
        }
    }
}
