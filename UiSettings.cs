using System.Collections.Generic;
using System.Windows.Forms;
using PoeHUD.Hud.Settings;
using PoeHUD.Plugins;

namespace Stashie
{
    public class UiSettings : SettingsBase
    {
        public const int DefaultMaxTabs = 40;


        public UiSettings()
        {
            // Hotkey or automatic
            HotkeyRequired = true;
            HotkeySetting = new HotkeyNode(Keys.F3);

            // Tab Index (deprecated)
            Currency = new ListIndexNode {Value = "Ignore", Index = -1};
            DivinationCards = new ListIndexNode {Value = "Ignore", Index = -1};
            Essences = new ListIndexNode {Value = "Ignore", Index = -1};

            Jewels = new ListIndexNode {Value = "Ignore", Index = -1};
            Gems = new ListIndexNode {Value = "Ignore", Index = -1};
            Leaguestones = new ListIndexNode {Value = "Ignore", Index = -1};

            Flasks = new ListIndexNode {Value = "Ignore", Index = -1};
            Jewelery = new ListIndexNode {Value = "Ignore", Index = -1};
            WhiteItems = new ListIndexNode {Value = "Ignore", Index = -1};
            Talismen = new ListIndexNode {Value = "Ignore", Index = -1};

            // Portal and Wisdom Scrolls
            ReFillScrolls = true;
            PortalScrolls = new RangeNode<int>(20, 0, 40);
            WisdomScrolls = new RangeNode<int>(20, 0, 40);

            // Orb of Chance
            ChanceItemTabs = true;
            LeatherBelt = new ListIndexNode {Value = "Ignore", Index = -1};
            SorcererBoots = new ListIndexNode {Value = "Ignore", Index = -1};

            // Sorting
            SortingSettings = true;
            SortyByUniqueName = new ToggleNode(true);
            SortByBaseName = new ToggleNode(true);
            SortyByClassName = new ToggleNode(true);
            SortyByRarity = new ToggleNode(true);
            StashTabToInventory = new ToggleNode(false);

            // Vendor Recipes
            VendorRecipeTabs = true;
            ChaosRecipeLvlOne = new ListIndexNode {Value = "Ignore", Index = -1};
            ChaosRecipeLvlTwo = new ListIndexNode {Value = "Ignore", Index = -1};
            ChaosRecipeLvlThree = new ListIndexNode {Value = "Ignore", Index = -1};
            ChiselRecipe = new ListIndexNode {Value = "Ignore", Index = -1};
            QualityFlasks = new ListIndexNode {Value = "Ignore", Index = -1};
            QualityGems = new ListIndexNode {Value = "Ignore", Index = -1};

            // Maps
            MapTabsHolder = new ToggleNode(true);
            StrandShaped = new ListIndexNode {Value = "Ignore", Index = -1};
            ShoreShaped = new ListIndexNode {Value = "Ignore", Index = -1};
            UniqueMaps = new ListIndexNode {Value = "Ignore", Index = -1};
            OtherMaps = new ListIndexNode {Value = "Ignore", Index = -1};
            ShapedMaps = new ListIndexNode {Value = "Ignore", Index = -1};

            // Latency Slider
            LatencySlider = new RangeNode<int>(0, 0, 1000);
        }


        // Parent index 1000

        #region Portal and Wisdom Scroll Settings

        [Menu("Re-fill Scrolls", 1000)]
        public ToggleNode ReFillScrolls { get; set; }

        [Menu("Wisdom Scrolls", 100, 1000)]
        public RangeNode<int> WisdomScrolls { get; set; }

        [Menu("Portal Scrolls", 101, 1000)]
        public RangeNode<int> PortalScrolls { get; set; }

        #endregion


        // Parent index 2000

        #region Sorting

        [Menu("Sorting", 1339)]
        public ToggleNode SortingSettings { get; set; }

        [Menu("UniqueName", 200, 1339)]
        public ToggleNode SortyByUniqueName { get; set; }

        [Menu("BaseName", 201, 1339)]
        public ToggleNode SortByBaseName { get; set; }

        [Menu("Type of Item", 202, 1339)]
        public ToggleNode SortyByClassName { get; set; }

