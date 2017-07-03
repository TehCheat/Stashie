#region Header

//-----------------------------------------------------------------
//   Class:          StashieLogicSettings
//   Description:    Main settings for a plugin.
//   Author:         Stridemann        Date: 08.26.2017
//-----------------------------------------------------------------

#endregion

using System.Collections.Generic;
using System.Windows.Forms;
using PoeHUD.Hud.Settings;
using PoeHUD.Plugins;

namespace Stashie.Settings
{
    public class StashieSettings : SettingsBase
    {
        public StashieSettings()
        {
            Enable = false;
            RequireHotkey = true;
            DropHotkey = Keys.F3;
            ExtraDelay = new RangeNode<int>(0, 0, 2000);
            BlockInput = new ToggleNode(false);

            RefillCurrency = false;
            CurrencyStashTab = new ListIndexNode();
            AllowHaveMore = false;
            CustomFilterOptions = new Dictionary<string, ListIndexNode>();
            CustomRefillOptions = new Dictionary<string, RangeNode<int>>();
        }

        [Menu("Require Hotkey", 1000)]
        public ToggleNode RequireHotkey { get; set; }

        [Menu("Hotkey", 1001, 1000)]
        public HotkeyNode DropHotkey { get; set; }

        [Menu("Extra Delay", 2000)]
        public RangeNode<int> ExtraDelay { get; set; }

        [Menu("Block Input", 3000)]
        public ToggleNode BlockInput { get; set; }

        public ToggleNode RefillCurrency { get; set; }
        public ListIndexNode CurrencyStashTab { get; set; }
        public ToggleNode AllowHaveMore { get; set; }


        public List<string> AllStashNames = new List<string>();
        public Dictionary<string, ListIndexNode> CustomFilterOptions;
        public Dictionary<string, RangeNode<int>> CustomRefillOptions;
    }
}