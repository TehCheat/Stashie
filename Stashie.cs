using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Nodes;
using ImGuiNET;
using Newtonsoft.Json;
using SharpDX;
using Vector4 = System.Numerics.Vector4;

namespace Stashie
{
    public class StashieCore : BaseSettingsPlugin<StashieSettings>
    {
        private const string StashTabsNameChecker = "Stash Tabs Name Checker";
        private const string FiltersConfigFile = "FiltersConfig.txt";
        private const int WhileDelay = 5;
        private const int InputDelay = 15;
        private const string CoroutineName = "Drop To Stash";
        private readonly Stopwatch _debugTimer = new Stopwatch();
        private readonly Stopwatch _stackItemTimer = new Stopwatch();
        private readonly WaitTime _wait10Ms = new WaitTime(10);
        private readonly WaitTime _wait3Ms = new WaitTime(3);
        private readonly WaitTime _wait1Ms = new WaitTime(1);
        private Vector2 _clickWindowOffset;
        private List<CustomFilter> _customFilters;
        private List<RefillProcessor> _customRefills;
        private List<FilterResult> _dropItems;
        private List<ListIndexNode> _settingsListNodes;
        private uint _coroutineIteration;
        private Coroutine _coroutineWorker;
        private Action _filterTabs;
        private string[] _stashTabNamesByIndex;
        private Coroutine _stashTabNamesCoroutine;
        private int _visibleStashIndex = -1;
        private const int MaxShownSidebarStashTabs = 32;
        private int _stashCount;

        public StashieCore()
        {
            Name = "Stashie";
        }

        public override bool Initialise()
        {
            Settings.Enable.OnValueChanged += (sender, b) =>
            {
                if (b)
                {
                    if (Core.ParallelRunner.FindByName(StashTabsNameChecker) == null) InitCoroutine();
                    _stashTabNamesCoroutine?.Resume();
                }
                else
                {
                    _stashTabNamesCoroutine?.Pause();
                }

                SetupOrClose();
            };

            InitCoroutine();
            SetupOrClose();

            Input.RegisterKey(Settings.DropHotkey);
            Input.RegisterKey(Keys.ShiftKey);

            Settings.DropHotkey.OnValueChanged += () => { Input.RegisterKey(Settings.DropHotkey); };
            _stashCount = (int) GameController.Game.IngameState.IngameUi.StashElement.TotalStashes;

            return true;
        }

        public override void AreaChange(AreaInstance area)
        {
            //TODO Add lab name with stash
            if (area.IsHideout || area.DisplayName.Contains("Azurite Mine"))
                _stashTabNamesCoroutine?.Resume();
            else
                _stashTabNamesCoroutine?.Pause();
        }

        private void InitCoroutine()
        {
            _stashTabNamesCoroutine = new Coroutine(StashTabNamesUpdater_Thread(), this, StashTabsNameChecker);
            Core.ParallelRunner.Run(_stashTabNamesCoroutine);
        }

        /// <summary>
        /// Creates a new file and adds the content to it if the file doesn't exists.
        /// If the file already exists, then no action is taken.
        /// </summary>
        /// <param name="path">The path to the file on disk</param>
        /// <param name="content">The content it should contain</param>
        private static void WriteToNonExistentFile(string path, string content)
        {
            if (File.Exists(path)) return;

            using (var streamWriter = new StreamWriter(path, true))
            {
                streamWriter.Write(content);
                streamWriter.Close();
            }
        }

