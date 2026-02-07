using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace PhraseLauncher
{

    class KeyboardManager : NativeWindow, IDisposable
    {
        private Timer _timer = new() { Interval = 50 };
        private HashSet<Keys> _pressedKeys = new();
        private IntPtr _hookID = IntPtr.Zero;
        private NativeMethods.LowLevelKeyboardProc _proc;

        private DateTime _lastCtrlTime = DateTime.MinValue;
        private int _pressCount = 0;
        private const int DOUBLE_PRESS_MS = 300;
        private bool _isCtrlPressed = false;

        public KeyboardManager()
        {
            // ホットキー受信用のハンドルを作成
            this.CreateHandle(new CreateParams());

            _proc = HookCallback;
            _hookID = SetHook(_proc);

            // ホットキー登録 (Ctrl + Shift + O)
            NativeMethods.RegisterHotKey(this.Handle, NativeMethods.HOTKEY_ID,
                NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT, NativeMethods.VK_O);

            _timer.Tick += OnTimerTick;
            _timer.Start();
        }

        // メッセージループの監視（ホットキーはここで受け取る）
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeMethods.WM_HOTKEY && (int)m.WParam == NativeMethods.HOTKEY_ID)
            {
                if (LanguageManager.EnableHotKey)
                    JsonListForm.Show();
            }
            base.WndProc(ref m);
        }

        private IntPtr SetHook(NativeMethods.LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return NativeMethods.SetWindowsHookEx(
                    NativeMethods.WH_KEYBOARD_LL,
                    proc,
                    NativeMethods.GetModuleHandle(curModule.ModuleName),
                    0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);

                if (wParam == (IntPtr)NativeMethods.WM_KEYUP ||
                    wParam == (IntPtr)NativeMethods.WM_SYSKEYUP)
                {
                    if (vkCode == NativeMethods.VK_LCONTROL ||
                        vkCode == NativeMethods.VK_RCONTROL)
                    {
                        _isCtrlPressed = false;
                    }
                }

                if (wParam == (IntPtr)NativeMethods.WM_KEYDOWN ||
                    wParam == (IntPtr)NativeMethods.WM_SYSKEYDOWN)
                {
                    if (vkCode == NativeMethods.VK_LCONTROL ||
                        vkCode == NativeMethods.VK_RCONTROL)
                    {
                        if (_isCtrlPressed)
                            return NativeMethods.CallNextHookEx(_hookID, nCode, wParam, lParam);

                        _isCtrlPressed = true;

                        var now = DateTime.Now;
                        _pressCount = (now - _lastCtrlTime).TotalMilliseconds <= DOUBLE_PRESS_MS
                            ? _pressCount + 1
                            : 1;

                        _lastCtrlTime = now;

                        if (_pressCount >= LanguageManager.CtrlPressCount)
                        {
                            if (LanguageManager.EnableHotKey)
                                JsonListForm.Show();

                            _pressCount = 0;
                            _lastCtrlTime = DateTime.MinValue;
                        }
                    }
                    else
                    {
                        _pressCount = 0;
                        _lastCtrlTime = DateTime.MinValue;
                    }
                }
            }
            return NativeMethods.CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            // JsonFormが開いていない、またはテキストボックスにフォーカスがある場合は無効
            if (Program.JsonForm == null || Program.JsonForm.IsDisposed) return;
            if (GetFocusedControl(Program.JsonForm) is TextBox) return;

            var tab = Program.JsonForm.Controls.Find("tab", true);
            if (tab.Length == 0 || !(tab[0] is TabControl tabCtrl)) return;
            if (tabCtrl.SelectedTab.Controls.Count == 0) return;

            var lb = tabCtrl.SelectedTab.Controls[0] as ListBox;
            if (lb?.Tag is List<TemplateItem> currentList)
            {
                for (int i = 0; i < 9; i++)
                {
                    HandleNumericKey((Keys)(Keys.D1 + i), i, currentList);
                    HandleNumericKey((Keys)(Keys.NumPad1 + i), i, currentList);
                }
            }
        }

        private void HandleNumericKey(Keys key, int index, List<TemplateItem> list)
        {
            if ((NativeMethods.GetAsyncKeyState(key) & 0x8000) != 0)
            {
                if (!_pressedKeys.Contains(key))
                {
                    _pressedKeys.Add(key);
                    if (index < list.Count)
                    {
                        PasteHelper.Paste(list[index].text);
                        Program.JsonForm.Close();
                    }
                }
            }
            else { _pressedKeys.Remove(key); }
        }

        private Control GetFocusedControl(Control parent)
        {
            if (parent is ContainerControl container && container.ActiveControl != null)
                return GetFocusedControl(container.ActiveControl);
            return parent;
        }

        public void Dispose()
        {
            _timer.Stop();
            NativeMethods.UnregisterHotKey(this.Handle, NativeMethods.HOTKEY_ID);
            NativeMethods.UnhookWindowsHookEx(_hookID);
            this.DestroyHandle();
        }
    }
}