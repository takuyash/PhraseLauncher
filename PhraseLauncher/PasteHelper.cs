using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;
namespace PhraseLauncher
{

    static class PasteHelper
    {
        public static void Paste(string text)
        {
            Clipboard.SetText(text);
            Timer t = new() { Interval = 100 };
            t.Tick += (s, e) =>
            {
                t.Stop();
                SendKeys.SendWait("^v");
            };
            t.Start();
        }
    }
}
