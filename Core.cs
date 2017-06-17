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
                    {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
                    {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
                    {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
                    {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
                    {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}
                };

                _settings.Hotkey = "0x72";

                var defaultSettings = JsonConvert.SerializeObject(_settings);

                defaultSettings = defaultSettings.Replace("],[", "],\n\t\t[");
                defaultSettings = defaultSettings.Replace(":[", ":[\n\t\t");
                defaultSettings = defaultSettings.Replace("{", "{\n\t");
                defaultSettings = defaultSettings.Replace("}", "\n}");
                defaultSettings = defaultSettings.Replace(",\"", ",\n\t\"");
                defaultSettings = defaultSettings.Replace("]]", "]\n\t]");
                defaultSettings = defaultSettings.Replace("\"Hotkey\"",
                    "\n\t// Write the hex value of your desired hotkey.\n\t//https://msdn.microsoft.com/en-us/library/windows/desktop/dd375731(v=vs.85).aspx \n\t\"Hotkey\"");
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

            // Hotkey settings
            Settings.HotkeySetting.OnValueChanged += ChangeHotkey;
        }

        private void ChangeHotkey()
        {
            if (!Settings.HotkeySetting.Value)
            {
                return;
            }

            // TODO: if it's true, prompt the user to type in the hotkey.
            LogMessage($"Feature not implemented, hotkey is {_settings.Hotkey}!", 10);
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

            if (Settings.HotkeyRequired.Value && !_input.InputDeviceState.IsKeyDown((VirtualKeyCode) Convert.ToUInt32(_settings.Hotkey, 16)))
            {
                return;
            }

            Stashie();
            _moveItemsToStash = false;
        }

        public void Stashie()
        {
            // Instead of just itterating through each item in our inventory panel and switching to it's corresponding tab
            // we, itterate through the items in the inventory and place them in a list with position and the corresponding tab it should be moved to,
            // then we sort the list by stash tab stashTabIndex (0 to number of stashes)
            // and then we move the items.

            var itemsToMove = new Dictionary<int, List<Element>>();
            var inventoryPanel = GetInventoryPanel();

            var cursorPosPreMoving = Mouse.GetCursorPosition();

            itemsToMove = AssignIndexesToInventoryItems(itemsToMove, inventoryPanel);

            var sortedItemsToMove = itemsToMove.OrderBy(x => x.Key);

            //WinApi.BlockInput(true);

            // Now we know where each item in the inventory needs to be.
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

            MoveMousePoint(cursorPosPreMoving, Mouse.GetCursorPosition());

            //WinApi.BlockInput(false);
        }

        private Dictionary<int, List<Element>> AssignIndexesToInventoryItems(Dictionary<int, List<Element>> itemsToMove,
            Element inventoryPanel)
        {
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

            return _settings.IgnoredCells[y, x] == 1;
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

            var numberOfUsedCellsInStashTab = NumberOfUsedCellsInStashTab(stashTabIndex);
            var spaceEnoughToSortItemsInInventoryPanel =
                numberOfUsedCellsInStashTab <= NumberOfFreeCellsInInventory();

            if (Settings.SortingSettings.Value && stashTabInventoryType == InventoryType.NormalStash &&
                spaceEnoughToSortItemsInInventoryPanel && numberOfUsedCellsInStashTab > 0)
            {
                LogMessage($"UsedCellsInStashTab: {numberOfUsedCellsInStashTab}\n" +
                           $"FreeCellsInInventory: {NumberOfFreeCellsInInventory()}", 10);
                MoveItemsFromStashToInventory(stashTab.VisibleInventoryItems);
                Thread.Sleep(latency + 150);
                // TODO:We don't want to rely on a delay. Instead we should check: inventoryItemsPreMoving.count + stashTabItems.count == inventoryItemsPostMoving.count
                Stashie();
                return;
            }

            // Sort items
            sortedItems = SortListOfItemsAccordingToUserSettings(stashTabIndex, sortedItems);

            MoveItemsToStash(latency, sortedItems);
        }

        private Inventory GetStashTab(int index)
        {
            var stashTab = GameController.Game.IngameState.ServerData.StashPanel.getStashInventory(index);
            while (stashTab == null)
            {
                stashTab =
                    GameController.Game.IngameState.ServerData.StashPanel.getStashInventory(index);
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
                MoveMouseToCenterOfRec(item.GetClientRect(), Mouse.GetCursorPosition());
                Thread.Sleep(InputDelay);
                _mouse.LeftButtonClick();
            }
            _keyboard.KeyUp(VirtualKeyCode.CONTROL);
            Thread.Sleep(latency + 100);
        }

        private void FirstTab()
        {
            var latency = (int) GameController.Game.IngameState.CurLatency + Settings.LatencySlider.Value;
            while (GameController.Game.IngameState.ServerData.StashPanel.getStashInventory(0) == null ||
                   !GameController.Game.IngameState.ServerData.StashPanel.getStashInventory(0)
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

                while (GameController.Game.IngameState.ServerData.StashPanel.getStashInventory(i) == null ||
                       !GameController.Game.IngameState.ServerData.StashPanel.getStashInventory(i)
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
                    MoveMouseToCenterOfRec(viewAllTabsButton.GetClientRect(), Mouse.GetCursorPosition());
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
                MoveMouseToCenterOfRec(tabPos, Mouse.GetCursorPosition());
                Thread.Sleep(InputDelay);
                _mouse.LeftButtonClick();
                //Thread.Sleep(latency * 2);
            }
            catch (Exception e)
            {
                LogError($"Error in GoToTab: {e}", 5);
            }
        }

        private void MoveMouseToCenterOfRec(RectangleF to, POINT from)
        {
            var gameWindow = GameController.Window.GetWindowRectangle();
            var deltaX = (int) (gameWindow.X + to.X + to.Width / 2 - from.X);
            var deltaY = (int) (gameWindow.Y + to.Y + to.Height / 2 - from.Y);
            _mouse.MoveMouseBy(deltaX, deltaY);
        }

        private void MoveMousePoint(POINT to, POINT from)
        {
            var gameWindow = GameController.Window.GetWindowRectangle();
            var deltaX = (int) gameWindow.X + to.X - from.X;
            var deltaY = (int) gameWindow.Y + to.Y - from.Y;
            _mouse.MoveMouseBy(deltaX, deltaY);
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
                var stashTab = GameController.Game.IngameState.ServerData.StashPanel.getStashInventory(index);

                while (stashTab == null)
                {
                    stashTab = GameController.Game.IngameState.ServerData.StashPanel.getStashInventory(index);
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
                MoveMouseToCenterOfRec(item.GetClientRect(), Mouse.GetCursorPosition());
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
            var index = 9999; // magic number, that will never occur naturally.

            var item = element.AsObject<NormalInventoryItem>().Item;
            var baseItemType = GameController.Files.BaseItemTypes.Translate(item.Path);
            var baseName = baseItemType.BaseName;
            var className = baseItemType.ClassName;
            var rarity = item.GetComponent<Mods>().ItemRarity;
            var quality = item.GetComponent<Quality>().ItemQuality;
            var iLvl = item.GetComponent<Mods>().ItemLevel;

            var latency = (int) GameController.Game.IngameState.CurLatency;
            //LogMessage($"base: {baseName}, class: {className}", 5);  

            #region Quest Items

            if (item.Path.ToLower().Contains("quest"))
            {
                return itemsToMove;
            }

            #endregion

            #region Portal and Wisdom Scrolls in Ignored Cells

            if ((baseName.Equals("Scroll of Wisdom") || baseName.Equals("Portal Scroll")) &&
                IsCellIgnored(element.GetClientRect()))
            {
                var stack = item.GetComponent<Stack>();

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

                    #region Add new item to dictionary.

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
                }
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
                    index = Settings.ShoreShaped.Value;
                }

                #endregion

                #region Shaped Strand

                else if (baseName.Equals("Shaped Strand Map"))
                {
                    index = Settings.StrandShaped.Value;
                }

                #endregion

                #region Shaped Maps

                else if (baseName.Contains("Shaped"))
                {
                    index = Settings.ShapedMaps.Value;
                }

                #endregion

                #region Other Maps and Unique Maps

                else
                {
                    index = rarity == ItemRarity.Unique ? Settings.UniqueMaps : Settings.OtherMaps;
                }

                #endregion
            }

            #endregion

            #region Chance Items

            #region Sorcerer Boots

            else if (baseName.Equals("Sorcerer Boots") && rarity == ItemRarity.Normal &&
                     Settings.ChanceItemTabs.Value)
            {
                index = Settings.SorcererBoots.Value;
            }

            #endregion

            #region Leather Belt

            else if (baseName.Equals("Leather Belt") && rarity == ItemRarity.Normal &&
                     Settings.ChanceItemTabs.Value)
            {
                index = Settings.LeatherBelt.Value;
            }

            #endregion

            #endregion

            #region Vendor Recipes

            #region Chisel Recipe

            else if (baseName.Equals("Stone Hammer") || baseName.Equals("Rock Breaker") ||
                     baseName.Equals("Gavel") && quality == 20)
            {
                index = Settings.ChiselRecipe.Value;
            }

            #endregion

            #region Chaos Recipes

            else if (Settings.VendorRecipeTabs.Value && item.GetComponent<Mods>().ItemRarity == ItemRarity.Rare &&
                     !className.Equals("Jewel") && iLvl >= 60 && iLvl <= 74)
            {
                var mods = item.GetComponent<Mods>();
                if (mods.Identified && quality < 20)
                {
                    index = Settings.ChaosRecipeLvlOne.Value;
                }
                else if (!mods.Identified || quality == 20)
                {
                    index = Settings.ChaosRecipeLvlTwo.Value;
                }
                else if (!mods.Identified && quality == 20)
                {
                    index = Settings.ChaosRecipeLvlThree.Value;
                }
            }

            #endregion

            #region Quality Gems

            else if (className.Contains("Skill Gem") && quality > 0)
            {
                index = Settings.QualityGems.Value;
            }

            #endregion

            #region Quality Flasks

            else if (className.Contains("Flask") && quality > 0)
            {
                index = Settings.QualityFlasks.Value;
            }

            #endregion

            #endregion

            #region Default Tabs

            #region Divination Cards

            else if (className.Equals("DivinationCard"))
            {
                index = Settings.DivinationCards.Value;
            }

            #endregion

            #region Gems

            else if (className.Contains("Skill Gem") && quality == 0)
            {
                index = Settings.Gems.Value;
            }

            #endregion

            #region Currency

            else if (className.Equals("StackableCurrency") && !item.Path.Contains("Essence"))
            {
                index = Settings.Currency.Value;
            }

            #endregion

            #region Leaguestone

            else if (className.Equals("Leaguestone"))
            {
                index = Settings.LeagueStones.Value;
            }

            #endregion

            #region Essence

            else if (baseName.Contains("Essence") && className.Equals("StackableCurrency"))
            {
                index = Settings.Essences.Value;
            }

            #endregion

            #region Jewels

            else if (className.Equals("Jewel"))
            {
                index = Settings.Jewels.Value;
            }

            #endregion

            #region Flasks

            else if (className.Contains("Flask") && quality == 0)
            {
                index = Settings.Flasks.Value;
            }

            #endregion

            #region Talisman

            else if (className.Equals("Amulet") && baseName.Contains("Talisman"))
            {
                index = Settings.Talismen.Value;
            }

            #endregion

            #region jewelery

            else if (className.Equals("Amulet") || className.Equals("Ring"))
            {
                index = Settings.Jewelery.Value;
            }

            #endregion

            #endregion

            #region White Items

            else if (rarity == ItemRarity.Normal)
            {
                index = Settings.WhiteItems.Value;
            }

            #endregion

            if (index == 9999)
            {
                return itemsToMove;
            }

            #region Add item with corresponding Index to ItemsToMove

            if (!itemsToMove.ContainsKey(index))
            {
                itemsToMove.Add(index, new List<Element>());
            }

            itemsToMove[index].Add(element);

            #endregion

            return itemsToMove;
        }

        private void SplitStackAndMoveToFreeCell(Element element, int latency, Stack stack, int wantedStackSize,
            RectangleF freeCell)
        {
            var numberOfItemsInAStackToMove = stack.Size - wantedStackSize;

            //WinApi.BlockInput(true);

            #region Move Mouse to Item that needs to be splitted.

            MoveMouseToCenterOfRec(element.GetClientRect(), Mouse.GetCursorPosition());
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

            MoveMouseToCenterOfRec(freeCell, Mouse.GetCursorPosition());
            Thread.Sleep(latency + 50);
            _mouse.LeftButtonClick();

            #endregion
        }
    }
}