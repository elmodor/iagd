using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IAGrim.UI.Controller {
    [Obsolete]
    interface ISettingsController {
        bool MinimizeToTray { get; set; }

        void LoadDefaults();

        void DonateNow();

        void OpenDataFolder();

        void OpenLogFolder();
    }
}
