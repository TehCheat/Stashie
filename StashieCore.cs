using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Newtonsoft.Json;
using PoeHUD.Hud.Menu;
using PoeHUD.Hud.PluginExtension;
using PoeHUD.Hud.Settings;
using PoeHUD.Models.Enums;
using PoeHUD.Models.Interfaces;
using PoeHUD.Plugins;
using PoeHUD.Poe.Components;
using PoeHUD.Poe.Elements;
using PoeHUD.Poe.EntityComponents;
using PoeHUD.Poe.RemoteMemoryObjects;
using SharpDX;
using Stashie.Filters;
using Stashie.Settings;
using Stashie.Utils;
using PoeHUD.Hud.Menu.SettingsDrawers;
using PoeHUD.Controllers;

namespace Stashie
{
    public class StashieCore : BaseSettingsPlugin<StashieSettings>
    {
        private const string FITERS_CONFIG_FILE = "FitersConfig.txt";

        private const int WHILE_DELAY = 5;
        private const int INPUT_DELAY = 15;
        private MethodInfo _callPluginEventMethod;

        private Vector2 _windowOffset;

        private List<CustomFilter> _customFilters;
        private List<RefillProcessor> _customRefills;
        private List<FilterResult> _dropItems;
        private int[,] _ignoredCells;

        private IngameState _ingameState;
        private bool _playerHasDropdownMenu;

        public StashieCore()
        {
            PluginName = "Stashie";
        }

        public override void Initialise()
        {
            _callPluginEventMethod = typeof(PluginExtensionPlugin).GetMethod("CallPluginEvent");
            _ingameState = GameController.Game.IngameState;

            SaveDefaultConfigsToDisk();

            _customRefills = RefillParser.Parse(PluginDirectory);

            var filtersLines = File.ReadAllLines(Path.Combine(PluginDirectory, FITERS_CONFIG_FILE));
            _customFilters = FilterParser.Parse(filtersLines);

            LoadIgnoredCells();

            _playerHasDropdownMenu = _ingameState.ServerData.StashPanel.TotalStashes > 10;
        }

        private static void CreateFileAndAppendTextIfItDoesNotExitst(string path, string content)
        {
            if (File.Exists(path))
                return;

            using (var streamWriter = new StreamWriter(path, true))
            {
                streamWriter.Write(content);
                streamWriter.Close();
            }
        }

