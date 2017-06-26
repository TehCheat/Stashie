using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Newtonsoft.Json;
using PoeHUD.Models.Enums;
using PoeHUD.Plugins;
using PoeHUD.Poe;
using PoeHUD.Poe.Components;
using PoeHUD.Poe.Elements;
using PoeHUD.Poe.RemoteMemoryObjects;
using SharpDX;
using Stashie.Utilities;

namespace Stashie
{
    public class Core : BaseSettingsPlugin<UiSettings>
    {
        private const int WhileDelay = 5;

        private RectangleF _gameWindow = new RectangleF(0, 0, 1920, 1080);
        private bool _moveItemsToStash = true;

        private bool _movePortalScrolls = true;
        private bool _moveWisdomScrolls = true;

        private List<string> _renamedAllStashNames;

        private Settings _settings = new Settings();
        private List<ListIndexNode> _settingsListNodes;
        private Thread _tabNamesUpdaterThread;

        public override void Initialise()
        {
            PluginName = "STASHIE";

            SetupOrClose();
            Settings.Enable.OnValueChanged += SetupOrClose;
        }

        public override void OnClose()
        {
            CloseThreads();
        }

        private void CloseThreads()
        {
            if (_tabNamesUpdaterThread != null && _tabNamesUpdaterThread.IsAlive)
            {
                _tabNamesUpdaterThread.IsBackground = true;
            }
        }

        private void SetupOrClose()
        {
            if (!Settings.Enable.Value)
            {
                CloseThreads();
                return;
            }
            _settingsListNodes = new List<ListIndexNode>
            {
                Settings.Currency,
                Settings.DivinationCards,
                Settings.Essences,
                Settings.Jewels,
                Settings.Gems,
                Settings.Leaguestones,
                Settings.Flasks,
                Settings.Jewelery,
                Settings.WhiteItems,
                Settings.Talismen,

                Settings.LeatherBelt,
                Settings.SorcererBoots,

                Settings.ChaosRecipeLvlOne,
                Settings.ChaosRecipeLvlTwo,
                Settings.ChaosRecipeLvlThree,
                Settings.ChiselRecipe,
                Settings.QualityFlasks,
                Settings.QualityGems,

                Settings.StrandShaped,
                Settings.ShoreShaped,
                Settings.UniqueMaps,
                Settings.OtherMaps,
                Settings.ShapedMaps
            };

            var names = GameController.Game.IngameState.ServerData.StashPanel.AllStashNames;
            UpdateStashNames(names);

            foreach (var settingsListNode in _settingsListNodes)
            {
                var node = settingsListNode; //Enumerator delegate fix
                node.OnValueSelected += delegate(string newValue) { OnSettingsStashNameChanged(node, newValue); };
            }
            SetIgnoredCells();

            _tabNamesUpdaterThread = new Thread(StashTabNamesUpdater);
            _tabNamesUpdaterThread.Start();
        }

        private void SetIgnoredCells()
        {
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
        }

        private void OnSettingsStashNameChanged(ListIndexNode node, string newValue)
        {
            node.Index = GetInventIndexByStashName(newValue);
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
                !Keyboard.IsKeyPressed(Settings.HotkeySetting.Value))
            {
                return;
            }

            Stashie();
            FillUpScrolls();
            _moveItemsToStash = false;
        }

        public void FillUpScrolls()
        {
            if (!Settings.ReFillScrolls.Value)
            {
                return;
            }
            // We currently don't have any scrolls in our inventory.
            var index = GetInventIndexByStashName(Settings.Currency.Value);
            GoToTab(index);
            var stashTab = GameController.Game.IngameState.ServerData.StashPanel.VisibleStash;
            var latency = (int) GameController.Game.IngameState.CurLatency + Settings.LatencySlider.Value;

            if (stashTab.VisibleInventoryItems == null)
            {
                return;
            }

            var doesStashContainPortalScrolls = stashTab.VisibleInventoryItems.Any(item => GameController.Files
                .BaseItemTypes
                .Translate(item.Item.Path).BaseName.Equals("Portal Scroll"));

            RectangleF emptyCell;


            if (doesStashContainPortalScrolls && _movePortalScrolls)
            {
                var portalScroll = stashTab.VisibleInventoryItems.ToList().First(item => GameController.Files
                    .BaseItemTypes
                    .Translate(item.Item.Path).BaseName.Equals("Portal Scroll"));

                emptyCell = FindEmptyOneCell(portalScroll);

                SplitStackAndMoveToFreeCell(portalScroll, latency, Settings.PortalScrolls.Value, emptyCell);
            }

            var doesStashContainWisdomScrolls = stashTab.VisibleInventoryItems.Any(item => GameController.Files
                .BaseItemTypes
                .Translate(item.Item.Path).BaseName.Equals("Scroll of Wisdom"));


            if (!_moveWisdomScrolls)
            {
                return;
            }

            if (!doesStashContainWisdomScrolls)
            {
                return;
            }

            var wisdomScroll = stashTab.VisibleInventoryItems.ToList().First(item => GameController.Files
                .BaseItemTypes
                .Translate(item.Item.Path).BaseName.Equals("Scroll of Wisdom"));

            emptyCell = FindEmptyOneCell(wisdomScroll);

            SplitStackAndMoveToFreeCell(wisdomScroll, latency, Settings.WisdomScrolls.Value, emptyCell);
        }

