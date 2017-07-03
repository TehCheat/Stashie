using System.Runtime.InteropServices;

namespace Stashie.Utils
{
    public class WinApi
    {
        [DllImport("user32.dll")]
        public static extern bool BlockInput(bool fBlockIt);
    }
}