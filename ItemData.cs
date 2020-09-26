using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.PoEMemory.Models;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using SharpDX;
using System.Linq;

namespace Stashie
{
    public class ItemData
    {
        public NormalInventoryItem InventoryItem { get; }
        public string Path { get; }
        public string ClassName { get; }
        public string BaseName { get; }
        public string Name { get; }
        public string Description { get; }
        public string ProphecyName { get; }
        public string ProphecyDescription { get; }
        //public string ClusterJewelBase { get; }
        public ItemRarity Rarity { get; }
        public int ItemQuality { get; }
        public int Veiled { get; }
        public int Fractured { get; }
        public int ItemLevel { get; }
        public int MapTier { get; }
        public int NumberOfSockets { get; }
        public int LargestLinkSize { get; }
        //public int ClusterJewelpassives { get; }
        public bool BIdentified { get; }
        public bool isCorrupted { get; }
        public bool isElder { get; }
        public bool isShaper { get; }
        public bool isCrusader { get; }
        public bool isRedeemer { get; }
        public bool isHunter { get; }
        public bool isWarlord { get; }
        public bool isInfluenced { get; }
        public bool Synthesised { get; }
        public bool isBlightMap { get; }
        public bool isElderGuardianMap { get; }
        public Vector2 clientRect { get; }

        public ItemData(NormalInventoryItem inventoryItem, BaseItemType baseItemType)
        {
            InventoryItem = inventoryItem;
            var item = inventoryItem.Item;
            Path = item.Path;
            var baseComponent = item.GetComponent<Base>();
            isElder = baseComponent.isElder;
            isShaper = baseComponent.isShaper;
            isCorrupted = baseComponent.isCorrupted;
            isCrusader = baseComponent.isCrusader;
            isRedeemer = baseComponent.isRedeemer;
            isWarlord = baseComponent.isWarlord;
            isHunter = baseComponent.isHunter;
            isInfluenced = isCrusader || isRedeemer || isWarlord || isHunter || isShaper || isElder;
            var mods = item.GetComponent<Mods>();
            Rarity = mods?.ItemRarity ?? ItemRarity.Normal;
            BIdentified = mods?.Identified ?? true;
            ItemLevel = mods?.ItemLevel ?? 0;
            Veiled = mods?.ItemMods.Where(m => m.DisplayName.Contains("Veil")).Count() ?? 0;
            Fractured = mods?.CountFractured ?? 0;
            Synthesised = mods?.Synthesised ?? false;
            isBlightMap = mods?.ItemMods.Where(m => m.Name.Contains("InfectedMap")).Count() > 0;
            isElderGuardianMap = mods?.ItemMods.Where(m => m.Name.Contains("MapElderContainsBoss")).Count() > 0;
            

            NumberOfSockets = item.GetComponent<Sockets>()?.NumberOfSockets ?? 0;
            LargestLinkSize = item.GetComponent<Sockets>()?.LargestLinkSize ?? 0;

            ItemQuality = item.GetComponent<Quality>()?.ItemQuality ?? 0;
            ClassName = baseItemType.ClassName;
            BaseName = baseItemType.BaseName;
            /*
            if (baseItemType.BaseName.Contains("Cluster"))
            {
                ClusterJewelpassives = int.Parse(new string(mods?.HumanStats.ElementAt(0).
                    SkipWhile(c => c < '0' || c > '9').TakeWhile(c => c >= '0' && c <= '9').ToArray()));
                ClusterJewelBase = mods?.HumanStats.ElementAt(1).ToString();
            }
            else
            {
                ClusterJewelpassives = 0;
                ClusterJewelBase = "";
            }*/

            Name = "";
            Description = "";
            MapTier = item.HasComponent<Map>() ? item.GetComponent<Map>().Tier : 0;
            clientRect = InventoryItem.GetClientRect().Center;
            
            if (@baseComponent.Name == "Prophecy")
            {
                var @prophParse = item.GetComponent<Prophecy>();
                ProphecyName = @prophParse.DatProphecy.Name.ToLower();
                ProphecyName = ProphecyName.Replace(" ", "");
                ProphecyName = ProphecyName.Replace(",", "");
                ProphecyName = ProphecyName.Replace("'", "");
                ProphecyDescription = @prophParse.DatProphecy.PredictionText.ToLower();
                ProphecyDescription = ProphecyDescription.Replace(" ", "");
                ProphecyDescription = ProphecyDescription.Replace(",", "");
                ProphecyDescription = ProphecyDescription.Replace("'", "");
                Description = ProphecyDescription;
                Name = ProphecyName;
                BaseName = "Prophecy";
            }
        }
        
        public Vector2 GetClickPosCache()
        {
            return clientRect;
        }

        public Vector2 GetClickPos()
        {
            var paddingPixels = 3;
            var clientRect = InventoryItem.GetClientRect();
            var x = MathHepler.Randomizer.Next((int) clientRect.TopLeft.X + paddingPixels, (int) clientRect.TopRight.X - paddingPixels);
            var y = MathHepler.Randomizer.Next((int) clientRect.TopLeft.Y + paddingPixels, (int) clientRect.BottomLeft.Y - paddingPixels);
            return new Vector2(x, y);
        }
    }
}
