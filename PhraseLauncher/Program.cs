using PhraseLauncher;
using System;
using System.Windows.Forms;

static class Program
{
    public static HiddenForm HiddenForm;
    public static Form JsonForm;

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        NotifyIcon tray = new()
        {
            Icon = new Icon(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico")),
            Visible = true,
            Text = "PhraseLauncher"
        };

        ContextMenuStrip menu = new();
        menu.Items.Add(new ToolStripMenuItem("ˆê——•\Ž¦", null, (s, e) => JsonListForm.Show()));
        menu.Items.Add(new ToolStripMenuItem("•ÒW/“o˜^", null, (s, e) => new JsonEditorForm().Show()));
        menu.Items.Add(new ToolStripMenuItem("ƒwƒ‹ƒv", null, (s, e) => new HelpForm().Show()));
        menu.Items.Add(new ToolStripMenuItem("I—¹", null, (s, e) => Application.Exit()));
        tray.ContextMenuStrip = menu;

        HiddenForm = new HiddenForm { ShowInTaskbar = false, Opacity = 0 };
        HiddenForm.Show();

        Application.Run();
    }
}