        private void SaveDefaultConfigsToDisk()
        {
            // WriteInventoryNames();
            var path = $"{DirectoryFullName}\\GitUpdateConfig.txt";
            const string gitUpdateConfig = "Owner:nymann\r\n" + "Name:Stashie\r\n" + "Release\r\n";
            WriteToNonExistentFile(path, gitUpdateConfig);
            path = $"{DirectoryFullName}\\RefillCurrency.txt";

            const string refillCurrency = "//MenuName:\t\t\tClassName,\t\t\tStackSize,\tInventoryX,\tInventoryY\r\n" +
                                          "Portal Scrolls:\t\tPortal Scroll,\t\t40,\t\t\t12,\t\t\t1\r\n" +
                                          "Scrolls of Wisdom:\tScroll of Wisdom,\t40,\t\t\t12,\t\t\t2\r\n" +
                                          "//Chances:\t\t\tOrb of Chance,\t\t20,\t\t\t12,\t\t\t3";

            WriteToNonExistentFile(path, refillCurrency);
            path = $"{DirectoryFullName}\\FiltersConfig.txt";

            const string filtersConfig =

                #region default config String

                "//FilterName(menu name):\tfilters\t\t:ParentMenu(optionaly, will be created automatially for grouping)\r\n" +
                "//Filter parts should divided by coma or | (for OR operation(any filter part can pass))\r\n" +
                "\r\n" +
                "////////////\tAvailable properties:\t/////////////////////\r\n" +
                "/////////\tString (name) properties:\r\n" +
                "//classname\r\n" +
                "//basename\r\n" +
                "//path\r\n" +
                "/////////\tNumerical properties:\r\n" +
                "//itemquality\r\n" +
                "//rarity\r\n" +
                "//ilvl\r\n" +
                "//tier\r\n" +
                "//numberofsockets\r\n" +
                "//numberoflinks\r\n" +
                "//veiled\r\n" +
                "//fractured\r\n" +
                "/////////\tBoolean properties:\r\n" +
                "//identified\r\n" +
                "//fractured\r\n" +
                "//corrupted\r\n" +
                "//influenced\r\n" +
                "//Elder\r\n" +
                "//Shaper\r\n" +
                "//Crusader\r\n" +
                "//Hunter\r\n" +
                "//Redeemer\r\n" +
                "//Warlord\r\n" +
                "//blightedMap\r\n" +
                "//elderGuardianMap\r\n" +
                "/////////////////////////////////////////////////////////////\r\n" +
                "////////////\tAvailable operations:\t/////////////////////\r\n" +
                "/////////\tString (name) operations:\r\n" +
                "//!=\t(not equal)\r\n" +
                "//=\t\t(equal)\r\n" +
                "//^\t\t(contains)\r\n" +
                "//!^\t(not contains)\r\n" +
                "/////////\tNumerical operations:\r\n" +
                "//!=\t(not equal)\r\n" +
                "//=\t\t(equal)\r\n" +
                "//>\t\t(bigger)\r\n" +
                "//<\t\t(less)\r\n" +
                "//<=\t(less or qual)\r\n" +
                "//>=\t(bigger or qual)\r\n" +
                "/////////\tBoolean operations:\r\n" +
                "//!\t\t(not/invert)\r\n" +
                "/////////////////////////////////////////////////////////////\r\n" +
                "\r\n" +
                "//Default Tabs\r\n" +
                "Currency:\t\t\tClassName=StackableCurrency,path!^Essence,BaseName!^Remnant,path!^CurrencyDelveCrafting,BaseName!^Splinter,Path!^CurrencyItemisedProphecy,Path!^CurrencyAfflictionOrb,Path!^Mushrune\t:Default Tabs\r\n" +
                "Divination Cards:\t\t\tClassName=DivinationCard\t\t\t\t\t:Default Tabs\r\n" +
                "Essences:\t\t\tBaseName^Essence|BaseName^Remnant,ClassName=StackableCurrency:Default Tabs\r\n" +
                "Fragments:\t\t\tClassName=MapFragment|BaseName^Splinter,ClassName=StackableCurrency|ClassName=LabyrinthMapItem|BaseName^Scarab\t:Default Tabs\r\n" +
                "Maps:\t\t\tClassName=Map,!blightedMap\t\t\t:Default Tabs\r\n" +
                "Fossils/Resonators:\t\t\tpath^CurrencyDelveCrafting | path^DelveStackableSocketableCurrency\t:Default Tabs" +
                "Gems:\t\t\t\tClassName^Skill Gem,ItemQuality=0\t\t\t:Default Tabs\r\n" +
                "6-Socket:\t\t\tnumberofsockets=6,numberoflinks!=6\t\t\t:Default Tabs\r\n" +
                "Prophecies:\t\t\tPath^CurrencyItemisedProphecy\t\t\t:Default Tabs\r\n" +
                "Jewels:\t\t\t\tClassName=Jewel,Rarity != Unique\t\t\t\t\t\t\t\t:Default Tabs\r\n" +
                "\r\n" +
                "//Special Items\r\n" +
                "Veiled:\t\t\tVeiled>0\t:Special items\r\n" +
                "AnyInfluence:\t\t\tinfluenced\t:Special items\r\n" +
                "\r\n" +
                "//league Content\r\n" +
                "Legion-Incubators:\t\t\tpath^CurrencyIncubation\t:League Items\r\n" +
                "Delirium-Splinter:\t\t\tpath^CurrencyAfflictionShard\t:League Items\r\n" +
                "Delirium-Simulacrum:\t\t\tpath^CurrencyAfflictionFragment\t:League Items\r\n" +
                "Blight-AnnointOils:\t\t\tpath^Mushrune\t:League Items\r\n" +
                "//Chance Items\r\n" +
                "Sorcerer Boots:\tBaseName=Sorcerer Boots,Rarity=Normal\t:Chance Items\r\n" +
                "Leather Belt:\tBaseName=Leather Belt,Rarity=Normal\t\t:Chance Items\r\n" +
                "\r\n" +
                "//Vendor Recipes\r\n" +
                "Chisel Recipe:\t\tBaseName=Stone Hammer|BaseName=Rock Breaker,ItemQuality=20\t:Vendor Recipes\r\n" +
                "Quality Gems:\t\tClassName^Skill Gem,ItemQuality>0\t\t\t\t\t\t\t:Vendor Recipes\r\n" +
                "Quality Flasks:\t\tClassName^Flask,ItemQuality>0\t\t\t\t\t\t\t\t:Vendor Recipes\r\n" +
                "\r\n" +
                "//Chaos Recipe LVL 2 (unindentified and ilvl 60 or above)\r\n" +
                "Weapons:\t\t!identified,Rarity=Rare,ilvl>=60,ClassName^Two Hand|ClassName^One Hand|ClassName=Bow|ClassName=Staff|ClassName=Sceptre|ClassName=Wand|ClassName=Dagger|ClassName=Claw|ClassName=Shield :Chaos Recipe\r\n" +
                "Jewelry:\t\t!identified,Rarity=Rare,ilvl>=60,ClassName=Ring|ClassName=Amulet \t:Chaos Recipe\r\n" +
                "Belts:\t\t\t!identified,Rarity=Rare,ilvl>=60,ClassName=Belt \t\t\t\t\t:Chaos Recipe\r\n" +
                "Helms:\t\t\t!identified,Rarity=Rare,ilvl>=60,ClassName=Helmet \t\t\t\t\t:Chaos Recipe\r\n" +
                "Body Armours:\t!identified,Rarity=Rare,ilvl>=60,ClassName=Body Armour \t\t\t\t:Chaos Recipe\r\n" +
                "Boots:\t\t\t!identified,Rarity=Rare,ilvl>=60,ClassName=Boots \t\t\t\t\t:Chaos Recipe\r\n" +
                "Gloves:\t\t\t!identified,Rarity=Rare,ilvl>=60,ClassName=Gloves \t\t\t\t\t:Chaos Recipe";

            #endregion

            WriteToNonExistentFile(path, filtersConfig);
        }

        public override void DrawSettings()
        {
            DrawReloadConfigButton();
            DrawIgnoredCellsSettings();
            base.DrawSettings();

            foreach (var settingsCustomRefillOption in Settings.CustomRefillOptions)
            {
                var value = settingsCustomRefillOption.Value.Value;
                ImGui.SliderInt(settingsCustomRefillOption.Key, ref value, settingsCustomRefillOption.Value.Min,
                    settingsCustomRefillOption.Value.Max);
                settingsCustomRefillOption.Value.Value = value;
            }

            _filterTabs?.Invoke();
        }

