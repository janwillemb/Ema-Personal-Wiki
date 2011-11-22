using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Forms;

namespace EmaPersonalWiki
{
    internal class HotKeyWinApi
    {
        public const int WmHotKey = 0x0312;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, ModifierKeys fsModifiers, Keys vk);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}
