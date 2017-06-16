using PoeHUD.Hud.Settings;
using PoeHUD.Plugins;

namespace Stashie
{
    public class Settings : SettingsBase
    {
        public const int DefaultMaxTabs = 40;


        public Settings()
        {
            // Hotkey or automatic
            HotkeyRequired = true;
            HotkeySetting = new ToggleNode(false);

            // Tab Index (deprecated)
            Currency = new RangeNode<int>(0, 0, DefaultMaxTabs);
            DivinationCards = new RangeNode<int>(1, 0, DefaultMaxTabs);
            Essences = new RangeNode<int>(2, 0, DefaultMaxTabs);

            Jewels = new RangeNode<int>(7, 0, DefaultMaxTabs);
            Gems = new RangeNode<int>(8, 0, DefaultMaxTabs);
            LeagueStones = new RangeNode<int>(9, 0, DefaultMaxTabs);

            Flasks = new RangeNode<int>(11, 0, DefaultMaxTabs);
            Jewelery = new RangeNode<int>(12, 0, DefaultMaxTabs);
            WhiteItems = new RangeNode<int>(13, 0, DefaultMaxTabs);
            Talismen = new RangeNode<int>(14, 0, DefaultMaxTabs);

            // Portal and Wisdom Scrolls
            KeepSettings = true;
            PortalScrolls = new RangeNode<int>(20, 0, 40);
            WisdomScrolls = new RangeNode<int>(20, 0, 40);

            // Orb of Chance
            ChanceItemTabs = true;
            LeatherBelt = new RangeNode<int>(17, 0, DefaultMaxTabs);
            SorcererBoots = new RangeNode<int>(1, 0, DefaultMaxTabs);

            // Sorting
            SortingSettings = true;
            SortyByUniqueName = new ToggleNode(true);
            SortByBaseName = new ToggleNode(true);
            SortyByClassName = new ToggleNode(true);
            SortyByRarity = new ToggleNode(true);

            // Vendor Recipes
            VendorRecipeTabs = true;
            ChaosRecipeLvlOne = new RangeNode<int>(14, 0, DefaultMaxTabs);
            ChaosRecipeLvlTwo = new RangeNode<int>(15, 0, DefaultMaxTabs);
            ChaosRecipeLvlThree = new RangeNode<int>(16, 0, DefaultMaxTabs);
            ChiselRecipe = new RangeNode<int>(10, 0, DefaultMaxTabs);
            QualityFlasks = new RangeNode<int>(10, 0, DefaultMaxTabs);
            QualityGems = new RangeNode<int>(10, 0, DefaultMaxTabs);

            // Maps
            StrandShaped = new RangeNode<int>(3, 0, DefaultMaxTabs);
            ShoreShaped = new RangeNode<int>(4, 0, DefaultMaxTabs);
            UniqueMaps = new RangeNode<int>(5, 0, DefaultMaxTabs);
            OtherMaps = new RangeNode<int>(6, 0, DefaultMaxTabs);
            ShapedMaps = new RangeNode<int>(7, 0, DefaultMaxTabs);

            // Latency Slider
            LatencySlider = new RangeNode<int>(0, 0, 1000);
        }


        // Parent index 1000

        #region Portal and Wisdom Scroll Settings

        [Menu("Scrolls", 1000)]
        public ToggleNode KeepSettings { get; set; }

        [Menu("Wisdom, keep", 100, 1000)]
        public RangeNode<int> WisdomScrolls { get; set; }

        [Menu("Portal, keep", 101, 1000)]
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

        #endregion


        // Parent index 3000

        #region Tab Indexes Settings

        [Menu("Default Tabs", 3000)]
        public EmptyNode TabNum { get; set; }

        [Menu("Currency", 300, 3000)]
        public RangeNode<int> Currency { get; set; }

        [Menu("Divination Cards", 301, 3000)]
        public RangeNode<int> DivinationCards { get; set; }

        [Menu("Essences", 302, 3000)]
        public RangeNode<int> Essences { get; set; }

        [Menu("Jewels", 307, 3000)]
        public RangeNode<int> Jewels { get; set; }

        [Menu("Gems", 308, 3000)]
        public RangeNode<int> Gems { get; set; }

        [Menu("Leaguestones", 309, 3000)]
        public RangeNode<int> LeagueStones { get; set; }

        [Menu("Flask", 311, 3000)]
        public RangeNode<int> Flasks { get; set; }

        [Menu("Rings And Amulets", 312, 3000)]
        public RangeNode<int> Jewelery { get; set; }

        [Menu("White Items", 313, 3000)]
        public RangeNode<int> WhiteItems { get; set; }

        [Menu("Talismen", 314, 3000)]
        public RangeNode<int> Talismen { get; set; }
        #endregion


        // Parent index 4000

        #region Orb of Chance

        [Menu("Chance Items", 4000)]
        public ToggleNode ChanceItemTabs { get; set; }

        [Menu("Leather Belt", 400, 4000)]
        public RangeNode<int> LeatherBelt { get; set; }

        [Menu("Sorcerer Boots", 401, 4000)]
        public RangeNode<int> SorcererBoots { get; set; }

        #endregion


        // Parent index 5000

        #region Vendor Recipe Settings

        [Menu("Vendor Recipes", 5000)]
        public ToggleNode VendorRecipeTabs { get; set; }

        [Menu("Chaos Recipe 1", 500, 5000)]
        public RangeNode<int> ChaosRecipeLvlOne { get; set; }

        [Menu("Chaos Recipe 2", 501, 5000)]
        public RangeNode<int> ChaosRecipeLvlTwo { get; set; }

        [Menu("Chaos Recipe 3", 502, 5000)]
        public RangeNode<int> ChaosRecipeLvlThree { get; set; }

        [Menu("Chisel Recipe", 503, 5000)]
        public RangeNode<int> ChiselRecipe { get; set; }

        [Menu("Quality Flasks", 504, 5000)]
        public RangeNode<int> QualityFlasks { get; set; }

        [Menu("Quality Gems", 505, 5000)]
        public RangeNode<int> QualityGems { get; set; }

        #endregion

        // Parent index 6000

        #region Maps

        [Menu("Map Tabs", 6000)]
        public EmptyNode MapTabsHOlder { get; set; }

        [Menu("Shaped Strands", 600, 6000)]
        public RangeNode<int> StrandShaped { get; set; }

        [Menu("Shaped Shores", 601, 6000)]
        public RangeNode<int> ShoreShaped { get; set; }

        [Menu("Unique Maps", 602, 6000)]
        public RangeNode<int> UniqueMaps { get; set; }

        [Menu("Other Maps", 603, 6000)]
        public RangeNode<int> OtherMaps { get; set; }

        [Menu("Shaped Maps", 604, 6000)]
        public RangeNode<int> ShapedMaps { get; set; }

        #endregion


        // Parent index 7000

        #region HotkeyRequired

        [Menu("Require Hotkey", 7000)]
        public ToggleNode HotkeyRequired { get; set; }

        [Menu("Change Hotkey", 700, 7000)]
        public ToggleNode HotkeySetting { get; set; }

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