using System;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;
namespace PhraseLauncher
{

    static class PasteHelper
    {
        /// <summary>
        /// 定型文を貼り付ける。空欄 {名前} があれば先に入力画面を出し、
        /// 入力値で置き換えてから貼り付け先のウィンドウに戻して貼り付ける。
        /// ランチャー画面を閉じてから呼ぶこと。
        /// </summary>
        public static void PasteTemplate(string? text, IntPtr targetWindow)
        {
            // 閉じたランチャーのキー処理中に入力画面（モーダル）を出さないよう、処理を抜けてから実行
            RunLater(10, () => Run(text ?? "", targetWindow));
        }

        private static void Run(string text, IntPtr targetWindow)
        {
            int delay = 40;

            var names = PlaceholderHelper.GetNames(text);
            if (names.Count > 0)
            {
                var values = ModernDialog.InputPlaceholders(names);
                if (values == null) return; // キャンセル時は貼り付けない

                text = PlaceholderHelper.Fill(text, values);

                // 入力画面に移ったフォーカスを貼り付け先へ戻す
                if (targetWindow != IntPtr.Zero)
                    NativeMethods.SetForegroundWindow(targetWindow);
                delay = 100;
            }

            if (text.Length == 0) return; // Clipboard.SetText は空文字で例外になる
            Clipboard.SetText(text);
            RunLater(delay, () => SendKeys.SendWait("^v"));
        }

        private static void RunLater(int interval, Action action)
        {
            Timer t = new() { Interval = interval };
            t.Tick += (s, e) =>
            {
                t.Stop();
                t.Dispose();
                action();
            };
            t.Start();
        }
    }
}
