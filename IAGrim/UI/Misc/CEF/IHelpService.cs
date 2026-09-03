// using IAGrim.Backup.Cloud;
// using IAGrim.Utilities;
// using System;
// using System.Collections.Generic;
// using System.Diagnostics;
// using System.IO;
// using System.Linq;
// using System.Text;
// using System.Threading.Tasks;
// using System.Windows.Forms;

namespace IAGrim.Services {
    public interface IHelpService {
        public enum HelpType {
            BuddyItems,
            CloudSavesEnabled,
            CannotFindGrimdawn,
            TransferToAnyMod,
            RestoreBackup,
            DuplicateItem,
            NoStacks,
            NotLootingUnidentified,
            MultiplePcs,
            DelayWhenSearching,
            RegularUpdates,
            WhatIsBuddyId,
            WhatIsBuddyNickname,
            OnlineBackups,
            NotEnoughStashTabs,
            StashError,
            PathError,
            No32Bit,
            WindowsAntiRansomwareIssue,
        }
        void ShowHelp(IHelpService.HelpType type);
        void ShowCharacterBackups(); // TODO: Not strictly the right place for it..
        void SetIsGrimParsed(bool enabled); // TODO: Not strictly the right place for it..
        void SetIsFirstRun(bool value);
    }
}
