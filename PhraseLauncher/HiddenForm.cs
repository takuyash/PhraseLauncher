using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace PhraseLauncher
{
    class HiddenForm : Form
    {
        Timer timer = new() { Interval = 50 };
        HashSet<Keys> pressed = new();

        // --- Hook Fields ---
        private static IntPtr _hookID = IntPtr.Zero;
        private NativeMethods.LowLevelKeyboardProc _proc;
        private DateTime _lastCtrlTime = DateTime.MinValue;
        private const int DOUBLE_PRESS_MS = 300;
        private bool _isCtrlPressed = false;

        public HiddenForm()
        {
            _proc = HookCallback;

            Load += (s, e) =>
            {
                NativeMethods.RegisterHotKey(
                    Handle,
                    NativeMethods.HOTKEY_ID,
                    NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT,
                    NativeMethods.VK_O);

                _hookID = SetHook(_proc);
            };

            FormClosing += (s, e) =>
            {
                NativeMethods.UnregisterHotKey(Handle, NativeMethods.HOTKEY_ID);
                NativeMethods.UnhookWindowsHookEx(_hookID);
            };

            timer.Tick += Tick;
            timer.Start();
        }

        private IntPtr SetHook(NativeMethods.LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return NativeMethods.SetWindowsHookEx(NativeMethods.WH_KEYBOARD_LL, proc,
                    NativeMethods.GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);

                // UP: 押し下げ状態解除
                if (wParam == (IntPtr)NativeMethods.WM_KEYUP || wParam == (IntPtr)NativeMethods.WM_SYSKEYUP)
                {
                    if (vkCode == NativeMethods.VK_LCONTROL || vkCode == NativeMethods.VK_RCONTROL)
                    {
                        _isCtrlPressed = false;
                    }
                }

                // DOWN: 判定
                if (wParam == (IntPtr)NativeMethods.WM_KEYDOWN || wParam == (IntPtr)NativeMethods.WM_SYSKEYDOWN)
                {
                    if (vkCode == NativeMethods.VK_LCONTROL || vkCode == NativeMethods.VK_RCONTROL)
                    {
                        if (!_isCtrlPressed)
                        {
                            _isCtrlPressed = true;
                            var now = DateTime.Now;
                            if ((now - _lastCtrlTime).TotalMilliseconds <= DOUBLE_PRESS_MS)
                            {
                                JsonListForm.Show();
                                _lastCtrlTime = DateTime.MinValue;
                            }
                            else
                            {
                                _lastCtrlTime = now;
                            }
                        }
                    }
                    else
                    {
                        _lastCtrlTime = DateTime.MinValue;
                    }
                }
            }
            return NativeMethods.CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeMethods.WM_HOTKEY)
                JsonListForm.Show();

            base.WndProc(ref m);
        }

        void Tick(object sender, EventArgs e)
        {
            if (Program.JsonForm == null || Program.JsonForm.IsDisposed) return;

            var tab = Program.JsonForm.Controls[0] as TabControl;
            var list = JsonListForm.AllTemplates[tab.SelectedIndex];

            for (int i = 0; i < 9; i++)
            {
                HandleKey((Keys)(Keys.D1 + i), i, list);
                HandleKey((Keys)(Keys.NumPad1 + i), i, list);
            }
        }

        void HandleKey(Keys key, int index, List<TemplateItem> list)
        {
            if ((NativeMethods.GetAsyncKeyState(key) & 0x8000) != 0)
            {
                if (!pressed.Contains(key))
                {
                    pressed.Add(key);
                    if (index < list.Count)
                    {
                        PasteHelper.Paste(list[index].text);
                        Program.JsonForm.Close();
                        Program.JsonForm = null;
                    }
                }
            }
            else
            {
                pressed.Remove(key);
            }
        }
    }
}