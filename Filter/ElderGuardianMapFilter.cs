namespace Stashie
{
    public class ElderGuardianMapFilter : IIFilter
    {
        public bool isElderGuardianMap;

        public bool CompareItem(ItemData itemData)
        {
            return itemData.isElderGuardianMap == isElderGuardianMap;
        }
    }
}