        public void Stashie()
        {
            // Instead of just itterating through each itemToLookFor in our inventory panel and switching to it's corresponding tab
            // we, itterate through the items in the inventory and place them in a list with position and the corresponding tab it should be moved to,
            // then we sort the list by stash tab stashTabIndex (0 to number of stashes)
            // and then we move the items.
            _gameWindow = GameController.Window.GetWindowRectangle();
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

            Mouse.SetCursorPos(cursorPosPreMoving);

            //WinApi.BlockInput(false);
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
                MoveItemsToStash(sortedItems);
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

            MoveItemsToStash(sortedItems);
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

        private Element GetInventoryPanel()
        {
            return GameController.Game.IngameState.ReadObject<Element>(
                GameController.Game.IngameState.IngameUi.InventoryPanel.Address +
                Element.OffsetBuffers + 0x42C);
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

        private void MoveItemsToStash(IEnumerable<NormalInventoryItem> sortedItems)
        {
            var latency = (int) GameController.Game.IngameState.CurLatency + Settings.LatencySlider.Value;

            Keyboard.KeyDown(Keys.ControlKey);
            Thread.Sleep(Constants.InputDelay);

            foreach (var item in sortedItems)
            {
                Mouse.SetCursorPosAndLeftClick(item.GetClientRect().Center, _gameWindow);
                Thread.Sleep(latency);
            }
            Keyboard.KeyUp(Keys.ControlKey);
        }

        private void SplitStackAndMoveToFreeCell(Element element, int latency, int wantedStackSize,
            RectangleF freeCell)
        {
            var stackSizeOfItemToMove = wantedStackSize;

            //WinApi.BlockInput(true);

            Keyboard.KeyDown(Keys.ShiftKey);
            Mouse.SetCursorPosAndLeftClick(element.GetClientRect().Center, _gameWindow);
            Keyboard.KeyUp(Keys.ShiftKey);


            #region Enter split size.

            if (stackSizeOfItemToMove < 10)
            {
                var keyToPress = (int) Keys.D0 + stackSizeOfItemToMove;
                Keyboard.KeyPress((Keys) keyToPress);
            }
            else
            {
                var keyToPress = (int) Keys.D0 + stackSizeOfItemToMove / 10;
                Keyboard.KeyPress((Keys) keyToPress);
                Thread.Sleep(latency);
                keyToPress = (int) Keys.D0 + stackSizeOfItemToMove % 10;
                Keyboard.KeyPress((Keys) keyToPress);
            }

            #endregion

            Thread.Sleep(latency);
            Keyboard.KeyPress(Keys.Enter);
            Thread.Sleep(latency);

            Mouse.SetCursorPosAndLeftClick(freeCell.Center, _gameWindow);
        }


        private string GetStashNameFromIndex(int index)
        {
            try
            {
                var stashNames = GameController.Game.IngameState.ServerData.StashPanel.AllStashNames;
                return stashNames[index];
            }
            catch
            {
                return "Ignore";
            }
        }

        private int GetInventIndexByStashName(string name)
        {
            var index = _renamedAllStashNames.IndexOf(name);
            if (index != -1)
            {
                index--;
            }
            return index;
        }

        private void UpdateStashNames(List<string> newNames)
        {
            Settings.AllStashNames = newNames;
            _renamedAllStashNames = new List<string> {"Ignore"};

            for (var i = 0; i < Settings.AllStashNames.Count; i++)
            {
                var realStashName = Settings.AllStashNames[i];

                if (_renamedAllStashNames.Contains(realStashName))
                {
                    realStashName += " (" + i + ")";
                    LogMessage("Stashie: fixed same stash name to: " + realStashName, 3);
                }

                _renamedAllStashNames.Add(realStashName);
            }

            Settings.AllStashNames.Insert(0, "Ignore");

            foreach (var listIndexNode in _settingsListNodes)
            {
                listIndexNode.SetListValues(_renamedAllStashNames);

                var inventoryIndex = GetInventIndexByStashName(listIndexNode.Value);

                if (inventoryIndex == -1) //If the value doesn't exist in list (renamed)
                {
                    if (listIndexNode.Index != -1) //If the value doesn't exist in list and the value was not Ignore
                    {
                        LogMessage(
                            "Tab renamed? : " + listIndexNode.Value + " to " +
                            _renamedAllStashNames[listIndexNode.Index + 1],
                            5);

                        listIndexNode.Value =
                            _renamedAllStashNames[listIndexNode.Index + 1]; //    Just update it's name
                    }
                    else
                    {
                        listIndexNode.Value =
                            _renamedAllStashNames[0]; //Actually it was "Ignore", we just update it (can be removed)
                    }
                }
                else //tab just change it's index
                {
                    if (listIndexNode.Index != inventoryIndex)
                    {
                        LogMessage("Tab moved: " + listIndexNode.Index + " to " + inventoryIndex, 5);
                    }

                    listIndexNode.Index = inventoryIndex;
                    listIndexNode.Value = _renamedAllStashNames[inventoryIndex + 1];
                }
            }
        }

        public void StashTabNamesUpdater()
        {
            while (!_tabNamesUpdaterThread.IsBackground)
            {
                var stashPanel = GameController.Game.IngameState.ServerData.StashPanel;
                if (stashPanel == null)
                {
                    continue;
                }

                if (!stashPanel.IsVisible)
                {
                    Thread.Sleep(1000);
                    continue;
                }

                var cachedNames = Settings.AllStashNames;
                var realNames = stashPanel.AllStashNames;

                if (realNames.Count + 1 != cachedNames.Count)
                {
                    UpdateStashNames(realNames);
                    continue;
                }

                for (var index = 0; index < realNames.Count; ++index)
                {
                    var cachedName = cachedNames[index + 1];
                    if (cachedName.Equals(realNames[index]))
                    {
                        continue;
                    }

                    UpdateStashNames(realNames);
                    break;
                }

                Thread.Sleep(500);
            }

            _tabNamesUpdaterThread.Interrupt();
        }

        #region Functions for switching between tabs / 'going' to a tab.

        private void FirstTab()
        {
            var latency = (int) GameController.Game.IngameState.CurLatency + Settings.LatencySlider.Value;
            while (GameController.Game.IngameState.ServerData.StashPanel.GetStashInventoryByIndex(0) == null ||
                   !GameController.Game.IngameState.ServerData.StashPanel.GetStashInventoryByIndex(0)
                       .AsObject<Element>()
                       .IsVisible)
            {
                Keyboard.KeyPress(Keys.Left);
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
                Keyboard.KeyPress(Keys.Right);
            }
        }

        private void GoToTab(int tabIndex)
        {
            var stashPanel = GameController.Game.IngameState.ServerData.StashPanel;
            var visibleStash = stashPanel.VisibleStash;

            while (visibleStash == null)
            {
                Thread.Sleep(WhileDelay);
                visibleStash = stashPanel.VisibleStash;
            }

            var stashNameOfIndex = GetStashNameFromIndex(tabIndex);

            if (stashNameOfIndex.Equals(Settings.Currency.Value) && visibleStash.InvType == InventoryType.CurrencyStash)
            {
                return;
            }

            if (stashNameOfIndex.Equals(Settings.Essences.Value) && visibleStash.InvType == InventoryType.EssenceStash)
            {
                return;
            }

            if (stashNameOfIndex.Equals(Settings.DivinationCards.Value) &&
                visibleStash.InvType == InventoryType.DivinationStash)
            {
                return;
            }


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
                var viewAllTabsButton = stashPanel.ViewAllStashButton;

                var openLeftPanel = GameController.Game.IngameState.IngameUi.OpenLeftPanel;
                var parent = openLeftPanel.Children[2].Children[0].Children[1].Children[3];
                var dropDownTabElements = parent.Children[2];

                var totalStashes = stashPanel.TotalStashes;
                if (totalStashes > 30)
                {
                    // If the number of stashes is greater than 30, then parent.Children[1] becomes the ScrollBar
                    // and the DropDownElements becomes parent.Children[2]
                    dropDownTabElements = parent.Children[1];
                }

                if (!dropDownTabElements.IsVisible)
                {
                    Mouse.SetCursorPosAndLeftClick(viewAllTabsButton.GetClientRect().Center, _gameWindow);
                    while (!dropDownTabElements.IsVisible)
                    {
                        Thread.Sleep(WhileDelay);
                    }
                    Mouse.VerticalScroll(true, 5);
                    Thread.Sleep(latency + 50);
                }

                var tabPos = dropDownTabElements.Children[tabIndex].GetClientRect();
                Mouse.SetCursorPosAndLeftClick(tabPos.Center, _gameWindow);
                Thread.Sleep(latency);
            }
            catch (Exception e)
            {
                LogError($"Error in GoToTab: {e}", 5);
            }
        }

        #endregion


        #region Functions related to moving stash tab items to inventory and then putting them back in stash tab in sorted order.

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
            Keyboard.KeyDown(Keys.Control);
            foreach (var item in stashItems.ToList())
            {
                Mouse.SetCursorPos(item.GetClientRect().Center, _gameWindow);
                Thread.Sleep(latency);
            }
            Keyboard.KeyUp(Keys.Control);
            //winApi.BlockInput(false);
        }

