using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using System.Collections.Generic;
using System.Linq;
using MoyuBrowser.Core;
using System.Runtime.InteropServices;

namespace MoyuBrowser.UI.Forms;

public partial class MainForm : MoyuBaseForm
{
    private static MainForm? _instance;
    public static MainForm Instance => _instance ??= new MainForm();

    private Panel _headerPanel = null!;
    private Panel _contentContainer = null!;
    private FlowLayoutPanel _tabList = null!;
    private TextBox _addressBar = null!;
    private TextBox _searchBar = null!;
    private Panel _settingsMenu = null!;
    private Button _btnNewTab = null!;
    private System.Windows.Forms.Timer _stealthTimer = null!;
    private const int BOSS_KEY_ID = 888; 

    internal class TabData {
        public Panel TabPanel = null!;
        public WebView2 WebView = null!;
        public Panel HeaderBtn = null!;
        public string Title = "新标签页";
    }
    private List<TabData> _tabs = new List<TabData>();
    private TabData? _activeTab;

    public MainForm()
    {
        InitializeComponent();
        this.DoubleBuffered = true;
        this.Opacity = SettingsManager.Current.DefaultOpacity;
        
        _stealthTimer = new System.Windows.Forms.Timer { Interval = 200 };
        _stealthTimer.Tick += (s, e) => CheckStealthState();
        _stealthTimer.Start();

        ApplyTheme(SettingsManager.Current.CurrentTheme);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        NativeMethods.RegisterHotKey(this.Handle, BOSS_KEY_ID, NativeMethods.MOD_ALT | NativeMethods.MOD_NOREPEAT, (uint)Keys.B);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        NativeMethods.UnregisterHotKey(this.Handle, BOSS_KEY_ID);
        base.OnFormClosing(e);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_HOTKEY && (int)m.WParam == BOSS_KEY_ID) {
            ToggleBossKey();
            return;
        }
        base.WndProc(ref m);
    }

    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    private void CheckStealthState()
    {
        if (!SettingsManager.Current.AutoFadeOnBlur) return;
        IntPtr foregroundWindow = GetForegroundWindow();
        bool isFocused = (foregroundWindow == this.Handle);
        bool isMouseOver = this.Bounds.Contains(Cursor.Position);
        if (!isFocused && !isMouseOver) {
            double targetOpacity = SettingsManager.Current.StealthOpacity;
            if (this.Opacity > targetOpacity) this.Opacity = targetOpacity;
        } else if (isFocused || isMouseOver) {
            if (this.Opacity < SettingsManager.Current.DefaultOpacity) 
                this.Opacity = SettingsManager.Current.DefaultOpacity;
        }
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (_tabs.Count == 0) CreateNewTab(SettingsManager.Current.HomeUrl);
    }

    private void InitializeComponent()
    {
        this.Text = SettingsManager.Current.FakeTitle; 
        this.Size = new Size(1200, 800);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.MinimumSize = new Size(600, 400);

        var mainTable = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = Color.Transparent };
        mainTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
        mainTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        this.Controls.Add(mainTable);

        _headerPanel = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0) };
        EnableDrag(_headerPanel);
        mainTable.Controls.Add(_headerPanel, 0, 0);

        _headerPanel.Controls.Add(CreateIconButton("◀", 5, 6, (s, e) => _activeTab?.WebView.GoBack()));
        _headerPanel.Controls.Add(CreateIconButton("▶", 35, 6, (s, e) => _activeTab?.WebView.GoForward()));
        _headerPanel.Controls.Add(CreateIconButton("🏠", 65, 6, (s, e) => _activeTab?.WebView.CoreWebView2?.Navigate(SettingsManager.Current.HomeUrl)));
        _btnNewTab = CreateIconButton("➕", 95, 6, (s, e) => CreateNewTab(SettingsManager.Current.HomeUrl));
        _headerPanel.Controls.Add(_btnNewTab);

        _tabList = new FlowLayoutPanel { Location = new Point(130, 0), Size = new Size(this.Width / 3, 45), BackColor = Color.Transparent, WrapContents = false, Padding = new Padding(0) };
        EnableDrag(_tabList);
        _headerPanel.Controls.Add(_tabList);

        // 地址栏
        _addressBar = new TextBox { Location = new Point(this.Width / 3 + 140, 10), Size = new Size(this.Width / 4, 24), BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 10), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        _addressBar.KeyDown += (s, e) => {
            if (e.KeyCode == Keys.Enter) {
                string url = _addressBar.Text.Trim();
                if (!url.StartsWith("http") && !url.Contains("://")) url = "https://" + url;
                _activeTab?.WebView.CoreWebView2?.Navigate(url);
                e.Handled = true; e.SuppressKeyPress = true;
            }
        };
        _headerPanel.Controls.Add(_addressBar);

        // 搜索栏
        _searchBar = new TextBox { Location = new Point(this.Width /3 + 140 + this.Width / 4 + 10, 10), Size = new Size(150, 24), BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 10), Anchor = AnchorStyles.Top | AnchorStyles.Right, Text = " 🔍 搜索..." };
        _searchBar.GotFocus += (s, e) => { if (_searchBar.Text == " 🔍 搜索...") _searchBar.Text = ""; };
        _searchBar.LostFocus += (s, e) => { if (string.IsNullOrEmpty(_searchBar.Text)) _searchBar.Text = " 🔍 搜索..."; };
        _searchBar.KeyDown += (s, e) => {
            if (e.KeyCode == Keys.Enter) {
                string q = _searchBar.Text.Trim();
                if (!string.IsNullOrEmpty(q)) NavigateTo(SettingsManager.Current.SearchEngineUrl + Uri.EscapeDataString(q));
                e.Handled = true; e.SuppressKeyPress = true;
            }
        };
        _headerPanel.Controls.Add(_searchBar);

        var btnSettings = CreateIconButton("⚙", 0, 6, (s, e) => ToggleSettingsMenu());
        btnSettings.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        var btnMinimize = CreateIconButton("―", 35, 6, (s, e) => WindowState = FormWindowState.Minimized);
        btnMinimize.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        var btnCloseWindow = CreateIconButton("✕", 70, 6, (s, e) => Application.Exit());
        btnCloseWindow.Anchor = AnchorStyles.Top | AnchorStyles.Right; btnCloseWindow.ForeColor = Color.IndianRed;
        
        var btnGroupRight = new Panel { Dock = DockStyle.Right, Width = 110, BackColor = Color.Transparent };
        btnGroupRight.Controls.AddRange(new Control[] { btnSettings, btnMinimize, btnCloseWindow });
        EnableDrag(btnGroupRight);
        _headerPanel.Controls.Add(btnGroupRight);

        _contentContainer = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0), Padding = new Padding(0) };
        mainTable.Controls.Add(_contentContainer, 0, 1);
        InitializeSettingsMenu();
    }

    private void InitializeSettingsMenu()
    {
        _settingsMenu = new Panel { Size = new Size(200, 480), BackColor = Color.FromArgb(40, 40, 45), BorderStyle = BorderStyle.FixedSingle, Visible = false, Anchor = AnchorStyles.Top | AnchorStyles.Right };
        this.Controls.Add(_settingsMenu);
        _settingsMenu.Location = new Point(this.Width - 210, 50);
        _settingsMenu.BringToFront();

        var lblTheme = new Label { Text = "视觉主题", ForeColor = Color.White, Location = new Point(10, 10), AutoSize = true };
        _settingsMenu.Controls.Add(lblTheme);
        AddThemeOption("经典黑", AppTheme.Default, 1);
        AddThemeOption("VS Code 暗", AppTheme.VSDark, 2);
        AddThemeOption("Office 白", AppTheme.OfficeWhite, 3);
        
        var sep1 = new Panel { Size = new Size(180, 1), BackColor = Color.Gray, Location = new Point(10, 140) };
        _settingsMenu.Controls.Add(sep1);

        var lblEng = new Label { Text = "搜索引擎选择:", ForeColor = Color.Gray, Location = new Point(10, 150), AutoSize = true, Font = new Font("Segoe UI", 8) };
        _settingsMenu.Controls.Add(lblEng);
        AddEngineOption("Bing", "https://www.bing.com/search?q=", 175);
        AddEngineOption("Google", "https://www.google.com/search?q=", 210);
        AddEngineOption("Baidu", "https://www.baidu.com/s?wd=", 245);

        var sep2 = new Panel { Size = new Size(180, 1), BackColor = Color.Gray, Location = new Point(10, 290) };
        _settingsMenu.Controls.Add(sep2);
        
        var lblMoyu = new Label { Text = "核心方案:", ForeColor = Color.White, Location = new Point(10, 305), AutoSize = true };
        _settingsMenu.Controls.Add(lblMoyu);

        AddToggleOption("全局黑白模式 (极简护眼)", (s, e) => { SettingsManager.Current.IsGreyscale = !SettingsManager.Current.IsGreyscale; ApplyVisualEffects(); SettingsManager.Save(); }, 330);
        AddToggleOption("伪装身份：财务文档 (Alt+W)", (s, e) => { SettingsManager.Current.FakeTitle = "3月财务审计草案.docx"; this.Text = SettingsManager.Current.FakeTitle; SettingsManager.Save(); }, 365);
        AddToggleOption("全局老板键 (Alt+B 快捷隐藏)", (s, e) => { ToggleBossKey(); }, 400); 
        AddToggleOption("重置标识 (Wen 浏览器)", (s, e) => { SettingsManager.Current.FakeTitle = "Wen 浏览器"; this.Text = "Wen 浏览器"; SettingsManager.Save(); }, 435);
    }

    private void AddEngineOption(string name, string url, int y)
    {
        var btn = new Button { Text = name, Location = new Point(10, y), Size = new Size(180, 30), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 8) };
        btn.FlatAppearance.BorderSize = 0;
        btn.Click += (s, e) => { SettingsManager.Current.SearchEngineName = name; SettingsManager.Current.SearchEngineUrl = url; ApplyTheme(SettingsManager.Current.CurrentTheme); SettingsManager.Save(); };
        _settingsMenu.Controls.Add(btn);
    }

    private void AddToggleOption(string name, EventHandler click, int y)
    {
        var btn = new Button { Text = name, Location = new Point(10, y), Size = new Size(180, 30), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 8) };
        btn.FlatAppearance.BorderSize = 0;
        btn.Click += click;
        _settingsMenu.Controls.Add(btn);
    }

    private void AddThemeOption(string name, AppTheme theme, int index)
    {
        var btn = new Button { Text = name, Location = new Point(10, 35 + (index - 1) * 35), Size = new Size(180, 30), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 8) };
        btn.FlatAppearance.BorderSize = 0;
        btn.Click += (s, e) => { ApplyTheme(theme); SettingsManager.Current.CurrentTheme = theme; SettingsManager.Save(); };
        _settingsMenu.Controls.Add(btn);
    }

    private void ToggleSettingsMenu()
    {
        _settingsMenu.Visible = !_settingsMenu.Visible;
        if (_settingsMenu.Visible) _settingsMenu.BringToFront();
    }

    public void ApplyTheme(AppTheme theme)
    {
        var colors = ThemeManager.GetColors(theme);
        this.BackColor = colors.BorderColor;
        _headerPanel.BackColor = colors.HeaderBg;
        _contentContainer.BackColor = colors.ContentBg;
        _addressBar.BackColor = _searchBar.BackColor = (theme == AppTheme.OfficeWhite) ? Color.FromArgb(230, 230, 230) : Color.FromArgb(45, 45, 50);
        _addressBar.ForeColor = _searchBar.ForeColor = colors.TextColor;
        _settingsMenu.BackColor = colors.HeaderBg;
        _settingsMenu.ForeColor = colors.TextColor;
        _settingsMenu.BorderStyle = BorderStyle.FixedSingle;
        UpdateControlColors(this, colors);
        foreach (var tab in _tabs) { tab.TabPanel.BackColor = colors.ContentBg; UpdateTabVisual(tab, tab == _activeTab); }
    }

    private void UpdateControlColors(Control parent, ThemeColors colors)
    {
        foreach (Control ctrl in parent.Controls)
        {
            if (ctrl is Button btn) { 
                btn.ForeColor = (SettingsManager.Current.SearchEngineName == btn.Text) ? Color.CadetBlue : colors.TextColor;
                btn.FlatAppearance.MouseOverBackColor = (SettingsManager.Current.CurrentTheme == AppTheme.OfficeWhite) ? Color.FromArgb(220, 220, 220) : Color.FromArgb(50, 50, 55, 50);
            }
            else if (ctrl is Label lbl) { lbl.ForeColor = colors.TextColor; }
            else if (ctrl is Panel p && (p == _settingsMenu || p == _headerPanel || p.Parent == _headerPanel)) UpdateControlColors(ctrl, colors);
            else if (ctrl is FlowLayoutPanel flp) UpdateControlColors(ctrl, colors);
        }
    }

    private void ApplyVisualEffects()
    {
        string styleId = "moyu-stealth-shield-v-final";
        string css = "";
        if (SettingsManager.Current.IsGreyscale) { css += "html, body { filter: grayscale(100%) !important; } "; }
        string script = $"(function() {{ let style = document.getElementById('{styleId}'); if (style) style.remove(); style = document.createElement('style'); style.id = '{styleId}'; document.head.appendChild(style); style.innerHTML = `{css}`; }})();";
        foreach (var tab in _tabs) { if (tab.WebView.CoreWebView2 != null) tab.WebView.ExecuteScriptAsync(script); }
        this.Opacity = SettingsManager.Current.DefaultOpacity;
    }

    private Button CreateIconButton(string text, int x, int y, EventHandler click)
    {
        var btn = new Button { Text = text, Location = new Point(x, y), Size = new Size(30, 32), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Emoji", 9), Cursor = Cursors.Hand };
        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, 50, 55, 50);
        btn.Click += click;
        return btn;
    }

    public void NavigateTo(string url)
    {
        if (_activeTab != null && (_activeTab.WebView.Source.ToString().Contains("xiaoheiv.top") || _activeTab.WebView.Source.ToString() == "about:blank")) {
            _activeTab.WebView.CoreWebView2.Navigate(url);
        } else {
            CreateNewTab(url);
        }
    }

    public async void CreateNewTab(string url)
    {
        try {
            var colors = ThemeManager.GetColors(SettingsManager.Current.CurrentTheme);
            var wv = new WebView2 { Dock = DockStyle.Fill, DefaultBackgroundColor = colors.ContentBg };
            var tabPanel = new Panel { Dock = DockStyle.Fill, BackColor = colors.ContentBg };
            tabPanel.Controls.Add(wv);
            _contentContainer.Controls.Add(tabPanel);
            var tabBtn = new Panel { Size = new Size(140, 45), BackColor = Color.Transparent, Cursor = Cursors.Hand };
            var tabTitle = new Label { Text = "加载中...", Location = new Point(10, 12), AutoSize = false, Size = new Size(100, 20), Enabled = false };
            var closeBtn = new Label { Text = "✕", Location = new Point(115, 12), Size = new Size(20, 20), TextAlign = ContentAlignment.MiddleCenter };

            var tabData = new TabData { TabPanel = tabPanel, WebView = wv, HeaderBtn = tabBtn };
            _tabs.Add(tabData);
            tabBtn.Controls.AddRange(new Control[] { tabTitle, closeBtn });
            tabBtn.Click += (s, e) => SwitchToTab(tabData);
            closeBtn.Click += (s, e) => CloseTab(tabData);
            _tabList.Controls.Add(tabBtn);
            SwitchToTab(tabData);

            string userDataPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "MoyuBrowser_Data");
            var env = await CoreWebView2Environment.CreateAsync(null, userDataPath);
            await wv.EnsureCoreWebView2Async(env);

            wv.CoreWebView2.NewWindowRequested += (s, e) => {
                e.Handled = true;
                CreateNewTab(e.Uri);
            };

            wv.CoreWebView2.SourceChanged += (s, e) => { if (tabData == _activeTab) _addressBar.Text = wv.Source.ToString(); };
            wv.CoreWebView2.DocumentTitleChanged += (s, e) => { tabData.Title = wv.CoreWebView2.DocumentTitle; tabTitle.Text = tabData.Title; };
            wv.CoreWebView2.NavigationCompleted += (s, e) => { ApplyVisualEffects(); };
            wv.CoreWebView2.Navigate(url);
        } catch { }
    }

    private void SwitchToTab(TabData tab)
    {
        _activeTab = tab;
        tab.TabPanel.BringToFront();
        foreach (var t in _tabs) UpdateTabVisual(t, t == tab);
        if (IsHandleCreated) BeginInvoke(new Action(() => { if (!tab.WebView.IsDisposed) tab.WebView.Focus(); }));
    }

    private void UpdateTabVisual(TabData tab, bool active)
    {
        var colors = ThemeManager.GetColors(SettingsManager.Current.CurrentTheme);
        tab.HeaderBtn.BackColor = active ? colors.TabActive : colors.TabInactive;
        tab.HeaderBtn.Controls[0].ForeColor = colors.TextColor;
        tab.HeaderBtn.Controls[1].ForeColor = active ? colors.TextColor : Color.FromArgb(80, 80, 80);
    }

    private void CloseTab(TabData tab)
    {
        if (_tabs.Count <= 1) return;
        _tabs.Remove(tab); _tabList.Controls.Remove(tab.HeaderBtn); _contentContainer.Controls.Remove(tab.TabPanel);
        tab.WebView.Dispose();
        if (_activeTab == tab) SwitchToTab(_tabs.Last());
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (HandleShortcuts(keyData)) return true;
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private bool HandleShortcuts(Keys keyData)
    {
        if ((keyData & Keys.Alt) == Keys.Alt) {
            Keys baseKey = keyData & ~Keys.Alt;
            if (baseKey == Keys.Up) { Opacity = Math.Min(1.0, Opacity + 0.1); return true; }
            if (baseKey == Keys.Down) { Opacity = Math.Max(0.1, Opacity - 0.1); return true; }
            if (baseKey == Keys.T) { CreateNewTab(SettingsManager.Current.HomeUrl); return true; }
            if (baseKey == Keys.W) { if (_activeTab != null) CloseTab(_activeTab); return true; }
            if (baseKey == Keys.Space) { ToggleBossKey(); return true; }
            if (baseKey == Keys.Q) { WindowState = FormWindowState.Minimized; return true; }
        }
        return false;
    }

    private void ToggleBossKey() {
        if (Opacity > 0.0) { Tag = Opacity; Opacity = 0.0; ShowInTaskbar = false; }
        else { Opacity = (Tag is double op) ? op : SettingsManager.Current.DefaultOpacity; ShowInTaskbar = true; }
    }
}
