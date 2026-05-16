using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace WTLauncher
{
    // =========================================================================
    // Sub2API Design Token \u00b7 1:1 \u8bed\u4e49\u5bf9\u9f50
    // \u8272\u5f69\u4f53\u7cfb: Primary Teal/Cyan + Slate \u8f85\u52a9\u8272
    // \u5706\u89d2: rounded-xl=12px, rounded-2xl=16px
    // \u9634\u5f71: shadow-card, shadow-card-hover
    // =========================================================================
    public static class Sub2Colors
    {
        // Primary \u2014 Teal/Cyan
        public static readonly Color P50  = Color.FromArgb(0xF0, 0xFD, 0xFA);
        public static readonly Color P100 = Color.FromArgb(0xCC, 0xFB, 0xF1);
        public static readonly Color P200 = Color.FromArgb(0x99, 0xF6, 0xE4);
        public static readonly Color P300 = Color.FromArgb(0x5E, 0xEA, 0xD4);
        public static readonly Color P400 = Color.FromArgb(0x2D, 0xD4, 0xBF);
        public static readonly Color P500 = Color.FromArgb(0x14, 0xB8, 0xA6);
        public static readonly Color P600 = Color.FromArgb(0x0D, 0x94, 0x88);
        public static readonly Color P700 = Color.FromArgb(0x0F, 0x76, 0x6E);

        // Slate \u2014 \u8f85\u52a9/\u80cc\u666f
        public static readonly Color S50  = Color.FromArgb(0xF8, 0xFA, 0xFC);
        public static readonly Color S100 = Color.FromArgb(0xF1, 0xF5, 0xF9);
        public static readonly Color S200 = Color.FromArgb(0xE2, 0xE8, 0xF0);
        public static readonly Color S300 = Color.FromArgb(0xCB, 0xD5, 0xE1);
        public static readonly Color S400 = Color.FromArgb(0x94, 0xA3, 0xB8);
        public static readonly Color S500 = Color.FromArgb(0x64, 0x74, 0x8B);
        public static readonly Color S600 = Color.FromArgb(0x47, 0x55, 0x69);
        public static readonly Color S700 = Color.FromArgb(0x33, 0x41, 0x55);
        public static readonly Color S800 = Color.FromArgb(0x1E, 0x29, 0x3B);
        public static readonly Color S900 = Color.FromArgb(0x0F, 0x17, 0x2A);

        // Semantic
        public static readonly Color Success = Color.FromArgb(0x10, 0xB9, 0x81);
        public static readonly Color Danger  = Color.FromArgb(0xEF, 0x44, 0x44);
        public static readonly Color Warning = Color.FromArgb(0xF5, 0x9E, 0x0B);
        public static readonly Color Info    = Color.FromArgb(0x3B, 0x82, 0xF6);

        // Dark mode
        public static readonly Color DarkBg    = S900;
        public static readonly Color DarkCard  = S800;
        public static readonly Color DarkBorder = Color.FromArgb(0x33, 0x41, 0x55);
        public static readonly Color DarkTextPrimary = S200;
        public static readonly Color DarkTextSecondary = S400;

        public static LinearGradientBrush GetPrimaryGradient(Rectangle rect)
        {
            return new LinearGradientBrush(rect, P500, P600, LinearGradientMode.Horizontal);
        }

        public static LinearGradientBrush GetPrimaryGradientVertical(Rectangle rect)
        {
            return new LinearGradientBrush(rect, P500, P600, LinearGradientMode.Vertical);
        }
    }

    // =========================================================================
    // Graphics \u6269\u5c55
    // =========================================================================
    public static class GraphicsExtensions
    {
        public static GraphicsPath GetRoundedRect(Rectangle rect, int r)
        {
            var path = new GraphicsPath();
            if (r > rect.Width / 2) r = rect.Width / 2;
            if (r > rect.Height / 2) r = rect.Height / 2;
            path.AddArc(rect.X, rect.Y, r * 2, r * 2, 180, 90);
            path.AddArc(rect.Right - r * 2, rect.Y, r * 2, r * 2, 270, 90);
            path.AddArc(rect.Right - r * 2, rect.Bottom - r * 2, r * 2, r * 2, 0, 90);
            path.AddArc(rect.X, rect.Bottom - r * 2, r * 2, r * 2, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void FillRoundedRect(this Graphics g, Brush brush, Rectangle rect, int r)
        {
            using (var path = GetRoundedRect(rect, r)) g.FillPath(brush, path);
        }

        public static void DrawRoundedRect(this Graphics g, Pen pen, Rectangle rect, int r)
        {
            using (var path = GetRoundedRect(rect, r)) g.DrawPath(pen, path);
        }
    }

    // =========================================================================
    // \u6570\u636e\u6a21\u578b
    // =========================================================================
    public class ButtonItem
    {
        public string name { get; set; }
        public string directory { get; set; }
        public string command { get; set; }
        public bool confirm { get; set; }
        public ButtonItem()
        {
            name = "";
            directory = "";
            command = "";
            confirm = true;
        }
    }

    // =========================================================================
    // \u4e3b\u7a97\u53e3
    // =========================================================================
    public class MainForm : Form
    {
        private List<ButtonItem> _items = new List<ButtonItem>();
        private HashSet<int> _selected = new HashSet<int>();
        private string _configPath;
        private string _lastLaunched = "\u2014";
        private bool _darkMode = false;
        private bool _sidebarCollapsed = true;
        private const int SidebarExpanded = 200;
        private const int SidebarCollapsed = 68;
        private int CurrentSidebarWidth { get { return _sidebarCollapsed ? SidebarCollapsed : SidebarExpanded; } }

        // Layout panels
        private Panel _sidebar;
        private Panel _sidebarBrand;
        private Panel _sidebarNav;
        private Panel _sidebarBottom;
        private Panel _mainArea;
        private Panel _header;
        private Panel _contentWrap;
        private Panel _statsRow;
        private FlowLayoutPanel _cardPanel;
        private Panel _statusBar;

        // Sidebar controls
        private Button _btnCollapse;
        private Button _btnThemeToggle;
        private Button _btnNavDashboard;
        private Button _btnNavLauncher;
        private Button _btnNavSettings;
        private Label _lblBrandText;
        private Label _lblVersion;
        private Label _lblCollapseLabel;
        private Label _lblThemeLabel;

        // Header controls
        private Label _lblPageTitle;
        private Label _lblPageDesc;
        private Button _btnAdd;

        // Stats controls
        private Label _lblStat1Value;
        private Label _lblStat1Sub;
        private Label _lblStat2Value;
        private Label _lblStat2Sub;
        private Label _lblStat3Value;
        private Label _lblStat3Sub;
        private Label _lblStat4Value;
        private Label _lblStat4Sub;

        // Batch bar (inside content area)
        private Panel _batchBar;
        private CheckBox _chkSelectAll;
        private Label _lblSelCount;
        private Button _btnBatchLaunch;

        // Status bar
        private Label _lblStatus;

        // \u2014\u2014 \u6784\u9020\u51fd\u6570 \u2014\u2014
        private static string ResolveConfigPath(string startupPath)
        {
            if (string.IsNullOrWhiteSpace(startupPath))
                throw new ArgumentException("startupPath");

            var localConfig = Path.Combine(startupPath, "config.json");
            var parent = Directory.GetParent(startupPath);
            if (parent == null)
                return localConfig;

            var rootConfig = Path.Combine(parent.FullName, "config.json");
            return File.Exists(rootConfig) ? rootConfig : localConfig;
        }

        public MainForm()
        {
            _configPath = ResolveConfigPath(Application.StartupPath);
            _darkMode = LoadThemePref();
            InitializeComponent();
            LoadItems();
            RenderStats();
            RenderCards();
            ApplyTheme();
            UpdateSidebarLabels();
        }

        // \u2014\u2014 \u521d\u59cb\u5316\u7ec4\u4ef6 \u2014\u2014
        private void InitializeComponent()
        {
            Text = "WT Launcher";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(960, 640);
            Size = new Size(1100, 720);
            BackColor = _darkMode ? Sub2Colors.DarkBg : Sub2Colors.S50;
            ForeColor = _darkMode ? Sub2Colors.DarkTextPrimary : Sub2Colors.S700;
            Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
            FormBorderStyle = FormBorderStyle.Sizable;
            Padding = new Padding(0);
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw, true);

            // ── Background paint (mesh gradient) ──
            Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var bg = new SolidBrush(_darkMode ? Sub2Colors.DarkBg : Sub2Colors.S50))
                    g.FillRectangle(bg, ClientRectangle);
                if (!_darkMode)
                {
                    DrawRadialGlow(g, Color.FromArgb(25, Sub2Colors.P500), Width * 0.3f, Height * 0.15f, 300);
                    DrawRadialGlow(g, Color.FromArgb(18, Sub2Colors.P400), Width * 0.8f, Height * 0.85f, 350);
                    DrawRadialGlow(g, Color.FromArgb(14, Sub2Colors.P300), Width * 0.12f, Height * 0.65f, 250);
                }
            };

            // ====================================================================
            // SIDEBAR \u2014 \u56fa\u5b9a\u5de6\u4fa7\uff0c\u53ef\u6298\u53e0
            // ====================================================================
            _sidebar = new Panel
            {
                Width = CurrentSidebarWidth,
                Height = ClientSize.Height,
                Location = new Point(0, 0),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left,
                BackColor = _darkMode ? Sub2Colors.S900 : Color.FromArgb(0x0F, 0x17, 0x2A),
            };
            _sidebar.Paint += (s, e) =>
            {
                // right border
                using (var p = new Pen(_darkMode ? Sub2Colors.DarkBorder : Color.FromArgb(0x1E, 0x29, 0x3B)))
                    e.Graphics.DrawLine(p, _sidebar.Width - 1, 0, _sidebar.Width - 1, _sidebar.Height);
            };

            // ── Sidebar Brand (Logo + Title) ──
            _sidebarBrand = new Panel
            {
                Height = 64,
                Width = CurrentSidebarWidth,
                BackColor = Color.Transparent,
            };
            _sidebarBrand.Paint += (s, e) =>
            {
                var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
                // bottom border
                using (var p = new Pen(Color.FromArgb(0x1E, 0x29, 0x3B)))
                    g.DrawLine(p, 0, _sidebarBrand.Height - 1, _sidebarBrand.Width, _sidebarBrand.Height - 1);
                // Logo icon
                var logoRect = new Rectangle(_sidebarCollapsed ? 14 : 16, 14, 36, 36);
                using (var grad = Sub2Colors.GetPrimaryGradient(logoRect))
                    g.FillRoundedRect(grad, logoRect, 10);
                using (var font = new Font("Segoe UI", 14F, FontStyle.Bold))
                    TextRenderer.DrawText(g, "W", font, logoRect, Color.White,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };

            _lblBrandText = new Label
            {
                Text = "WT Launcher",
                Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Location = new Point(62, 15),
                AutoSize = true,
            };
            _lblVersion = new Label
            {
                Text = "v1.0",
                Font = new Font("Cascadia Code", 8F),
                ForeColor = Sub2Colors.S500,
                BackColor = Color.Transparent,
                Location = new Point(62, 37),
                AutoSize = true,
            };
            _sidebarBrand.Controls.Add(_lblBrandText);
            _sidebarBrand.Controls.Add(_lblVersion);

            // ── Sidebar Nav ──
            _sidebarNav = new Panel
            {
                Width = CurrentSidebarWidth,
                BackColor = Color.Transparent,
            };

            _btnNavDashboard = CreateSidebarNavButton(
                "\u25a0", "\u6982\u89c8", 0, false);
            _btnNavLauncher = CreateSidebarNavButton(
                "\u25b6", "\u542f\u52a8\u5668", 50, true);
            _btnNavSettings = CreateSidebarNavButton(
                "\u2699", "\u8bbe\u7f6e", 100, false);

            _sidebarNav.Controls.AddRange(new Control[] {
                _btnNavDashboard, _btnNavLauncher, _btnNavSettings
            });

            // ── Sidebar Bottom (Theme + Collapse) ──
            _sidebarBottom = new Panel
            {
                Width = CurrentSidebarWidth,
                Height = 100,
                BackColor = Color.Transparent,
            };
            _sidebarBottom.Paint += (s, e) =>
            {
                // top border
                using (var p = new Pen(Color.FromArgb(0x1E, 0x29, 0x3B)))
                    e.Graphics.DrawLine(p, 0, 0, _sidebarBottom.Width, 0);
            };
            // Theme toggle label
            _lblThemeLabel = new Label
            {
                Text = _darkMode ? "\u6d45\u8272\u6a21\u5f0f" : "\u6df1\u8272\u6a21\u5f0f",
                Font = new Font("Microsoft YaHei UI", 9F),
                ForeColor = Sub2Colors.S400,
                BackColor = Color.Transparent,
                Location = new Point(56, 14),
                AutoSize = true,
            };
            _btnThemeToggle = new Button
            {
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Sub2Colors.S400,
                Font = new Font("Segoe UI", 14F),
                Cursor = Cursors.Hand,
                Size = new Size(32, 32),
                Location = new Point(14, 10),
                Text = _darkMode ? "\u2600" : "\u263e",
                TabStop = false,
            };
            _btnThemeToggle.FlatAppearance.BorderSize = 0;
            _btnThemeToggle.Click += (s, e) => ToggleDarkMode();

            // Collapse label
            _lblCollapseLabel = new Label
            {
                Text = "\u6298\u53e0",
                Font = new Font("Microsoft YaHei UI", 9F),
                ForeColor = Sub2Colors.S400,
                BackColor = Color.Transparent,
                Location = new Point(56, 54),
                AutoSize = true,
            };
            _btnCollapse = new Button
            {
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Sub2Colors.S400,
                Font = new Font("Segoe UI", 12F),
                Cursor = Cursors.Hand,
                Size = new Size(32, 32),
                Location = new Point(14, 50),
                Text = _sidebarCollapsed ? "\u25b6" : "\u25c0",
                TabStop = false,
            };
            _btnCollapse.FlatAppearance.BorderSize = 0;
            _btnCollapse.Click += (s, e) => ToggleSidebar();

            _sidebarBottom.Controls.AddRange(new Control[] {
                _lblThemeLabel, _btnThemeToggle,
                _lblCollapseLabel, _btnCollapse
            });

            // Sidebar layout assembly
            _sidebarNav.Top = _sidebarBrand.Bottom;
            _sidebarNav.Height = _sidebar.Height - _sidebarBrand.Height - _sidebarBottom.Height - 10;
            _sidebarBottom.Top = _sidebarNav.Bottom + 10;
            _sidebarBottom.Left = 0;

            _sidebar.Controls.AddRange(new Control[] {
                _sidebarBrand, _sidebarNav, _sidebarBottom
            });

            // ====================================================================
            // MAIN AREA \u2014 \u4fa7\u8fb9\u680f\u53f3\u4fa7\u5185\u5bb9
            // ====================================================================
            _mainArea = new Panel
            {
                Left = _sidebar.Width,
                Width = ClientSize.Width - _sidebar.Width,
                Height = ClientSize.Height,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.Transparent,
            };

            // ── HEADER (glass, sticky, h-16) ──
            _header = new Panel
            {
                Height = 64,
                Dock = DockStyle.Top,
                BackColor = Color.Transparent,
            };
            _header.Paint += (s, e) =>
            {
                var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
                // glass background: semi-transparent white/dark
                var bgColor = _darkMode
                    ? Color.FromArgb(220, Sub2Colors.DarkCard)
                    : Color.FromArgb(230, 0xFF, 0xFF, 0xFF);
                var rect = new Rectangle(0, 0, _header.Width, _header.Height);
                using (var bgBrush = new SolidBrush(bgColor))
                    g.FillRectangle(bgBrush, rect);
                // bottom border
                using (var p = new Pen(_darkMode ? Sub2Colors.DarkBorder : Color.FromArgb(0xE2, 0xE8, 0xF0)))
                    g.DrawLine(p, 0, _header.Height - 1, _header.Width, _header.Height - 1);
            };

            // Page title (left side)
            _lblPageTitle = new Label
            {
                Text = "\u542f\u52a8\u5668",
                Font = new Font("Microsoft YaHei UI", 16F, FontStyle.Bold),
                ForeColor = _darkMode ? Sub2Colors.DarkTextPrimary : Sub2Colors.S700,
                Location = new Point(28, 12),
                AutoSize = true,
                BackColor = Color.Transparent,
            };
            _lblPageDesc = new Label
            {
                Text = "\u7ba1\u7406\u5e76\u542f\u52a8 Windows Terminal \u4f1a\u8bdd",
                Font = new Font("Microsoft YaHei UI", 10F),
                ForeColor = _darkMode ? Sub2Colors.DarkTextSecondary : Sub2Colors.S400,
                Location = new Point(28, 38),
                AutoSize = true,
                BackColor = Color.Transparent,
            };

            // Right actions
            _btnAdd = new Button
            {
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Size = new Size(120, 36),
                Text = "  +  \u65b0\u5efa",
            };
            _btnAdd.FlatAppearance.BorderSize = 0;
            _btnAdd.Paint += (s, e) =>
            {
                var b = (Button)s;
                var rect = new Rectangle(0, 0, b.Width - 1, b.Height - 1);
                using (var grad = Sub2Colors.GetPrimaryGradient(rect))
                    e.Graphics.FillRoundedRect(grad, rect, 10);
                using (var font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold))
                    TextRenderer.DrawText(e.Graphics, b.Text, font, rect, Color.White,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            _btnAdd.Click += (s, e) => ShowEditDialog(-1);
            _btnAdd.Location = new Point(_header.Width - 160, 14);
            _btnAdd.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            _header.Controls.AddRange(new Control[] {
                _lblPageTitle, _lblPageDesc, _btnAdd
            });

            // ── Content Wrap (fill, with proper padding) ──
            _contentWrap = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(28, 24, 28, 16),
            };

            // ── Stats Row (4 cards grid) ──
            _statsRow = new Panel
            {
                Height = 86,
                Dock = DockStyle.Top,
                BackColor = Color.Transparent,
            };

            // Stat 1: Total items (primary)
            var stat1 = CreateStatCard(0, Sub2Colors.P100, Sub2Colors.P600,
                "\u25a0",
                "\u542f\u52a8\u9879", out _lblStat1Value, out _lblStat1Sub,
                "\u5171\u8ba1\u9879\u76ee");
            // Stat 2: Last launched (info)
            var stat2 = CreateStatCard(1, Color.FromArgb(0xDB, 0xEA, 0xFE), Sub2Colors.Info,
                "\u25b6",
                "\u6700\u8fd1\u542f\u52a8", out _lblStat2Value, out _lblStat2Sub,
                "\u4e0a\u6b21\u4f7f\u7528");
            // Stat 3: Batch ready (success)
            CreateStatCard(2, Color.FromArgb(0xD1, 0xFA, 0xE5), Sub2Colors.Success,
                "\u2713",
                "\u6279\u91cf\u5f85\u547d", out _lblStat3Value, out _lblStat3Sub,
                "\u5df2\u52fe\u9009\u9879");
            // Stat 4: Selected (warning)
            CreateStatCard(3, Color.FromArgb(0xFE, 0xF3, 0xC7), Sub2Colors.Warning,
                "\u2605",
                "\u5df2\u9009\u4e2d", out _lblStat4Value, out _lblStat4Sub,
                "\u53ef\u6279\u91cf\u542f\u52a8");

            // Keep the active path close to the retired HTA: show only the two
            // high-signal counters and leave batch state in the batch bar.
            _statsRow.Controls.AddRange(new Control[] { stat1, stat2 });
            PositionStatCards();

            // ── Batch Bar ──
            _batchBar = new Panel
            {
                Height = 46,
                Dock = DockStyle.Top,
                BackColor = Color.Transparent,
            };

            _chkSelectAll = new CheckBox
            {
                Text = "\u5168\u9009",
                FlatStyle = FlatStyle.Standard,
                Location = new Point(4, 12),
                AutoSize = true,
                Font = new Font("Microsoft YaHei UI", 10F),
                ForeColor = _darkMode ? Sub2Colors.DarkTextSecondary : Sub2Colors.S400,
            };
            _chkSelectAll.CheckedChanged += SelectAll_CheckedChanged;

            _lblSelCount = new Label
            {
                AutoSize = true,
                Location = new Point(64, 14),
                Font = new Font("Microsoft YaHei UI", 10F),
                ForeColor = _darkMode ? Sub2Colors.DarkTextSecondary : Sub2Colors.S400,
            };

            _btnBatchLaunch = new Button
            {
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false,
                Size = new Size(120, 34),
                Text = "\u6279\u91cf\u542f\u52a8",
            };
            _btnBatchLaunch.FlatAppearance.BorderSize = 0;
            _btnBatchLaunch.Paint += (s, e) =>
            {
                var b = (Button)s;
                var rect = new Rectangle(0, 0, b.Width - 1, b.Height - 1);
                if (b.Enabled)
                {
                    using (var grad = Sub2Colors.GetPrimaryGradient(rect))
                        e.Graphics.FillRoundedRect(grad, rect, 10);
                }
                else
                {
                    using (var b2 = new SolidBrush(_darkMode ? Sub2Colors.S700 : Sub2Colors.S200))
                        e.Graphics.FillRoundedRect(b2, rect, 10);
                }
                using (var font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold))
                    TextRenderer.DrawText(e.Graphics, b.Text, font, rect,
                        b.Enabled ? Color.White : Sub2Colors.S400,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            _btnBatchLaunch.Click += (s, e) => LaunchSelected();
            _btnBatchLaunch.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            _batchBar.Controls.AddRange(new Control[] { _chkSelectAll, _lblSelCount, _btnBatchLaunch });

            // ── Card Panel (FlowLayout) ──
            _cardPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 12, 0, 0),
            };

            // ── Status Bar ──
            _statusBar = new Panel
            {
                Height = 40,
                Dock = DockStyle.Bottom,
                BackColor = Color.Transparent,
            };
            _lblStatus = new Label
            {
                Text = "Ready",
                AutoSize = false,
                Dock = DockStyle.Fill,
                Font = new Font("Microsoft YaHei UI", 9F),
                ForeColor = _darkMode ? Sub2Colors.DarkTextSecondary : Sub2Colors.S500,
                Padding = new Padding(28, 10, 24, 0),
                BackColor = Color.Transparent,
            };
            _statusBar.Paint += (s, e) =>
            {
                var g = e.Graphics;
                using (var p = new Pen(_darkMode ? Sub2Colors.DarkBorder : Sub2Colors.S200))
                    g.DrawLine(p, 0, 0, _statusBar.Width, 0);
                // accent bar on status
                if (!string.IsNullOrEmpty(_lblStatus.Text) && _lblStatus.Text != "Ready")
                {
                    var accent = _lblStatus.ForeColor == Sub2Colors.Success ? Sub2Colors.Success : Sub2Colors.S400;
                    using (var b = new SolidBrush(accent))
                        g.FillRectangle(b, 26, 12, 3, 16);
                }
                using (var font = new Font("Microsoft YaHei UI", 8F))
                using (var b = new SolidBrush(_darkMode ? Sub2Colors.S600 : Sub2Colors.S200))
                    TextRenderer.DrawText(g, "config.json", font,
                        new Point(_statusBar.Width - 100, 12), b.Color);
            };
            _statusBar.Controls.Add(_lblStatus);

            // ── Content assembly ──
            _contentWrap.Controls.Add(_cardPanel);
            _contentWrap.Controls.Add(_batchBar);
            _contentWrap.Controls.Add(_statsRow);

            // ── Main area assembly ──
            _mainArea.Controls.Add(_contentWrap);
            _mainArea.Controls.Add(_header);

            // ── Form assembly ──
            Controls.Add(_mainArea);
            Controls.Add(_statusBar);
            Controls.Add(_sidebar);

            // ── Key events ──
            KeyPreview = true;
            KeyDown += (s, e) =>
            {
                if (e.Control && e.KeyCode == Keys.Enter)
                {
                    if (_items.Count > 0) LaunchItem(0);
                    e.SuppressKeyPress = true;
                }
            };

            // Resize handlers
            Resize += (s, e) => LayoutShell();
            LayoutShell();
        }

        private void LayoutShell()
        {
            if (_sidebar == null || _mainArea == null) return;

            _mainArea.Left = _sidebar.Width;
            _mainArea.Width = ClientSize.Width - _sidebar.Width;
            _mainArea.Height = ClientSize.Height;

            if (_sidebarBrand != null && _sidebarNav != null && _sidebarBottom != null)
            {
                _sidebar.Height = ClientSize.Height;
                _sidebarNav.Height = _sidebar.Height - _sidebarBrand.Height - _sidebarBottom.Height - 10;
                _sidebarBottom.Top = _sidebarNav.Bottom + 10;
            }

            if (_statsRow != null) PositionStatCards();
            if (_batchBar != null && _btnBatchLaunch != null)
                _btnBatchLaunch.Location = new Point(Math.Max(4, _batchBar.Width - 140), 6);
        }

        private void SelectAll_CheckedChanged(object sender, EventArgs e)
        {
            _selected.Clear();
            if (_chkSelectAll.Checked)
            {
                for (int i = 0; i < _items.Count; i++) _selected.Add(i);
            }
            UpdateBatchUI();
            RenderCards();
        }

        // ── Sidebar Navigation Button ──
        private Button CreateSidebarNavButton(string icon, string label, int top, bool active)
        {
            var btn = new Button
            {
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                Font = new Font("Microsoft YaHei UI", 10F),
                Cursor = Cursors.Hand,
                Size = new Size(CurrentSidebarWidth, 42),
                Location = new Point(0, top),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(0),
                TabStop = false,
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Paint += (s, e) =>
            {
                var b = (Button)s;
                var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
                bool isActive = active;
                var rect = new Rectangle(8, 0, _sidebarCollapsed ? 52 : b.Width - 16, b.Height);

                // Active bg
                if (isActive)
                {
                    using (var bg = new SolidBrush(Color.FromArgb(30, Sub2Colors.P500)))
                        g.FillRoundedRect(bg, rect, 10);
                }

                // Icon
                using (var font = new Font("Segoe UI", _sidebarCollapsed ? 16F : 12F))
                {
                    var col = isActive ? Sub2Colors.P400 : Sub2Colors.S400;
                    TextRenderer.DrawText(g, icon, font,
                        new Point(_sidebarCollapsed ? 24 : 20, 10),
                        col, TextFormatFlags.HorizontalCenter);
                }

                // Label (hidden when collapsed)
                if (!_sidebarCollapsed)
                {
                    using (var font = new Font("Microsoft YaHei UI", 10F))
                    {
                        var col = isActive ? Sub2Colors.P400 : Sub2Colors.S400;
                        TextRenderer.DrawText(g, label, font,
                            new Point(52, 11), col,
                            TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
                    }
                }
            };
            return btn;
        }

        // ── Toggle Sidebar ──
        private void ToggleSidebar()
        {
            _sidebarCollapsed = !_sidebarCollapsed;
            int newWidth = _sidebarCollapsed ? SidebarCollapsed : SidebarExpanded;

            _sidebar.Width = newWidth;
            _sidebarBrand.Width = newWidth;
            _sidebarNav.Width = newWidth;

            foreach (Control c in _sidebarNav.Controls)
            {
                if (c is Button)
                    c.Width = newWidth;
            }

            _sidebarBottom.Width = newWidth;

            _btnCollapse.Text = _sidebarCollapsed ? "\u25b6" : "\u25c0";

            UpdateSidebarLabels();

            _sidebarBrand.Invalidate();
            _sidebarNav.Invalidate();
            foreach (Control c in _sidebarNav.Controls) c.Invalidate();
            _sidebarBottom.Invalidate();

            LayoutShell();
        }

        private void UpdateSidebarLabels()
        {
            _lblBrandText.Visible = !_sidebarCollapsed;
            _lblVersion.Visible = !_sidebarCollapsed;
            _lblThemeLabel.Visible = !_sidebarCollapsed;
            _lblCollapseLabel.Visible = !_sidebarCollapsed;

            int iconOffset = _sidebarCollapsed ? 10 : 14;
            _btnThemeToggle.Location = new Point(iconOffset, 10);
            _btnCollapse.Location = new Point(iconOffset, 50);
        }

        // \u2014\u2014 \u5f84\u5411\u5149\u6655 \u2014\u2014
        private void DrawRadialGlow(Graphics g, Color color, float cx, float cy, float radius)
        {
            using (var path = new GraphicsPath())
            {
                path.AddEllipse(cx - radius, cy - radius, radius * 2, radius * 2);
                using (var brush = new PathGradientBrush(path))
                {
                    brush.CenterColor = color;
                    brush.SurroundColors = new Color[] { Color.FromArgb(0, color) };
                    g.FillPath(brush, path);
                }
            }
        }

        // \u2014\u2014 \u7edf\u8ba1\u5361\u7247 \u2014\u2014
        private Panel CreateStatCard(int index, Color iconBg, Color iconColor,
            string iconText, string label,
            out Label valueLabel, out Label subLabel,
            string subText)
        {
            var panel = new Panel
            {
                Width = 220,
                Height = 78,
                BackColor = Color.Transparent,
            };
            // Card bg
            panel.Paint += (s, e) =>
            {
                var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, panel.Width - 1, panel.Height - 1);
                var bg = _darkMode ? Sub2Colors.DarkCard : Color.White;
                using (var b = new SolidBrush(bg))
                    g.FillRoundedRect(b, rect, 14);
                using (var p = new Pen(_darkMode ? Sub2Colors.DarkBorder : Sub2Colors.S200))
                    g.DrawRoundedRect(p, rect, 14);
            };

            // Icon container (rounded-lg, colored)
            var icon = new Label
            {
                Size = new Size(38, 38),
                Location = new Point(14, 20),
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 14F),
            };
            icon.Paint += (s, e) =>
            {
                var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
                var r = new Rectangle(0, 0, 38, 38);
                using (var b = new SolidBrush(iconBg))
                    g.FillRoundedRect(b, r, 10);
                using (var font = new Font("Segoe UI", 13F))
                    TextRenderer.DrawText(g, iconText, font, r, iconColor,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            icon.Text = iconText;

            // Label (text-xs font-medium)
            var lbl = new Label
            {
                Text = label,
                Font = new Font("Microsoft YaHei UI", 9F),
                ForeColor = _darkMode ? Sub2Colors.DarkTextSecondary : Sub2Colors.S400,
                Location = new Point(62, 18),
                AutoSize = true,
            };

            // Value (text-xl font-bold)
            valueLabel = new Label
            {
                Font = new Font("Microsoft YaHei UI", 20F, FontStyle.Bold),
                ForeColor = _darkMode ? Sub2Colors.DarkTextPrimary : Sub2Colors.S700,
                Location = new Point(62, 36),
                AutoSize = true,
                Text = "0",
            };

            // Sub (text-xs)
            subLabel = new Label
            {
                Text = subText,
                Font = new Font("Microsoft YaHei UI", 8F),
                ForeColor = _darkMode ? Sub2Colors.S500 : Sub2Colors.S300,
                Location = new Point(valueLabel.Right + 6, 46),
                AutoSize = true,
            };

            panel.Controls.Add(icon);
            panel.Controls.Add(lbl);
            panel.Controls.Add(valueLabel);
            panel.Controls.Add(subLabel);

            return panel;
        }

        private void PositionStatCards()
        {
            int gap = 12;
            int startX = 0;
            int totalW = _statsRow.Width;
            int cardW = 220;
            int cards = Math.Max(1, _statsRow.Controls.Count);
            int usedW = cards * cardW + (cards - 1) * gap;
            if (usedW < totalW)
                startX = (totalW - usedW) / 2;

            int idx = 0;
            foreach (Control c in _statsRow.Controls)
            {
                c.Location = new Point(startX + idx * (cardW + gap), 4);
                idx++;
            }
        }

        // \u2014\u2014 \u5361\u7247\u6e32\u67d3 \u2014\u2014
        private void RenderCards()
        {
            _cardPanel.Controls.Clear();

            if (_items.Count == 0)
            {
                ShowEmptyState();
                return;
            }

            for (int i = 0; i < _items.Count; i++)
            {
                int idx = i;
                var item = _items[i];
                var card = CreateCard(idx, item);
                _cardPanel.Controls.Add(card);
            }

            // Add card "+"
            var addCard = CreateAddCard();
            _cardPanel.Controls.Add(addCard);
        }

        private Panel CreateCard(int idx, ButtonItem item)
        {
            var card = new Panel
            {
                Size = new Size(320, 146),
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 16, 16),
                Cursor = Cursors.Hand,
                Tag = idx,
            };

            bool isHovered = false;

            card.Paint += (s, e) =>
            {
                var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                var isSel = _selected.Contains(idx);
                var bgColor = _darkMode ? Sub2Colors.DarkCard : Color.White;
                var borderColor = isSel ? Sub2Colors.P500
                    : isHovered ? (_darkMode ? Sub2Colors.S500 : Sub2Colors.S300)
                    : (_darkMode ? Sub2Colors.DarkBorder : Sub2Colors.S200);
                float borderWidth = isSel ? 2 : 1;
                int radius = 16;

                // Shadow (card shadow)
                var shadowR = new Rectangle(2, 3, rect.Width, rect.Height);
                using (var shadow = new SolidBrush(_darkMode
                    ? Color.FromArgb(35, 0, 0, 0)
                    : Color.FromArgb(14, 0, 0, 0)))
                    g.FillRoundedRect(shadow, shadowR, radius);

                // Card bg
                using (var b = new SolidBrush(bgColor))
                    g.FillRoundedRect(b, rect, radius);
                using (var p = new Pen(borderColor, borderWidth))
                    g.DrawRoundedRect(p, rect, radius);

                // Selection top accent
                if (isSel)
                {
                    using (var grad = Sub2Colors.GetPrimaryGradient(
                        new Rectangle(radius, 0, rect.Width - radius * 2, 3)))
                        g.FillRectangle(grad, radius, 0, rect.Width - radius * 2, 3);
                }

                // Name
                using (var font = new Font("Microsoft YaHei UI", 15F, FontStyle.Bold))
                    TextRenderer.DrawText(g, item.name, font, new Rectangle(16, 12, card.Width - 56, 28),
                        _darkMode ? Sub2Colors.DarkTextPrimary : Sub2Colors.S700,
                        TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter);

                // Directory
                if (!string.IsNullOrEmpty(item.directory))
                {
                    using (var font = new Font("Cascadia Code", 10F))
                        TextRenderer.DrawText(g, item.directory, font,
                            new Rectangle(16, 48, card.Width - 48, 24),
                            _darkMode ? Sub2Colors.DarkTextSecondary : Sub2Colors.S500,
                            TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
                }

                // Command
                if (!string.IsNullOrEmpty(item.command))
                {
                    using (var font = new Font("Cascadia Code", 10F))
                        TextRenderer.DrawText(g, item.command, font,
                            new Rectangle(16, 74, card.Width - 48, 24),
                            _darkMode ? Sub2Colors.S500 : Sub2Colors.S300,
                            TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
                }

                // Confirm badge
                if (!item.confirm)
                {
                    using (var font = new Font("Microsoft YaHei UI", 8F))
                    using (var b = new SolidBrush(Sub2Colors.S300))
                        TextRenderer.DrawText(g, "\u76f4\u63a5\u542f\u52a8", font,
                            new Point(16, 116), b.Color,
                            TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
                }

                // Arrow indicator (right side)
                using (var arrow = new SolidBrush(_darkMode ? Sub2Colors.S600 : Sub2Colors.S200))
                {
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    float ax = card.Width - 22;
                    float ay = card.Height / 2 - 4;
                    g.FillPolygon(arrow, new PointF[] {
                        new PointF(ax, ay), new PointF(ax + 8, ay + 4), new PointF(ax, ay + 8)
                    });
                }
            };

            // Hover effects
            card.MouseEnter += (s, e) =>
            {
                isHovered = true;
                card.Invalidate();
            };
            card.MouseLeave += (s, e) =>
            {
                isHovered = false;
                card.Invalidate();
            };
            card.Click += (s, e) => LaunchItem(idx);

            // Context menu
            var cms = new ContextMenuStrip();
            cms.Items.Add("\u7f16\u8f91", null, (s, e) => ShowEditDialog(idx));
            cms.Items.Add("\u5220\u9664", null, (s, e) => DeleteItem(idx));
            cms.Items.Add("-");
            cms.Items.Add("\u52fe\u9009/\u53d6\u6d88", null, (s, e) => ToggleSelect(idx));
            card.ContextMenuStrip = cms;

            // Hover checkbox
            var chk = new CheckBox
            {
                Checked = _selected.Contains(idx),
                Location = new Point(card.Width - 30, 12),
                AutoSize = false,
                Size = new Size(18, 18),
                FlatStyle = FlatStyle.Standard,
                Visible = false,
            };
            chk.CheckedChanged += (s, e) =>
            {
                if (chk.Checked) { _selected.Add(idx); } else { _selected.Remove(idx); }
                card.Invalidate();
                UpdateBatchUI();
            };
            card.MouseEnter += (s, e) =>
            {
                if (!chk.Visible) { chk.Visible = true; card.Invalidate(); }
            };
            card.MouseLeave += (s, e) =>
            {
                if (!_selected.Contains(idx)) { chk.Visible = false; card.Invalidate(); }
            };
            card.Controls.Add(chk);

            return card;
        }

        private Panel CreateAddCard()
        {
            var addCard = new Panel
            {
                Size = new Size(320, 146),
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 16, 16),
                Cursor = Cursors.Hand,
            };
            addCard.Paint += (s, e) =>
            {
                var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, addCard.Width - 1, addCard.Height - 1);
                var dashColor = _darkMode ? Sub2Colors.S600 : Sub2Colors.S300;
                using (var p = new Pen(dashColor, 2)) { p.DashStyle = DashStyle.Dash; g.DrawRoundedRect(p, rect, 16); }
                using (var font = new Font("Microsoft YaHei UI", 24F))
                    TextRenderer.DrawText(g, "+", font, new Point(addCard.Width / 2 - 15, 30),
                        dashColor, TextFormatFlags.HorizontalCenter);
                using (var font = new Font("Microsoft YaHei UI", 11F))
                    TextRenderer.DrawText(g, "\u65b0\u5efa\u542f\u52a8\u9879", font,
                        new Point(addCard.Width / 2 - 45, 76),
                        _darkMode ? Sub2Colors.S500 : Sub2Colors.S300,
                        TextFormatFlags.HorizontalCenter);
            };
            addCard.MouseEnter += (s, e) => addCard.BackColor = _darkMode
                ? Color.FromArgb(30, Sub2Colors.P500)
                : Color.FromArgb(8, Sub2Colors.P500);
            addCard.MouseLeave += (s, e) => addCard.BackColor = Color.Transparent;
            addCard.Click += (s, e) => ShowEditDialog(-1);
            return addCard;
        }

        private void ShowEmptyState()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
            };
            panel.Paint += (s, e) =>
            {
                var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
                var cx = panel.Width / 2;
                var cy = panel.Height / 2 - 20;

                // Icon circle
                var iconR = new Rectangle(cx - 32, cy - 60, 64, 64);
                using (var b = new SolidBrush(_darkMode
                    ? Color.FromArgb(30, Sub2Colors.P500)
                    : Color.FromArgb(12, Sub2Colors.P500)))
                    g.FillRoundedRect(b, iconR, 16);
                using (var font = new Font("Segoe UI", 24F))
                    TextRenderer.DrawText(g, "\u25a1", font, iconR,
                        _darkMode ? Sub2Colors.S500 : Sub2Colors.S300,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                // Title
                using (var font = new Font("Microsoft YaHei UI", 14F, FontStyle.Bold))
                    TextRenderer.DrawText(g, "\u6682\u65e0\u542f\u52a8\u9879", font,
                        new Point(cx - 60, cy + 20),
                        _darkMode ? Sub2Colors.DarkTextPrimary : Sub2Colors.S700,
                        TextFormatFlags.HorizontalCenter);

                // Description
                using (var font = new Font("Microsoft YaHei UI", 10F))
                    TextRenderer.DrawText(g,
                        "\u70b9\u51fb\u53f3\u4e0a\u89d2\u300c+ \u65b0\u5efa\u300d\u6dfb\u52a0\u7b2c\u4e00\u4e2a\u542f\u52a8\u9879",
                        font, new Point(cx - 130, cy + 50),
                        _darkMode ? Sub2Colors.DarkTextSecondary : Sub2Colors.S400,
                        TextFormatFlags.HorizontalCenter);
            };
            _cardPanel.Controls.Add(panel);
        }

        // \u2014\u2014 \u7edf\u8ba1\u6570\u636e\u5237\u65b0 \u2014\u2014
        private void RenderStats()
        {
            _lblStat1Value.Text = _items.Count.ToString();
            _lblStat2Value.Text = _lastLaunched;
            _lblStat3Value.Text = "0";
            _lblStat4Value.Text = "0";

            _lblStat1Sub.Text = "\u5171\u8ba1\u9879\u76ee";
            _lblStat2Sub.Text = "\u4e0a\u6b21\u4f7f\u7528";
            _lblStat3Sub.Text = "\u5df2\u52fe\u9009\u9879";
            _lblStat4Sub.Text = "\u53ef\u6279\u91cf\u542f\u52a8";

            // Reposition sub labels after value text changes
            _lblStat2Sub.Left = _lblStat2Value.Right + 6;
            _lblStat3Sub.Left = _lblStat3Value.Right + 6;
            _lblStat4Sub.Left = _lblStat4Value.Right + 6;
        }

        // \u2014\u2014 Toggle select \u2014\u2014
        private void ToggleSelect(int idx)
        {
            if (_selected.Contains(idx)) _selected.Remove(idx); else _selected.Add(idx);
            UpdateBatchUI();
            RenderCards();
        }

        // \u2014\u2014 Batch UI \u2014\u2014
        private void UpdateBatchUI()
        {
            _lblSelCount.Text = string.Format("\u5df2\u9009 {0} \u9879", _selected.Count);
            _btnBatchLaunch.Enabled = _selected.Count > 0;
            _btnBatchLaunch.Invalidate();
            _chkSelectAll.CheckedChanged -= SelectAll_CheckedChanged;
            _chkSelectAll.Checked = _selected.Count == _items.Count && _items.Count > 0;
            _chkSelectAll.CheckedChanged += SelectAll_CheckedChanged;

            _lblStat3Value.Text = _selected.Count.ToString();
            _lblStat4Value.Text = _selected.Count > 0 ? _selected.Count.ToString() : "0";
            _lblStat3Sub.Left = _lblStat3Value.Right + 6;
            _lblStat4Sub.Left = _lblStat4Value.Right + 6;
        }

        // \u2014\u2014 Launch \u2014\u2014
        private static string QuoteArgument(string value)
        {
            if (value == null) value = "";
            var quoted = new StringBuilder();
            quoted.Append('"');

            int backslashes = 0;
            foreach (char ch in value)
            {
                if (ch == '\\')
                {
                    backslashes++;
                    continue;
                }
                if (ch == '"')
                {
                    quoted.Append('\\', backslashes * 2 + 1);
                    quoted.Append('"');
                    backslashes = 0;
                    continue;
                }
                if (backslashes > 0)
                {
                    quoted.Append('\\', backslashes);
                    backslashes = 0;
                }
                quoted.Append(ch);
            }

            if (backslashes > 0) quoted.Append('\\', backslashes * 2);
            quoted.Append('"');
            return quoted.ToString();
        }

        private static bool TryBuildWtArguments(ButtonItem item, out string arguments, out string error)
        {
            arguments = "";
            error = "";
            if (item == null) { error = "\u542f\u52a8\u9879\u4e3a\u7a7a"; return false; }

            var directory = (item.directory ?? "").Trim();
            var command = (item.command ?? "").Trim();
            if (directory.Length == 0) { error = "\u76ee\u5f55\u4e3a\u7a7a"; return false; }
            if (!Directory.Exists(directory)) { error = "\u76ee\u5f55\u4e0d\u5b58\u5728: " + directory; return false; }
            if (command.Length == 0) { error = "\u547d\u4ee4\u4e3a\u7a7a"; return false; }

            arguments = string.Format("-d {0} pwsh -NoExit -Command {1}",
                QuoteArgument(directory), QuoteArgument(command));
            return true;
        }

        private void LaunchItem(int idx)
        {
            if (idx < 0 || idx >= _items.Count) return;
            var item = _items[idx];
            string arguments, error;
            if (!TryBuildWtArguments(item, out arguments, out error))
            {
                SetStatus(string.Format("\u2717 \u914d\u7f6e\u65e0\u6548: {0}", error), false);
                return;
            }
            if (item.confirm)
            {
                if (MessageBox.Show(
                    string.Format("\u542f\u52a8: {0}\n\n\u76ee\u5f55: {1}\n\u547d\u4ee4: {2}\n\n\u662f\u5426\u7ee7\u7eed\uff1f",
                        item.name, item.directory ?? "-", item.command ?? "-"),
                    "\u786e\u8ba4\u542f\u52a8", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                { SetStatus(string.Format("\u5df2\u53d6\u6d88: {0}", item.name), false); return; }
            }
            SetStatus(string.Format("\u542f\u52a8: {0}", item.name), false);
            try
            {
                Process.Start(new ProcessStartInfo("wt.exe")
                {
                    Arguments = arguments,
                    UseShellExecute = false,
                });
                _lastLaunched = item.name;
                _lblStat2Value.Text = item.name;
                _lblStat2Sub.Left = _lblStat2Value.Right + 6;
                SetStatus(string.Format("\u2713 \u5df2\u542f\u52a8: {0}", item.name), true);
            }
            catch (Exception ex)
            {
                SetStatus(string.Format("\u2717 \u5931\u8d25: {0}", ex.Message), false);
            }
        }

        private void LaunchSelected()
        {
            if (_selected.Count == 0) return;
            var keys = new List<int>(_selected);
            var names = new List<string>();
            foreach (var k in keys)
            {
                if (k >= 0 && k < _items.Count && _items[k] != null)
                    names.Add(_items[k].name);
            }
            if (MessageBox.Show(
                string.Format("\u4e00\u952e\u542f\u52a8\u4ee5\u4e0b {0} \u9879\uff1f\n\n{1}",
                    keys.Count, string.Join("\n", names)),
                "\u6279\u91cf\u542f\u52a8", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            int ok = 0, fail = 0;
            foreach (var k in keys)
            {
                if (k < 0 || k >= _items.Count) { fail++; continue; }
                var b = _items[k]; if (b == null) { fail++; continue; }
                try
                {
                    string arguments, error;
                    if (!TryBuildWtArguments(b, out arguments, out error)) { fail++; continue; }
                    Process.Start(new ProcessStartInfo("wt.exe")
                    {
                        Arguments = arguments,
                        UseShellExecute = false,
                    });
                    ok++;
                }
                catch { fail++; }
            }
            _selected.Clear();
            UpdateBatchUI();
            RenderCards();
            SetStatus(fail > 0
                ? string.Format("\u2713 {0} \u6210\u529f, {1} \u5931\u8d25", ok, fail)
                : string.Format("\u2713 \u4e00\u952e\u542f\u52a8: {0} \u6210\u529f", ok),
                fail == 0);
        }

        // \u2014\u2014 Edit dialog \u2014\u2014
        private void ShowEditDialog(int idx)
        {
            var isNew = idx < 0 || idx >= _items.Count;
            var item = isNew
                ? new ButtonItem { command = "codex --enable goals --dangerously-bypass-approvals-and-sandbox" }
                : _items[idx];

            using (var form = new Form
            {
                Text = isNew ? "\u65b0\u5efa\u542f\u52a8\u9879" : "\u7f16\u8f91\u542f\u52a8\u9879",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MinimizeBox = false,
                MaximizeBox = false,
                ShowInTaskbar = false,
                ClientSize = new Size(620, 460),
                BackColor = _darkMode ? Sub2Colors.DarkCard : Color.White,
                Font = new Font("Microsoft YaHei UI", 12F),
            })
            {
                form.Paint += (s, e) =>
                {
                    var g = e.Graphics;
                    var r = new Rectangle(0, 0, form.Width - 1, form.Height - 1);
                    using (var b = new SolidBrush(form.BackColor))
                        g.FillRoundedRect(b, r, 16);
                    var borderColor = _darkMode ? Sub2Colors.DarkBorder : Sub2Colors.S200;
                    using (var p = new Pen(borderColor))
                        g.DrawRoundedRect(p, r, 16);
                };

                // Header
                var hdr = new Label
                {
                    Text = form.Text,
                    Font = new Font("Microsoft YaHei UI", 17F, FontStyle.Bold),
                    ForeColor = _darkMode ? Sub2Colors.DarkTextPrimary : Sub2Colors.S700,
                    Location = new Point(24, 18),
                    AutoSize = true,
                };
                var hdrLine = new Label
                {
                    Height = 1,
                    Width = form.ClientSize.Width - 48,
                    Location = new Point(24, 58),
                    BackColor = _darkMode ? Sub2Colors.DarkBorder : Color.FromArgb(0xEE, 0xEE, 0xEE),
                };

                // Body
                var lblName = new Label { Text = "\u540d\u79f0", Location = new Point(24, 78), AutoSize = true,
                    ForeColor = _darkMode ? Sub2Colors.DarkTextSecondary : Sub2Colors.S400 };
                var txtName = new TextBox
                {
                    Text = item.name, Location = new Point(24, 104), Width = 572, Height = 34,
                    Font = new Font("Microsoft YaHei UI", 13F), BorderStyle = BorderStyle.FixedSingle,
                };

                var lblDir = new Label { Text = "\u76ee\u5f55", Location = new Point(24, 150), AutoSize = true,
                    ForeColor = _darkMode ? Sub2Colors.DarkTextSecondary : Sub2Colors.S400 };
                var txtDir = new TextBox
                {
                    Text = item.directory, Location = new Point(24, 176), Width = 522, Height = 34,
                    Font = new Font("Microsoft YaHei UI", 13F), BorderStyle = BorderStyle.FixedSingle,
                };
                var btnBrowse = new Button
                {
                    Text = "\u2026", Location = new Point(552, 176), Size = new Size(44, 34),
                    FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
                    Font = new Font("Microsoft YaHei UI", 12F),
                    ForeColor = _darkMode ? Sub2Colors.DarkTextSecondary : Sub2Colors.S400,
                };
                btnBrowse.FlatAppearance.BorderColor = Sub2Colors.S200;
                btnBrowse.Click += (s, e) =>
                {
                    using (var dlg = new FolderBrowserDialog { Description = "\u9009\u62e9\u5de5\u4f5c\u76ee\u5f55", SelectedPath = txtDir.Text })
                        if (dlg.ShowDialog() == DialogResult.OK) txtDir.Text = dlg.SelectedPath;
                };

                var lblCmd = new Label { Text = "\u547d\u4ee4", Location = new Point(24, 222), AutoSize = true,
                    ForeColor = _darkMode ? Sub2Colors.DarkTextSecondary : Sub2Colors.S400 };
                var txtCmd = new TextBox
                {
                    Text = item.command, Location = new Point(24, 248), Width = 572, Height = 88,
                    Font = new Font("Cascadia Code", 12F), BorderStyle = BorderStyle.FixedSingle,
                    Multiline = true, ScrollBars = ScrollBars.Vertical,
                };

                var chkConfirm = new CheckBox
                {
                    Text = "\u542f\u52a8\u524d\u786e\u8ba4", Checked = item.confirm,
                    Location = new Point(24, 348), AutoSize = true,
                    Font = new Font("Microsoft YaHei UI", 12F),
                    ForeColor = _darkMode ? Sub2Colors.DarkTextSecondary : Sub2Colors.S400,
                };

                // Footer
                var footerLine = new Label
                {
                    Height = 1, Width = form.ClientSize.Width - 48,
                    Location = new Point(24, 386),
                    BackColor = _darkMode ? Sub2Colors.DarkBorder : Color.FromArgb(0xEE, 0xEE, 0xEE),
                };

                var btnOk = new Button
                {
                    DialogResult = DialogResult.OK,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.Transparent,
                    ForeColor = Color.White,
                    Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    Size = new Size(104, 40),
                    Location = new Point(384, 402),
                    Text = "\u4fdd\u5b58",
                };
                btnOk.FlatAppearance.BorderSize = 0;
                btnOk.Paint += (s, e) =>
                {
                    var b = (Button)s;
                    var r = new Rectangle(0, 0, b.Width - 1, b.Height - 1);
                    using (var grad = Sub2Colors.GetPrimaryGradient(r))
                        e.Graphics.FillRoundedRect(grad, r, 10);
                    using (var font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold))
                        TextRenderer.DrawText(e.Graphics, b.Text, font, r, Color.White,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                };

                var btnCancel = new Button
                {
                    DialogResult = DialogResult.Cancel,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = _darkMode ? Sub2Colors.S700 : Sub2Colors.S100,
                    ForeColor = _darkMode ? Sub2Colors.DarkTextSecondary : Sub2Colors.S400,
                    Font = new Font("Microsoft YaHei UI", 12F),
                    Cursor = Cursors.Hand,
                    Size = new Size(90, 40),
                    Location = new Point(506, 402),
                    Text = "\u53d6\u6d88",
                };
                btnCancel.FlatAppearance.BorderColor = Sub2Colors.S200;

                var btnDelete = new Button
                {
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.Transparent,
                    ForeColor = _darkMode ? Sub2Colors.DarkTextSecondary : Sub2Colors.S400,
                    Font = new Font("Microsoft YaHei UI", 12F),
                    Cursor = Cursors.Hand,
                    Size = new Size(82, 40),
                    Location = new Point(24, 402),
                    Text = "\u5220\u9664",
                    Visible = !isNew,
                };
                btnDelete.FlatAppearance.BorderColor = Sub2Colors.S200;
                btnDelete.Click += (s, e) =>
                {
                    if (DeleteItem(idx)) form.Close();
                };

                // Dark mode coloring
                if (_darkMode)
                {
                    txtName.BackColor = Sub2Colors.S800; txtName.ForeColor = Sub2Colors.S200;
                    txtDir.BackColor = Sub2Colors.S800; txtDir.ForeColor = Sub2Colors.S200;
                    txtCmd.BackColor = Sub2Colors.S800; txtCmd.ForeColor = Sub2Colors.S200;
                    btnBrowse.BackColor = Sub2Colors.S700;
                    btnCancel.BackColor = Sub2Colors.S700;
                    btnDelete.ForeColor = Sub2Colors.S500;
                }

                form.Controls.AddRange(new Control[] {
                    hdr, hdrLine,
                    lblName, txtName, lblDir, txtDir, btnBrowse,
                    lblCmd, txtCmd, chkConfirm,
                    footerLine, btnOk, btnCancel, btnDelete
                });
                form.AcceptButton = btnOk;
                form.CancelButton = btnCancel;

                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    item.name = txtName.Text.Trim();
                    item.directory = txtDir.Text.Trim();
                    item.command = txtCmd.Text.Trim();
                    item.confirm = chkConfirm.Checked;
                    if (string.IsNullOrEmpty(item.name)) { MessageBox.Show("\u8bf7\u8f93\u5165\u540d\u79f0"); return; }
                    if (isNew) _items.Add(item); else _items[idx] = item;
                    SaveItems();
                    RenderStats();
                    RenderCards();
                    SetStatus(string.Format("\u5df2\u4fdd\u5b58: {0}", item.name), true);
                }
            }
        }

        // \u2014\u2014 Delete \u2014\u2014
        private bool DeleteItem(int idx)
        {
            if (idx < 0 || idx >= _items.Count) return false;
            if (MessageBox.Show(
                string.Format("\u5220\u9664\u300c{0}\u300d\uff1f", _items[idx].name),
                "\u786e\u8ba4\u5220\u9664", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return false;
            _items.RemoveAt(idx);
            _selected.Clear();
            SaveItems();
            RenderStats();
            RenderCards();
            SetStatus("\u5df2\u5220\u9664", false);
            return true;
        }

        // \u2014\u2014 Config I/O \u2014\u2014
        private void LoadItems()
        {
            if (!File.Exists(_configPath)) return;
            try
            {
                var json = File.ReadAllText(_configPath, System.Text.Encoding.UTF8);
                var data = new JavaScriptSerializer().Deserialize<List<ButtonItem>>(json);
                if (data != null) _items = data;
            }
            catch { SetStatus("\u914d\u7f6e\u8bfb\u53d6\u5931\u8d25", false); }

            _lblStat1Value.Text = _items.Count.ToString();
            _lblStat2Value.Text = _lastLaunched;
            _lblStat2Sub.Left = _lblStat2Value.Right + 6;
        }

        private void SaveItems()
        {
            try
            {
                var bak = _configPath + ".bak";
                if (File.Exists(_configPath))
                {
                    if (File.Exists(bak)) File.Delete(bak);
                    File.Copy(_configPath, bak, true);
                }
                var json = new JavaScriptSerializer().Serialize(_items);
                File.WriteAllText(_configPath, json, System.Text.Encoding.UTF8);
            }
            catch (Exception ex) { SetStatus(string.Format("\u4fdd\u5b58\u5931\u8d25: {0}", ex.Message), false); }
        }

        // \u2014\u2014 Dark mode \u2014\u2014
        private bool LoadThemePref()
        {
            var prefPath = Path.Combine(Application.StartupPath, ".theme");
            try
            {
                if (File.Exists(prefPath))
                {
                    var val = File.ReadAllText(prefPath, System.Text.Encoding.UTF8).Trim();
                    return val == "dark";
                }
            }
            catch { }
            return false;
        }

        private void SaveThemePref(bool dark)
        {
            var prefPath = Path.Combine(Application.StartupPath, ".theme");
            try { File.WriteAllText(prefPath, dark ? "dark" : "light", System.Text.Encoding.UTF8); }
            catch { }
        }

        private void ToggleDarkMode()
        {
            _darkMode = !_darkMode;
            SaveThemePref(_darkMode);
            _btnThemeToggle.Text = _darkMode ? "\u2600" : "\u263e";
            _lblThemeLabel.Text = _darkMode ? "\u6d45\u8272\u6a21\u5f0f" : "\u6df1\u8272\u6a21\u5f0f";
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            BackColor = _darkMode ? Sub2Colors.DarkBg : Sub2Colors.S50;
            ForeColor = _darkMode ? Sub2Colors.DarkTextPrimary : Sub2Colors.S700;
            _sidebar.BackColor = _darkMode ? Sub2Colors.S900 : Color.FromArgb(0x0F, 0x17, 0x2A);
            _sidebar.Invalidate();

            _chkSelectAll.ForeColor = _darkMode ? Sub2Colors.DarkTextSecondary : Sub2Colors.S400;
            _lblSelCount.ForeColor = _darkMode ? Sub2Colors.DarkTextSecondary : Sub2Colors.S400;
            _lblPageTitle.ForeColor = _darkMode ? Sub2Colors.DarkTextPrimary : Sub2Colors.S700;
            _lblPageDesc.ForeColor = _darkMode ? Sub2Colors.DarkTextSecondary : Sub2Colors.S400;
            _lblStatus.ForeColor = _darkMode ? Sub2Colors.DarkTextSecondary : Sub2Colors.S500;

            foreach (Control c in _cardPanel.Controls) c.Invalidate();
            _header.Invalidate();
            _statsRow.Invalidate();
            _batchBar.Invalidate();
            _statusBar.Invalidate();
            Invalidate();
        }

        // \u2014\u2014 Status \u2014\u2014
        private void SetStatus(string msg, bool success)
        {
            _lblStatus.Text = msg;
            _lblStatus.ForeColor = success
                ? (_darkMode ? Sub2Colors.P400 : Sub2Colors.Success)
                : (_darkMode ? Sub2Colors.DarkTextSecondary : Sub2Colors.S500);
            _statusBar.Invalidate();
        }
    }

    // =========================================================================
    // \u5165\u53e3
    // =========================================================================
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
}
