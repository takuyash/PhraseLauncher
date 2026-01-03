using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

static class NativeMethods
{
    public const int HOTKEY_ID = 9001;
    public const uint MOD_CONTROL_SHIFT = 0x0002 | 0x0004;
    public const uint VK_SPACE = 0x20;
    public const int WM_HOTKEY = 0x0312;

    [DllImport("user32.dll")]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(Keys vKey);
}
