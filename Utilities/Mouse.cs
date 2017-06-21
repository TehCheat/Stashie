using System.Runtime.InteropServices;
using System.Threading;
using SharpDX;

namespace Stashie.Utilities
{
    public class Mouse
    {
        public const int MouseEventfLeftdown = 0x02;
        public const int MouseEventfLeftup = 0x04;

        public const int MouseEventfMiddown = 0x0020;
        public const int MouseEventfMidup = 0x0040;

        public const int MouseEventfRightdown = 0x0008;
        public const int MouseEventfRightup = 0x0010;

        public const int MouseEventWheel = 0x800;


        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetCursorPos(int x, int y);

        public static POINT GetCursorPosition()
        {
            GetCursorPos(out POINT lpPoint);

            return lpPoint;
        }

        public static bool SetCursorPos(Vector2 position, RectangleF gameWindow)
        {
            return SetCursorPos((int) (gameWindow.X + position.X),
                (int) (gameWindow.Y + position.Y));
        }

        public static bool SetCursorPos(POINT position)
        {
            return SetCursorPos(position.X, position.Y);
        }

        public static void SetCursorPosAndLeftClick(Vector2 position, RectangleF gameWindow)
        {
            SetCursorPos((int) (gameWindow.X + position.X), (int) (gameWindow.Y + position.Y));
            Thread.Sleep(Constants.InputDelay);
            LeftButtonClick();
        }

        public static void VerticalScroll(bool forward, int clicks)
        {
            if (forward)
            {
                mouse_event(MouseEventWheel, 0, 0, clicks * 120, 0);
            }
            else
            {
                mouse_event(MouseEventWheel, 0, 0, -(clicks * 120), 0);
            }
        }

        public static void LeftButtonClick()
        {
            mouse_event(MouseEventfLeftdown, 0, 0, 0, 0);
            mouse_event(MouseEventfLeftup, 0, 0, 0, 0);
        }

        public static void MidButtonClick()
        {
            mouse_event(MouseEventfMiddown, 0, 0, 0, 0);
            mouse_event(MouseEventfMidup, 0, 0, 0, 0);
        }

        public static void LeftButtonDown()
        {
            mouse_event(MouseEventfLeftdown, 0, 0, 0, 0);
        }

        public static void LeftButtonUp()
        {
            mouse_event(MouseEventfLeftup, 0, 0, 0, 0);
        }

        public static void RightButtonDown()
        {
            mouse_event(MouseEventfRightdown, 0, 0, 0, 0);
        }

        public static void RightButtonUp()
        {
            mouse_event(MouseEventfRightup, 0, 0, 0, 0);
        }
    }
}