using System;
using System.Windows.Forms;
using WenBrowser.UI.Forms;

namespace WenBrowser;

static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }
}