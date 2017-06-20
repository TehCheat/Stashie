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
            Currency = new ListNode {Value = "Ignore"};
            DivinationCards = new ListNode {Value = "Ignore"};
            Essences = new ListNode {Value = "Ignore"};

            Jewels = new ListNode {Value = "Ignore"};
            Gems = new ListNode {Value = "Ignore"};
            Leaguestones = new ListNode {Value = "Ignore"};

            Flasks = new ListNode {Value = "Ignore"};
            Jewelery = new ListNode {Value = "Ignore"};
            WhiteItems = new ListNode {Value = "Ignore"};
            Talismen = new ListNode {Value = "Ignore"};

            // Portal and Wisdom Scrolls
            ReFillScrolls = true;
            PortalScrolls = new RangeNode<int>(20, 0, 40);
            WisdomScrolls = new RangeNode<int>(20, 0, 40);

            // Orb of Chance
            ChanceItemTabs = true;
            LeatherBelt = new ListNode {Value = "Ignore"};
            SorcererBoots = new ListNode {Value = "Ignore"};

            // Sorting
            SortingSettings = true;
            SortyByUniqueName = new ToggleNode(true);
            SortByBaseName = new ToggleNode(true);
            SortyByClassName = new ToggleNode(true);
            SortyByRarity = new ToggleNode(true);
            StashTabToInventory = new ToggleNode(false);

            // Vendor Recipes
            VendorRecipeTabs = true;
            ChaosRecipeLvlOne = new ListNode {Value = "Ignore"};
            ChaosRecipeLvlTwo = new ListNode {Value = "Ignore"};
            ChaosRecipeLvlThree = new ListNode {Value = "Ignore"};
            ChiselRecipe = new ListNode {Value = "Ignore"};
            QualityFlasks = new ListNode {Value = "Ignore"};
            QualityGems = new ListNode {Value = "Ignore"};

            // Maps
            MapTabsHolder = new ToggleNode(true);
            StrandShaped = new ListNode {Value = "Ignore"};
            ShoreShaped = new ListNode {Value = "Ignore"};
            UniqueMaps = new ListNode {Value = "Ignore"};
            OtherMaps = new ListNode {Value = "Ignore"};
            ShapedMaps = new ListNode {Value = "Ignore"};

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
        public ListNode Currency { get; set; }

        [Menu("Divination Cards", 301, 3000)]
        public ListNode DivinationCards { get; set; }

        [Menu("Essences", 302, 3000)]
        public ListNode Essences { get; set; }

        [Menu("Jewels", 307, 3000)]
        public ListNode Jewels { get; set; }

        [Menu("Gems", 308, 3000)]
        public ListNode Gems { get; set; }

        [Menu("Leaguestones", 309, 3000)]
        public ListNode Leaguestones { get; set; }

        [Menu("Flask", 311, 3000)]
        public ListNode Flasks { get; set; }

        [Menu("Rings And Amulets", 312, 3000)]
        public ListNode Jewelery { get; set; }

        [Menu("White Items", 313, 3000)]
        public ListNode WhiteItems { get; set; }

        [Menu("Talismen", 314, 3000)]
        public ListNode Talismen { get; set; }

        #endregion


        // Parent index 4000

        #region Orb of Chance

        [Menu("Chance Items", 4000)]
        public ToggleNode ChanceItemTabs { get; set; }

        [Menu("Leather Belt", 400, 4000)]
        public ListNode LeatherBelt { get; set; }

        [Menu("Sorcerer Boots", 401, 4000)]
        public ListNode SorcererBoots { get; set; }

        #endregion


        // Parent index 5000

        #region Vendor Recipe Settings

        [Menu("Vendor Recipes", 5000)]
        public ToggleNode VendorRecipeTabs { get; set; }

        [Menu("Chaos Recipe 1", 500, 5000)]
        public ListNode ChaosRecipeLvlOne { get; set; }

        [Menu("Chaos Recipe 2", 501, 5000)]
        public ListNode ChaosRecipeLvlTwo { get; set; }

        [Menu("Chaos Recipe 3", 502, 5000)]
        public ListNode ChaosRecipeLvlThree { get; set; }

        [Menu("Chisel Recipe", 503, 5000)]
        public ListNode ChiselRecipe { get; set; }

        [Menu("Quality Flasks", 504, 5000)]
        public ListNode QualityFlasks { get; set; }

        [Menu("Quality Gems", 505, 5000)]
        public ListNode QualityGems { get; set; }

        #endregion

        // Parent index 6000

        #region Maps

        [Menu("Map Tabs", 6000)]
        public ToggleNode MapTabsHolder { get; set; }

        [Menu("Shaped Strands", 600, 6000)]
        public ListNode StrandShaped { get; set; }

        [Menu("Shaped Shores", 601, 6000)]
        public ListNode ShoreShaped { get; set; }

        [Menu("Unique Maps", 602, 6000)]
        public ListNode UniqueMaps { get; set; }

        [Menu("Other Maps", 603, 6000)]
        public ListNode OtherMaps { get; set; }

        [Menu("Shaped Maps", 604, 6000)]
        public ListNode ShapedMaps { get; set; }

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
    }
}