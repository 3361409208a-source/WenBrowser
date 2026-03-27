using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using System.Collections.Generic;
using System.Linq;
using WenBrowser.Core;
using System.Runtime.InteropServices;

namespace WenBrowser.UI.Forms;

public partial class MainForm : WenBaseForm
{
    private static MainForm? _instance;
    public static MainForm Instance => _instance ??= new MainForm();

    private Panel _headerPanel = null!;
    private Panel _tabRow = null!;
    private Panel _navRow = null!;
    private Panel _contentContainer = null!;
    private FlowLayoutPanel _tabList = null!;
    private TextBox _addressBar = null!;
    private TextBox _searchBar = null!;
    private Panel _settingsMenu = null!;
    private Button _btnNewTab = null!;
    private System.Windows.Forms.Timer _stealthTimer = null!;
    private NotifyIcon _trayIcon = null!;
    private const int BOSS_KEY_ID = 888; 
    private const int NEW_TAB_ID = 889;
    private const int CLOSE_TAB_ID = 890;
    private const int OPACITY_UP_ID = 891;
    private const int OPACITY_DOWN_ID = 892;
    private const int TOGGLE_BOSS_ID = 893;

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
        InitializeTrayIcon();
        this.DoubleBuffered = true;
        this.Opacity = SettingsManager.Current.DefaultOpacity;
        
        _stealthTimer = new System.Windows.Forms.Timer { Interval = 200 };
        _stealthTimer.Tick += (s, e) => CheckStealthState();
        _stealthTimer.Start();