        private void LoadCustomFilters()
        {
            var filterPath = Path.Combine(DirectoryFullName, FiltersConfigFile);
            var filtersLines = File.ReadAllLines(filterPath);
            var unused = new FilterParser();
            _customFilters = FilterParser.Parse(filtersLines);

            foreach (var customFilter in _customFilters)
            {
                if (!Settings.CustomFilterOptions.TryGetValue(customFilter.Name, out var indexNodeS))
                {
                    indexNodeS = new ListIndexNode {Value = "Ignore", Index = -1};
                    Settings.CustomFilterOptions.Add(customFilter.Name, indexNodeS);
                }

                customFilter.StashIndexNode = indexNodeS;
                _settingsListNodes.Add(indexNodeS);
            }
        }

        public void SaveIgnoredSLotsFromInventoryTemplate()
        {
            Settings.IgnoredCells = new[,]
            {
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}
            };
            try
            {
                var inventory = GameController.Game.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory];
                foreach (var item in inventory.VisibleInventoryItems)
                {
                    var baseC = item.Item.GetComponent<Base>();
                    var itemSizeX = baseC.ItemCellsSizeX;
                    var itemSizeY = baseC.ItemCellsSizeY;
                    var inventPosX = item.InventPosX;
                    var inventPosY = item.InventPosY;
                    for (var y = 0; y < itemSizeY; y++)
                    for (var x = 0; x < itemSizeX; x++)
                        Settings.IgnoredCells[y + inventPosY, x + inventPosX] = 1;
                }
            }
            catch (Exception e)
            {
                LogError($"{e}", 5);
            }
        }

        private void DrawReloadConfigButton()
        {
            if (ImGui.Button("Reload config"))
            {
                LoadCustomFilters();
                DebugWindow.LogMsg("Reloaded Stashie config", 2, Color.LimeGreen);
            }
        }

        private void DrawIgnoredCellsSettings()
        {
            try
            {
                if (ImGui.Button("Copy Inventory")) SaveIgnoredSLotsFromInventoryTemplate();

                ImGui.SameLine();
                ImGui.TextDisabled("(?)");
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(
                        $"Checked = Item will be ignored{Environment.NewLine}UnChecked = Item will be processed");
            }
            catch (Exception e)
            {
                LogError(e.ToString(), 10);
            }

            var numb = 1;
            for (var i = 0; i < 5; i++)
            for (var j = 0; j < 12; j++)
            {
                var toggled = Convert.ToBoolean(Settings.IgnoredCells[i, j]);
                if (ImGui.Checkbox($"##{numb}IgnoredCells", ref toggled)) Settings.IgnoredCells[i, j] ^= 1;

                if ((numb - 1) % 12 < 11) ImGui.SameLine();

                numb += 1;
            }
        }


        private void GenerateMenu()
        {
            _stashTabNamesByIndex = _renamedAllStashNames.ToArray();

            _filterTabs = null;

            foreach (var customFilter in _customFilters.GroupBy(x => x.SubmenuName, e => e))
                _filterTabs += () =>
                {
                    ImGui.TextColored(new Vector4(0f, 1f, 0.022f, 1f), customFilter.Key);

                    foreach (var filter in customFilter)
                        if (Settings.CustomFilterOptions.TryGetValue(filter.Name, out var indexNode))
                        {
                            var formattableString = $"{filter.Name} => {_renamedAllStashNames[indexNode.Index + 1]}";

                            ImGui.Columns(2, formattableString, true);
                            ImGui.SetColumnWidth(0, 300);
                            ImGui.SetColumnWidth(1, 160);

                            if (ImGui.Button(formattableString, new System.Numerics.Vector2(180, 20)))
                                ImGui.OpenPopup(formattableString);

                            ImGui.SameLine();
                            ImGui.NextColumn();

                            var item = indexNode.Index + 1;
                            var filterName = filter.Name;

                            if (string.IsNullOrWhiteSpace(filterName))
                                filterName = "Null";

                            if (ImGui.Combo($"##{filterName}", ref item, _stashTabNamesByIndex,
                                _stashTabNamesByIndex.Length))
                            {
                                indexNode.Value = _stashTabNamesByIndex[item];
                                OnSettingsStashNameChanged(indexNode, _stashTabNamesByIndex[item]);
                            }

                            ImGui.NextColumn();
                            ImGui.Columns(1, "", false);
                            var pop = true;

                            if (!ImGui.BeginPopupModal(formattableString, ref pop,
                                ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize)) continue;
                            var x = 0;

                            foreach (var name in _renamedAllStashNames)
                            {
                                x++;

                                if (ImGui.Button($"{name}", new System.Numerics.Vector2(100, 20)))
                                {
                                    indexNode.Value = name;
                                    OnSettingsStashNameChanged(indexNode, name);
                                    ImGui.CloseCurrentPopup();
                                }

                                if (x % 10 != 0)
                                    ImGui.SameLine();
                            }

                            ImGui.Spacing();
                            ImGuiNative.igIndent(350);
                            if (ImGui.Button("Close", new System.Numerics.Vector2(100, 20)))
                                ImGui.CloseCurrentPopup();

                            ImGui.EndPopup();
                        }
                        else
                        {
                            indexNode = new ListIndexNode {Value = "Ignore", Index = -1};
                        }
                };
        }

        private void LoadCustomRefills()
        {
            _customRefills = RefillParser.Parse(DirectoryFullName);
            if (_customRefills.Count == 0) return;

            foreach (var refill in _customRefills)
            {
                if (!Settings.CustomRefillOptions.TryGetValue(refill.MenuName, out var amountOption))
                {
                    amountOption = new RangeNode<int>(15, 0, refill.StackSize);
                    Settings.CustomRefillOptions.Add(refill.MenuName, amountOption);
                }

                amountOption.Max = refill.StackSize;
                refill.AmountOption = amountOption;
            }

            _settingsListNodes.Add(Settings.CurrencyStashTab);
        }

        private void LoadIgnoredCells()
        {
            const string fileName = @"/IgnoredCells.json";
            var filePath = DirectoryFullName + fileName;

            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);

                try
                {
                    Settings.IgnoredCells = JsonConvert.DeserializeObject<int[,]>(json);
                    var ignoredHeight = Settings.IgnoredCells.GetLength(0);
                    var ignoredWidth = Settings.IgnoredCells.GetLength(1);

                    if (ignoredHeight != 5 || ignoredWidth != 12)
                        LogError("Stashie: Wrong IgnoredCells size! Should be 12x5. Resetting to default..", 5);
                    else
                        return;
                }
                catch (Exception ex)
                {
                    LogError(
                        "Stashie: Can't decode IgnoredCells settings in " + fileName +
                        ". Resetting to default. Error: " + ex.Message,
                        5);
                }
            }

            var defaultSettings = JsonConvert.SerializeObject(Settings.IgnoredCells);
            defaultSettings = defaultSettings.Replace("[[", "[\n[");
            defaultSettings = defaultSettings.Replace("],[", "],\n[");
            defaultSettings = defaultSettings.Replace("]]", "]\n]");
            File.WriteAllText(filePath, defaultSettings);
        }

        public override void Render()
        {
            if (_coroutineWorker != null && _coroutineWorker.IsDone)
            {
                Input.KeyUp(Keys.LControlKey);
                _coroutineWorker = null;
            }

            var uiTabsOpened = GameController.Game.IngameState.IngameUi.InventoryPanel.IsVisible &&
                               GameController.Game.IngameState.IngameUi.StashElement.IsVisibleLocal;

            if (!uiTabsOpened && _coroutineWorker != null && !_coroutineWorker.IsDone)
            {
                Input.KeyUp(Keys.LControlKey);
                _coroutineWorker = Core.ParallelRunner.FindByName(CoroutineName);
                _coroutineWorker?.Done();
                LogError($"When stashie working, stash UI was  closed, error happens in tab #{_visibleStashIndex}", 5);
            }

            if (_coroutineWorker != null && _coroutineWorker.Running && _debugTimer.ElapsedMilliseconds > 15000)
            {
                LogError(
                    $"Stopped because work more than 15 sec. Error in {GetIndexOfCurrentVisibleTab()} type {GetTypeOfCurrentVisibleStash()} visibleStashIndex: {_visibleStashIndex}",
                    5);

                _coroutineWorker?.Done();
                _debugTimer.Restart();
                _debugTimer.Stop();
                Input.KeyUp(Keys.LControlKey);
            }

            if (!Settings.DropHotkey.PressedOnce()) return;
            if (!uiTabsOpened) return;
            _coroutineWorker = new Coroutine(ProcessInventoryItems(), this, CoroutineName);
            Core.ParallelRunner.Run(_coroutineWorker);
        }

        private IEnumerator ProcessInventoryItems()
        {
            _debugTimer.Restart();
            yield return ParseItems();

            var cursorPosPreMoving = Input.ForceMousePosition;
            if (_dropItems.Count > 0) yield return DropToStash();

            yield return ProcessRefills();
            yield return Input.SetCursorPositionSmooth(new Vector2(cursorPosPreMoving.X, cursorPosPreMoving.Y));
            Input.MouseMove();

            _coroutineWorker = Core.ParallelRunner.FindByName(CoroutineName);
            _coroutineWorker?.Done();

            _debugTimer.Restart();
            _debugTimer.Stop();
        }

        private IEnumerator ParseItems()
        {
            var inventory = GameController.Game.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory];
            var invItems = inventory.VisibleInventoryItems;

            if (invItems == null)
            {
                LogMessage("Player inventory->VisibleInventoryItems is null!", 5);
                yield return new WaitRender();
            }
            else
            {
                _dropItems = new List<FilterResult>();
                _clickWindowOffset = GameController.Window.GetWindowRectangle().TopLeft;

                foreach (var invItem in invItems)
                {
                    if (invItem.Item == null || invItem.Address == 0) continue;

                    if (CheckIgnoreCells(invItem)) continue;


                    var baseItemType = GameController.Files.BaseItemTypes.Translate(invItem.Item.Path);
                    var testItem = new ItemData(invItem, baseItemType);
                    var result = CheckFilters(testItem);
                    if (result != null)
                        _dropItems.Add(result);
                }
            }
        }

        private void WriteInventoryNames()
        {
            var path = $"{DirectoryFullName}\\InventoryDebugList.txt";
            var sb = new StringBuilder();
            foreach (var inventoryItem in _dropItems)
            {
                sb.AppendLine(inventoryItem.ItemData.BaseName);
                sb.AppendLine(inventoryItem.ItemData.ClassName);
                sb.AppendLine(inventoryItem.ItemData.Path.ToString());
            }

            File.WriteAllText(path, sb.ToString());
        }

        private bool CheckIgnoreCells(NormalInventoryItem inventItem)
        {
            var inventPosX = inventItem.InventPosX;
            var inventPosY = inventItem.InventPosY;

            if (Settings.RefillCurrency &&
                _customRefills.Any(x => x.InventPos.X == inventPosX && x.InventPos.Y == inventPosY))
                return true;

            if (inventPosX < 0 || inventPosX >= 12) return true;

            if (inventPosY < 0 || inventPosY >= 5) return true;

            return Settings.IgnoredCells[inventPosY, inventPosX] != 0; //No need to check all item size
        }

        private FilterResult CheckFilters(ItemData itemData)
        {
            foreach (var filter in _customFilters)
                try
                {
                    if (!filter.AllowProcess) continue;

                    if (filter.CompareItem(itemData)) return new FilterResult(filter, itemData);
                }
                catch (Exception ex)
                {
                    DebugWindow.LogError($"Check filters error: {ex}");
                }

            return null;
        }

        private IEnumerator DropToStash()
        {
            _coroutineIteration++;

            yield return DropItemsToStash();
        }

        private IEnumerator DropItemsToStash()
        {
            var tries = 0;
            NormalInventoryItem lastHoverItem = null;
            PublishEvent("stashie_start_drop_items", null);

            while (_dropItems.Count > 0 && tries < 2)
            {
                tries++;
                _visibleStashIndex = -1;
                _visibleStashIndex = GetIndexOfCurrentVisibleTab();
                var sortedByStash = _dropItems.OrderByDescending(x => x.StashIndex == _visibleStashIndex)
                    .ThenBy(x => x.StashIndex).ToList();
                var ingameStateCurLatency = GameController.Game.IngameState.CurLatency;
                var latency = (int) ingameStateCurLatency + Settings.ExtraDelay;
                Input.KeyDown(Keys.LControlKey);
                var waitedItems = new List<FilterResult>(8);
                var dropItemsToStashWaitTime = new WaitTime(latency);
                yield return new WaitTime((int) ingameStateCurLatency);
                LogMessage($"Want drop {sortedByStash.Count} items.");

                foreach (var stashResults in sortedByStash)
                {
                    _coroutineIteration++;
                    _coroutineWorker?.UpdateTicks(_coroutineIteration);
                    var tryTime = _debugTimer.ElapsedMilliseconds + 2000 + latency;

                    if (stashResults.StashIndex != _visibleStashIndex)
                    {
                        _stackItemTimer.Restart();
                        var waited = waitedItems.Count > 0;

                        while (waited)
                        {
                            waited = false;

                            var visibleInventoryItems = GameController.Game.IngameState.IngameUi
                                .InventoryPanel[InventoryIndex.PlayerInventory]
                                .VisibleInventoryItems;

                            foreach (var waitedItem in waitedItems)
                            {
                                var contains = visibleInventoryItems.Contains(waitedItem.ItemData.InventoryItem);

                                if (!contains) continue;
                                Input.SetCursorPos(waitedItem.ClickPos + _clickWindowOffset);
                                yield return _wait3Ms;

                                Input.Click(MouseButtons.Left);
                                yield return _wait10Ms;
                                Input.MouseMove();
                                waited = true;
                            }

                            yield return new WaitTime(100);

                            PublishEvent("stashie_finish_drop_items_to_stash_tab", null);

                            if (!waited) waitedItems.Clear();

                            if (_debugTimer.ElapsedMilliseconds > tryTime)
                            {
                                LogMessage($"Error while waiting items {waitedItems.Count}");
                                yield break;
                            }

                            yield return dropItemsToStashWaitTime;

                            if (_stackItemTimer.ElapsedMilliseconds > 1000 + latency)
                                break;
                        }

                        yield return SwitchToTab(stashResults.StashIndex);
                    }

                    var visibleInventory =
                        GameController.IngameState.IngameUi.StashElement.AllInventories[_visibleStashIndex];

                    while (visibleInventory == null)
                    {
                        visibleInventory =
                            GameController.IngameState.IngameUi.StashElement.AllInventories[_visibleStashIndex];
                        yield return _wait10Ms;

                        if (_debugTimer.ElapsedMilliseconds <= tryTime + 2000) continue;
                        LogMessage($"Error while loading tab, Index: {_visibleStashIndex}");
                        yield break;
                    }

                    while (GetTypeOfCurrentVisibleStash() == InventoryType.InvalidInventory)
                    {
                        yield return dropItemsToStashWaitTime;

                        if (_debugTimer.ElapsedMilliseconds <= tryTime) continue;
                        LogMessage($"Error with inventory type, Index: {_visibleStashIndex}");
                        yield break;
                    }

                    var inventory = GameController.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory];
                    Input.SetCursorPos(stashResults.ClickPos + _clickWindowOffset);

                    while (inventory.HoverItem == null)
                    {
                        Input.SetCursorPos(stashResults.ClickPos + _clickWindowOffset);
                        yield return _wait3Ms;

                        if (_debugTimer.ElapsedMilliseconds <= tryTime) continue;
                        LogMessage($"Error while wait hover item null, Index: {_visibleStashIndex}");
                        yield break;
                    }

                    if (lastHoverItem != null)
                        while (inventory.HoverItem == null || inventory.HoverItem.Address == lastHoverItem.Address)
                        {
                            Input.SetCursorPos(stashResults.ClickPos + _clickWindowOffset);
                            yield return _wait3Ms;

                            if (_debugTimer.ElapsedMilliseconds <= tryTime) continue;
                            LogMessage($"Error while wait hover item, Index: {_visibleStashIndex}");
                            yield break;
                        }

                    lastHoverItem = inventory.HoverItem;
                    Input.Click(MouseButtons.Left);
                    yield return _wait10Ms;
                    yield return dropItemsToStashWaitTime;
                    var typeOfCurrentVisibleStash = GetTypeOfCurrentVisibleStash();

                    if (typeOfCurrentVisibleStash == InventoryType.MapStash ||
                        typeOfCurrentVisibleStash == InventoryType.DivinationStash)
                        waitedItems.Add(stashResults);

                    _debugTimer.Restart();

                    PublishEvent("stashie_finish_drop_items_to_stash_tab", null);
                }

                if (Settings.VisitTabWhenDone.Value) yield return SwitchToTab(Settings.TabToVisitWhenDone.Value);

                Input.KeyUp(Keys.LControlKey);
                yield return ParseItems();
            }

            PublishEvent("stashie_stop_drop_items", null);
        }

        #region Refill

        private IEnumerator ProcessRefills()
        {
            if (!Settings.RefillCurrency.Value || _customRefills.Count == 0) yield break;

            if (Settings.CurrencyStashTab.Index == -1)
            {
                LogError("Can't process refill: CurrencyStashTab is not set.", 5);
                yield break;
            }

            var delay = (int) GameController.Game.IngameState.CurLatency + Settings.ExtraDelay.Value;
            var currencyTabVisible = false;
            var inventory = GameController.Game.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory];
            var stashItems = inventory.VisibleInventoryItems;

            if (stashItems == null)
            {
                LogError("Can't process refill: VisibleInventoryItems is null!", 5);
                yield break;
            }

            _customRefills.ForEach(x => x.Clear());
            var filledCells = new int[5, 12];

            foreach (var inventItem in stashItems)
            {
                var item = inventItem.Item;
                if (item == null) continue;

                if (!Settings.AllowHaveMore.Value)
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
                }

                if (!item.HasComponent<ExileCore.PoEMemory.Components.Stack>()) continue;

                foreach (var refill in _customRefills)
                {
                    var bit = GameController.Files.BaseItemTypes.Translate(item.Path);
                    if (bit.BaseName != refill.CurrencyClass) continue;

                    var stack = item.GetComponent<ExileCore.PoEMemory.Components.Stack>();
                    refill.OwnedCount = stack.Size;
                    refill.ClickPos = inventItem.GetClientRect().Center;

                    if (refill.OwnedCount < 0 || refill.OwnedCount > 40)
                    {
                        LogError(
                            $"Ignoring refill: {refill.CurrencyClass}: Stack size {refill.OwnedCount} not in range 0-40 ",
                            5);
                        refill.OwnedCount = -1;
                    }

                    break;
                }
            }

            var inventoryRec = inventory.InventoryUIElement.GetClientRect();
            var cellSize = inventoryRec.Width / 12;
            var freeCellFound = false;
            var freeCelPos = new Point();

            if (!Settings.AllowHaveMore.Value)
                for (var x = 0; x <= 11; x++)
                {
                    for (var y = 0; y <= 4; y++)
                    {
                        if (filledCells[y, x] != 0) continue;

                        freeCellFound = true;
                        freeCelPos = new Point(x, y);
                        break;
                    }

                    if (freeCellFound) break;
                }

            foreach (var refill in _customRefills)
            {
                if (refill.OwnedCount == -1) continue;

                if (refill.OwnedCount == refill.AmountOption.Value) continue;

                if (refill.OwnedCount < refill.AmountOption.Value)

                    #region Refill

                {
                    if (!currencyTabVisible)
                    {
                        if (Settings.CurrencyStashTab.Index != _visibleStashIndex)
                        {
                            yield return SwitchToTab(Settings.CurrencyStashTab.Index);
                        }
                        else
                        {
                            currencyTabVisible = true;
                            yield return new WaitTime(delay);
                        }
                    }

                    var moveCount = refill.AmountOption.Value - refill.OwnedCount;
                    var currentStashItems = GameController.Game.IngameState.IngameUi.StashElement.VisibleStash
                        .VisibleInventoryItems;

                    var foundSourceOfRefill = currentStashItems
                        .Where(x => GameController.Files.BaseItemTypes.Translate(x.Item.Path).BaseName ==
                                    refill.CurrencyClass).ToList();

                    foreach (var sourceOfRefill in foundSourceOfRefill)
                    {
                        var stackSize = sourceOfRefill.Item.GetComponent<ExileCore.PoEMemory.Components.Stack>().Size;
                        var getCurCount = moveCount > stackSize ? stackSize : moveCount;
                        var destination = refill.ClickPos;

                        if (refill.OwnedCount == 0)
                        {
                            destination = GetInventoryClickPosByCellIndex(inventory, refill.InventPos.X,
                                refill.InventPos.Y, cellSize);

                            // If cells is not free then continue.
                            if (GameController.Game.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory][
                                refill.InventPos.X, refill.InventPos.Y, 12] != null)
                            {
                                moveCount--;
                                LogMessage(
                                    $"Inventory ({refill.InventPos.X}, {refill.InventPos.Y}) is occupied by the wrong item!",
                                    5);
                                continue;
                            }
                        }

                        yield return SplitStack(moveCount, sourceOfRefill.GetClientRect().Center, destination);
                        moveCount -= getCurCount;
                        if (moveCount == 0) break;
                    }

                    if (moveCount > 0)
                        LogMessage($"Not enough currency (need {moveCount} more) to fill {refill.CurrencyClass} stack",
                            5);
                }

                #endregion

                else if (!Settings.AllowHaveMore.Value && refill.OwnedCount > refill.AmountOption.Value)

                    #region Devastate

                {
                    if (!freeCellFound)
                    {
                        LogMessage("Can\'t find free cell in player inventory to move excess currency.", 5);
                        continue;
                    }

                    if (!currencyTabVisible)
                    {
                        if (Settings.CurrencyStashTab.Index != _visibleStashIndex)
                        {
                            yield return SwitchToTab(Settings.CurrencyStashTab.Index);
                            continue;
                        }

                        currencyTabVisible = true;
                        yield return new WaitTime(delay);
                    }

                    var destination = GetInventoryClickPosByCellIndex(inventory, freeCelPos.X, freeCelPos.Y, cellSize) +
                                      _clickWindowOffset;
                    var moveCount = refill.OwnedCount - refill.AmountOption.Value;
                    yield return new WaitTime(delay);
                    yield return SplitStack(moveCount, refill.ClickPos, destination);
                    yield return new WaitTime(delay);
                    Input.KeyDown(Keys.LControlKey);

                    yield return Input.SetCursorPositionSmooth(destination + _clickWindowOffset);
                    yield return new WaitTime(Settings.ExtraDelay);
                    Input.Click(MouseButtons.Left);
                    Input.MouseMove();
                    Input.KeyUp(Keys.LControlKey);
                    yield return new WaitTime(delay);
                }

                #endregion
            }
        }

        private static Vector2 GetInventoryClickPosByCellIndex(Inventory inventory, int indexX, int indexY,
            float cellSize)
        {
            return inventory.InventoryUIElement.GetClientRect().TopLeft +
                   new Vector2(cellSize * (indexX + 0.5f), cellSize * (indexY + 0.5f));
        }

        private IEnumerator SplitStack(int amount, Vector2 from, Vector2 to)
        {
            var delay = (int) GameController.Game.IngameState.CurLatency * 2 + Settings.ExtraDelay;
            Input.KeyDown(Keys.ShiftKey);

            while (!Input.IsKeyDown(Keys.ShiftKey)) yield return new WaitTime(WhileDelay);

            yield return Input.SetCursorPositionSmooth(from + _clickWindowOffset);
            yield return new WaitTime(Settings.ExtraDelay);
            Input.Click(MouseButtons.Left);
            Input.MouseMove();
            yield return new WaitTime(InputDelay);
            Input.KeyUp(Keys.ShiftKey);
            yield return new WaitTime(InputDelay + 50);

            if (amount > 40)
            {
                LogMessage("Can't select amount more than 40, current value: " + amount, 5);
                amount = 40;
            }

            if (amount < 10)
            {
                var keyToPress = (int) Keys.D0 + amount;
                yield return Input.KeyPress((Keys) keyToPress);
            }
            else
            {
                var keyToPress = (int) Keys.D0 + amount / 10;
                yield return Input.KeyPress((Keys) keyToPress);
                yield return new WaitTime(delay);
                keyToPress = (int) Keys.D0 + amount % 10;
                yield return Input.KeyPress((Keys) keyToPress);
            }

            yield return new WaitTime(delay);
            yield return Input.KeyPress(Keys.Enter);
            yield return new WaitTime(delay + InputDelay);

            yield return Input.SetCursorPositionSmooth(to + _clickWindowOffset);
            yield return new WaitTime(Settings.ExtraDelay);
            Input.Click(MouseButtons.Left);

            yield return new WaitTime(delay + InputDelay);
        }

        #endregion

        #region Switching between StashTabs

        public IEnumerator SwitchToTab(int tabIndex)
        {
            var latency = (int) GameController.Game.IngameState.CurLatency;

            // We don't want to Switch to a tab that we are already on
            //var stashPanel = GameController.Game.IngameState.IngameUi.StashElement;

            _visibleStashIndex = GetIndexOfCurrentVisibleTab();
            var travelDistance = Math.Abs(tabIndex - _visibleStashIndex);
            if (travelDistance == 0) yield break;

            // We want to maximum wait 20 times the Current Latency before giving up in our while loops.
            //var maxNumberOfTries = latency * 20 > 2000 ? latency * 20 / WHILE_DELAY : 2000 / WHILE_DELAY;
            if (travelDistance > 3 && !Settings.UseArrow)
            {
                yield return SwitchToTabViaDropdownMenu(tabIndex);
                yield return new WaitTime(latency + Settings.ExtraDelay);
            }
            else
            {
                yield return SwitchToTabViaArrowKeys(tabIndex);
                yield return new WaitTime(latency + Settings.ExtraDelay);
            }

            _visibleStashIndex = GetIndexOfCurrentVisibleTab();
        }

        private IEnumerator SwitchToTabViaArrowKeys(int tabIndex)
        {
            var indexOfCurrentVisibleTab = GetIndexOfCurrentVisibleTab();
            var difference = tabIndex - indexOfCurrentVisibleTab;
            var tabIsToTheLeft = difference < 0;
            var retry = 0;
            var waitTime = new WaitTime(Settings.ExtraDelay);

            while (GetIndexOfCurrentVisibleTab() != tabIndex && retry < 3)
            {
                for (var i = 0; i < Math.Abs(difference); i++)
                {
                    Input.KeyDown(tabIsToTheLeft ? Keys.Left : Keys.Right);
                    Input.KeyUp(tabIsToTheLeft ? Keys.Left : Keys.Right);
                    yield return waitTime;

                    // yield return Input.KeyPress(tabIsToTheLeft ? Keys.Left : Keys.Right);
                }

                yield return new WaitTime(20);
                retry++;
            }
        }

        private IEnumerator SwitchToTabViaDropdownMenu(int tabIndex)
        {
            var latency = (int) GameController.Game.IngameState.CurLatency;
            var viewAllTabsButton = GameController.Game.IngameState.IngameUi.StashElement.ViewAllStashButton;
            var waitTime = new WaitTime(Settings.ExtraDelay);

            var dropdownMenu = GameController.Game.IngameState.IngameUi.StashElement.ViewAllStashPanel;
            var allTabsButton = viewAllTabsButton.GetClientRect();
            var slider = _stashCount > MaxShownSidebarStashTabs;
            if (!dropdownMenu.IsVisible)
            {
                Input.SetCursorPos(allTabsButton.Center);
                yield return _wait3Ms;
                Input.MouseMove();
                Input.LeftDown();
                yield return _wait1Ms;
                Input.LeftUp();
                yield return _wait10Ms;
                //wait for the dropdown menu to become visible
                if (!dropdownMenu.IsVisible)
                {
                    LogError($"Error in opening DropdownMenu.", 5);
                    yield break;
                }
            }

            // Make sure that we are scrolled to the top in the menu.
            if (slider)
                for (var i = 0; i < _stashCount - MaxShownSidebarStashTabs + 1; ++i)
                {
                    Input.KeyDown(Keys.Up);
                    yield return _wait1Ms;
                    Input.KeyUp(Keys.Up);
                    yield return _wait1Ms;
                }

            //get click position of tab label
            for (var i = 0; i < tabIndex; ++i)
            {
                Input.KeyDown(Keys.Down);
                yield return _wait1Ms;
                Input.KeyUp(Keys.Down);
                yield return _wait1Ms;
            }

            //enter-key Method
            Input.KeyDown(Keys.Enter);
            yield return _wait1Ms;
            Input.KeyUp(Keys.Enter);
            yield return _wait1Ms;

            //reset Slider position
            if (!slider) yield break;
            {
                if (!dropdownMenu.IsVisible)
                {
                    //opening DropdownMenu
                    Input.SetCursorPos(allTabsButton.Center);
                    yield return _wait10Ms;
                    Input.MouseMove();
                    Input.LeftDown();
                    yield return _wait1Ms;
                    Input.LeftUp();
                    yield return _wait10Ms;
                    // wait for the dropdown menu to become visible.
                    for (var count = 0; !dropdownMenu.IsVisible && count <= 20; ++count)
                        yield return new WaitTime(latency);

                    if (!dropdownMenu.IsVisible)
                    {
                        LogError($"Error in Scrolling back to the top.", 5);
                        yield break;
                    }
                }

                //"scrolling" back up
                for (var i = 0; i < _stashCount - MaxShownSidebarStashTabs + 1; ++i)
                {
                    Input.KeyDown(Keys.Up);
                    yield return new WaitTime(1);
                    Input.KeyUp(Keys.Up);
                    yield return new WaitTime(1);
                }
            }
        }

        private int GetIndexOfCurrentVisibleTab()
        {
            return GameController.Game.IngameState.IngameUi.StashElement.IndexVisibleStash;
        }

        private InventoryType GetTypeOfCurrentVisibleStash()
        {
            var stashPanelVisibleStash = GameController.Game.IngameState.IngameUi?.StashElement?.VisibleStash;
            return stashPanelVisibleStash?.InvType ?? InventoryType.InvalidInventory;
        }

        #endregion

        #region Stashes update

        private void OnSettingsStashNameChanged(ListIndexNode node, string newValue)
        {
            node.Index = GetInventIndexByStashName(newValue);
        }

        public override void OnClose()
        {
        }

        private void SetupOrClose()
        {
            SaveDefaultConfigsToDisk();
            _settingsListNodes = new List<ListIndexNode>(100);
            LoadCustomRefills();
            LoadCustomFilters();

            try
            {
                Settings.TabToVisitWhenDone.Max =
                    (int) GameController.Game.IngameState.IngameUi.StashElement.TotalStashes - 1;
                var names = GameController.Game.IngameState.IngameUi.StashElement.AllStashNames;
                UpdateStashNames(names);
            }
            catch (Exception e)
            {
                LogError($"Cant get stash names when init. {e}");
            }
        }

        private int GetInventIndexByStashName(string name)
        {
            var index = _renamedAllStashNames.IndexOf(name);
            if (index != -1) index--;

            return index;
        }

        private List<string> _renamedAllStashNames;

        private void UpdateStashNames(ICollection<string> newNames)
        {
            Settings.AllStashNames = newNames.ToList();

            if (newNames.Count < 4)
            {
                LogError("Can't parse names.");
                return;
            }

            _renamedAllStashNames = new List<string> {"Ignore"};
            var settingsAllStashNames = Settings.AllStashNames;

            for (var i = 0; i < settingsAllStashNames.Count; i++)
            {
                var realStashName = settingsAllStashNames[i];

                if (_renamedAllStashNames.Contains(realStashName))
                {
                    realStashName += " (" + i + ")";
#if DebugMode
                    LogMessage("Stashie: fixed same stash name to: " + realStashName, 3);
#endif
                }

                _renamedAllStashNames.Add(realStashName ?? "%NULL%");
            }

            Settings.AllStashNames.Insert(0, "Ignore");

            foreach (var lOption in _settingsListNodes)
                try
                {
                    lOption.SetListValues(_renamedAllStashNames);
                    var inventoryIndex = GetInventIndexByStashName(lOption.Value);

                    if (inventoryIndex == -1) //If the value doesn't exist in list (renamed)
                    {
                        if (lOption.Index != -1) //If the value doesn't exist in list and the value was not Ignore
                        {
#if DebugMode
                        LogMessage("Tab renamed : " + lOption.Value + " to " + _renamedAllStashNames[lOption.Index + 1],
                            5);
#endif
                            if (lOption.Index + 1 >= _renamedAllStashNames.Count)
                            {
                                lOption.Index = -1;
                                lOption.Value = _renamedAllStashNames[0];
                            }
                            else
                            {
                                lOption.Value = _renamedAllStashNames[lOption.Index + 1]; //    Just update it's name
                            }
                        }
                        else
                        {
                            lOption.Value =
                                _renamedAllStashNames[0]; //Actually it was "Ignore", we just update it (can be removed)
                        }
                    }
                    else //tab just change it's index
                    {
#if DebugMode
                    if (lOption.Index != inventoryIndex)
                    {
                        LogMessage("Tab moved: " + lOption.Index + " to " + inventoryIndex, 5);
                    }
#endif
                        lOption.Index = inventoryIndex;
                        lOption.Value = _renamedAllStashNames[inventoryIndex + 1];
                    }
                }
                catch (Exception e)
                {
                    DebugWindow.LogError($"UpdateStashNames _settingsListNodes {e}");
                }

            GenerateMenu();
        }

        private static readonly WaitTime Wait2Sec = new WaitTime(2000);
        private static readonly WaitTime Wait1Sec = new WaitTime(1000);
        private uint _counterStashTabNamesCoroutine;

        public IEnumerator StashTabNamesUpdater_Thread()
        {
            while (true)
            {
                while (!GameController.Game.IngameState.InGame) yield return Wait2Sec;

                var stashPanel = GameController.Game.IngameState?.IngameUi?.StashElement;

                while (stashPanel == null || !stashPanel.IsVisibleLocal) yield return Wait1Sec;

                _counterStashTabNamesCoroutine++;
                _stashTabNamesCoroutine?.UpdateTicks(_counterStashTabNamesCoroutine);
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
                    if (cachedName.Equals(realNames[index])) continue;

                    UpdateStashNames(realNames);
                    break;
                }

                yield return Wait1Sec;
            }
        }

        #endregion
    }
}