        private void SaveDefaultConfigsToDisk()
        {
            var path = $"{PluginDirectory}\\GitUpdateConfig.txt";
            const string gitUpdateConfig = "Owner:nymann\r\n" +
                                           "Name:Stashie\r\n" +
                                           "Release\r\n";
            CreateFileAndAppendTextIfItDoesNotExitst(path, gitUpdateConfig);

            path = $"{PluginDirectory}\\RefillCurrency.txt";

            const string refillCurrency =
                "//MenuName:\t\t\tClassName,\t\t\tStackSize,\tInventoryX,\tInventoryY\r\n" +
                "Portal Scrolls:\t\tPortal Scroll,\t\t40,\t\t\t12,\t\t\t1\r\n" +
                "Scrolls of Wisdom:\tScroll of Wisdom,\t40,\t\t\t12,\t\t\t2\r\n" +
                "//Chances:\t\t\tOrb of Chance,\t\t20,\t\t\t12,\t\t\t3";

            CreateFileAndAppendTextIfItDoesNotExitst(path, refillCurrency);

            path = $"{PluginDirectory}\\FitersConfig.txt";

            #region default config

            const string filtersConfig =
                "//FilterName(menu name):\tfilters\t\t:ParentMenu(optionaly, will be created automatially for grouping)\r\n" +
                "//Filter parts should divided by coma or | (for OR operation(any filter part can pass))\r\n" +
                "\r\n" +
                "////////////\tAvailable properties:\t/////////////////////\r\n" +
                "/////////\tString (name) properties:\r\n//classname\r\n" +
                "//basename\r\n" +
                "//path\r\n" +
                "/////////\tNumerical properties:\r\n" +
                "//itemquality\r\n" +
                "//rarity\r\n" +
                "//ilvl\r\n" +
                "/////////\tBoolean properties:\r\n" +
                "//identified\r\n" +
                "//Shaped\r\n" +
                "//Elder\r\n" +
                "/////////////////////////////////////////////////////////////\r\n" +
                "////////////\tAvailable operations:\t/////////////////////\r\n" +
                "/////////\tString (name) operations:\r\n" +
                "//!=\t(not equal)\r\n//=\t\t(equal)\r\n" +
                "//^\t\t(contains)\r\n//!^\t(not contains)\r\n" +
                "/////////\tNumerical operations:\r\n" +
                "//!=\t(not equal)\r\n//=\t\t(equal)\r\n" +
                "//>\t\t(bigger)\r\n//<\t\t(less)\r\n" +
                "//<=\t(less or equal)\r\n" +
                "//>=\t(greater or equal)\r\n" +
                "/////////\tBoolean operations:\r\n" +
                "//!\t\t(not/invert)\r\n" +
                "/////////////////////////////////////////////////////////////\r\n" +
                "\r\n" +
                "//Default Tabs\r\n" +
                "Divination Cards:\tClassName=DivinationCard\t\t\t\t\t\t\t\t\t\t:Default Tabs\r\n" +
                "Gems:\t\t\t\tClassName^Skill Gem,ItemQuality=0\t\t\t\t\t\t\t\t:Default Tabs\r\n" +
                "Splinters:\t\t\tBaseName^Splinter of,ClassName=StackableCurrency\t\t\t\t:Default Tabs\r\n" +
                "Currency:\t\t\tClassName=StackableCurrency,path!^Essence\t\t\t\t\t\t:Default Tabs\r\n" +
                "Abyss Jewels:\t\tClassName=AbyssJewel\t\t\t\t\t\t\t\t\t\t\t:Default Tabs\r\n" +
                "Essences:\t\t\tBaseName^Essence,ClassName=StackableCurrency\t\t\t\t\t:Default Tabs\r\n" +
                "Jewels:\t\t\t\tClassName=Jewel\t\t\t\t\t\t\t\t\t\t\t\t\t:Default Tabs\r\n" +
                "Flasks:\t\t\t\tClassName^Flask,ItemQuality=0\t\t\t\t\t\t\t\t\t:Default Tabs\r\n" +
                "\r\n" +
                "\r\n" +
                "//Chance Items\r\n" +
                "Sorcerer Boots:\tBaseName=Sorcerer Boots,Rarity=Normal\t:Chance Items\r\n" +
                "Leather Belt:\tBaseName=Leather Belt,Rarity=Normal\t\t:Chance Items\r\n" +
                "\r\n" +
                "//Vendor Recipes\r\n" +
                "Chisel Recipe:\t\tBaseName=Stone Hammer|BaseName=Rock Breaker,ItemQuality=20\t:Vendor Recipes\r\n" +
                "Quality Gems:\t\tClassName^Skill Gem,ItemQuality>0\t\t\t\t\t\t\t:Vendor Recipes\r\n" +
                "Quality Flasks:\t\tClassName^Flask,ItemQuality>0\t\t\t\t\t\t\t\t:Vendor Recipes\r\n" +
                "\r\n//Chaos Recipe LVL 2 (unindentified and ilvl 60 or above)\r\n" +
                "CR-Weapons:\t\t\t!identified,Rarity=Rare,ilvl>=60,ClassName^Two Hand|ClassName^One Hand|ClassName=Bow|ClassName=Staff|ClassName=Sceptre|ClassName=Wand|ClassName=Dagger|ClassName=Claw|ClassName=Shield|ClassName=Quiver :Chaos Recipe\r\n" +
                "CR-Amulets:\t\t\t!identified,Rarity=Rare,ilvl>=60,ClassName=Amulet \t\t\t\t:Chaos Recipe\r\n" +
                "CR-Rings:\t\t\t!identified,Rarity=Rare,ilvl>=60,ClassName=Ring \t\t\t\t:Chaos Recipe\r\n" +
                "CR-Belts:\t\t\t!identified,Rarity=Rare,ilvl>=60,ClassName=Belt \t\t\t\t:Chaos Recipe\r\n" +
                "CR-Helmets:\t\t\t!identified,Rarity=Rare,ilvl>=60,ClassName=Helmet \t\t\t\t:Chaos Recipe\r\n" +
                "CR-Chests:\t\t\t!identified,Rarity=Rare,ilvl>=60,ClassName=Body Armour \t\t\t:Chaos Recipe\r\n" +
                "CR-Boots:\t\t\t!identified,Rarity=Rare,ilvl>=60,ClassName=Boots \t\t\t\t:Chaos Recipe\r\n" +
                "CR-Gloves:\t\t\t!identified,Rarity=Rare,ilvl>=60,ClassName=Gloves \t\t\t\t:Chaos Recipe\r\n" +
                "\r\n" +
                "\r\n" +
                "// Non-Chaos Recipe Rares (eg. unindentified|ilvl<60)\r\n" +
                "Idd-Weapons:\t\tidentified|ilvl<60,Rarity=Rare,ClassName^Two Hand|ClassName^One Hand|ClassName=Bow|ClassName=Staff|ClassName=Sceptre|ClassName=Wand|ClassName=Dagger|ClassName=Claw|ClassName=Shield|ClassName=Quiver :Rares\r\n" +
                "Idd-Amulets:\t\tidentified|ilvl<60,Rarity=Rare,ClassName=Amulet \t\t\t\t:Rares\r\n" +
                "Idd-Rings:\t\t\tidentified|ilvl<60,Rarity=Rare,ClassName=Ring \t\t\t\t\t:Rares\r\n" +
                "Idd-Belts:\t\t\tidentified|ilvl<60,Rarity=Rare,ClassName=Belt \t\t\t\t\t:Rares\r\n" +
                "Idd-Helms:\t\t\tidentified|ilvl<60,Rarity=Rare,ClassName=Helmet \t\t\t\t:Rares\r\n" +
                "Idd-Body Armours:\tidentified|ilvl<60,Rarity=Rare,ClassName=Body Armour \t\t\t:Rares\r\n" +
                "Idd-Boots:\t\t\tidentified|ilvl<60,Rarity=Rare,ClassName=Boots \t\t\t\t\t:Rares\r\n" +
                "Idd-Gloves:\t\t\tidentified|ilvl<60,Rarity=Rare,ClassName=Gloves \t\t\t\t:Rares\r\n" +
                "\r\n" +
                "// Craftable stuff\r\n" +
                "Craft-Weapons:\t\t\tilvl>=84,Rarity=Normal|Rarity=Magic,\tClassName^Two Hand|ClassName^One Hand|ClassName=Bow|ClassName=Staff|ClassName=Sceptre|ClassName=Wand|ClassName=Dagger|ClassName=Claw|ClassName=Shield|ClassName=Quiver :Crafting\r\n" +
                "Craft-Amulets:\t\t\tilvl>=84,Rarity=Normal|Rarity=Magic,\tClassName=Amulet \t\t:Crafting\r\n" +
                "Craft-Rings:\t\t\tilvl>=84,Rarity=Normal|Rarity=Magic,\tClassName=Ring \t\t\t:Crafting\r\n" +
                "Craft-Belts:\t\t\tilvl>=84,Rarity=Normal|Rarity=Magic,\tClassName=Belt \t\t\t:Crafting\r\n" +
                "Craft-Helms:\t\t\tilvl>=84,Rarity=Normal|Rarity=Magic,\tClassName=Helmet \t\t:Crafting\r\n" +
                "Craft-Chests:\t\t\tilvl>=84,Rarity=Normal|Rarity=Magic,\tClassName=Body Armour \t:Crafting\r\n" +
                "Craft-Boots:\t\t\tilvl>=84,Rarity=Normal|Rarity=Magic,\tClassName=Boots \t\t:Crafting\r\n" +
                "Craft-Gloves:\t\t\tilvl>=84,Rarity=Normal|Rarity=Magic,\tClassName=Gloves \t\t:Crafting\r\n" +
                "\r\n" +
                "// Uniques\r\n" +
                "Uniques-Weapons:\t\t\tRarity=Unique,\tClassName^Two Hand|ClassName^One Hand|ClassName=Bow|ClassName=Staff|ClassName=Sceptre|ClassName=Wand|ClassName=Dagger|ClassName=Claw|ClassName=Shield|ClassName=Quiver :Uniques\r\n" +
                "Uniques-Amulets:\t\t\tRarity=Unique,\tClassName=Amulet \t\t:Uniques\r\n" +
                "Uniques-Rings:\t\t\tRarity=Unique,\tClassName=Ring \t\t\t\t:Uniques\r\n" +
                "Uniques-Belts:\t\t\tRarity=Unique,\tClassName=Belt \t\t\t\t:Uniques\r\n" +
                "Uniques-Helms:\t\t\tRarity=Unique,\tClassName=Helmet \t\t\t:Uniques\r\n" +
                "Uniques-Chests:\t\t\tRarity=Unique,\tClassName=Body Armour \t\t:Uniques\r\n" +
                "Uniques-Boots:\t\t\tRarity=Unique,\tClassName=Boots \t\t\t:Uniques\r\n" +
                "Uniques-Gloves:\t\t\tRarity=Unique,\tClassName=Gloves \t\t\t:Uniques\r\n" +
                "\r\n" +
                "\r\n" +
                "// Maps & Stuff\r\n" +
                "Maps:\t\t\t\tClassName=Map\t\t\t\t\t\t\t:Maps & Stuff\r\n" +
                "Fragments:\t\t\tClassName=MapFragment\t\t\t\t\t:Maps & Stuff\r\n" +
                "Labyrinth:\t\t\tClassName=LabyrinthMapItem\t\t\t\t:Maps & Stuff";

            #endregion

            CreateFileAndAppendTextIfItDoesNotExitst(path, filtersConfig);
        }

