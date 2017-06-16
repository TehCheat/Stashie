using System;
using System.Collections.Generic;
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
using SharpDX;

namespace Stashie
{
    public class Core : BaseSettingsPlugin<Settings>
    {
        private readonly InputSimulator _input = new InputSimulator();
        private readonly MouseSimulator _mouse = new MouseSimulator(new InputSimulator());
        private readonly KeyboardSimulator _keyboard = new KeyboardSimulator(new InputSimulator());

        private bool _moveItemsToStash = true;

        private const int DebugDelay = 30;
        private const int InputDelay = 10;
        private const int WhileDelay = 5;

        // 1, cell is ignored. 0 cells is not ignored.
        private int[,] _ignoredCells = {
            {1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
        };

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
                var defaultSettings = JsonConvert.SerializeObject(_ignoredCells)
                    .Replace("],[", $"],{Environment.NewLine} [");
                File.WriteAllText(path, defaultSettings);
            }
            var json = File.ReadAllText(path);
            _ignoredCells = JsonConvert.DeserializeObject<int[,]>(json);

            // Sets the maximum of the range node to the number of the players total stashes.
            var totalStashes = (int)GameController.Game.IngameState.ServerData.StashPanel.TotalStashes - 1;

            #region Default Tabs

            Settings.Currency.Max = totalStashes;
            Settings.DivinationCards.Max = totalStashes;
            Settings.Essences.Max = totalStashes;

            Settings.Jewels.Max = totalStashes;
            Settings.Gems.Max = totalStashes;
            Settings.LeagueStones.Max = totalStashes;

            Settings.Flasks.Max = totalStashes;
            Settings.Jewelery.Max = totalStashes;
            Settings.WhiteItems.Max = totalStashes; // Todo: Should be expanded to crafting
            Settings.Talismen.Max = totalStashes;

            #endregion

            #region Orb of Chance

            Settings.LeatherBelt.Max = totalStashes;
            Settings.SorcererBoots.Max = totalStashes;

            #endregion

            #region Vendor Recipes

            Settings.ChiselRecipe.Max = totalStashes;
            Settings.ChaosRecipeLvlOne.Max = totalStashes;
            Settings.ChaosRecipeLvlTwo.Max = totalStashes;
            Settings.ChaosRecipeLvlThree.Max = totalStashes;
            Settings.QualityFlasks.Max = totalStashes;
            Settings.QualityGems.Max = totalStashes;

            #endregion

            #region Maps

            Settings.StrandShaped.Max = totalStashes;
            Settings.ShoreShaped.Max = totalStashes;
            Settings.UniqueMaps.Max = totalStashes;
            Settings.OtherMaps.Max = totalStashes;
            Settings.ShapedMaps.Max = totalStashes;

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
            LogMessage("Feature not implemented, hotkey is F3!", 10);
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

            if (Settings.HotkeyRequired.Value && !_input.InputDeviceState.IsKeyDown(VirtualKeyCode.F3))
            {
                return;
            }

            MoveItemsToStash();
            _moveItemsToStash = false;
        }

        public bool IsCellIgnored(RectangleF position)
        {
            var inventoryPanel = GetInventoryElement();
            var invPoint = inventoryPanel.GetClientRect();
            var wCell = invPoint.Width / 12;
            var hCell = invPoint.Height / 5;
            var x = (int)((1f + position.X - invPoint.X) / wCell);
            var y = (int)((1f + position.Y - invPoint.Y) / hCell);

            return _ignoredCells[y, x] == 1;
        }

