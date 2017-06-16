using System.Runtime.InteropServices;

namespace Stashie
{
    public class WinApi
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool BlockInput(bool fBlockIt);
    }
}