        #endregion


        #region Assigning an index to an item (which tab should the item be placed in?)

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

            if (baseName.Equals("Scroll of Wisdom") && Settings.ReFillScrolls.Value)
            {
                if (item.GetComponent<Stack>().Size == Settings.WisdomScrolls.Value)
                {
                    _moveWisdomScrolls = false;
                    return itemsToMove;
                }

                tabIndex = GetInventIndexByStashName(Settings.Currency.Value);
            }

            else if (baseName.Equals("Portal Scroll") && Settings.ReFillScrolls.Value)
            {
                if (item.GetComponent<Stack>().Size == Settings.PortalScrolls.Value)
                {
                    _movePortalScrolls = false;
                    return itemsToMove;
                }

                tabIndex = GetInventIndexByStashName(Settings.Currency.Value);
            }

            #endregion

            #region Ignored Cell

            if (IsCellIgnored(itemPos))
            {
                return itemsToMove;
            }

            #endregion

            // Handle items based of user settings.

            #region Maps

            if (className.Equals("Map") && Settings.MapTabsHolder)
            {
                #region Shaped Shore

                if (baseName.Equals("Shaped Shore Map"))
                {
                    if (!Settings.ShoreShaped.Value.Equals("Ignore"))
                    {
                        tabIndex = GetInventIndexByStashName(Settings.ShoreShaped.Value);
                    }
                }

                #endregion

                #region Shaped Strand

                else if (baseName.Equals("Shaped Strand Map"))
                {
                    if (!Settings.StrandShaped.Value.Equals("Ignore"))
                    {
                        tabIndex = GetInventIndexByStashName(Settings.StrandShaped.Value);
                    }
                }

                #endregion

                #region Shaped Maps

                else if (baseName.Contains("Shaped"))
                {
                    if (!Settings.ShapedMaps.Value.Equals("Ignore"))
                    {
                        tabIndex = GetInventIndexByStashName(Settings.ShapedMaps.Value);
                    }
                }

                #endregion

                #region Other Maps and Unique Maps

                else if (rarity == ItemRarity.Unique)
                {
                    if (!Settings.UniqueMaps.Value.Equals("Ignore"))
                    {
                        tabIndex = GetInventIndexByStashName(Settings.UniqueMaps.Value);
                    }
                }

                else if (!Settings.OtherMaps.Value.Equals("Ignore"))
                {
                    tabIndex = GetInventIndexByStashName(Settings.OtherMaps.Value);
                }

                #endregion
            }