        /// <summary>
        /// Moves the items from the inventory to the stash, and sorts them.
        /// </summary>
        public void MoveItemsToStash()
        {
            // Instead of just itterating through each item in our inventory panel and switching to it's corresponding tab
            // we, itterate through the items in the inventory and place them in a list with position and the corresponding tab it should be moved to,
            // then we sort the list by stash tab index (0 to number of stashes)
            // and then we move the items.

            var latency = (int)GameController.Game.IngameState.CurLatency;
            var itemsToMove = new Dictionary<int, List<Element>>();

            var inventoryPanel = GetInventoryElement();
            while (inventoryPanel == null)
            {
                inventoryPanel = GetInventoryElement();
                Thread.Sleep(WhileDelay);
            }

            var cursorPosPreMoving = Mouse.GetCursorPosition();

            #region Assign the stashTab index to each item in the inventory that needs to me moved.

            foreach (var element in inventoryPanel.Children.ToList())
            {
                var itemPos = element.GetClientRect();
                var index = 9999; // magic number, that will never occur naturally.
                const int inputDelay = 10;

                var item = element.AsObject<NormalInventoryItem>().Item;
                var baseItemType = GameController.Files.BaseItemTypes.Translate(item.Path);
                var baseName = baseItemType.BaseName;
                var className = baseItemType.ClassName;
                var rarity = item.GetComponent<Mods>().ItemRarity;
                var quality = item.GetComponent<Quality>().ItemQuality;
                var iLvl = item.GetComponent<Mods>().ItemLevel;
                //LogMessage($"base: {baseName}, class: {className}", 5);  

                #region Quest Items

                if (item.Path.ToLower().Contains("quest"))
                {
                    continue;
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
                            continue;
                        }

                        var numberOfItemsInAStackToMove = stack.Size - wantedStackSize;

                        //WinApi.BlockInput(true);

                        #region Move Mouse to Item that needs to be splitted.

                        MoveMouseToCenterOfRec(element.GetClientRect(), Mouse.GetCursorPosition());
                        Thread.Sleep(inputDelay);

                        #endregion

                        #region Shift + Left Click.

                        _keyboard.KeyDown(VirtualKeyCode.SHIFT);
                        Thread.Sleep(inputDelay);
                        _mouse.LeftButtonClick();
                        _keyboard.KeyUp(VirtualKeyCode.SHIFT);

                        #endregion

                        #region Enter split size.

                        if (numberOfItemsInAStackToMove < 10)
                        {
                            var keyToPress = (VirtualKeyCode)((int)VirtualKeyCode.VK_0 + numberOfItemsInAStackToMove);
                            _keyboard.KeyPress(keyToPress);
                        }
                        else
                        {
                            var keyToPress =
                                (VirtualKeyCode)((int)VirtualKeyCode.VK_0 + (numberOfItemsInAStackToMove / 10));
                            _keyboard.KeyPress(keyToPress);
                            Thread.Sleep(latency);
                            keyToPress =
                                (VirtualKeyCode)((int)VirtualKeyCode.VK_0 + (numberOfItemsInAStackToMove % 10));
                            _keyboard.KeyPress(keyToPress);
                        }

                        #endregion

                        #region Press Enter

                        Thread.Sleep(latency + DebugDelay);
                        _keyboard.KeyPress(VirtualKeyCode.RETURN);
                        Thread.Sleep(latency + DebugDelay);

                        #endregion

                        #region Move Cursor to Empty Cell

                        MoveMouseToCenterOfRec(freeCell, Mouse.GetCursorPosition());
                        Thread.Sleep(latency + 50);
                        _mouse.LeftButtonClick();

                        #endregion

                        //                        /WinApi.BlockInput(false);

                        #region Add new item to dictionary.

                        index = Settings.Currency.Value;

                        if (!itemsToMove.ContainsKey(index))
                        {
                            itemsToMove.Add(index, new List<Element>());
                        }

                        Thread.Sleep(latency + DebugDelay);


                        var newInventoryPanel = GetInventoryElement();
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
                    continue;
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
                    continue;
                }

                #region Add item with corresponding Index to ItemsToMove

                if (!itemsToMove.ContainsKey(index))
                {
                    itemsToMove.Add(index, new List<Element>());
                }

                itemsToMove[index].Add(element);

                #endregion
            }

