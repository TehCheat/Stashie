using SharpDX;
using PoeHUD.Hud.Settings;

namespace Stashie.Filters
{
    public class FilterResult
    {
        public StashTabNode StashNode;
        public Vector2 ClickPos;

        public FilterResult(StashTabNode stashNode, ItemData itemData)
        {
            StashNode = stashNode;
            ClickPos = itemData.GetClickPos();
        }
    }
}