using System.Runtime.InteropServices;

namespace Stashie
{
    public class Mouse
    {
        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        public static POINT GetCursorPosition()
        {
            GetCursorPos(out POINT lpPoint);

            return lpPoint;
        }
    }
}