        [Menu("Rarity", 203, 1339)]
        public ToggleNode SortyByRarity { get; set; }

        [Menu("Stashtab to Inventory", 204, 1339)]
        public ToggleNode StashTabToInventory { get; set; }

        #endregion


        // Parent index 3000

        #region Tab Indexes Settings

        [Menu("Default Tabs", 3000)]
        public EmptyNode TabNum { get; set; }

        [Menu("Currency", 300, 3000)]
        public ListIndexNode Currency { get; set; }

        [Menu("Divination Cards", 301, 3000)]
        public ListIndexNode DivinationCards { get; set; }

        [Menu("Essences", 302, 3000)]
        public ListIndexNode Essences { get; set; }

        [Menu("Jewels", 307, 3000)]
        public ListIndexNode Jewels { get; set; }

        [Menu("Gems", 308, 3000)]
        public ListIndexNode Gems { get; set; }

        [Menu("Leaguestones", 309, 3000)]
        public ListIndexNode Leaguestones { get; set; }

        [Menu("Flask", 311, 3000)]
        public ListIndexNode Flasks { get; set; }

        [Menu("Rings And Amulets", 312, 3000)]
        public ListIndexNode Jewelery { get; set; }

        [Menu("White Items", 313, 3000)]
        public ListIndexNode WhiteItems { get; set; }

        [Menu("Talismen", 314, 3000)]
        public ListIndexNode Talismen { get; set; }

        #endregion


        // Parent index 4000

        #region Orb of Chance

        [Menu("Chance Items", 4000)]
        public ToggleNode ChanceItemTabs { get; set; }

        [Menu("Leather Belt", 400, 4000)]
        public ListIndexNode LeatherBelt { get; set; }

        [Menu("Sorcerer Boots", 401, 4000)]
        public ListIndexNode SorcererBoots { get; set; }

        #endregion


        // Parent index 5000

        #region Vendor Recipe Settings

        [Menu("Vendor Recipes", 5000)]
        public ToggleNode VendorRecipeTabs { get; set; }

        [Menu("Chaos Recipe 1", 500, 5000)]
        public ListIndexNode ChaosRecipeLvlOne { get; set; }

        [Menu("Chaos Recipe 2", 501, 5000)]
        public ListIndexNode ChaosRecipeLvlTwo { get; set; }

        [Menu("Chaos Recipe 3", 502, 5000)]
        public ListIndexNode ChaosRecipeLvlThree { get; set; }

        [Menu("Chisel Recipe", 503, 5000)]
        public ListIndexNode ChiselRecipe { get; set; }

        [Menu("Quality Flasks", 504, 5000)]
        public ListIndexNode QualityFlasks { get; set; }

        [Menu("Quality Gems", 505, 5000)]
        public ListIndexNode QualityGems { get; set; }

        #endregion

        // Parent index 6000

        #region Maps

        [Menu("Map Tabs", 6000)]
        public ToggleNode MapTabsHolder { get; set; }

        [Menu("Shaped Strands", 600, 6000)]
        public ListIndexNode StrandShaped { get; set; }

        [Menu("Shaped Shores", 601, 6000)]
        public ListIndexNode ShoreShaped { get; set; }

        [Menu("Unique Maps", 602, 6000)]
        public ListIndexNode UniqueMaps { get; set; }

        [Menu("Other Maps", 603, 6000)]
        public ListIndexNode OtherMaps { get; set; }

        [Menu("Shaped Maps", 604, 6000)]
        public ListIndexNode ShapedMaps { get; set; }

        #endregion


        // Parent index 7000

        #region HotkeyRequired

        [Menu("Require Hotkey", 7000)]
        public ToggleNode HotkeyRequired { get; set; }

        [Menu("Change Hotkey", 700, 7000)]
        public HotkeyNode HotkeySetting { get; set; }

        #endregion

        // Parent index 8000

        #region Rarity Tabs

        #endregion

        // Parent index 9000

        #region Latency Slider

        [Menu("Extra Latency", 9000)]
        public RangeNode<int> LatencySlider { get; set; }

        #endregion

        public List<string> AllStashNames = new List<string>();
    }
}