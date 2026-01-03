using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace PhraseLauncher
{


    class HiddenForm : Form
    {
        Timer timer = new() { Interval = 50 };
        HashSet<Keys> pressed = new();

        public HiddenForm()
        {
            Load += (s, e) =>
                NativeMethods.RegisterHotKey(
                    Handle,
                    NativeMethods.HOTKEY_ID,
                    NativeMethods.MOD_CONTROL_SHIFT,
                    NativeMethods.VK_SPACE);

            FormClosing += (s, e) =>
                NativeMethods.UnregisterHotKey(Handle, NativeMethods.HOTKEY_ID);

            timer.Tick += Tick;
            timer.Start();
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