            #endregion

            #region Chance Items

            #region Sorcerer Boots

            else if (baseName.Equals("Sorcerer Boots") && rarity == ItemRarity.Normal &&
                     Settings.ChanceItemTabs.Value &&
                     !Settings.SorcererBoots.Value.Equals("Ignore"))
            {
                tabIndex = GetInventIndexByStashName(Settings.SorcererBoots.Value);
            }

            #endregion

            #region Leather Belt

            else if (baseName.Equals("Leather Belt") && rarity == ItemRarity.Normal &&
                     Settings.ChanceItemTabs.Value &&
                     !Settings.LeatherBelt.Value.Equals("Ignore"))
            {
                tabIndex = GetInventIndexByStashName(Settings.LeatherBelt.Value);
            }

            #endregion

            #endregion

            #region Vendor Recipes

            #region Chisel Recipe

            else if ((baseName.Equals("Stone Hammer") || baseName.Equals("Rock Breaker") ||
                      baseName.Equals("Gavel") && quality == 20) && Settings.VendorRecipeTabs.Value &&
                     !Settings.ChiselRecipe.Value.Equals("Ignore"))
            {
                tabIndex = GetInventIndexByStashName(Settings.ChiselRecipe.Value);
            }

            #endregion

            #region Chaos Recipes

