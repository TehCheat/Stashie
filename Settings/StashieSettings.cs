using System.Collections.Generic;
using System.Windows.Forms;
using PoeHUD.Hud.Settings;
using PoeHUD.Plugins;

namespace Stashie.Settings
{
    public class StashieSettings : SettingsBase
    {
        public Dictionary<string, StashTabNode> FilterOptions;
        public Dictionary<string, RangeNode<int>> RefillOptions;

        public StashieSettings()
        {
            Enable = false;
            RequireHotkey = true;
            DropHotkey = Keys.F3;
            ExtraDelay = new RangeNode<int>(0, 0, 2000);
            BlockInput = new ToggleNode(false);

            RefillCurrency = false;
            AllowHaveMore = false;
            FilterOptions = new Dictionary<string, StashTabNode>();
            RefillOptions = new Dictionary<string, RangeNode<int>>();

            VisitTabWhenDone = false;
        }

        [Menu("Settings", 500)]
        public EmptyNode Settings { get; set; }

        [Menu("Require Hotkey", "If you just want Stashie to drop items to stash, as soon as you open it.", 1000, 500)]
        public ToggleNode RequireHotkey { get; set; }

        [Menu("Hotkey", 1001, 1000)]
        public HotkeyNode DropHotkey { get; set; }

        [Menu("Extra Delay", "Is it going too fast? Then add a delay (in ms).", 2000, 500)]
        public RangeNode<int> ExtraDelay { get; set; }

        [Menu("Block Input", "Block user input (except: Ctrl+Alt+Delete) when dropping items to stash.", 3000, 500)]
        public ToggleNode BlockInput { get; set; }

        [Menu("When done, go to tab.",
            "After Stashie has dropped all items to their respective tabs, then go to the following tab.", 4000, 500)]
        public ToggleNode VisitTabWhenDone { get; set; }

        [Menu("tab (index)", 4001, 4000)]
        public StashTabNode TabToVisitWhenDone { get; set; } = new StashTabNode();

        public ToggleNode RefillCurrency { get; set; }
        public StashTabNode CurrencyStashTab { get; set; } = new StashTabNode();
        public ToggleNode AllowHaveMore { get; set; }
    }
}