        private void ProcessInventoryItems()
        {
            var inventory =
                _ingameState.IngameUi.InventoryPanel[
                    InventoryIndex.PlayerInventory];

            var invItems = inventory.VisibleInventoryItems;

            if (invItems == null)
            {
                LogMessage("Player inventory->VisibleInventoryItems is null!", 5);
                return;
            }

            _dropItems = new List<FilterResult>();
            _windowOffset = GameController.Window.GetWindowRectangle().TopLeft;

            foreach (var invItem in invItems)
            {
                if (invItem.Item == null)
                    continue;

                if(string.IsNullOrEmpty(invItem.Item.Path))
                {
                    LogMessage($"Bugged item detected on X:{invItem.InventPosX}, Y:{invItem.InventPosY}, skipping.. Change location to fix this or restart the game (exit to character selection).", 5);
                    continue;
                }
                if (CheckIgnoreCells(invItem))
                    continue;

                var baseItemType = GameController.Files.BaseItemTypes.Translate(invItem.Item.Path);
                var testItem = new ItemData(invItem, baseItemType);

                var result = CheckFilters(testItem);

                if (result != null)
                    _dropItems.Add(result);
            }
        }

        private bool CheckIgnoreCells(NormalInventoryItem inventItem)
        {
            var inventPosX = inventItem.InventPosX;
            var inventPosY = inventItem.InventPosY;

            if (Settings.RefillCurrency.Value &&
                _customRefills.Any(x => x.InventPos.X == inventPosX && x.InventPos.Y == inventPosY))
                return true;

            if (inventPosX < 0 || inventPosX >= 12)
                return true;
            if (inventPosY < 0 || inventPosY >= 5)
                return true;

            return _ignoredCells[inventPosY, inventPosX] != 0; //No need to check all item size
        }

