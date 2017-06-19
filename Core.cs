using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using WindowsInput;
using WindowsInput.Native;
using Newtonsoft.Json;
using PoeHUD.Models.Enums;
using PoeHUD.Plugins;
using PoeHUD.Poe;
using PoeHUD.Poe.Components;
using PoeHUD.Poe.Elements;
using PoeHUD.Poe.RemoteMemoryObjects;
using SharpDX;

namespace Stashie
{
    public class Core : BaseSettingsPlugin<UiSettings>
    {
        private readonly InputSimulator _input = new InputSimulator();
        private readonly MouseSimulator _mouse = new MouseSimulator(new InputSimulator());
        private readonly KeyboardSimulator _keyboard = new KeyboardSimulator(new InputSimulator());

        private bool _movePortalScrolls = true;
        private bool _moveWisdomScrolls = true;
        private bool _moveItemsToStash = true;

        private const int InputDelay = 15;
        private const int WhileDelay = 5;

        private Settings _settings = new Settings();

        public override void Initialise()
        {
            PluginName = "STASHIE";

            if (Settings.Enable.Value)
            {
                SetupOrClose();
            }

            Settings.Enable.OnValueChanged += SetupOrClose;
        }

        private void SetupOrClose()
        {
            if (!Settings.Enable.Value)
            {
                return;
            }

            var path = PluginDirectory + @"/Settings.json";

            if (!File.Exists(path))
            {
                _settings.IgnoredCells = new[,]
                {
                    {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, Constants.Ignored},
                    {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, Constants.Ignored},
                    {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
                    {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
                    {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}
                };

                var defaultSettings = JsonConvert.SerializeObject(_settings);

                defaultSettings = defaultSettings.Replace("],[", "],\n\t\t[");
                defaultSettings = defaultSettings.Replace(":[", ":[\n\t\t");
                defaultSettings = defaultSettings.Replace("{", "{\n\t");
                defaultSettings = defaultSettings.Replace("}", "\n}");
                defaultSettings = defaultSettings.Replace(",\"", ",\n\t\"");
                defaultSettings = defaultSettings.Replace("]]", "]\n\t]");
                
                File.WriteAllText(path, defaultSettings);
            }

            var json = File.ReadAllText(path);

            _settings = JsonConvert.DeserializeObject<Settings>(json);

            // Sets the maximum of the range node to the number of the players total stashes.
            var totalStashes = (int) GameController.Game.IngameState.ServerData.StashPanel.TotalStashes - 1;

            #region Default Tabs

            Settings.Currency.Max = totalStashes;
            Settings.Currency.Value %= totalStashes;

            Settings.DivinationCards.Max = totalStashes;
            Settings.DivinationCards.Value %= totalStashes;

            Settings.Essences.Max = totalStashes;
            Settings.Essences.Value %= totalStashes;

            Settings.Jewels.Max = totalStashes;
            Settings.Jewels.Value %= totalStashes;

            Settings.Gems.Max = totalStashes;
            Settings.Gems.Value %= totalStashes;

            Settings.LeagueStones.Max = totalStashes;
            Settings.LeagueStones.Value %= totalStashes;

            Settings.Flasks.Max = totalStashes;
            Settings.Flasks.Value %= totalStashes;

            Settings.Jewelery.Max = totalStashes;
            Settings.Jewelery.Value %= totalStashes;

            Settings.WhiteItems.Max = totalStashes; // Todo: Should be expanded to crafting
            Settings.WhiteItems.Value %= totalStashes;

            Settings.Talismen.Max = totalStashes;
            Settings.Talismen.Value %= totalStashes;

            #endregion

            #region Orb of Chance

            Settings.LeatherBelt.Max = totalStashes;
            Settings.LeatherBelt.Value %= totalStashes;

            Settings.SorcererBoots.Max = totalStashes;
            Settings.SorcererBoots.Value %= totalStashes;

            #endregion

            #region Vendor Recipes

            Settings.ChiselRecipe.Max = totalStashes;
            Settings.ChiselRecipe.Value %= totalStashes;

            Settings.ChaosRecipeLvlOne.Max = totalStashes;
            Settings.ChaosRecipeLvlOne.Value %= totalStashes;

            Settings.ChaosRecipeLvlTwo.Max = totalStashes;
            Settings.ChaosRecipeLvlTwo.Value %= totalStashes;

            Settings.ChaosRecipeLvlThree.Max = totalStashes;
            Settings.ChaosRecipeLvlThree.Value %= totalStashes;

            Settings.QualityFlasks.Max = totalStashes;
            Settings.QualityFlasks.Value %= totalStashes;

            Settings.QualityGems.Max = totalStashes;
            Settings.QualityGems.Value %= totalStashes;

            #endregion

            #region Maps

            Settings.StrandShaped.Max = totalStashes;
            Settings.StrandShaped.Value %= totalStashes;

            Settings.ShoreShaped.Value %= totalStashes;
            Settings.ShoreShaped.Max = totalStashes;

            Settings.UniqueMaps.Max = totalStashes;
            Settings.UniqueMaps.Value %= totalStashes;

            Settings.OtherMaps.Max = totalStashes;
            Settings.OtherMaps.Value %= totalStashes;

            Settings.ShapedMaps.Max = totalStashes;
            Settings.ShapedMaps.Value %= totalStashes;

            #endregion

        }

        public override void Render()
        {
            var stashPanel = GameController.Game.IngameState.ServerData.StashPanel;

            if (stashPanel != null && !stashPanel.IsVisible)
            {
                _moveItemsToStash = true;
                return;
            }

            if (!_moveItemsToStash && !Settings.HotkeyRequired.Value)
            {
                return;
            }

            if (Settings.HotkeyRequired.Value &&
                !_input.InputDeviceState.IsKeyDown((VirtualKeyCode) Settings.HotkeySetting.Value))
            {
                return;
            }

            Stashie();
            FillUpScrolls();
            _moveItemsToStash = false;
        }

        public void FillUpScrolls()
        {
            // We currently don't have any scrolls in our inventory.
            var stashTab = GameController.Game.IngameState.ServerData.StashPanel.VisibleStash;
            if (stashTab.InvType != InventoryType.CurrencyStash)
            {
                GoToTab(Settings.Currency.Value);
                Thread.Sleep(100);
                stashTab = GameController.Game.IngameState.ServerData.StashPanel.VisibleStash;
            }
            var latency = (int) GameController.Game.IngameState.CurLatency;

            var doesStashContainPortalScrolls = stashTab.VisibleInventoryItems.ToList().Any(item => GameController.Files
                .BaseItemTypes
                .Translate(item.Item.Path).BaseName.Equals("Portal Scroll"));

            if (doesStashContainPortalScrolls && _movePortalScrolls)
            {
                var portalScroll = stashTab.VisibleInventoryItems.ToList().First(item => GameController.Files
                    .BaseItemTypes
                    .Translate(item.Item.Path).BaseName.Equals("Portal Scroll"));

                var emptyCell = FindEmptyOneCell(portalScroll);

                SplitStackAndMoveToFreeCell(portalScroll, latency, Settings.PortalScrolls.Value, emptyCell);
            }

            if (!_moveWisdomScrolls)
            {
                return;
            }

            var doesStashContainWisdomScrolls = stashTab.VisibleInventoryItems.ToList().Any(item => GameController.Files
                .BaseItemTypes
                .Translate(item.Item.Path).BaseName.Equals("Scroll of Wisdom"));

            if (doesStashContainWisdomScrolls)
            {
                var wisdomScroll = stashTab.VisibleInventoryItems.ToList().First(item => GameController.Files
                    .BaseItemTypes
                    .Translate(item.Item.Path).BaseName.Equals("Scroll of Wisdom"));

                var emptyCell = FindEmptyOneCell(wisdomScroll);

                SplitStackAndMoveToFreeCell(wisdomScroll, latency, Settings.WisdomScrolls.Value, emptyCell);
            }
        }

        #region Advanced Portal and Wisdom Scroll 'Manager', for the time being this is not worth the implementation time.

        /*private void TransferScrollsToInventory(string baseName, int ammount)
        {
            
        }

        private void Scrolls(string baseName)
        {
            // Make sure that we have the right ammount of portal scrolls in our inventory panel
            // Where the right amount is the number of scrolls the user wants UiSettings.ScrollsToKeep

            var wantedAmmountOfScrolls = baseName.Equals("Portal Scroll") ? Settings.PortalScrolls.Value : Settings.WisdomScrolls.Value;

            // Next, find all the elements (stacks of items) in the inventory panel that are portal scrolls.
            var inventoryPanel = GetInventoryPanel();

            var portalScrollStackElements = inventoryPanel.Children.ToList()
                .Where(element => GameController.Files.BaseItemTypes
                    .Translate(element.AsObject<NormalInventoryItem>().Item.Path).BaseName.Equals(baseName))
                .Select(element => element.AsObject<NormalInventoryItem>().Item).Cast<IEntity>().ToList();

            if (portalScrollStackElements.Count == 0)
            {
                // No Scrolls.
                TransferScrollsToInventory(baseName, wantedAmmountOfScrolls);
            }

            var numberOfScrolls = GetItemInventoryCount(portalScrollStackElements);
            var numberOfStacks = portalScrollStackElements.Count;

            if (numberOfScrolls == wantedAmmountOfScrolls)
            {
                if (numberOfStacks > 1)
                {
                    // Combine the stacks.
                }

                return;
            }

            if (numberOfScrolls < wantedAmmountOfScrolls)
            {
                // We have atleast 1 scroll in our inventory, combine the stacks.
                if (numberOfStacks > 1)
                {
                    // Combine the stacks.
                }

                TransferScrollsToInventory(baseName, wantedAmmountOfScrolls - numberOfScrolls);
                return;
            }

            if (numberOfScrolls > wantedAmmountOfScrolls)
            {
                // Combine the stacks.
            }
        }*/

        #endregion

        public void Stashie()
        {
            // Instead of just itterating through each itemToLookFor in our inventory panel and switching to it's corresponding tab
            // we, itterate through the items in the inventory and place them in a list with position and the corresponding tab it should be moved to,
            // then we sort the list by stash tab stashTabIndex (0 to number of stashes)
            // and then we move the items.

            var itemsToMove = new Dictionary<int, List<Element>>();
            var inventoryPanel = GetInventoryPanel();

            var cursorPosPreMoving = Mouse.GetCursorPosition();

            // Make sure we have the right amount of Wisdom - & Portal Scrolls.


            itemsToMove = AssignIndexesToInventoryItems(itemsToMove, inventoryPanel);

            var sortedItemsToMove = itemsToMove.OrderBy(x => x.Key);

            //WinApi.BlockInput(true);

            // Now we know where each itemToLookFor in the inventory needs to be.
            foreach (var keyValuePair in sortedItemsToMove.ToList())
            {
                List<NormalInventoryItem> sortedItems;
                try
                {
                    sortedItems = keyValuePair.Value.Select(element => element.AsObject<NormalInventoryItem>())
                        .ToList();
                }
                catch
                {
                    continue;
                }
                GoToTab(keyValuePair.Key);
                SortTab(keyValuePair.Key, sortedItems);
            }

            MoveMousePoint(cursorPosPreMoving);

            //WinApi.BlockInput(false);
        }

        private Dictionary<int, List<Element>> AssignIndexesToInventoryItems(Dictionary<int, List<Element>> itemsToMove,
            Element inventoryPanel)
        {
            _movePortalScrolls = true;
            _moveWisdomScrolls = true;
            foreach (var element in inventoryPanel.Children.ToList())
            {
                itemsToMove = AssignIndexToItem(element, itemsToMove);
            }

            return itemsToMove;
        }

        public bool IsCellIgnored(RectangleF position)
        {
            var inventoryPanel = GetInventoryPanel();
            var invPoint = inventoryPanel.GetClientRect();
            var wCell = invPoint.Width / 12;
            var hCell = invPoint.Height / 5;
            var x = (int) ((1f + position.X - invPoint.X) / wCell);
            var y = (int) ((1f + position.Y - invPoint.Y) / hCell);

            return _settings.IgnoredCells[y, x] != Constants.Free;
        }

        private void SortTab(int stashTabIndex, List<NormalInventoryItem> sortedItems)
        {
            var latency = (int) GameController.Game.IngameState.CurLatency;

            // If the user don't want to sort the items that are put into the stash.
            if (!Settings.SortingSettings.Value)
            {
                // Then move them into the stash, and return.
                MoveItemsToStash(latency, sortedItems);
                return;
            }

            // If we have space enough, to sort stash tab and inventory items, in our inventory, then do that.
            var stashTab = GetStashTab(stashTabIndex);
            var stashTabInventoryType = stashTab.InvType;

            if (Settings.StashTabToInventory.Value && stashTabInventoryType == InventoryType.NormalStash)
            {
                var numberOfUsedCellsInStashTab = NumberOfUsedCellsInStashTab(stashTabIndex);
                var spaceEnoughToSortItemsInInventoryPanel =
                    numberOfUsedCellsInStashTab <= NumberOfFreeCellsInInventory();

                if (spaceEnoughToSortItemsInInventoryPanel && numberOfUsedCellsInStashTab > 0)
                {
                    LogMessage($"UsedCellsInStashTab: {numberOfUsedCellsInStashTab}\n" +
                               $"FreeCellsInInventory: {NumberOfFreeCellsInInventory()}", 10);
                    MoveItemsFromStashToInventory(stashTab.VisibleInventoryItems);
                    Thread.Sleep(latency + 150);
                    // TODO:We don't want to rely on a delay. Instead we should check: inventoryItemsPreMoving.count + stashTabItems.count == inventoryItemsPostMoving.count
                    Stashie();
                    return;
                }
            }

            // Sort items
            sortedItems = SortListOfItemsAccordingToUserSettings(stashTabIndex, sortedItems);

            MoveItemsToStash(latency, sortedItems);
        }

        private Inventory GetStashTab(int index)
        {
            var stashTab = GameController.Game.IngameState.ServerData.StashPanel.GetStashInventoryByIndex(index);
            while (stashTab == null)
            {
                stashTab =
                    GameController.Game.IngameState.ServerData.StashPanel.GetStashInventoryByIndex(index);
                Thread.Sleep(WhileDelay);
            }

            return stashTab;
        }

        private List<NormalInventoryItem> SortListOfItemsAccordingToUserSettings(int index,
            List<NormalInventoryItem> sortedItems)
        {
            var stashTab = GetStashTab(index);
            var stashTabInventoryType = stashTab.InvType;

            if (stashTabInventoryType != InventoryType.NormalStash)
            {
                // If the tab is a currency tab, divination card tab or essence tab, then we don't have to sort anything.
                return sortedItems;
            }

            if (Settings.SortyByUniqueName.Value)
            {
                sortedItems = sortedItems.OrderBy(element => element.AsObject<NormalInventoryItem>().Item
                    .GetComponent<Mods>().UniqueName).ToList();
            }

            if (Settings.SortyByClassName.Value)
            {
                sortedItems = sortedItems
                    .OrderBy(element => GameController.Files.BaseItemTypes.Translate(element
                        .AsObject<NormalInventoryItem>().Item.Path).ClassName).ToList();
            }

            if (Settings.SortyByRarity.Value)
            {
                sortedItems = sortedItems.OrderBy(element => element.AsObject<NormalInventoryItem>().Item
                    .GetComponent<Mods>().ItemRarity).ToList();
            }
            if (Settings.SortByBaseName.Value)
            {
                sortedItems = sortedItems
                    .OrderBy(element => GameController.Files.BaseItemTypes.Translate(element
                        .AsObject<NormalInventoryItem>().Item.Path).BaseName).ToList();
            }

            return sortedItems;
        }

        private void MoveItemsToStash(int latency, IEnumerable<NormalInventoryItem> sortedItems)
        {
            var minLatency = latency * 2 > 50 ? latency * 2 : 50 + latency;
            minLatency += Settings.LatencySlider.Value;

            _keyboard.KeyDown(VirtualKeyCode.CONTROL);
            foreach (var item in sortedItems)
            {
                Thread.Sleep(minLatency);
                MoveMouseToCenterOfRec(item.GetClientRect());
                Thread.Sleep(InputDelay);
                _mouse.LeftButtonClick();
            }
            _keyboard.KeyUp(VirtualKeyCode.CONTROL);
            Thread.Sleep(latency + 100);
        }

        private void FirstTab()
        {
            var latency = (int) GameController.Game.IngameState.CurLatency + Settings.LatencySlider.Value;
            while (GameController.Game.IngameState.ServerData.StashPanel.GetStashInventoryByIndex(0) == null ||
                   !GameController.Game.IngameState.ServerData.StashPanel.GetStashInventoryByIndex(0)
                       .AsObject<Element>()
                       .IsVisible)
            {
                _keyboard.KeyPress(VirtualKeyCode.LEFT);
                Thread.Sleep(latency);
            }
        }

        private void GoToTabLeftArrow(int tabIndex)
        {
            var latency = (int) GameController.Game.IngameState.CurLatency + Settings.LatencySlider.Value;
            var numberOfStashes = GameController.Game.IngameState.ServerData.StashPanel.TotalStashes - 1;
            FirstTab();
            for (var i = 0; i < numberOfStashes; i++)
            {
                if (i == tabIndex)
                {
                    break;
                }

                while (GameController.Game.IngameState.ServerData.StashPanel.GetStashInventoryByIndex(i) == null ||
                       !GameController.Game.IngameState.ServerData.StashPanel.GetStashInventoryByIndex(i)
                           .AsObject<Element>()
                           .IsVisible)
                {
                    Thread.Sleep(latency);
                }
                _keyboard.KeyPress(VirtualKeyCode.RIGHT);
            }
        }

        private void GoToTab(int tabIndex)
        {
            if (tabIndex > 30)
            {
                LogError(
                    $"WARNING (can be ignored): {tabIndex}. tab requested, using old method since it's greater than 30 which requires scrolling!\n\tHint, it's suggested to use tabs under stashTabIndex 30.",
                    5);
                GoToTabLeftArrow(tabIndex);
                return;
            }

            try
            {
                var latency = (int) (GameController.Game.IngameState.CurLatency + Settings.LatencySlider.Value);

                // Obs, this method only works with 31 stashtabs on 1920x1080, since you have to scroll at 32 tabs, and the frame stays in place.
                var openLeftPanel = GameController.Game.IngameState.IngameUi.OpenLeftPanel;
                var viewAllTabsButton = GameController.Game.IngameState.UIRoot.Children[1].Children[21].Children[2]
                    .Children[0]
                    .Children[1].Children[2];


                var parent = openLeftPanel.Children[2].Children[0].Children[1].Children[3];
                var element = Settings.IndexVersion.Value ? parent.Children[1] : parent.Children[2];

                if (!element.IsVisible)
                {
                    MoveMouseToCenterOfRec(viewAllTabsButton.GetClientRect());
                    Thread.Sleep(InputDelay);
                    _mouse.LeftButtonClick();
                    var sw = new Stopwatch();
                    sw.Start();
                    while (!element.IsVisible)
                    {
                        Thread.Sleep(WhileDelay);
                    }
                    _mouse.VerticalScroll(5);
                    Thread.Sleep(latency + 50);
                }

                var tabPos = element.Children[tabIndex].GetClientRect();
                MoveMouseToCenterOfRec(tabPos);
                Thread.Sleep(InputDelay);
                _mouse.LeftButtonClick();
            }
            catch (Exception e)
            {
                LogError($"Error in GoToTab: {e}", 5);
            }
        }

        private void MoveMouseToCenterOfRec(RectangleF to)
        {
            var gameWindow = GameController.Window.GetWindowRectangle();
            var x = (int) (gameWindow.X + to.X + to.Width / 2);
            var y = (int) (gameWindow.Y + to.Y + to.Height / 2);
            Mouse.SetCursorPos(x, y);
        }

        private void MoveMousePoint(POINT to)
        {
            var gameWindow = GameController.Window.GetWindowRectangle();
            var x = (int) gameWindow.X + to.X;
            var y = (int) gameWindow.Y + to.Y;
            Mouse.SetCursorPos(x, y);
        }

        public RectangleF FindEmptyOneCell(Element element)
        {
            var latency = (int) GameController.Game.IngameState.CurLatency;
            Thread.Sleep(latency + 50);
            var inventoryPanel = GetInventoryPanel();
            var inventoryRec = inventoryPanel.GetClientRect();
            var topLeftX = inventoryRec.TopLeft.X;
            var topLeftY = inventoryRec.TopLeft.Y;

            var borderSize = 10;
            var widthCell = inventoryRec.Width / 12 - borderSize;
            var heightCell = inventoryRec.Height / 5 - borderSize;

            var elementCellWidth = (int) (element.GetClientRect().Width / widthCell);
            var elementCellHeight = (int) (element.GetClientRect().Height / heightCell);

            for (var widthDirection = 0; widthDirection < 12; widthDirection++)
            {
                for (var heightDirection = 0; heightDirection < 5; heightDirection++)
                {
                    var x = (widthCell + borderSize) * widthDirection + borderSize * 0.5f + topLeftX;
                    var y = (heightCell + borderSize) * heightDirection + borderSize * 0.5f + topLeftY;

                    var cell = new RectangleF(x,
                        y,
                        widthCell, // Width
                        heightCell); // Height
                    var doesContain = inventoryPanel.Children.Any(child => cell.Intersects(child.GetClientRect()));
                    if (doesContain || _settings.IgnoredCells[heightDirection, widthDirection] != 0)
                    {
                        continue;
                    }

                    if (elementCellWidth == 1 && elementCellHeight == 1)
                    {
                        return cell;
                    }
                }
            }
            // There might be a possible space, if we move arround other items. (TODO:Implement in the future)
            return new RectangleF(-1, -1, -1, -1);
        }

        public int NumberOfFreeCellsInInventory()
        {
            var numberOfFreeCells = 0;
            var latency = (int) GameController.Game.IngameState.CurLatency;
            Thread.Sleep(latency + 50);
            var inventoryPanel = GetInventoryPanel();
            var inventoryRec = inventoryPanel.GetClientRect();

            var topLeftX = inventoryRec.TopLeft.X;
            var topLeftY = inventoryRec.TopLeft.Y;

            var borderSize = 10;
            var widthCell = inventoryRec.Width / 12 - borderSize;
            var heightCell = inventoryRec.Height / 5 - borderSize;

            for (var widthDirection = 0; widthDirection < 12; widthDirection++)
            {
                for (var heightDirection = 0; heightDirection < 5; heightDirection++)
                {
                    var x = (widthCell + borderSize) * widthDirection + borderSize * 0.5f + topLeftX;
                    var y = (heightCell + borderSize) * heightDirection + borderSize * 0.5f + topLeftY;

                    var cell = new RectangleF(x,
                        y,
                        widthCell, // Width
                        heightCell); // Height
                    var cellContainsItem = inventoryPanel.Children.Any(child => cell.Intersects(child.GetClientRect()));
                    if (!cellContainsItem)
                    {
                        numberOfFreeCells++;
                    }
                }
            }

            return numberOfFreeCells;
        }

        public int NumberOfUsedCellsInStashTab(int index)
        {
            var numberOfUsedCells = 0;
            if (index < 0 || index > 31)
            {
                LogMessage($"stashTabIndex: {index}", 1);
                return 0;
            }

            try
            {
                var stashTab = GameController.Game.IngameState.ServerData.StashPanel.GetStashInventoryByIndex(index);

                while (stashTab == null)
                {
                    stashTab = GameController.Game.IngameState.ServerData.StashPanel.GetStashInventoryByIndex(index);
                    Thread.Sleep(WhileDelay);
                }

                var stashInventoryItems = stashTab.VisibleInventoryItems;
                var stashTabArea = stashTab.InventoryRootElement.GetClientRect();

                var topLeftX = stashTabArea.TopLeft.X;
                var topLeftY = stashTabArea.TopLeft.Y;

                var borderSize = 10;
                var widthCell = stashTabArea.Width / 12 - borderSize;
                var heightCell = stashTabArea.Height / 12 - borderSize;

                for (var widthDirection = 0; widthDirection < 12; widthDirection++)
                {
                    for (var heightDirection = 0; heightDirection < 12; heightDirection++)
                    {
                        var x = (widthCell + borderSize) * widthDirection + borderSize * 0.5f + topLeftX;
                        var y = (heightCell + borderSize) * heightDirection + borderSize * 0.5f + topLeftY;

                        var cell = new RectangleF(x,
                            y,
                            widthCell, // Width
                            heightCell); // Height
                        var cellContainsItem = stashInventoryItems.Any(child => cell.Intersects(child.GetClientRect()));
                        if (cellContainsItem)
                        {
                            numberOfUsedCells++;
                        }
                    }
                }
            }
            catch
            {
                return numberOfUsedCells;
            }

            return numberOfUsedCells;
        }

        private void MoveItemsFromStashToInventory(IEnumerable<NormalInventoryItem> stashItems)
        {
            var latency = (int) GameController.Game.IngameState.CurLatency + Settings.LatencySlider.Value;

            //winApi.BlockInput(true);
            _keyboard.KeyDown(VirtualKeyCode.CONTROL);
            foreach (var item in stashItems.ToList())
            {
                MoveMouseToCenterOfRec(item.GetClientRect());
                Thread.Sleep(latency);
                _mouse.LeftButtonClick();
                Thread.Sleep(latency);
            }
            _keyboard.KeyUp(VirtualKeyCode.CONTROL);
            //winApi.BlockInput(false);
        }

        public void TestFunction()
        {
            var inventoryPanel = GetInventoryPanel();
            var inventoryRec = inventoryPanel.GetClientRect();
            var topLeftX = inventoryRec.TopLeft.X;
            var topLeftY = inventoryRec.TopLeft.Y;

            var borderSize = 10;
            var widthCell = inventoryRec.Width / 12 - borderSize;
            var heightCell = inventoryRec.Height / 5 - borderSize;


            for (var widthDirection = 0; widthDirection < 12; widthDirection++)
            {
                for (var heightDirection = 0; heightDirection < 5; heightDirection++)
                {
                    var x = (widthCell + borderSize) * widthDirection + borderSize * 0.5f + topLeftX;
                    var y = (heightCell + borderSize) * heightDirection + borderSize * 0.5f + topLeftY;

                    var cell = new RectangleF(x,
                        y,
                        widthCell, // Width
                        heightCell); // Height
                    var doesContain = inventoryPanel.Children.Any(child => cell.Intersects(child.GetClientRect()));
                    if (!doesContain && _settings.IgnoredCells[heightDirection, widthDirection] == 0)
                    {
                        Graphics.DrawBox(cell, new Color(new Vector3(255, 255, 255), 0.5f));
                    }
                }
            }
        }

        private Element GetInventoryPanel()
        {
            return GameController.Game.IngameState.ReadObject<Element>(
                GameController.Game.IngameState.IngameUi.InventoryPanel.Address +
                Element.OffsetBuffers + 0x42C);
        }

        private Dictionary<int, List<Element>> AssignIndexToItem(Element element,
            Dictionary<int, List<Element>> itemsToMove)
        {
            var itemPos = element.GetClientRect();
            var tabIndex = 9999; // magic number, that will never occur naturally.

            var item = element.AsObject<NormalInventoryItem>().Item;
            var baseItemType = GameController.Files.BaseItemTypes.Translate(item.Path);
            var baseName = baseItemType.BaseName;
            var className = baseItemType.ClassName;
            var rarity = item.GetComponent<Mods>().ItemRarity;
            var quality = item.GetComponent<Quality>().ItemQuality;
            var iLvl = item.GetComponent<Mods>().ItemLevel;

            #region Quest Items

            if (item.Path.ToLower().Contains("quest"))
            {
                return itemsToMove;
            }

            #endregion

            #region Portal and Wisdom Scrolls in Ignored Cells

            if (baseName.Equals("Scroll of Wisdom"))
            {
                if (item.GetComponent<Stack>().Size == Settings.WisdomScrolls.Value)
                {
                    _moveWisdomScrolls = false;
                    return itemsToMove;
                }

                tabIndex = Settings.Currency.Value;

                #region deprectated

                /*var stack = item.GetComponent<Stack>();

                var wantedStackSize = item.Path.Contains("CurrencyPortal")
                    ? Settings.PortalScrolls.Value
                    : Settings.WisdomScrolls.Value;

                if (stack.Size > wantedStackSize)
                {
                    // Split
                    var freeCell = FindEmptyOneCell(element);
                    if (freeCell.X <= -1)
                    {
                        // No free space was found.
                        // Todo: Add it to a list, after all the other items has been move to the stash, then we should check if there's a free space again.
                        LogError("Couldn't find a free cell!", 5);
                        return itemsToMove;
                    }

                    SplitStackAndMoveToFreeCell(element, latency, stack, wantedStackSize, freeCell);

                    //WinApi.BlockInput(false);

                    #region Add new itemToLookFor to dictionary.

                    index = Settings.Currency.Value;

                    if (!itemsToMove.ContainsKey(index))
                    {
                        itemsToMove.Add(index, new List<Element>());
                    }

                    Thread.Sleep(latency + 50);


                    var newInventoryPanel = GetInventoryPanel();
                    var movedItemStack =
                        newInventoryPanel.Children.FirstOrDefault(x => x.GetClientRect().Intersects(freeCell));

                    itemsToMove[index].Add(movedItemStack);

                    #endregion
                }*/

                #endregion
            }

            else if (baseName.Equals("Portal Scroll"))
            {
                if (item.GetComponent<Stack>().Size == Settings.PortalScrolls.Value)
                {
                    _movePortalScrolls = false;
                    return itemsToMove;
                }

                tabIndex = Settings.Currency.Value;
            }

            #endregion

            #region Ignored Cell

            else if (IsCellIgnored(itemPos))
            {
                return itemsToMove;
            }

            #endregion

            // Handle items based of user settings.

            #region Maps

            else if (className.Equals("Map"))
            {
                #region Shaped Shore

                if (baseName.Equals("Shaped Shore Map"))
                {
                    tabIndex = Settings.ShoreShaped.Value;
                }

                #endregion

                #region Shaped Strand

                else if (baseName.Equals("Shaped Strand Map"))
                {
                    tabIndex = Settings.StrandShaped.Value;
                }

                #endregion

                #region Shaped Maps

                else if (baseName.Contains("Shaped"))
                {
                    tabIndex = Settings.ShapedMaps.Value;
                }

                #endregion

                #region Other Maps and Unique Maps

                else
                {
                    tabIndex = rarity == ItemRarity.Unique ? Settings.UniqueMaps : Settings.OtherMaps;
                }

                #endregion
            }

            #endregion

            #region Chance Items

            #region Sorcerer Boots

            else if (baseName.Equals("Sorcerer Boots") && rarity == ItemRarity.Normal &&
                     Settings.ChanceItemTabs.Value)
            {
                tabIndex = Settings.SorcererBoots.Value;
            }

            #endregion

            #region Leather Belt

            else if (baseName.Equals("Leather Belt") && rarity == ItemRarity.Normal &&
                     Settings.ChanceItemTabs.Value)
            {
                tabIndex = Settings.LeatherBelt.Value;
            }

            #endregion

            #endregion

            #region Vendor Recipes

            #region Chisel Recipe

            else if (baseName.Equals("Stone Hammer") || baseName.Equals("Rock Breaker") ||
                     baseName.Equals("Gavel") && quality == 20)
            {
                tabIndex = Settings.ChiselRecipe.Value;
            }

            #endregion

            #region Chaos Recipes

            else if (Settings.VendorRecipeTabs.Value && item.GetComponent<Mods>().ItemRarity == ItemRarity.Rare &&
                     !className.Equals("Jewel") && iLvl >= 60 && iLvl <= 74)
            {
                var mods = item.GetComponent<Mods>();
                if (mods.Identified && quality < 20)
                {
                    tabIndex = Settings.ChaosRecipeLvlOne.Value;
                }
                else if (!mods.Identified || quality == 20)
                {
                    tabIndex = Settings.ChaosRecipeLvlTwo.Value;
                }
                else if (!mods.Identified && quality == 20)
                {
                    tabIndex = Settings.ChaosRecipeLvlThree.Value;
                }
            }

            #endregion

            #region Quality Gems

            else if (className.Contains("Skill Gem") && quality > 0)
            {
                tabIndex = Settings.QualityGems.Value;
            }

            #endregion

            #region Quality Flasks

            else if (className.Contains("Flask") && quality > 0)
            {
                tabIndex = Settings.QualityFlasks.Value;
            }

            #endregion

            #endregion

            #region Default Tabs

            #region Divination Cards

            else if (className.Equals("DivinationCard"))
            {
                tabIndex = Settings.DivinationCards.Value;
            }

            #endregion

            #region Gems

            else if (className.Contains("Skill Gem") && quality == 0)
            {
                tabIndex = Settings.Gems.Value;
            }

            #endregion

            #region Currency

            else if (className.Equals("StackableCurrency") && !item.Path.Contains("Essence"))
            {
                tabIndex = Settings.Currency.Value;
            }

            #endregion

            #region Leaguestone

            else if (className.Equals("Leaguestone"))
            {
                tabIndex = Settings.LeagueStones.Value;
            }

            #endregion

            #region Essence

            else if (baseName.Contains("Essence") && className.Equals("StackableCurrency"))
            {
                tabIndex = Settings.Essences.Value;
            }

            #endregion

            #region Jewels

            else if (className.Equals("Jewel"))
            {
                tabIndex = Settings.Jewels.Value;
            }

            #endregion

            #region Flasks

            else if (className.Contains("Flask") && quality == 0)
            {
                tabIndex = Settings.Flasks.Value;
            }

            #endregion

            #region Talisman

            else if (className.Equals("Amulet") && baseName.Contains("Talisman"))
            {
                tabIndex = Settings.Talismen.Value;
            }

            #endregion

            #region jewelery

            else if (className.Equals("Amulet") || className.Equals("Ring"))
            {
                tabIndex = Settings.Jewelery.Value;
            }

            #endregion

            #endregion

            #region White Items

            else if (rarity == ItemRarity.Normal)
            {
                tabIndex = Settings.WhiteItems.Value;
            }

            #endregion

            if (tabIndex == 9999)
            {
                return itemsToMove;
            }

            #region Add itemToLookFor with corresponding Index to ItemsToMove

            if (!itemsToMove.ContainsKey(tabIndex))
            {
                itemsToMove.Add(tabIndex, new List<Element>());
            }

            itemsToMove[tabIndex].Add(element);

            #endregion

            return itemsToMove;
        }

        private void SplitStackAndMoveToFreeCell(Element element, int latency, int wantedStackSize,
            RectangleF freeCell)
        {
            var numberOfItemsInAStackToMove = wantedStackSize;

            //WinApi.BlockInput(true);

            #region Move Mouse to Item that needs to be splitted.

            MoveMouseToCenterOfRec(element.GetClientRect());
            Thread.Sleep(InputDelay);

            #endregion

            #region Shift + Left Click.

            _keyboard.KeyDown(VirtualKeyCode.SHIFT);
            Thread.Sleep(InputDelay);
            _mouse.LeftButtonClick();
            _keyboard.KeyUp(VirtualKeyCode.SHIFT);

            #endregion

            #region Enter split size.

            if (numberOfItemsInAStackToMove < 10)
            {
                var keyToPress = (VirtualKeyCode) ((int) VirtualKeyCode.VK_0 + numberOfItemsInAStackToMove);
                _keyboard.KeyPress(keyToPress);
            }
            else
            {
                var keyToPress =
                    (VirtualKeyCode) ((int) VirtualKeyCode.VK_0 + (numberOfItemsInAStackToMove / 10));
                _keyboard.KeyPress(keyToPress);
                Thread.Sleep(latency);
                keyToPress =
                    (VirtualKeyCode) ((int) VirtualKeyCode.VK_0 + (numberOfItemsInAStackToMove % 10));
                _keyboard.KeyPress(keyToPress);
            }

            #endregion

            #region Press Enter

            Thread.Sleep(latency + 50);
            _keyboard.KeyPress(VirtualKeyCode.RETURN);
            Thread.Sleep(latency + 50);

            #endregion

            #region Move Cursor to Empty Cell

            MoveMouseToCenterOfRec(freeCell);
            Thread.Sleep(latency + 50);
            _mouse.LeftButtonClick();

            #endregion
        }

        /*private int GetItemInventoryCount(IEnumerable<IEntity> items)
        {
            return items.ToList().Sum(item => item.GetComponent<Stack>().Size);
        }*/
    }
}