            #endregion

            var sortedItemsToMove = itemsToMove.OrderBy(x => x.Key);

            //WinApi.BlockInput(true);
            foreach (var keyValuePair in sortedItemsToMove.ToList())
            {
                GoToTab(keyValuePair.Key);

                #region StashTab Sorting

                //Thread.Sleep(latency * 2 + _debugDelay);


                var numberOfUsedCellsInStashTab = NumberOfUsedCellsInStashTab(keyValuePair.Key);
                var spaceEnoughToSortItemsInInventoryPanel =
                    numberOfUsedCellsInStashTab <= NumberOfFreeCellsInInventory();

                var stashPanel = GameController.Game.IngameState.ServerData.StashPanel;
                while (stashPanel == null)
                {
                    stashPanel = GameController.Game.IngameState.ServerData.StashPanel;
                    Thread.Sleep(WhileDelay);
                }

                var stashTab =
                    stashPanel.getStashInventory(keyValuePair.Key);
                while (stashTab == null)
                {
                    stashTab =
                        stashPanel.getStashInventory(keyValuePair.Key);
                    Thread.Sleep(WhileDelay);
                }

                var stashTabInventoryType = stashTab.InvType;

                if (Settings.SortingSettings.Value && stashTabInventoryType == InventoryType.NormalStash &&
                    spaceEnoughToSortItemsInInventoryPanel && numberOfUsedCellsInStashTab > 0)
                {
                    LogMessage($"UsedCellsInStashTab: {numberOfUsedCellsInStashTab}\n" +
                               $"FreeCellsInInventory: {NumberOfFreeCellsInInventory()}", 10);
                    MoveItemsFromStashToInventory(stashTab.VisibleInventoryItems);
                    MoveItemsToStash();
                    return;
                }

                var sortedItems = keyValuePair.Value.Select(element => element.AsObject<NormalInventoryItem>())
                    .ToList();

                #endregion

                #region Sort items if it's a normal inventory type (aka not a currency, divination card or essence tab).

                if (stashTabInventoryType == InventoryType.NormalStash)
                {
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
                }

                // Step 3. Ctrl+Leftclick inventory items that needs to go into the tab.

                #endregion

                var minLatency = latency * 2 > 50 ? latency * 2 : 50 + latency;
                _keyboard.KeyDown(VirtualKeyCode.CONTROL);
                foreach (var item in sortedItems)
                {
                    Thread.Sleep(minLatency);
                    MoveMouseToCenterOfRec(item.GetClientRect(), Mouse.GetCursorPosition());
                    Thread.Sleep(InputDelay);
                    _mouse.LeftButtonClick();
                }
                _keyboard.KeyUp(VirtualKeyCode.CONTROL);
                Thread.Sleep(latency + DebugDelay);
            }

            MoveMousePoint(cursorPosPreMoving, Mouse.GetCursorPosition());