        private FilterResult CheckFilters(ItemData itemData)
        {
            foreach (var filter in _customFilters)
            {
                if (!filter.AllowProcess)
                    continue;

                if (filter.CompareItem(itemData))
                    return new FilterResult(filter.StashIndexNode, itemData);
            }

            return null;
        }

        private void DropToStash()
        {
            var cursorPosPreMoving = Mouse.GetCursorPosition();

            if (Settings.BlockInput.Value)
                WinApi.BlockInput(true);

            if (_dropItems.Count > 0)
            {
                // Dictionary where key is the index (stashtab index) and Value is the items to drop.
                var itemsToDrop = (from dropItem in _dropItems
                                   group dropItem by dropItem.StashNode.VisibleIndex
                    into itemsToDropByTab
                                   select itemsToDropByTab).ToDictionary(tab => tab.Key, tab => tab.ToList());

                var latency = (int)_ingameState.CurLatency;

                foreach (var stashResults in itemsToDrop)
                {
                    // If we are more than 2 tabs away from our target, then use dropdown approach if user has it.
                    if (!Keyboard.IsKeyToggled(Settings.DropHotkey.Value))
                        return;

                    if (!SwitchToTab(stashResults.Value[0].StashNode))
                        continue;
                    try
                    {
                        Keyboard.KeyDown(Keys.LControlKey);
                        Thread.Sleep(INPUT_DELAY);

                        foreach (var stashResult in stashResults.Value)
                        {
                            Mouse.SetCursorPosAndLeftClick(stashResult.ClickPos, Settings.ExtraDelay, _windowOffset);
                            Thread.Sleep(latency + Settings.ExtraDelay.Value);
                        }
                    }
                    catch
                    {
                        Keyboard.KeyUp(Keys.LControlKey);
                    }

                    // QVIN's version of Hud doesn't support Subscription events, so we use reflection.
                    if (_callPluginEventMethod != null)
                    {
                        _callPluginEventMethod.Invoke(API, new object[] { "StashUpdate", new object[0] });
                    }
                }

                Keyboard.KeyUp(Keys.LControlKey);
            }

            ProcessRefills();
            Mouse.SetCursorPos(cursorPosPreMoving.X, cursorPosPreMoving.Y);

            // TODO:Go back to a specific tab, if user has that setting enabled.
            if (Settings.VisitTabWhenDone.Value)
                SwitchToTab(Settings.TabToVisitWhenDone);

            if (Settings.BlockInput.Value)
            {
                WinApi.BlockInput(false);
                Thread.Sleep(INPUT_DELAY);
            }
        }

