using System.Collections.Generic;
using System.Windows.Forms;
using PoeHUD.Hud.Settings;
using PoeHUD.Plugins;

namespace Stashie.Settings
{
    public class StashieSettings : SettingsBase
    {
        public Dictionary<string, StashTabNode> FilterOptions = new Dictionary<string, StashTabNode>();

        [Menu("Settings", 500)]
        public EmptyNode Settings { get; set; }

        [Menu("Require Hotkey", "If you just want Stashie to drop items to stash, as soon as you open it.", 1000, 500)]
        public ToggleNode RequireHotkey { get; set; } = true;

        [Menu("Hotkey", 1001, 1000)]
        public HotkeyNode DropHotkey { get; set; } = Keys.F3;

        [Menu("Extra Delay", "Is it going too fast? Then add a delay (in ms).", 2000, 500)]
        public RangeNode<int> ExtraDelay { get; set; } = new RangeNode<int>(30, 0, 1000);

        [Menu("Block Input", "Block user input (except: Ctrl+Alt+Delete) when dropping items to stash.", 3000, 500)]
        public ToggleNode BlockInput { get; set; } = false;

        [Menu("When done, go to tab.",
            "After Stashie has dropped all items to their respective tabs, then go to the following tab.", 4000, 500)]
        public ToggleNode VisitTabWhenDone { get; set; } = false;

        [Menu("tab (index)", 4001, 4000)]
        public StashTabNode TabToVisitWhenDone { get; set; } = new StashTabNode();

        public ToggleNode RefillCurrency { get; set; } = false;
        public StashTabNode CurrencyStashTab { get; set; } = new StashTabNode();
        public ToggleNode AllowHaveMore { get; set; } = true;

        public int[,] IgnoredCells { get; set; } = {
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1}
        };

        public List<RefillProcessor> Refills = new List<RefillProcessor>();
    }
}