            //WinApi.BlockInput(false);
        }

        private void FirstTab()
        {
            var latency = (int)GameController.Game.IngameState.CurLatency;
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
            var latency = (int)GameController.Game.IngameState.CurLatency;
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
                    $"WARNING (can be ignored): {tabIndex}. tab requested, using old method since it's greater than 30 which requires scrolling!\n\tHint, it's suggested to use tabs under index 30.",
                    5);
                GoToTabLeftArrow(tabIndex);
                return;
            }

            try
            {
                var latency = (int)(GameController.Game.IngameState.CurLatency + DebugDelay);

                // Obs, this method only works with 31 stashtabs on 1920x1080, since you have to scroll at 32 tabs, and the frame stays in place.
                var openLeftPanel = GameController.Game.IngameState.IngameUi.OpenLeftPanel;
                var viewAllTabsButton = GameController.Game.IngameState.UIRoot.Children[1].Children[21].Children[2]
                    .Children[0]
                    .Children[1].Children[2];

                var element = openLeftPanel.Children[2].Children[0].Children[1].Children[3]
                    .Children[1];

                if (!element.IsVisible)
                {
                    MoveMouseToCenterOfRec(viewAllTabsButton.GetClientRect(), Mouse.GetCursorPosition());
                    Thread.Sleep(InputDelay);
                    _mouse.LeftButtonClick();
                    Thread.Sleep(latency);
                    _mouse.VerticalScroll(5);
                    Thread.Sleep(latency);
                }

                var tabPos = element.Children[tabIndex].GetClientRect();
                MoveMouseToCenterOfRec(tabPos, Mouse.GetCursorPosition());
                Thread.Sleep(InputDelay);
                _mouse.LeftButtonClick();
                Thread.Sleep(latency);
            }
            catch (Exception e)
            {
                LogError($"Error in GoToTab: {e}", 5);
            }
        }

        private void MoveMouseToCenterOfRec(RectangleF to, POINT from)
        {
            var deltaX = (int)(to.X + to.Width / 2 - from.X);
            var deltaY = (int)(to.Y + to.Height / 2 - from.Y);
            _mouse.MoveMouseBy(deltaX, deltaY);
        }

        private void MoveMousePoint(POINT to, POINT from)
        {
            var deltaX = to.X - from.X;
            var deltaY = to.Y - from.Y;
            _mouse.MoveMouseBy(deltaX, deltaY);
        }

        public RectangleF FindEmptyOneCell(Element element)
        {
            var latency = (int)GameController.Game.IngameState.CurLatency;
            Thread.Sleep(latency + DebugDelay);
            var inventoryPanel = GetInventoryElement();
            var inventoryRec = inventoryPanel.GetClientRect();
            var topLeftX = inventoryRec.TopLeft.X;
            var topLeftY = inventoryRec.TopLeft.Y;

            var borderSize = 10;
            var widthCell = inventoryRec.Width / 12 - borderSize;
            var heightCell = inventoryRec.Height / 5 - borderSize;

            var elementCellWidth = (int)(element.GetClientRect().Width / widthCell);
            var elementCellHeight = (int)(element.GetClientRect().Height / heightCell);

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
                    if (doesContain || _ignoredCells[heightDirection, widthDirection] != 0)
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
            var latency = (int)GameController.Game.IngameState.CurLatency;
            Thread.Sleep(latency + DebugDelay);
            var inventoryPanel = GetInventoryElement();
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
                LogMessage($"index: {index}", 1);
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
                LogMessage($"{index} didn't work", 2);
            }

            return numberOfUsedCells;
        }

        private void MoveItemsFromStashToInventory(IEnumerable<NormalInventoryItem> stashItems)
        {
            const int inputDelay = 20;
            var latency = (int)GameController.Game.IngameState.CurLatency;
            //winApi.BlockInput(true);
            _keyboard.KeyDown(VirtualKeyCode.CONTROL);
            foreach (var item in stashItems.ToList())
            {
                MoveMouseToCenterOfRec(item.GetClientRect(), Mouse.GetCursorPosition());
                Thread.Sleep(inputDelay);
                _mouse.LeftButtonClick();
                Thread.Sleep(latency);
            }
            _keyboard.KeyUp(VirtualKeyCode.CONTROL);
            //winApi.BlockInput(false);
        }

        public void TestFunction()
        {
            var inventoryPanel = GetInventoryElement();
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
                    if (!doesContain && _ignoredCells[heightDirection, widthDirection] == 0)
                    {
                        Graphics.DrawBox(cell, new Color(new Vector3(255, 255, 255), 0.5f));
                    }
                }
            }
        }

        private Element GetInventoryElement()
        {
            return GameController.Game.IngameState.ReadObject<Element>(
                GameController.Game.IngameState.IngameUi.InventoryPanel.Address +
                Element.OffsetBuffers + 0x42C);
        }
    }
}