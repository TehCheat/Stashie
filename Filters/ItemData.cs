using PoeHUD.Models;
using PoeHUD.Models.Enums;
using PoeHUD.Poe.Components;
using PoeHUD.Poe.Elements;
using SharpDX;
using System.IO;
using Map = PoeHUD.Poe.Components.Map;

namespace Stashie.Filters
{
    public class ItemData
    {
        private readonly NormalInventoryItem _inventoryItem;
        public string BaseName;
        public string Name;
        public string ProphecyName;
        public bool BIdentified;
        public string ClassName;
        public bool IsElder;
        public bool IsShaper;
        public bool IsCorrupted;
        public int ItemLevel;
        public int NumberOfSockets;
        public int LargetsLinkSize;
        public int ItemQuality;
        public int MapTier;

        public string Path;
        public ItemRarity Rarity;

        public ItemData( NormalInventoryItem inventoryItem, BaseItemType baseItemType )
        {
            _inventoryItem = inventoryItem;
            var item = inventoryItem.Item;
            Path = item.Path;
            var @base = item.GetComponent<Base>();
            IsElder = @base.isElder;
            IsShaper = @base.isShaper;
            IsCorrupted = @base.isCorrupted;
            var mods = item.GetComponent<Mods>();
            Rarity = mods.ItemRarity;
            BIdentified = mods.Identified;
            ItemLevel = mods.ItemLevel;
            
            var sockets = item.GetComponent<Sockets>();
            NumberOfSockets = sockets.NumberOfSockets;
            LargestLinkSize = sockets.LargestLinkSize;

            var quality = item.GetComponent<Quality>();
            ItemQuality = quality.ItemQuality;
            ClassName = baseItemType.ClassName;
            BaseName = baseItemType.BaseName;

            if ( @base.Name == "Prophecy" )
            {
                var @prophParse = item.GetComponent<Prophecy>();
                ProphecyName = @prophParse.DatProphecy.Name.ToLower( );
                ProphecyName = ProphecyName.Replace( " ", "" );
                ProphecyName = ProphecyName.Replace( ",", "" );
                ProphecyName = ProphecyName.Replace( "'", "" );
                Name = ProphecyName;
                BaseName = "Prophecy";
            }

            MapTier = item.HasComponent<Map>( ) ? item.GetComponent<Map>( ).Tier : 0;
        }

        public Vector2 GetClickPos( )
        {
            return _inventoryItem.GetClientRect( ).Center;
        }
    }
}