        public override void Render()
        {
            if (!Settings.Enable)
                return;

            var uiTabsOpened = _ingameState.IngameUi.InventoryPanel.IsVisible &&
                               _ingameState.ServerData.StashPanel.IsVisible;

            if (!uiTabsOpened)
            {
                if (Keyboard.IsKeyToggled(Settings.DropHotkey.Value))
                    Keyboard.KeyPress(Settings.DropHotkey.Value);

                return;
            }

            if (!Keyboard.IsKeyToggled(Settings.DropHotkey.Value))
                return;

            ProcessInventoryItems();

            if (_dropItems.Count == 0)
            {
                ProcessRefills();
                Keyboard.KeyPress(Settings.DropHotkey.Value);
                return;
            }

            DropToStash();

            Keyboard.KeyPress(Settings.DropHotkey.Value);
        }

        #region Loads


        public override void InitializeSettingsMenu()
        {
            base.InitializeSettingsMenu();
            GenerateStashieSettingsMenu();
        }

        private BaseSettingsDrawer FiltersMenuRootMenu;
        private BaseSettingsDrawer RefillMenuRootMenu;
        private List<StashTabNode> StashTabNodes = new List<StashTabNode>();//This is for hot reload, we will unload it

        private void GenerateStashieSettingsMenu()//Separate func cuz we can call it in anu moment to reload all menu
        {
            if(FiltersMenuRootMenu != null)
                SettingsDrawers.Remove(FiltersMenuRootMenu);
            if (RefillMenuRootMenu != null)
                SettingsDrawers.Remove(RefillMenuRootMenu);


            FiltersMenuRootMenu = new BaseSettingsDrawer { SettingName = "Filters", SettingId = GetUniqDrawerId() };
            SettingsDrawers.Add(FiltersMenuRootMenu);

            var submenu = new Dictionary<string, BaseSettingsDrawer>();
            foreach (var customFilter in _customFilters)
            {
                if (!Settings.FilterOptions.TryGetValue(customFilter.Name, out var tabNode))
                {
                    tabNode = new StashTabNode();
                    Settings.FilterOptions.Add(customFilter.Name, tabNode);
                }

                StashTabNodes.Add(tabNode);
                StashTabController.RegisterStashNode(tabNode);

                var filterParent = FiltersMenuRootMenu;
                if (!string.IsNullOrEmpty(customFilter.SubmenuName))
                {
                    if (!submenu.TryGetValue(customFilter.SubmenuName, out filterParent))
                    {
                        filterParent = new BaseSettingsDrawer { SettingName = customFilter.SubmenuName, SettingId = GetUniqDrawerId() };
                        FiltersMenuRootMenu.Children.Add(filterParent);
                        submenu.Add(customFilter.SubmenuName, filterParent);
                    }
                }

                filterParent.Children.Add(new StashTabNodeSettingDrawer(tabNode) { SettingName = customFilter.Name, SettingId = GetUniqDrawerId() });
                customFilter.StashIndexNode = tabNode;
            }


            if (_customRefills.Count > 0)
            {
                RefillMenuRootMenu = new CheckboxSettingDrawer(Settings.RefillCurrency) { SettingName = "Refill Currency", SettingId = GetUniqDrawerId() };
                SettingsDrawers.Add(RefillMenuRootMenu);

                RefillMenuRootMenu.Children.Add(new StashTabNodeSettingDrawer(Settings.CurrencyStashTab) { SettingName = "Currency Tab", SettingId = GetUniqDrawerId() });
                StashTabController.RegisterStashNode(Settings.CurrencyStashTab);
                RefillMenuRootMenu.Children.Add(new CheckboxSettingDrawer(Settings.AllowHaveMore) { SettingName = "Allow Have More", SettingId = GetUniqDrawerId() });

                foreach (var refill in _customRefills)
                {
                    if (!Settings.RefillOptions.TryGetValue(refill.MenuName, out var amountOption))
                    {
                        amountOption = new RangeNode<int>(0, 0, refill.StackSize);
                        Settings.RefillOptions.Add(refill.MenuName, amountOption);
                    }
                    amountOption.Max = refill.StackSize;
                    refill.AmountOption = amountOption;
                    RefillMenuRootMenu.Children.Add(new IntegerSettingsDrawer(amountOption) { SettingName = refill.MenuName, SettingId = GetUniqDrawerId() });
                }
            }
        }