        ApplyTheme(SettingsManager.Current.CurrentTheme);
    }

    private void InitializeTrayIcon()
    {
        _trayIcon = new NotifyIcon();
        string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "logo.ico");
        if (File.Exists(iconPath)) {
            try {
                this.Icon = new Icon(iconPath);
                _trayIcon.Icon = this.Icon;
            } catch {
                _trayIcon.Icon = SystemIcons.Application;
            }
        } else {
            _trayIcon.Icon = SystemIcons.Application;
        }

        _trayIcon.Visible = true;
        _trayIcon.Text = "Wen 浏览器 - 后台运行中";

        var trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("显示窗口", null, (s, e) => ShowMainForm());
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add("彻底退出", null, (s, e) => {
            _trayIcon.Visible = false;
            Environment.Exit(0);
        });

        _trayIcon.ContextMenuStrip = trayMenu;
        _trayIcon.DoubleClick += (s, e) => ShowMainForm();
    }

    private void ShowMainForm()
    {
        this.Visible = true;
        this.ShowInTaskbar = true;
        this.Opacity = SettingsManager.Current.DefaultOpacity;
        this.BringToFront();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        NativeMethods.RegisterHotKey(this.Handle, BOSS_KEY_ID, NativeMethods.MOD_ALT | NativeMethods.MOD_NOREPEAT, (uint)Keys.B);
        NativeMethods.RegisterHotKey(this.Handle, NEW_TAB_ID, NativeMethods.MOD_ALT | NativeMethods.MOD_NOREPEAT, (uint)Keys.T);
        NativeMethods.RegisterHotKey(this.Handle, CLOSE_TAB_ID, NativeMethods.MOD_ALT | NativeMethods.MOD_NOREPEAT, (uint)Keys.W);
        NativeMethods.RegisterHotKey(this.Handle, OPACITY_UP_ID, NativeMethods.MOD_ALT | NativeMethods.MOD_NOREPEAT, (uint)Keys.Up);
        NativeMethods.RegisterHotKey(this.Handle, OPACITY_DOWN_ID, NativeMethods.MOD_ALT | NativeMethods.MOD_NOREPEAT, (uint)Keys.Down);
        NativeMethods.RegisterHotKey(this.Handle, TOGGLE_BOSS_ID, NativeMethods.MOD_ALT | NativeMethods.MOD_NOREPEAT, (uint)Keys.Space);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // 【核心修改】拦截关闭事件，改为影身至托盘
        if (e.CloseReason == CloseReason.UserClosing) {
            e.Cancel = true;
            this.Visible = false;
            this.ShowInTaskbar = false;
            _trayIcon.ShowBalloonTip(1000, "Wen 浏览器", "已切换至后台静默运行", ToolTipIcon.Info);
        }
        NativeMethods.UnregisterHotKey(this.Handle, BOSS_KEY_ID);
        NativeMethods.UnregisterHotKey(this.Handle, NEW_TAB_ID);
        NativeMethods.UnregisterHotKey(this.Handle, CLOSE_TAB_ID);
        NativeMethods.UnregisterHotKey(this.Handle, OPACITY_UP_ID);
        NativeMethods.UnregisterHotKey(this.Handle, OPACITY_DOWN_ID);
        NativeMethods.UnregisterHotKey(this.Handle, TOGGLE_BOSS_ID);
        base.OnFormClosing(e);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_HOTKEY) {
            int id = (int)m.WParam;
            switch (id) {
                case BOSS_KEY_ID:
                case TOGGLE_BOSS_ID:
                    ToggleBossKey();
                    break;
                case NEW_TAB_ID:
                    CreateNewTab(SettingsManager.Current.HomeUrl);
                    break;
                case CLOSE_TAB_ID:
                    if (_activeTab != null) CloseTab(_activeTab);
                    break;
                case OPACITY_UP_ID:
                    this.Opacity = Math.Min(1.0, this.Opacity + 0.1);
                    SettingsManager.Current.DefaultOpacity = this.Opacity;
                    SettingsManager.Save();
                    break;
                case OPACITY_DOWN_ID:
                    this.Opacity = Math.Max(0.1, this.Opacity - 0.1);
                    SettingsManager.Current.DefaultOpacity = this.Opacity;
                    SettingsManager.Save();
                    break;
            }
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

        var colors = ThemeManager.GetColors(SettingsManager.Current.CurrentTheme);
        
        // --- 最终解决方案：使用 TableLayoutPanel 锁定物理区域 ---
        var mainLayout = new TableLayoutPanel { 
            Dock = DockStyle.Fill, 
            ColumnCount = 1, 
            RowCount = 2,
            BackColor = Color.Transparent 
        };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 75F)); // 顶栏固定 75
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // 网页填充
        this.Controls.Add(mainLayout);

        _headerPanel = new Panel { Dock = DockStyle.Fill, BackColor = colors.HeaderBg, Margin = new Padding(0) };
        EnableDrag(_headerPanel);
        mainLayout.Controls.Add(_headerPanel, 0, 0);
        
        _contentContainer = new Panel { Dock = DockStyle.Fill, BackColor = colors.ContentBg, Margin = new Padding(0) };
        mainLayout.Controls.Add(_contentContainer, 0, 1);

        // 2. 内部行面板拆分（使用内嵌表格确保绝对顺序）
        var innerLayout = new TableLayoutPanel { 
            Dock = DockStyle.Fill, 
            ColumnCount = 1, 
            RowCount = 2, 
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };
        innerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F)); // Row 0: 标签栏
        innerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F)); // Row 1: 导航栏
        _headerPanel.Controls.Add(innerLayout);

        _tabRow = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Margin = new Padding(0) };
        _navRow = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Margin = new Padding(0) };
        innerLayout.Controls.Add(_tabRow, 0, 0);
        innerLayout.Controls.Add(_navRow, 0, 1);

        EnableDrag(_headerPanel);
        EnableDrag(_tabRow);
        EnableDrag(_navRow);

        // --- 第一行：Logo、标签栏、窗口控制 ---
        Control btnLogo;
        string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "logo.png");
        if (File.Exists(logoPath)) {
            btnLogo = new PictureBox { 
                Image = Image.FromFile(logoPath),
                Location = new Point(10, 5), 
                Size = new Size(24, 24),
                SizeMode = PictureBoxSizeMode.Zoom
            };
        } else {
            btnLogo = new Label { Text = "WEN", Location = new Point(10, 5), Size = new Size(50, 24), ForeColor = colors.TextColor, Font = new Font("Segoe UI", 11, FontStyle.Bold) };
        }
        _tabRow.Controls.Add(btnLogo);

        var btnHome = CreateIconButton("🏠", 65, 2, (s, e) => _activeTab?.WebView.CoreWebView2.Navigate(SettingsManager.Current.HomeUrl));
        _tabRow.Controls.Add(btnHome);

        _btnNewTab = CreateIconButton("➕", 0, 2, (s, e) => CreateNewTab(SettingsManager.Current.HomeUrl));
        _btnNewTab.Size = new Size(35, 30);
        
        int rightMargin = 115;
        _tabList = new FlowLayoutPanel { 
            Location = new Point(100, 0), 
            Size = new Size(Math.Max(100, this.Width - 100 - rightMargin), 35), 
            BackColor = Color.Transparent, 
            WrapContents = false, 
            Padding = new Padding(0),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        _tabRow.Controls.Add(_tabList);
        _tabList.Controls.Add(_btnNewTab);

        var btnSettings = CreateIconButton("⚙", 0, 2, (s, e) => ToggleSettingsMenu());
        var btnMinimize = CreateIconButton("―", 35, 2, (s, e) => WindowState = FormWindowState.Minimized);
        var btnCloseWindow = CreateIconButton("✕", 70, 2, (s, e) => this.Close()); 
        btnCloseWindow.ForeColor = Color.IndianRed;
        
        var btnGroupRight = new Panel { Dock = DockStyle.Right, Width = 110, BackColor = Color.Transparent };
        btnGroupRight.Controls.AddRange(new Control[] { btnSettings, btnMinimize, btnCloseWindow });
        _tabRow.Controls.Add(btnGroupRight);
        
        // 扩展可拖动区域
        EnableDrag(_tabList);
        EnableDrag(btnLogo);
        EnableDrag(btnGroupRight);

        // --- 第二行：地址栏栏组件 ---
        int searchWidth = 120;
        int opControlWidth = 65;
        int spacing = 10;

        _searchBar = new TextBox { 
            Location = new Point(this.Width - rightMargin - opControlWidth - 5, 8), 
            Size = new Size(searchWidth, 24), 
            BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10), 
            Anchor = AnchorStyles.Top | AnchorStyles.Right, Text = " 🔍 搜索..." 
        };
        _searchBar.GotFocus += (s, e) => { if (_searchBar.Text == " 🔍 搜索...") _searchBar.Text = ""; };
        _searchBar.LostFocus += (s, e) => { if (string.IsNullOrEmpty(_searchBar.Text)) _searchBar.Text = " 🔍 搜索..."; };
        _searchBar.KeyDown += (s, e) => {
            if (e.KeyCode == Keys.Enter) {
                string q = _searchBar.Text.Trim();
                if (!string.IsNullOrEmpty(q)) NavigateTo(SettingsManager.Current.SearchEngineUrl + Uri.EscapeDataString(q));
                e.Handled = true; e.SuppressKeyPress = true;
            }
        };
        _navRow.Controls.Add(_searchBar);

        _addressBar = new TextBox { 
            Location = new Point(10, 8), 
            Size = new Size(this.Width - (this.Width - _searchBar.Left) - spacing - 10, 24), 
            BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10), 
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right 
        };
        _addressBar.KeyDown += (s, e) => {
            if (e.KeyCode == Keys.Enter) {
                string url = _addressBar.Text.Trim();
                if (!string.IsNullOrEmpty(url)) NavigateTo(url);
                e.Handled = true; e.SuppressKeyPress = true;
            }
        };
        _navRow.Controls.Add(_addressBar);

        var btnOpacityUp = CreateIconButton("+", 2, 0, (s, e) => { this.Opacity = Math.Min(1.0, this.Opacity + 0.1); SettingsManager.Current.DefaultOpacity = this.Opacity; SettingsManager.Save(); });
        var btnOpacityDown = CreateIconButton("-", 28, 0, (s, e) => { this.Opacity = Math.Max(0.1, this.Opacity - 0.1); SettingsManager.Current.DefaultOpacity = this.Opacity; SettingsManager.Save(); });
        
        var opacityControlPanel = new Panel { 
            Location = new Point(this.Width - rightMargin - opControlWidth + 5, 4), 
            Size = new Size(65, 32), 
            BackColor = Color.Transparent,
            Anchor = AnchorStyles.Top | AnchorStyles.Right 
        };
        opacityControlPanel.Controls.Add(btnOpacityUp);
        opacityControlPanel.Controls.Add(btnOpacityDown);
        _navRow.Controls.Add(opacityControlPanel);
        EnableDrag(opacityControlPanel);

        InitializeSettingsMenu();
    }

    private void InitializeSettingsMenu()
    {
        _settingsMenu = new Panel { Size = new Size(200, 520), BackColor = Color.FromArgb(40, 40, 45), BorderStyle = BorderStyle.FixedSingle, Visible = false, Anchor = AnchorStyles.Top | AnchorStyles.Right };
        this.Controls.Add(_settingsMenu);
        _settingsMenu.Location = new Point(this.Width - 210, 50);
        _settingsMenu.BringToFront();

        int currentY = 10;
        var lblTheme = new Label { Text = "视觉主题", ForeColor = Color.White, Location = new Point(10, currentY), AutoSize = true };
        _settingsMenu.Controls.Add(lblTheme);
        currentY += 25;

        AddThemeOption("经典黑", AppTheme.Default, currentY); currentY += 35;
        AddThemeOption("VS Code 暗", AppTheme.VSDark, currentY); currentY += 35;
        AddThemeOption("Office 白", AppTheme.OfficeWhite, currentY); currentY += 35;
        AddThemeOption("极简透明", AppTheme.Transparent, currentY); currentY += 35;
        AddThemeOption("樱花粉", AppTheme.Pink, currentY); currentY += 35;
        
        currentY += 5;
        var sep1 = new Panel { Size = new Size(180, 1), BackColor = Color.Gray, Location = new Point(10, currentY) };
        _settingsMenu.Controls.Add(sep1);
        currentY += 10;

        var lblEng = new Label { Text = "搜索引擎选择:", ForeColor = Color.Gray, Location = new Point(10, currentY), AutoSize = true, Font = new Font("Segoe UI", 8) };
        _settingsMenu.Controls.Add(lblEng);
        currentY += 25;

        AddEngineOption("Bing", "https://www.bing.com/search?q=", currentY); currentY += 35;
        AddEngineOption("Google", "https://www.google.com/search?q=", currentY); currentY += 35;
        AddEngineOption("Baidu", "https://www.baidu.com/s?wd=", currentY); currentY += 35;

        currentY += 10;
        var sep2 = new Panel { Size = new Size(180, 1), BackColor = Color.Gray, Location = new Point(10, currentY) };
        _settingsMenu.Controls.Add(sep2);
        currentY += 15;
        
        var lblFont = new Label { Text = "界面字体选择:", ForeColor = Color.White, Location = new Point(10, currentY), AutoSize = true };
        _settingsMenu.Controls.Add(lblFont);
        currentY += 25;

        AddFontOption("默认字体 (Segoe UI)", currentY, true);
        currentY += 35;

        foreach (var fontName in FontManager.AvailableFontNames)
        {
            AddFontOption(fontName, currentY);
            currentY += 35;
        }
        
        currentY += 10;
        var lblWen = new Label { Text = "核心方案 (隐蔽增强):", ForeColor = Color.White, Location = new Point(10, currentY), AutoSize = true };
        _settingsMenu.Controls.Add(lblWen);
        currentY += 25;

        AddToggleOption("全局黑白模式 (极简护眼)", (s, e) => { SettingsManager.Current.IsGreyscale = !SettingsManager.Current.IsGreyscale; ApplyVisualEffects(); SettingsManager.Save(); }, currentY); currentY += 35;
        AddToggleOption("伪装身份：财务文档 (Alt+W)", (s, e) => { SettingsManager.Current.FakeTitle = "3月财务审计草案.docx"; this.Text = SettingsManager.Current.FakeTitle; SettingsManager.Save(); }, currentY); currentY += 35;
        AddToggleOption("全局老板键 (Alt+B 快捷隐藏)", (s, e) => { ToggleBossKey(); }, currentY); currentY += 35; 
        AddToggleOption("重置标识 (Wen 浏览器)", (s, e) => { SettingsManager.Current.FakeTitle = "Wen 浏览器"; this.Text = "Wen 浏览器"; SettingsManager.Save(); }, currentY); currentY += 35;
        
        _settingsMenu.Height = currentY + 20;
        _settingsMenu.Location = new Point(this.Width - 210, 50);

        ApplyGlobalFont();
    }

    private void AddFontOption(string familyName, int y, bool isDefault = false)
    {
        var displayFont = isDefault ? new Font("Segoe UI", 8) : FontManager.GetFont(familyName, 8);
        var btn = new Button { Text = familyName, Location = new Point(10, y), Size = new Size(180, 30), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Font = displayFont };
        btn.FlatAppearance.BorderSize = 0;
        btn.Click += (s, e) => {
            if (isDefault) {
                SettingsManager.Current.UseCustomFont = false;
                SettingsManager.Current.CustomFontFamily = "Segoe UI";
            } else {
                SettingsManager.Current.UseCustomFont = true;
                SettingsManager.Current.CustomFontFamily = familyName;
            }
            SettingsManager.Save();
            ApplyGlobalFont();
        };
        _settingsMenu.Controls.Add(btn);
    }

    private void ApplyGlobalFont()
    {
        float baseSize = 9f;
        this.Font = FontManager.GetDefaultFont(baseSize);
        UpdateControlFonts(this);
    }

    private void UpdateControlFonts(Control parent)
    {
        foreach (Control ctrl in parent.Controls)
        {
            if (ctrl is TextBox tb) {
                tb.Font = FontManager.GetDefaultFont(10);
            } else if (ctrl is Button btn) {
                // Keep icons with Emoji font if they are icon buttons
                if (btn.Text.Length == 1 && (btn.Text[0] > 1000 || "◀▶🏠➕⚙―✕".Contains(btn.Text))) {
                    btn.Font = new Font("Segoe UI Emoji", 9);
                } else {
                    btn.Font = FontManager.GetDefaultFont(8);
                }
            } else if (ctrl is Label lbl) {
                lbl.Font = FontManager.GetDefaultFont(9);
            }
            
            if (ctrl.HasChildren) UpdateControlFonts(ctrl);
        }
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

    private void AddThemeOption(string name, AppTheme theme, int y)
    {
        var btn = new Button { Text = name, Location = new Point(10, y), Size = new Size(180, 30), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 8) };
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
        _tabRow.BackColor = colors.HeaderBg;
        _navRow.BackColor = colors.HeaderBg;
        _contentContainer.BackColor = colors.ContentBg;
        
        _addressBar.BackColor = _searchBar.BackColor = colors.InputBg;
        _addressBar.ForeColor = _searchBar.ForeColor = colors.TextColor;
        
        _settingsMenu.BackColor = colors.HeaderBg;
        _settingsMenu.ForeColor = colors.TextColor;
        _settingsMenu.BorderStyle = BorderStyle.FixedSingle;
        UpdateControlColors(this, colors);
        foreach (var tab in _tabs) { 
            tab.TabPanel.BackColor = colors.ContentBg; 
            tab.WebView.DefaultBackgroundColor = (theme == AppTheme.Transparent) ? Color.Transparent : colors.ContentBg;
            UpdateTabVisual(tab, tab == _activeTab); 
        }
        
        if (SettingsManager.Current.CurrentTheme == AppTheme.Transparent) {
            this.Opacity = 0.65;
        } else {
            this.Opacity = SettingsManager.Current.DefaultOpacity;
        }

        ApplyVisualEffects();
    }

    private void UpdateControlColors(Control parent, ThemeColors colors)
    {
        foreach (Control ctrl in parent.Controls)
        {
            if (ctrl is Button btn) { 
                btn.ForeColor = (SettingsManager.Current.SearchEngineName == btn.Text) ? Color.CadetBlue : colors.TextColor;
                btn.FlatAppearance.MouseOverBackColor = colors.HoverColor;
            }
            else if (ctrl is Label lbl) { lbl.ForeColor = colors.TextColor; }
            else if (ctrl is Panel p && (p == _settingsMenu || p == _headerPanel || p.Parent == _headerPanel)) UpdateControlColors(ctrl, colors);
            else if (ctrl is FlowLayoutPanel flp) UpdateControlColors(ctrl, colors);
        }
    }

    private void ApplyVisualEffects()
    {
        string styleId = "wen-stealth-shield-v-final";
        string css = "";
        
        // 黑白模式
        // 黑白模式
        if (SettingsManager.Current.IsGreyscale) { 
            css += "html, body { filter: grayscale(100%) !important; } "; 
        }

        // --- 网页核心样式自适应 (仅保留必要的滤镜效果，不再强制修改背景/文字颜色) ---
        bool isOfficeWhite = SettingsManager.Current.CurrentTheme == AppTheme.OfficeWhite;
        bool isTransparent = SettingsManager.Current.CurrentTheme == AppTheme.Transparent;
        if (SettingsManager.Current.UseCustomFont) 
        {
            string? fileName = FontManager.GetFontPath(SettingsManager.Current.CustomFontFamily);
            if (!string.IsNullOrEmpty(fileName))
            {
                css += $"@font-face {{ font-family: 'WenCustomFont'; src: url('http://wen.fonts/{fileName}'); }} ";
                css += "* { font-family: 'WenCustomFont', sans-serif !important; } ";
            }
        }

        string script = $"(function() {{ " +
                        $"  let style = document.getElementById('{styleId}'); " +
                        $"  if (style) style.remove(); " +
                        $"  style = document.createElement('style'); " +
                        $"  style.id = '{styleId}'; " +
                        $"  document.head.appendChild(style); " +
                        $"  style.innerHTML = `{css}`; " +
                        $"}})();";

        foreach (var tab in _tabs) { 
            if (tab.WebView.CoreWebView2 != null) {
                // 同步 WebView2 原生暗色偏好
                bool isWebDark = SettingsManager.Current.CurrentTheme != AppTheme.OfficeWhite;
                try {
                    tab.WebView.CoreWebView2.Profile.PreferredColorScheme = isWebDark ? 
                        CoreWebView2PreferredColorScheme.Dark : CoreWebView2PreferredColorScheme.Light;
                } catch { }

                tab.WebView.ExecuteScriptAsync(script); 
            }
        }
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
        if (_activeTab != null && (_activeTab.WebView.Source != null && (_activeTab.WebView.Source.ToString().Contains("xiaoheiv.top") || _activeTab.WebView.Source.ToString() == "about:blank"))) {
            _activeTab.WebView.CoreWebView2.Navigate(url);
        } else {
            CreateNewTab(url);
        }
    }

    public async void CreateNewTab(string url)
    {
        try {
            var theme = SettingsManager.Current.CurrentTheme;
            var colors = ThemeManager.GetColors(theme);
            var wv = new WebView2 { 
                Dock = DockStyle.Fill, 
                DefaultBackgroundColor = (theme == AppTheme.Transparent) ? Color.Transparent : colors.ContentBg 
            };
            var tabPanel = new Panel { Dock = DockStyle.Fill, BackColor = colors.ContentBg };
            tabPanel.Controls.Add(wv);
            _contentContainer.Controls.Add(tabPanel);
            var tabBtn = new Panel { Size = new Size(110, 35), BackColor = Color.Transparent, Cursor = Cursors.Hand };
            var tabTitle = new Label { Text = "加载中...", Location = new Point(8, 8), AutoSize = false, Size = new Size(70, 20), Enabled = false };
            var closeBtn = new Label { Text = "✕", Location = new Point(85, 8), Size = new Size(20, 20), TextAlign = ContentAlignment.MiddleCenter };

            var tabData = new TabData { TabPanel = tabPanel, WebView = wv, HeaderBtn = tabBtn };
            _tabs.Add(tabData);
            tabBtn.Controls.AddRange(new Control[] { tabTitle, closeBtn });
            tabBtn.Click += (s, e) => SwitchToTab(tabData);
            closeBtn.Click += (s, e) => CloseTab(tabData);
            
            // 插入到“新建”按钮之前
            _tabList.Controls.Add(tabBtn);
            _tabList.Controls.SetChildIndex(tabBtn, _tabList.Controls.Count - 2); 
            _tabList.Controls.SetChildIndex(_btnNewTab, _tabList.Controls.Count - 1); // 确保在新后面
            
            SwitchToTab(tabData);

            string userDataPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "WenBrowser_Data");
            var env = await CoreWebView2Environment.CreateAsync(null, userDataPath);
            await wv.EnsureCoreWebView2Async(env);

            wv.CoreWebView2.SetVirtualHostNameToFolderMapping("wen.fonts", System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fonts"), CoreWebView2HostResourceAccessKind.Allow);

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
        tab.HeaderBtn.BackColor = active ? colors.TabActive : (colors.TabInactive == Color.Transparent ? Color.FromArgb(1, colors.HeaderBg) : colors.TabInactive);
        tab.HeaderBtn.Controls[0].ForeColor = colors.TextColor;
        tab.HeaderBtn.Controls[1].ForeColor = active ? colors.TextColor : Color.FromArgb(80, 80, 80);
        
        // Update font for tab title
        tab.HeaderBtn.Controls[0].Font = FontManager.GetDefaultFont(9);
    }

    private void CloseTab(TabData tab)
    {
        if (_tabs.Count <= 1) return;
        _tabs.Remove(tab); _tabList.Controls.Remove(tab.HeaderBtn); _contentContainer.Controls.Remove(tab.TabPanel);
        tab.WebView.Dispose();
        if (_activeTab == tab) SwitchToTab(_tabs.Last());
    }

    // --- 全局快捷键拦截器 ---
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;
    public bool PreFilterMessage(ref Message m)
    {
        if (m.Msg == WM_KEYDOWN || m.Msg == WM_SYSKEYDOWN) {
            Keys keyData = (Keys)m.WParam | Control.ModifierKeys;
            if (HandleShortcuts(keyData)) return true;
        }
        return false;
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (HandleShortcuts(keyData)) return true;
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private bool HandleShortcuts(Keys keyData)
    {
        return HandleShortcuts((uint)(keyData & Keys.KeyCode), 0, (keyData & Keys.Alt) == Keys.Alt);
    }

    private bool HandleShortcuts(uint virtualKey, int keyData, bool altDown = false)
    {
        Keys key = (Keys)virtualKey;
        if (altDown) {
            if (key == Keys.Up) { Opacity = Math.Min(1.0, Opacity + 0.1); return true; }
            if (key == Keys.Down) { Opacity = Math.Max(0.1, Opacity - 0.1); return true; }
            if (key == Keys.T) { CreateNewTab(SettingsManager.Current.HomeUrl); return true; }
            if (key == Keys.W) { if (_activeTab != null) CloseTab(_activeTab); return true; }
            if (key == Keys.Space) { ToggleBossKey(); return true; }
            if (key == Keys.Q) { WindowState = FormWindowState.Minimized; return true; }
            if (key == Keys.B) { ToggleBossKey(); return true; }
        }
        return false;
    }

    private void ToggleBossKey() {
        if (Opacity > 0.0) { 
            Tag = Opacity; 
            this.Visible = false; // 彻底消失
            ShowInTaskbar = false; 
            _trayIcon.ShowBalloonTip(500, "Wen 浏览器", "已进入老板模式", ToolTipIcon.Info);
        }
        else { 
            this.Visible = true;
            Opacity = (Tag is double op) ? op : SettingsManager.Current.DefaultOpacity; 
            ShowInTaskbar = true; 
            this.BringToFront();
        }
    }
}
