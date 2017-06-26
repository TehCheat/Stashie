using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Stashie.Utilities
{
    public class Keyboard
    {
        private const int KeyeventfExtendedkey = 0x0001;
        private const int KeyeventfKeyup = 0x0002;

        private const int KeyPressed = 0x8000;
        private const int KeyToggled = 0x1;

        private const int ActionDelay = 100;

        [DllImport("user32.dll")]
        private static extern uint keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);


        public static void KeyDown(Keys key)
        {
            keybd_event((byte) key, 0, KeyeventfExtendedkey | 0, 0);
        }

        public static void KeyUp(Keys key)
        {
            keybd_event((byte) key, 0, KeyeventfExtendedkey | KeyeventfKeyup, 0); //0x7F
        }

        public static void KeyPress(Keys key)
        {
            KeyDown(key);
            Thread.Sleep(ActionDelay);
            KeyUp(key);
        }

        [DllImport("USER32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        public static bool IsKeyPressed(Keys key)
        {
            return Convert.ToBoolean(GetKeyState((int) key) & KeyPressed);
        }

        public static bool IsKeyToggled(Keys key)
        {
            return Convert.ToBoolean(GetKeyState((int) key) & KeyToggled);
        }
    }
}