        private void LoadIgnoredCells()
        {
            const string fileName = @"/IgnoredCells.json";
            var filePath = PluginDirectory + fileName;

            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                try
                {
                    _ignoredCells = JsonConvert.DeserializeObject<int[,]>(json);

                    var ignoredHeight = _ignoredCells.GetLength(0);
                    var ignoredWidth = _ignoredCells.GetLength(1);

                    if (ignoredHeight != 5 || ignoredWidth != 12)
                        LogError("Stashie: Wrong IgnoredCells size! Should be 12x5. Reseting to default..", 5);
                    else
                        return;
                }
                catch (Exception ex)
                {
                    LogError(
                        "Stashie: Can't decode IgnoredCells settings in " + fileName +
                        ". Reseting to default. Error: " + ex.Message, 5);
                }
            }

            _ignoredCells = new[,] {
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1}
            };

            var defaultSettings = JsonConvert.SerializeObject(_ignoredCells);
            defaultSettings = defaultSettings.Replace("[[", "[\n[");
            defaultSettings = defaultSettings.Replace("],[", "],\n[");
            defaultSettings = defaultSettings.Replace("]]", "]\n]");
            File.WriteAllText(filePath, defaultSettings);
        }

        #endregion

        #region Refill

        private void ProcessRefills()
        {
            if (!Settings.RefillCurrency.Value || _customRefills.Count == 0)
                return;
            if (!Settings.CurrencyStashTab.Exist)
            {
                LogError("Can't process refill: CurrencyStashTab is not set.", 5);
                return;
            }

            var delay = (int)_ingameState.CurLatency + Settings.ExtraDelay.Value;
            var currencyTabVisible = false;

            var inventory = _ingameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory];
            var inventoryItems = inventory.VisibleInventoryItems;

            if (inventoryItems == null)
            {
                LogError("Can't process refill: VisibleInventoryItems is null!", 5);
                return;
            }
            _customRefills.ForEach(x => x.Clear());

            var filledCells = new int[5, 12];

            foreach (var inventItem in inventoryItems)
            {
                var item = inventItem.Item;
                if (item == null)
                    continue;

                if (!Settings.AllowHaveMore.Value)
                    filledCells = GetFilledCells(filledCells, inventItem, item);

                if (!item.HasComponent<Stack>())
                    continue;

                foreach (var refill in _customRefills)
                {
                    var bit = GameController.Files.BaseItemTypes.Translate(item.Path);
                    if (bit.BaseName != refill.CurrencyClass)
                        continue;

                    var stack = item.GetComponent<Stack>();
                    refill.OwnedCount = stack.Size;
                    refill.ClickPos = inventItem.GetClientRect().Center;

                    if (refill.OwnedCount < 0 || refill.OwnedCount > 40)
                    {
                        LogError(
                            $"Ignoring refill: {refill.CurrencyClass}: Stacksize {refill.OwnedCount} not in range 0-40 ",
                            5);
                        refill.OwnedCount = -1;
                    }
                    break;
                }
            }

            var inventoryRec = inventory.InventoryUiElement.GetClientRect();
            var cellSize = inventoryRec.Width / 12;

            var freeCellFound = false;
            var freeCellPos = new Point();

            if (!Settings.AllowHaveMore.Value)
                FreeCellFound(filledCells, ref freeCellFound, ref freeCellPos);

            foreach (var refill in _customRefills)
            {
                if (refill.OwnedCount == -1)
                    continue;

                if (refill.OwnedCount == refill.AmountOption.Value)
                    continue;

                if (refill.OwnedCount < refill.AmountOption.Value)
                {
                    #region Refill

                    if (!currencyTabVisible)
                    {
                        if (!SwitchToTab(Settings.CurrencyStashTab))
                            continue;
                        currencyTabVisible = true;
                        Thread.Sleep(delay);
                    }

                    var moveCount = refill.AmountOption.Value - refill.OwnedCount;

                    var currStashItems = _ingameState.ServerData.StashPanel.VisibleStash
                        .VisibleInventoryItems;
                    var foundSourceOfRefill = currStashItems
                        .Where(x => GameController.Files.BaseItemTypes.Translate(x.Item.Path).BaseName ==
                                    refill.CurrencyClass).ToList();

                    foreach (var sourceOfRefill in foundSourceOfRefill)
                    {
                        var stackSize = sourceOfRefill.Item.GetComponent<Stack>().Size;
                        var getCurCount = moveCount > stackSize ? stackSize : moveCount;

                        var destination = refill.ClickPos;

                        if (refill.OwnedCount == 0)
                        {
                            destination = GetInventoryClickPosByCellIndex(inventory, refill.InventPos.X,
                                refill.InventPos.Y, cellSize);

                            // If cells is not free then continue.
                            if (_ingameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory]
                                    [refill.InventPos.X, refill.InventPos.Y, 12] != null)
                            {
                                moveCount--;
                                LogMessage(
                                    $"Inventoy ({refill.InventPos.X}, {refill.InventPos.Y}) is occupied by the wrong item!",
                                    5);
                                continue;
                            }
                        }

                        SplitStack(moveCount, sourceOfRefill.GetClientRect().Center, destination);
                        moveCount -= getCurCount;

                        if (moveCount == 0)
                            break;
                    }

                    if (moveCount > 0)
                        LogMessage(
                            $"Not enough currency (need {moveCount} more) to fill {refill.CurrencyClass} stack", 5);

                    #endregion
                }

                else if (!Settings.AllowHaveMore.Value && refill.OwnedCount > refill.AmountOption.Value)
                {
                    #region Remove excess items

                    if (!freeCellFound)
                    {
                        LogMessage(@"Can't find free cell in player inventory to move excess currency.", 5);
                        continue;
                    }

                    if (!currencyTabVisible)
                    {
                        if (!SwitchToTab(Settings.CurrencyStashTab))
                            continue;
                        currencyTabVisible = true;
                        Thread.Sleep(delay);
                    }

                    var destination =
                        GetInventoryClickPosByCellIndex(inventory, freeCellPos.X, freeCellPos.Y, cellSize);
                    var moveCount = refill.OwnedCount - refill.AmountOption.Value;

                    Thread.Sleep(delay);
                    SplitStack(moveCount, refill.ClickPos, destination);
                    Thread.Sleep(delay);

                    Keyboard.KeyDown(Keys.LControlKey);
                    Mouse.SetCursorPosAndLeftClick(destination, Settings.ExtraDelay.Value, _windowOffset);
                    Keyboard.KeyUp(Keys.LControlKey);

                    Thread.Sleep(delay);

                    #endregion
                }
            }
        }

        private static void FreeCellFound(int[,] filledCells, ref bool freeCellFound, ref Point freeCellPos)
        {
            for (var x = 0; x <= 11; x++)
            {
                for (var y = 0; y <= 4; y++)
                {
                    if (filledCells[y, x] != 0)
                        continue;

                    freeCellFound = true;
                    freeCellPos = new Point(x, y);
                    break;
                }
                if (freeCellFound)
                    break;
            }
        }

        private static int[,] GetFilledCells(int[,] filledCells, NormalInventoryItem inventItem, IEntity item)
        {
            var iPosX = inventItem.InventPosX;
            var iPosY = inventItem.InventPosY;
            var iBase = item.GetComponent<Base>();

            for (var x = iPosX; x <= iPosX + iBase.ItemCellsSizeX - 1; x++)
                for (var y = iPosY; y <= iPosY + iBase.ItemCellsSizeY - 1; y++)
                    if (x >= 0 && x <= 11 && y >= 0 && y <= 4)
                        filledCells[y, x] = 1;
                    else
                        LogMessage($"Out of range: {x} {y}", 10);

            return filledCells;
        }

        private static Vector2 GetInventoryClickPosByCellIndex(Inventory inventory, int indexX, int indexY,
            float cellSize)
        {
            return inventory.InventoryUiElement.GetClientRect().TopLeft +
                   new Vector2(cellSize * (indexX + 0.5f), cellSize * (indexY + 0.5f));
        }

        private void SplitStack(int amount, Vector2 from, Vector2 to)
        {
            var delay = (int)_ingameState.CurLatency * 2 + Settings.ExtraDelay;

            Keyboard.KeyDown(Keys.ShiftKey);

            while (!Keyboard.IsKeyDown((int)Keys.ShiftKey))
                Thread.Sleep(WHILE_DELAY);

            Mouse.SetCursorPosAndLeftClick(from, Settings.ExtraDelay.Value, _windowOffset);
            Thread.Sleep(INPUT_DELAY);
            Keyboard.KeyUp(Keys.ShiftKey);
            Thread.Sleep(delay + 50);
            if (amount > 40)
            {
                LogMessage("Can't select amount more than 40, current value: " + amount, 5);
                amount = 40;
            }

            if (amount < 10)
            {
                var keyToPress = (int)Keys.D0 + amount;
                Keyboard.KeyPress((Keys)keyToPress);
            }
            else
            {
                var keyToPress = (int)Keys.D0 + amount / 10;
                Keyboard.KeyPress((Keys)keyToPress);
                Thread.Sleep(delay);
                keyToPress = (int)Keys.D0 + amount % 10;
                Keyboard.KeyPress((Keys)keyToPress);
            }
            Thread.Sleep(delay);
            Keyboard.KeyPress(Keys.Enter);
            Thread.Sleep(delay + 50);

            Mouse.SetCursorPosAndLeftClick(to, Settings.ExtraDelay.Value, _windowOffset);
            Thread.Sleep(delay + 50);
        }

        #endregion

        #region Switching between StashTabs

        public bool SwitchToTabViaDropdownMenu(StashTabNode tabNode)
        {
            var latency = (int)_ingameState.CurLatency;
            var stashPanel = _ingameState.ServerData.StashPanel;

            // We want to maximum wait 20 times the Current Latency before giving up in our while loops.
            var maxNumberOfTries = latency * 20 > 2000 ? latency * 20 / WHILE_DELAY : 2000 / WHILE_DELAY;
            try
            {
                var viewAllTabsButton = _ingameState.ServerData.StashPanel.ViewAllStashButton;

                if (stashPanel.IsVisible && !viewAllTabsButton.IsVisible)
                    return SwitchToTabViaArrowKeys(tabNode);

                var dropdownMenu = _ingameState.ServerData.StashPanel.ViewAllStashPanel;

                if (!dropdownMenu.IsVisible)
                {
                    var pos = viewAllTabsButton.GetClientRect();
                    Mouse.SetCursorPosAndLeftClick(pos.Center, Settings.ExtraDelay, _windowOffset);

                    var brCounter = 0;

                    while (!dropdownMenu.IsVisible)
                    {
                        Thread.Sleep(WHILE_DELAY);

                        if (brCounter++ <= maxNumberOfTries)
                            continue;
                        LogMessage($"Error in SwitchToTabViaDropdownMenu({tabNode.Name}).", 5);
                        return false;
                    }

                    // Make sure that we are scrolled to the top in the menu.
                    if (_ingameState.ServerData.StashPanel.TotalStashes > 30)
                    {
                        Thread.Sleep(WHILE_DELAY);
                        Mouse.VerticalScroll(true, 10);
                        Thread.Sleep(WHILE_DELAY);
                        Mouse.VerticalScroll(true, 10);
                    }
                }

                var tabPos = dropdownMenu.Children[tabNode.VisibleIndex].GetClientRect();

                Mouse.SetCursorPosAndLeftClick(tabPos.Center, Settings.ExtraDelay, _windowOffset);
                Thread.Sleep(latency);
            }
            catch (Exception e)
            {
                LogError($"Error in GoToTab {tabNode.Name}: {e.Message}", 5);
                return false;
            }

            Inventory stash;

            var counter = 0;

            do
            {
                Thread.Sleep(WHILE_DELAY);
                stash = stashPanel.VisibleStash;

                if (counter++ <= maxNumberOfTries)
                    continue;
                LogMessage(
                    $"2. Error opening stash : {tabNode.Name}. Inventory type is: {stash.InvType.ToString()}",
                    5);
                return false;
            } while (stash?.VisibleInventoryItems == null);
            return true;
        }

        public bool SwitchToTab(StashTabNode tabNode)
        {
            var stashPanel = _ingameState.ServerData.StashPanel;

            // We don't want to Switch to a tab that we are already on
            if (stashPanel.IndexVisibleStash == tabNode.VisibleIndex)
                return true;

            if (!_playerHasDropdownMenu)
                return SwitchToTabViaArrowKeys(tabNode);

            var indexOfVisibleStash = stashPanel.IndexVisibleStash;
            var travelDistance = Math.Abs(tabNode.VisibleIndex - indexOfVisibleStash);

            if (tabNode.VisibleIndex > 30 && indexOfVisibleStash < 30)
            {
                SwitchToTab(new StashTabNode { VisibleIndex = 30 });
                return SwitchToTabViaArrowKeys(tabNode);
            }

            if (travelDistance > 3)
                return SwitchToTabViaDropdownMenu(tabNode);

            return SwitchToTabViaArrowKeys(tabNode);
        }

        private bool SwitchToTabViaArrowKeys(StashTabNode tabNode)
        {
            var latency = (int)_ingameState.CurLatency;
            var indexOfCurrentVisibleTab = _ingameState.ServerData.StashPanel.IndexVisibleStash;
            var difference = tabNode.VisibleIndex - indexOfCurrentVisibleTab;
            var tabIsToTheLeft = difference < 0;

            for (var i = 0; i < Math.Abs(difference); i++)
            {
                Keyboard.KeyPress(tabIsToTheLeft ? Keys.Left : Keys.Right);
                Thread.Sleep(latency);
            }

            return true;
        }

        #endregion

        public override void OnClose()
        {
            StashTabNodes.ForEach(x => StashTabController.UnregisterStashNode(x));//This is for hot reload
        }
    }
}