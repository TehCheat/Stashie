#region Header

//-----------------------------------------------------------------
//   Class:          FilterResult
//   Description:    Result data from filter
//   Author:         Stridemann        Date: 08.26.2017
//-----------------------------------------------------------------

#endregion

using SharpDX;
using Stashie.Settings;

namespace Stashie.Filters
{
    public class FilterResult
    {
        public int StashIndex;
        public Vector2 ClickPos;

        public FilterResult(ListIndexNode option, ItemData itemData)
        {
            StashIndex = option.Index;
            ClickPos = itemData.GetClickPos();
        }
    }
}