            else if (Settings.VendorRecipeTabs.Value && item.GetComponent<Mods>().ItemRarity == ItemRarity.Rare &&
                     !className.Equals("Jewel") && iLvl >= 60 && iLvl <= 74)
            {
                var mods = item.GetComponent<Mods>();
                if (mods.Identified && quality < 20 && !Settings.ChaosRecipeLvlOne.Value.Equals("Ignore"))
                {
                    tabIndex = GetInventIndexByStashName(Settings.ChaosRecipeLvlOne.Value);
                }
                if ((!mods.Identified || quality == 20) && !Settings.ChaosRecipeLvlTwo.Value.Equals("Ignore"))
                {
                    tabIndex = GetInventIndexByStashName(Settings.ChaosRecipeLvlTwo.Value);
                }
                if (!mods.Identified && quality == 20 && !Settings.ChaosRecipeLvlThree.Value.Equals("Ignore"))
                {
                    tabIndex = GetInventIndexByStashName(Settings.ChaosRecipeLvlThree.Value);
                }
            }

            #endregion

            #region Quality Gems

            else if (className.Contains("Skill Gem") && quality > 0 && !Settings.QualityGems.Value.Equals("Ignore"))
            {
                tabIndex = GetInventIndexByStashName(Settings.QualityGems.Value);
            }

            #endregion

            #region Quality Flasks

            else if (className.Contains("Flask") && quality > 0 && !Settings.QualityFlasks.Value.Equals("Ignore"))
            {
                tabIndex = GetInventIndexByStashName(Settings.QualityFlasks.Value);
            }

            #endregion

            #endregion

            #region Default Tabs

            #region Divination Cards

            else if (className.Equals("DivinationCard") && !Settings.DivinationCards.Value.Equals("Ignore"))
            {
                tabIndex = GetInventIndexByStashName(Settings.DivinationCards.Value);
            }

            #endregion

            #region Gems

            else if (className.Contains("Skill Gem") && quality == 0 && !Settings.Gems.Value.Equals("Ignore"))
            {
                tabIndex = GetInventIndexByStashName(Settings.Gems.Value);
            }

            #endregion

            #region Currency

            else if (className.Equals("StackableCurrency") && !item.Path.Contains("Essence") &&
                     !Settings.Currency.Value.Equals("Ignore"))
            {
                tabIndex = GetInventIndexByStashName(Settings.Currency.Value);
            }

            #endregion

            #region Leaguestone

            else if (className.Equals("Leaguestone") && !Settings.Leaguestones.Value.Equals("Ignore"))
            {
                tabIndex = GetInventIndexByStashName(Settings.Leaguestones.Value);
            }

            #endregion

            #region Essence

            else if (baseName.Contains("Essence") && className.Equals("StackableCurrency") &&
                     !Settings.Essences.Value.Equals("Ignore"))
            {
                tabIndex = GetInventIndexByStashName(Settings.Essences.Value);
            }

            #endregion

            #region Jewels

            else if (className.Equals("Jewel") && !Settings.Jewels.Value.Equals("Ignore"))
            {
                tabIndex = GetInventIndexByStashName(Settings.Jewels.Value);
            }

            #endregion

            #region Flasks

            else if (className.Contains("Flask") && quality == 0 && !Settings.Flasks.Value.Equals("Ignore"))
            {
                tabIndex = GetInventIndexByStashName(Settings.Flasks.Value);
            }

            #endregion

            #region Talisman

            else if (className.Equals("Amulet") && baseName.Contains("Talisman") &&
                     !Settings.Talismen.Value.Equals("Ignore"))
            {
                tabIndex = GetInventIndexByStashName(Settings.Talismen.Value);
            }

            #endregion

            #region jewelery

            else if ((className.Equals("Amulet") || className.Equals("Ring")) &&
                     !Settings.Jewelery.Value.Equals("Ignore"))
            {
                tabIndex = GetInventIndexByStashName(Settings.Jewelery.Value);
            }

            #endregion

            #endregion

            #region White Items

            else if (rarity == ItemRarity.Normal && !Settings.WhiteItems.Value.Equals("Ignore"))
            {
                tabIndex = GetInventIndexByStashName(Settings.WhiteItems.Value);
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

        #endregion


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
    }
}