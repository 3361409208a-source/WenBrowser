using System;
using System.Drawing;
using System.Windows.Forms;
using WenBrowser.Core;

namespace WenBrowser.UI.Forms;

public class WenBaseForm : Form
{
    public WenBaseForm()
    {
        this.FormBorderStyle = FormBorderStyle.None;
        this.Padding = new Padding(3);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_NCHITTEST)
        {
            Point pos = PointToClient(new Point(m.LParam.ToInt32()));
            int gripSize = 10;

            if (pos.X <= gripSize && pos.Y <= gripSize) { m.Result = (IntPtr)NativeMethods.HTTOPLEFT; return; }
            if (pos.X >= ClientSize.Width - gripSize && pos.Y <= gripSize) { m.Result = (IntPtr)NativeMethods.HTTOPRIGHT; return; }
            if (pos.X <= gripSize && pos.Y >= ClientSize.Height - gripSize) { m.Result = (IntPtr)NativeMethods.HTBOTTOMLEFT; return; }
            if (pos.X >= ClientSize.Width - gripSize && pos.Y >= ClientSize.Height - gripSize) { m.Result = (IntPtr)NativeMethods.HTBOTTOMRIGHT; return; }
            if (pos.X <= gripSize) { m.Result = (IntPtr)NativeMethods.HTLEFT; return; }
            if (pos.X >= ClientSize.Width - gripSize) { m.Result = (IntPtr)NativeMethods.HTRIGHT; return; }
            if (pos.Y <= gripSize) { m.Result = (IntPtr)NativeMethods.HTTOP; return; }
            if (pos.Y >= ClientSize.Height - gripSize) { m.Result = (IntPtr)NativeMethods.HTBOTTOM; return; }
        }
        base.WndProc(ref m);
    }

    protected void EnableDrag(Control control)
    {
        control.MouseDown += (s, e) => {
            if (e.Button == MouseButtons.Left) {
                NativeMethods.ReleaseCapture();
                NativeMethods.SendMessage(Handle, NativeMethods.WM_NCLBUTTONDOWN, (IntPtr)NativeMethods.HT_CAPTION, IntPtr.Zero);
            }
        };
    }
}
