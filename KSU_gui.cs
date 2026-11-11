// KSU_gui.cs — KernelSU boot 修补工具（KSU 中文版的图形前端）
// 编译：csc /target:winexe /codepage:65001 /win32manifest:KSU_gui.manifest
//        /out:KSU-boot-gui.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll KSU_gui.cs

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace KsudGui
{
    static class T
    {
        public static readonly Color Bg = Color.FromArgb(0xF4, 0xF5, 0xF7);
        public static readonly Color Card = Color.White;
        public static readonly Color Border = Color.FromArgb(0xE3, 0xE6, 0xEB);
        public static readonly Color Text = Color.FromArgb(0x16, 0x18, 0x1D);
        public static readonly Color Muted = Color.FromArgb(0x6B, 0x72, 0x80);
        public static readonly Color Faint = Color.FromArgb(0x9C, 0xA3, 0xAF);
        public static readonly Color Accent = Color.FromArgb(0x25, 0x63, 0xEB);
        public static readonly Color AccentHover = Color.FromArgb(0x1D, 0x4E, 0xD8);
        public static readonly Color AccentSoft = Color.FromArgb(0xEA, 0xF0, 0xFE);
        public static readonly Color Danger = Color.FromArgb(0xDC, 0x26, 0x26);
        public static readonly Color Ok = Color.FromArgb(0x16, 0xA3, 0x4A);
        public static readonly Color Warn = Color.FromArgb(0xB4, 0x53, 0x09);
        public static readonly Color LogBg = Color.FromArgb(0x0F, 0x17, 0x2A);
        public static readonly Color LogFg = Color.FromArgb(0xE5, 0xE7, 0xEB);
        public static readonly Color LogDim = Color.FromArgb(0x8F, 0xA0, 0xB8);
        public static readonly Color LogStep = Color.FromArgb(0x93, 0xC5, 0xFD);
        public static readonly Color LogOk = Color.FromArgb(0x4A, 0xDE, 0x80);
        public static readonly Color LogWarn = Color.FromArgb(0xFB, 0xBF, 0x24);
        public static readonly Color LogErr = Color.FromArgb(0xF8, 0x71, 0x71);

        public static readonly string UiFont = "Microsoft YaHei UI";
        public static readonly string Mono = "Consolas";

        public static Font F(float size, FontStyle style)
        {
            return new Font(UiFont, size, style, GraphicsUnit.Point);
        }
        public static Font MonoF(float size)
        {
            return new Font(Mono, size, FontStyle.Regular, GraphicsUnit.Point);
        }
    }

    static class Gfx
    {
        public static GraphicsPath Round(Rectangle r, int radius)
        {
            var p = new GraphicsPath();
            if (radius <= 0 || r.Width <= 0 || r.Height <= 0) { p.AddRectangle(r); return p; }
            int d = radius * 2;
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }

    // ---------------------------------------------------------------- 扁平按钮
    class FlatButton : Control
    {
        public Color NormalBack = T.Card, HoverBack = T.AccentSoft, TextColor = T.Text;
        public bool Primary;
        public bool DrawBorder = true;
        bool hover, down;

        public FlatButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
            Font = T.F(9f, FontStyle.Regular);
            TabStop = true;
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; down = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { down = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { down = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            Color back;
            if (!Enabled) back = Primary ? Color.FromArgb(0xC7, 0xD3, 0xF7) : Color.FromArgb(0xF2, 0xF3, 0xF5);
            else if (Primary) back = (hover || down) ? T.AccentHover : T.Accent;
            else back = (hover || down) ? T.AccentSoft : NormalBack;

            using (var path = Gfx.Round(rect, 6))
            {
                using (var b = new SolidBrush(back)) g.FillPath(b, path);
                if (DrawBorder && !Primary)
                    using (var pen = new Pen(T.Border)) g.DrawPath(pen, path);
            }
            Color fg = Enabled ? (Primary ? Color.White : TextColor) : Color.FromArgb(0x9A, 0xA1, 0xAD);
            TextRenderer.DrawText(g, Text, Font, new Rectangle(0, 0, Width, Height), fg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis);
        }
    }

    // ---------------------------------------------------------------- 必填小标签
    class Chip : Control
    {
        Color bg, fg, line;
        public Chip(string text, float ak, bool muted)
        {
            Text = text;
            Font = T.F(7.5f, FontStyle.Regular);
            bg = muted ? T.Bg : T.AccentSoft;
            fg = muted ? T.Muted : T.Accent;
            line = muted ? T.Border : Blend(T.AccentSoft, T.Accent, 0.35f);
            ForeColor = fg;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = T.Card;
            int tw = TextRenderer.MeasureText(text, Font, new Size(int.MaxValue, int.MaxValue),
                                              TextFormatFlags.NoPadding).Width;
            int th = TextRenderer.MeasureText(text, Font).Height;
            // 尺寸按“布局单位”给，之后由统一的 DPI 流程放大，避免 1.5 倍系统上被放大两次
            int w = (int)Math.Ceiling(tw / ak) + 14;
            int h = (int)Math.Ceiling(th / ak) + 3;
            Size = new Size(w, h);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(T.Card);
            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = Gfx.Round(r, 4))
            {
                using (var b = new SolidBrush(bg)) g.FillPath(b, path);
                using (var pen = new Pen(line)) g.DrawPath(pen, path);
            }
            TextRenderer.DrawText(g, Text, Font, new Rectangle(0, 0, Width, Height), fg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
        static Color Blend(Color a, Color b, float t)
        {
            return Color.FromArgb(
                (int)(a.A + (b.A - a.A) * t), (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));
        }
    }

    // ---------------------------------------------------------------- 卡片
    class Card : Panel
    {
        public Card()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = T.Card;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(T.Bg);
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = Gfx.Round(rect, 8))
            {
                using (var b = new SolidBrush(T.Card)) g.FillPath(b, path);
                using (var pen = new Pen(T.Border)) g.DrawPath(pen, path);
            }
        }
    }

    // ---------------------------------------------------------------- 模式切换
    class TabsControl : Control
    {
        public int SelectedIndex = 0;
        public int HoverIndex = -1;
        public string[] Items = new string[0];
        public event EventHandler SelectionChanged;
        Rectangle[] rects = new Rectangle[0];

        public TabsControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Font = T.F(9.5f, FontStyle.Regular);
            Height = 36;
        }

        void LayoutItems()
        {
            rects = new Rectangle[Items.Length];
            int x = 0;
            for (int i = 0; i < Items.Length; i++)
            {
                int w = TextRenderer.MeasureText(Items[i], Font).Width + 34;
                rects[i] = new Rectangle(x, 0, w, Height - 1);
                x += w + 8;
            }
        }

        protected override void OnResize(EventArgs e) { LayoutItems(); base.OnResize(e); }
        protected override void OnFontChanged(EventArgs e) { LayoutItems(); base.OnFontChanged(e); }
        public void SetItems(string[] items, int selected)
        {
            Items = items; SelectedIndex = selected; LayoutItems(); Invalidate();
        }

        int HitTest(Point p)
        {
            for (int i = 0; i < rects.Length; i++) if (rects[i].Contains(p)) return i;
            return -1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int h = HitTest(e.Location);
            if (h != HoverIndex) { HoverIndex = h; Invalidate(); }
            Cursor = h >= 0 ? Cursors.Hand : Cursors.Default;
            base.OnMouseMove(e);
        }
        protected override void OnMouseLeave(EventArgs e) { HoverIndex = -1; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            int h = HitTest(e.Location);
            if (h >= 0 && h != SelectedIndex)
            {
                SelectedIndex = h; Invalidate();
                if (SelectionChanged != null) SelectionChanged(this, EventArgs.Empty);
            }
            base.OnMouseDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(T.Bg);
            for (int i = 0; i < rects.Length; i++)
            {
                bool sel = i == SelectedIndex;
                Color back = sel ? T.Accent : (i == HoverIndex ? T.AccentSoft : T.Card);
                Color fg = sel ? Color.White : T.Text;
                using (var path = Gfx.Round(rects[i], 6))
                {
                    using (var b = new SolidBrush(back)) g.FillPath(b, path);
                    if (!sel) using (var pen = new Pen(T.Border)) g.DrawPath(pen, path);
                }
                TextRenderer.DrawText(g, Items[i], sel ? T.F(9.5f, FontStyle.Bold) : Font,
                    rects[i], fg, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }
    }

    // ---------------------------------------------------------------- 输入框（带边框容器）
    class InputBox : Panel
    {
        public TextBox Box;
        public Label Browse;
        public event EventHandler Picked;
        bool focused;

        public InputBox(bool withBrowse)
        {
            BackColor = T.Card;
            Height = 32;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Box = new TextBox();
            Box.BorderStyle = BorderStyle.None;
            Box.Font = T.F(9f, FontStyle.Regular);
            Box.ForeColor = T.Text;
            Box.BackColor = T.Card;
            Box.Location = new Point(10, 8);
            Box.Width = 200;
            Controls.Add(Box);
            Box.GotFocus += delegate { focused = true; Invalidate(); };
            Box.LostFocus += delegate { focused = false; Invalidate(); };
            if (withBrowse)
            {
                Browse = new Label();
                Browse.Text = "浏览…";
                Browse.AutoSize = false;                  // 撑满整条右侧，点击热区大
                Browse.TextAlign = ContentAlignment.MiddleCenter;
                Browse.Font = T.F(9f, FontStyle.Regular);
                Browse.ForeColor = T.Accent;
                Browse.BackColor = T.Card;
                Browse.Cursor = Cursors.Hand;
                Browse.Click += delegate { Pick(); };
                Controls.Add(Browse);
            }
        }

        void Pick()
        {
            if (Picked != null) Picked(this, EventArgs.Empty);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (Browse != null) Pick();                    // 点输入框空白处也弹出选择
        }

        public void Relayout()
        {
            OnResize(EventArgs.Empty);        // 缩放/改宽后重新摆放文本框与“浏览…”
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            if (Box != null) OnResize(EventArgs.Empty);   // 缩放后字号变化，重新排布“浏览…”
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (Box == null) return;
            if (Browse != null)
            {
                // 用实测宽度定位：AutoSize 的 Width 在字体/缩放变化后可能不同步，会把“浏览…”挤出可视区
                int bw = TextRenderer.MeasureText(Browse.Text, Browse.Font, new Size(int.MaxValue, int.MaxValue),
                                                  TextFormatFlags.NoPadding).Width + 26;
                int bh = TextRenderer.MeasureText(Browse.Text, Browse.Font).Height + 2;
                int by = Math.Max(2, (Height - bh) / 2);          // 垂直居中，避免被输入框边框压住
                Browse.SetBounds(Width - bw - 8, by, bw, bh);
                Box.Width = Math.Max(40, Width - bw - 32);
            }
            else Box.Width = Width - 20;
            Box.Location = new Point(10, (Height - Box.PreferredHeight) / 2);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(T.Card);
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = Gfx.Round(rect, 5))
            {
                using (var pen = new Pen(focused ? T.Accent : T.Border)) g.DrawPath(pen, path);
            }
        }
    }

    class LogKind { public const int Info = 0, Dim = 1, Step = 2, Ok = 3, Warn = 4, Err = 5; }

    // ---------------------------------------------------------------- 主窗口
    class MainForm : Form
    {
        public string Mode = "v1";
        public string KsudPath = "";
        Dictionary<string, Control> fields = new Dictionary<string, Control>();
        Dictionary<string, bool> checks = new Dictionary<string, bool>();
        Dictionary<string, string> texts = new Dictionary<string, string>();
        List<string> kmis = new List<string>();

        Panel leftScroll, leftInner, cardHolder;
        RichTextBox rtb;
        TabsControl tabs;
        FlatButton btnStart, btnStop;
        ProgressBar progress;
        Label statusLabel, dotLabel, KSULabel;
        float K = 1f;                       // DPI 缩放系数，供 Resize 里计算边距
        Queue<object[]> logQueue = new Queue<object[]>();
        System.Windows.Forms.Timer pump, spinner;
        Process proc;
        Thread waiter;
        DateTime startedAt;
        string lastOutDir;
        int spinPhase;
        bool running;
        string cfgPath;

        public MainForm()
        {
            Text = "KernelSU boot 修补工具";
            AutoScaleMode = AutoScaleMode.None;
            BackColor = T.Bg;
            Font = T.F(9f, FontStyle.Regular);
            ClientSize = new Size(980, 664);
            FormBorderStyle = FormBorderStyle.FixedSingle;   // 固定尺寸，不允许拖动边缘改变大小
            MaximizeBox = false;
            MinimizeBox = true;
            MinimumSize = new Size(0, 0);
            MaximumSize = new Size(0, 0);
            StartPosition = FormStartPosition.CenterScreen;
            DoubleBuffered = true;
            AllowDrop = true;
            DragEnter += OnDragEnterF;
            DragDrop += OnDragDropF;

            cfgPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                   "KernelSU-GUI", "settings.ini");

            KsudPath = ResolveKsud();
            kmis = QueryKmis();

            BuildUi();
            RebuildForm();
            LoadSettings();
            RefreshKsudLabel();      // 启动时把 KSU 路径显示出来
            SyncTabs();              // 配置里恢复的模式要和页签高亮对上

            ApplyDpiScale();
            Log("欢迎使用 KernelSU boot 修补工具。", LogKind.Dim);
            Log("调用的 KSU：" + (KsudPath == "" ? "未找到，请点右上角“更换”" : KsudPath), LogKind.Dim);
            if (kmis.Count > 0) Log("内置 " + kmis.Count + " 个 KMI 模块，可在“KMI 版本”里手动指定。", LogKind.Dim);
            AutoFillFromSaved();     // 启动时把输出位置补到程序目录\输出
            Log("", LogKind.Dim);
        }

        void ApplyDpiScale()
        {
            float k = Dpi.OfWindow(Handle) / 96.0f;
            if (k < 0.9f || k > 3.0f) k = 1.0f;
            if (Math.Abs(k - 1.0f) < 0.01f) return;
            K = k;
            SuspendLayout();
            // Form.Scale() 会把尺寸连带应用两次，所以传平方根，实际得到 k 倍
            float inner = (float)Math.Sqrt(k);
            Scale(new SizeF(inner, inner));
            MinimumSize = new Size((int)(900 * k), (int)(600 * k));
            ResumeLayout(true);
            ClientSize = new Size((int)(980 * k), (int)(664 * k));
            // 左列面板与卡片等宽，避免卡片右边缘被纵向滚动条压住
            leftInner.Width = (int)(438 * k);
            foreach (Control c in leftInner.Controls)
            {
                if (!(c is Card)) continue;
                c.Width = leftInner.Width;
                foreach (Control cc in c.Controls)
                    if (cc is InputBox)
                    {
                        cc.Width = c.Width - (int)(32 * k);
                        cc.Left = (int)(16 * k);
                        ((InputBox)cc).Relayout();
                    }
            }
        }
        // ------------------------------------------------------------ 配置
        string ResolveKsud()
        {
            // 先找改名后的中文版，再兼容旧的 ksud 命名与上游官方文件名
            string[] names = { "KSU-x86_64-pc-windows-gnu-zh.exe", "ksud-x86_64-pc-windows-gnu-zh.exe",
                               "ksud.exe", "ksud-x86_64-pc-windows-gnu.exe" };
            List<string> bases = new List<string>();
            bases.Add(Path.GetDirectoryName(Application.ExecutablePath));
            bases.Add(Directory.GetCurrentDirectory());
            bases.Add(@"D:\ksu");
            foreach (string b in bases)
            {
                if (string.IsNullOrEmpty(b) || !Directory.Exists(b)) continue;
                foreach (string n in names)
                {
                    string p = Path.Combine(b, n);
                    if (File.Exists(p)) return p;
                }
                try
                {
                    string[] found = Directory.GetFiles(b, "KSU*.exe");
                    if (found.Length == 0) found = Directory.GetFiles(b, "ksud*.exe");   // 兼容改名前的文件
                    if (found.Length > 0) return found[0];
                }
                catch { }
            }
            return "";
        }

        List<string> QueryKmis()
        {
            var list = new List<string>();
            if (KsudPath == "" || !File.Exists(KsudPath)) return list;
            try
            {
                var psi = new ProcessStartInfo(KsudPath, "supported-kmis");
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.RedirectStandardOutput = true;
                psi.StandardOutputEncoding = Encoding.UTF8;
                using (var p = Process.Start(psi))
                {
                    string outp = p.StandardOutput.ReadToEnd();
                    p.WaitForExit(15000);
                    foreach (string line in outp.Split('\n'))
                    {
                        string s = line.Trim();
                        if (s.Length > 0) list.Add(s);
                    }
                }
            }
            catch { }
            return list;
        }

        void LoadSettings()
        {
            try
            {
                if (!File.Exists(cfgPath)) return;
                foreach (string line in File.ReadAllLines(cfgPath, Encoding.UTF8))
                {
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    string k = line.Substring(0, eq), v = line.Substring(eq + 1);
                    if (k == "KSU" && File.Exists(v))
                    {
                        KsudPath = v;
                        kmis = QueryKmis();
                        RefreshKsudLabel();
                        RebuildForm();
                    }
                    else if (k == "mode" && (v == "v1" || v == "v2" || v == "restore") && v != Mode)
                    {
                        Mode = v; RebuildForm();
                    }
                    else if (k.StartsWith("t."))
                    {
                        string key = k.Substring(2);
                        texts[key] = v;
                        if (fields.ContainsKey(key) && fields[key] is InputBox) ((InputBox)fields[key]).Box.Text = v;
                    }
                    else if (k.StartsWith("c."))
                    {
                        string key = k.Substring(2);
                        checks[key] = (v == "1");
                        if (fields.ContainsKey(key) && fields[key] is CheckBox) ((CheckBox)fields[key]).Checked = checks[key];
                    }
                    else if (k.StartsWith("a."))          // 旧记录：这个值是程序自动填的
                    {
                        string key = k.Substring(2);
                        if (v == "1" && texts.ContainsKey(key)) autoVals[key] = texts[key];
                    }
                }

                // 老版本把「输出」写在 boot 镜像目录下，这类历史自动值先标记成自动，好让新规则覆盖掉
                foreach (string key in new[] { "out", "out_name", "output" })
                {
                    string v = texts.ContainsKey(key) ? texts[key].Trim() : "";
                    if (v == "") continue;
                    bool isAuto = key == "out"
                        ? (v.Contains("\\输出") || v.Contains("/输出"))
                        : (v.EndsWith("-ksu.img") || v.EndsWith("-restore.img"));
                    if (isAuto) autoVals[key] = v;
                }
            }
            catch { }
        }

        void SaveSettings()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(cfgPath));
                var sb = new StringBuilder();
                sb.AppendLine("KSU=" + KsudPath);
                sb.AppendLine("mode=" + Mode);
                foreach (var kv in texts) sb.AppendLine("t." + kv.Key + "=" + kv.Value);
                foreach (var kv in checks) sb.AppendLine("c." + kv.Key + "=" + (kv.Value ? "1" : "0"));
                // 只给“值仍是程序写进去的那个”打自动标记；用户手改过就不再覆盖
                foreach (var kv in autoVals)
                    if (texts.ContainsKey(kv.Key) && texts[kv.Key] == kv.Value)
                        sb.AppendLine("a." + kv.Key + "=1");
                File.WriteAllText(cfgPath, sb.ToString(), new UTF8Encoding(false));
            }
            catch { }
        }

        // ------------------------------------------------------------ 界面
        void BuildUi()
        {
            // 主体必须最先加入，Dock=Fill 才能吃掉剩余空间
            var body = new Panel { Dock = DockStyle.Fill, BackColor = T.Bg };
            body.Padding = new Padding(24, 0, 24, 20);
            Controls.Add(body);

            // 顶部
            var header = new Panel { Dock = DockStyle.Top, Height = 92, BackColor = T.Bg };
            var title = new Label
            {
                Text = "KernelSU boot 修补工具",
                Font = T.F(17f, FontStyle.Bold),
                ForeColor = T.Text,
                AutoSize = true,
                Location = new Point(24, 18),
                BackColor = T.Bg
            };
            var sub = new Label
            {
                Text = "KSU 3.3.1 · 中文本地化图形前端 · 内核模块与 ksuinit 均为官方内置资产",
                Font = T.F(8.5f, FontStyle.Regular),
                ForeColor = T.Muted,
                AutoSize = true,
                Location = new Point(25, 52),
                BackColor = T.Bg
            };
            header.Controls.Add(title);
            header.Controls.Add(sub);

            var KSUCap = new Label
            {
                Text = "KSU 位置",
                Font = T.F(8f, FontStyle.Regular),
                ForeColor = T.Faint,
                AutoSize = true,
                BackColor = T.Bg,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            KSULabel = new Label
            {
                Text = "",
                Font = new Font(T.Mono, 7.5f, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = T.Muted,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = T.Bg,
                AutoEllipsis = true
            };
            var btnKsud = new FlatButton
            {
                Text = "更换",
                Font = T.F(8.5f, FontStyle.Regular),
                Size = new Size(64, 26)
            };
            btnKsud.Click += delegate { ChooseKsud(); };
            header.Controls.Add(KSUCap);
            header.Controls.Add(KSULabel);
            header.Controls.Add(btnKsud);
            header.Resize += delegate
            {
                btnKsud.Location = new Point(header.Width - (int)(24 * K) - btnKsud.Width, (int)(20 * K));
                KSULabel.Width = (int)(320 * K);
                KSULabel.Location = new Point(btnKsud.Left - (int)(10 * K) - KSULabel.Width, (int)(24 * K));
                KSULabel.Height = (int)(18 * K);
                KSUCap.Location = new Point(KSULabel.Left + KSULabel.Width - KSUCap.Width, (int)(5 * K));
            };
            Controls.Add(header);

            // 模式切换
            var tabHost = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = T.Bg };
            tabs = new TabsControl { Location = new Point(24, 6), Size = new Size(600, 36) };
            tabs.SetItems(TabTitles, 0);
            tabs.SelectionChanged += delegate
            {
                if (running)
                {
                    Log("正在运行，先停止当前任务再切换模式。", LogKind.Warn);
                    SyncTabs();                       // 高亮弹回当前真正生效的页签，避免“亮的页”和“显示的页”不一致
                    return;
                }
                Mode = TabModes[tabs.SelectedIndex];
                RebuildForm();
                AutoFillCurrent();          // 换页后输出文件名要按当前页规则重算（-ksu / -restore）
                SaveSettings();
            };
            tabHost.Controls.Add(tabs);
            Controls.Add(tabHost);

            // 底部状态栏
            var footer = new Panel { Dock = DockStyle.Bottom, Height = 38, BackColor = T.Bg };
            dotLabel = new Label
            {
                Text = "●",
                Font = T.F(8f, FontStyle.Regular),
                ForeColor = T.Muted,
                AutoSize = true,
                Location = new Point(24, 9),
                BackColor = T.Bg
            };
            statusLabel = new Label
            {
                Text = "就绪",
                Font = T.F(8.5f, FontStyle.Regular),
                ForeColor = T.Muted,
                AutoSize = true,
                Location = new Point(42, 9),
                BackColor = T.Bg
            };
            var hint = new Label
            {
                Text = "Ctrl+Enter 开始 · Esc 停止 · 可直接把 boot 镜像拖进窗口",
                Font = T.F(8f, FontStyle.Regular),
                ForeColor = T.Faint,
                AutoSize = true,
                BackColor = T.Bg,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            footer.Controls.Add(dotLabel);
            footer.Controls.Add(statusLabel);
            footer.Controls.Add(hint);
            footer.Resize += delegate { hint.Location = new Point(footer.Width - (int)(24 * K) - hint.Width, (int)(9 * K)); };
            Controls.Add(footer);

            // 主体内部（body 已在方法开头加入）
            // 右列：日志
            var rightCol = new Panel { Dock = DockStyle.Fill, BackColor = T.Bg };
            body.Controls.Add(rightCol);

            var logHead = new Panel { Dock = DockStyle.Top, Height = 34, BackColor = T.Bg };
            var logTitle = new Label
            {
                Text = "运行日志",
                Font = T.F(10.5f, FontStyle.Bold),
                ForeColor = T.Text,
                AutoSize = true,
                Location = new Point(0, 4),
                BackColor = T.Bg
            };
            logHead.Controls.Add(logTitle);
            var lkOpen = LinkLabelOn(logHead, "打开输出目录", delegate { OpenOutDir(); });
            var lkCopy = LinkLabelOn(logHead, "复制日志", delegate { CopyLog(); });
            var lkClear = LinkLabelOn(logHead, "清空", delegate { ClearLog(); });
            logHead.Resize += delegate
            {
                int x = logHead.Width - (int)(20 * K);
                foreach (var l in new[] { lkClear, lkCopy, lkOpen })
                {
                    x -= l.Width;
                    l.Location = new Point(x, (int)(7 * K));
                    x -= (int)(16 * K);
                }
            };
            rightCol.Controls.Add(logHead);

            var action = new Panel { Dock = DockStyle.Bottom, Height = 86, BackColor = T.Bg };
            progress = new ProgressBar { Style = ProgressBarStyle.Marquee, MarqueeAnimationSpeed = 26, Height = 3, Visible = false };
            btnStart = new FlatButton
            {
                Text = "▶  开始修补",
                Primary = true,
                Font = T.F(10f, FontStyle.Bold),
                Size = new Size(140, 40)
            };
            btnStart.Click += delegate { StartRun(); };
            btnStop = new FlatButton
            {
                Text = "停止",
                Font = T.F(9.5f, FontStyle.Regular),
                Size = new Size(84, 40),
                Enabled = false
            };
            btnStop.Click += delegate { StopRun(); };
            action.Controls.Add(progress);
            action.Controls.Add(btnStart);
            action.Controls.Add(btnStop);
            action.Resize += delegate
            {
                progress.Bounds = new Rectangle(0, (int)(4 * K), action.Width, (int)(3 * K));
                btnStart.Location = new Point(action.Width - (int)(20 * K) - btnStart.Width, (int)(22 * K));
                btnStop.Location = new Point(btnStart.Left - (int)(12 * K) - btnStop.Width, (int)(22 * K));
            };
            rightCol.Controls.Add(action);

            var logCard = new Panel { Dock = DockStyle.Fill, BackColor = T.LogBg };
            logCard.Padding = new Padding(0);
            rtb = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = T.LogBg,
                ForeColor = T.LogFg,
                BorderStyle = BorderStyle.None,
                Font = T.MonoF(9.5f),
                ReadOnly = true,
                WordWrap = true,
                DetectUrls = false,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                HideSelection = false,
                AcceptsTab = false
            };
            logCard.Controls.Add(rtb);
            rightCol.Controls.Add(logCard);
            rightCol.Controls.SetChildIndex(logCard, 0);   // Dock=Fill 至少在最后参与布局

            Controls.SetChildIndex(header, Controls.Count - 1);  // 顶部标题栏在最外层

            // 左列：表单（可滚动）
            var leftHost = new Panel { Dock = DockStyle.Left, Width = 478, BackColor = T.Bg };
            leftHost.Padding = new Padding(0, 0, 16, 0);
            body.Controls.Add(leftHost);

            leftScroll = new Panel { Dock = DockStyle.Fill, BackColor = T.Bg, AutoScroll = true };
            leftHost.Controls.Add(leftScroll);
            leftInner = new Panel { BackColor = T.Bg, Location = new Point(0, 0), Width = 456, Height = 10 };
            leftInner.Anchor = AnchorStyles.Top | AnchorStyles.Left;   // 宽度自管理，交给 AutoScroll 会被强制压缩
            leftScroll.Controls.Add(leftInner);

            pump = new System.Windows.Forms.Timer { Interval = 55 };
            pump.Tick += delegate { Drain(); };
            spinner = new System.Windows.Forms.Timer { Interval = 140 };
            spinner.Tick += delegate
            {
                if (!running) return;
                spinPhase = (spinPhase + 1) % 10;
                statusLabel.Text = "正在修补" + new string('·', spinPhase % 4 + 1) +
                                   (startedAt == DateTime.MinValue ? "" :
                                    "  " + (DateTime.Now - startedAt).TotalSeconds.ToString("0") + " 秒");
            };
            KeyPreview = true;
            KeyDown += delegate (object s, KeyEventArgs e)
            {
                if (e.Control && e.KeyCode == Keys.Enter) { StartRun(); e.Handled = true; }
                if (e.KeyCode == Keys.Escape) { StopRun(); }
            };
        }

        System.Windows.Forms.LinkLabel LinkLabelOn(Control parent, string text, EventHandler click)
        {
            var l = new LinkLabel
            {
                Text = text,
                AutoSize = true,
                LinkBehavior = LinkBehavior.NeverUnderline,
                Font = T.F(8.5f, FontStyle.Regular),
                LinkColor = T.Muted,
                ActiveLinkColor = T.Accent,
                VisitedLinkColor = T.Muted,
                BackColor = T.Bg
            };
            l.LinkClicked += (s, e) => { click(null, EventArgs.Empty); };
            parent.Controls.Add(l);
            return l;
        }

        ToolTip ttKsud;
        void RefreshKsudLabel()
        {
            KSULabel.Text = KsudPath == "" ? "未找到 KSU，请点“更换”" : Path.GetFileName(KsudPath);
            if (ttKsud == null) ttKsud = new ToolTip();
            ttKsud.SetToolTip(KSULabel, KsudPath == "" ? "当前没有可用的 KSU" : KsudPath);   // 悬停看完整路径
        }

        // ------------------------------------------------------------ 表单构建
        float Ak = 1f;   // 高 DPI 下 AutoSize 标签返回的是物理像素高度，换算回布局单位要用它

        int Lh(Control c)
        {
            int v = (int)Math.Ceiling(c.Height / Ak);
            return v <= 0 ? 18 : v;
        }

        void RebuildForm()
        {
            fields.Clear();
            leftInner.Controls.Clear();
            try { using (var gg = Graphics.FromHwnd(IntPtr.Zero)) Ak = Math.Max(1f, gg.DpiX / 96f); } catch { Ak = 1f; }
            var sections = SectionsFor(Mode);
            int y = 0;
            int width = 438;
            foreach (var sec in sections)
            {
                var card = new Card { Location = new Point(0, y), Width = width };
                leftInner.Controls.Add(card);
                int pad = 16;
                int cy = 14;
                var t = new Label
                {
                    Text = sec.Title,
                    Font = T.F(10f, FontStyle.Bold),
                    ForeColor = T.Text,
                    AutoSize = true,
                    Location = new Point(pad, cy),
                    BackColor = T.Card
                };
                card.Controls.Add(t);
                cy += 22;
                if (sec.Desc != "")
                {
                    var d = new Label
                    {
                        Text = sec.Desc,
                        Font = T.F(8.5f, FontStyle.Regular),
                        ForeColor = T.Muted,
                        AutoEllipsis = false,
                        AutoSize = true,                                   // 高度交给 WinForms 自适应，杜绝算错
                        MaximumSize = new Size(width - pad * 2, 0),        // 只限宽，不限高
                        Location = new Point(pad, cy),
                        BackColor = T.Card
                    };
                    card.Controls.Add(d);
                    cy += Lh(d) + 8;
                }
                cy += 2;
                foreach (var f in sec.Fields)
                    cy += RenderField(card, f, pad, cy, width - pad * 2);
                int bottom = cy;
                foreach (Control cc in card.Controls) bottom = Math.Max(bottom, cc.Bottom);
                card.Height = bottom + 14;
                y += card.Height + 12;
            }

            var tip = new Label
            {
                Text = "提示：输出文件先落到电脑上，再用手机侧工具刷入；建议每次保留原厂镜像备份。",
                Font = T.F(8f, FontStyle.Regular),
                ForeColor = T.Faint,
                AutoSize = true,
                MaximumSize = new Size(width - 4, 0),
                Location = new Point(2, y + 2),
                BackColor = T.Bg
            };
            leftInner.Controls.Add(tip);
            y += Lh(tip) + 10;
            leftInner.Height = y;
            leftInner.Width = width + 2;

            ScaleCards();        // 重建后必须再按 DPI 放大一次，否则切页后布局只有逻辑尺寸

            btnStart.Text = "▶  " + (Mode == "restore" ? "开始还原" : "开始修补");
        }

        // 把刚构建好的（96dpi 逻辑尺寸）卡片整体放大到当前 DPI 比例
        void ScaleCards()
        {
            if (K <= 1.001f)
            {
                leftInner.Width = 438;
                return;
            }
            SuspendLayout();
            leftInner.Width = (int)(438 * K);
            int maxBottom = 0;
            foreach (Control c in leftInner.Controls)
            {
                c.Scale(new SizeF(K, K));
                if (c is Card)
                {
                    c.Left = 0;
                    c.Width = leftInner.Width;
                    foreach (Control cc in c.Controls)
                        if (cc is InputBox)
                        {
                            cc.Width = c.Width - (int)(32 * K);
                            cc.Left = (int)(16 * K);
                            ((InputBox)cc).Relayout();
                        }
                }
                maxBottom = Math.Max(maxBottom, c.Bottom);
            }
            leftInner.Height = maxBottom + (int)(10 * K);
            ResumeLayout(true);
        }

        static int MeasureText(string text, Font font, int width)
        {
            if (width <= 10) return 16;
            int h = TextRenderer.MeasureText(text, font, new Size(width, 1000),
                                             TextFormatFlags.WordBreak).Height;
            return h;
        }

        int RenderField(Card card, Field f, int pad, int y, int w)
        {
            int startY = y;
            if (f.Kind == "check")
            {
                var chk = new CheckBox
                {
                    Text = f.Label,
                    Font = T.F(9f, FontStyle.Regular),
                    ForeColor = T.Text,
                    BackColor = T.Card,
                    AutoSize = true,
                    Location = new Point(pad - 2, y),
                    Cursor = Cursors.Hand,
                    Checked = checks.ContainsKey(f.Key) ? checks[f.Key] : false
                };
                chk.CheckedChanged += delegate { checks[f.Key] = chk.Checked; };
                card.Controls.Add(chk);
                fields[f.Key] = chk;
                int used = 24;
                if (f.Hint != "")
                {
                    var h = new Label
                    {
                        Text = f.Hint,
                        Font = T.F(8f, FontStyle.Regular),
                        ForeColor = T.Faint,
                        AutoSize = true,
                        MaximumSize = new Size(w - 20, 0),
                        Location = new Point(pad + 20, y + 24),
                        BackColor = T.Card
                    };
                    card.Controls.Add(h);
                    used += Lh(h) + 4;
                }
                return used + 6;
            }

            string nameText = f.Label;
            bool optional = nameText.EndsWith("（可选）");
            if (optional) nameText = nameText.Substring(0, nameText.Length - 4).TrimEnd();

            var lab = new Label
            {
                Text = nameText,
                Font = T.F(9f, FontStyle.Regular),
                ForeColor = T.Text,
                AutoSize = true,
                Location = new Point(pad, y),
                BackColor = T.Card
            };
            card.Controls.Add(lab);
            if (f.Required || optional)
            {
                var chip = new Chip(f.Required ? "必填" : "可选", Ak, !f.Required);
                int labW = (int)Math.Ceiling(lab.Width / Ak);    // AutoSize 返回的是物理像素，换算回布局单位
                int labH = (int)Math.Ceiling(lab.Height / Ak);
                chip.Location = new Point(pad + labW + 8, y + (labH - chip.Height) / 2);   // 与标题文字垂直居中
                card.Controls.Add(chip);
            }
            y += 22;

            if (f.Kind == "combo")
            {
                var cb = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    FlatStyle = FlatStyle.Flat,
                    Font = T.F(9f, FontStyle.Regular),
                    Location = new Point(pad, y),
                    Width = w,
                    BackColor = T.Card,
                    ForeColor = T.Text
                };
                cb.Items.Add("（自动识别）");
                string[] comboItems = f.Key == "kmi" ? kmis.ToArray() : f.Values;
                foreach (string k in comboItems) cb.Items.Add(k);
                string cur = texts.ContainsKey(f.Key) ? texts[f.Key] : "";
                int idx = 0;
                for (int i = 0; i < cb.Items.Count; i++) if (cb.Items[i].ToString() == cur) idx = i;
                cb.SelectedIndex = idx;
                cb.SelectedIndexChanged += delegate
                {
                    texts[f.Key] = cb.SelectedIndex <= 0 ? "" : cb.Items[cb.SelectedIndex].ToString();
                };
                card.Controls.Add(cb);
                fields[f.Key] = cb;
                y += 30;
            }
            else
            {
                bool browse = f.Kind == "file" || f.Kind == "dir" || f.Kind == "save";
                var ib = new InputBox(browse)
                {
                    Location = new Point(pad, y),
                    Width = w,
                    Height = 32
                };
                ib.Box.Text = texts.ContainsKey(f.Key) ? texts[f.Key] : "";
                ib.Box.TextChanged += delegate { texts[f.Key] = ib.Box.Text; };
                if (browse)
                {
                    ib.Picked += delegate { Browse(f); };
                    ib.Box.DoubleClick += delegate { Browse(f); };
                }
                card.Controls.Add(ib);
                fields[f.Key] = ib;
                y += 36;
            }

            if (f.Hint != "")
            {
                var h = new Label
                {
                    Text = f.Hint,
                    Font = T.F(8f, FontStyle.Regular),
                    ForeColor = T.Faint,
                    AutoSize = true,
                    MaximumSize = new Size(w, 0),
                    Location = new Point(pad, y),
                    BackColor = T.Card
                };
                card.Controls.Add(h);
                y += Lh(h) + 4;
            }
            return (y + 6) - startY;
        }

        void Browse(Field f)
        {
            InputBox ib = fields[f.Key] as InputBox;
            if (ib == null) return;
            string cur = ib.Box.Text.Trim().Trim('"');
            string start = cur != "" ? Path.GetDirectoryName(cur) : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (start == null || !Directory.Exists(start)) start = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (f.Kind == "dir")
            {
                using (var dlg = new FolderBrowserDialog())
                {
                    dlg.Description = "选择输出目录";
                    if (dlg.ShowDialog(this) == DialogResult.OK) ib.Box.Text = dlg.SelectedPath;
                }
            }
            else if (f.Kind == "save")
            {
                using (var dlg = new SaveFileDialog())
                {
                    dlg.Filter = "镜像文件 (*.img;*.bin)|*.img;*.bin|所有文件 (*.*)|*.*";
                    dlg.DefaultExt = "img";
                    dlg.FileName = cur != "" ? Path.GetFileName(cur) : "boot-ksu.img";
                    dlg.InitialDirectory = start;
                    dlg.OverwritePrompt = true;
                    if (dlg.ShowDialog(this) == DialogResult.OK) ib.Box.Text = dlg.FileName;
                }
            }
            else
            {
                using (var dlg = new OpenFileDialog())
                {
                    dlg.Filter = "镜像文件 (*.img;*.bin)|*.img;*.bin|所有文件 (*.*)|*.*";
                    dlg.InitialDirectory = start;
                    dlg.CheckFileExists = true;
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        ib.Box.Text = dlg.FileName;
                        if (f.Key == "boot") AutoFillOutput(dlg.FileName);
                    }
                }
            }
        }

        static readonly string[] TabTitles = { "修补 boot（经典）", "修补 boot（新版）", "还原 boot" };
        static readonly string[] TabModes = { "v1", "v2", "restore" };

        // 让页签高亮与当前真正生效的模式保持一致（启动恢复配置、运行中误点都会用到）
        void SyncTabs()
        {
            if (tabs == null) return;
            int idx = Array.IndexOf(TabModes, Mode);
            if (idx < 0) idx = 0;
            if (tabs.SelectedIndex != idx) { tabs.SelectedIndex = idx; tabs.Invalidate(); }
        }

        // 启动时：把输出位置补到程序目录\输出（boot 路径有记录就顺带用在文件名里）
        void AutoFillFromSaved()
        {
            string boot = texts.ContainsKey("boot") ? texts["boot"].Trim().Trim('"') : "";
            if (boot != "" && !File.Exists(boot)) boot = "";
            AutoFillOutput(boot);
        }

        void AutoFillCurrent()
        {
            AutoFillOutput(Val("boot"));
        }

        // 输出位置统一放在**程序所在目录\输出**，与 boot 镜像在哪无关
        static string OutputRoot()
        {
            return Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "输出");
        }

        // 选了 boot 镜像 / 切页 / 启动时：把输出位置补到「程序目录\输出」，没有就建一个。
        // 只覆盖“上次由程序自动写入、用户没改过”的值，手填的内容不动。
        void AutoFillOutput(string bootPath)
        {
            string dir = OutputRoot();
            if (!Directory.Exists(dir))
            {
                try { Directory.CreateDirectory(dir); }
                catch (Exception ex) { Log("创建输出目录失败：" + ex.Message, LogKind.Warn); return; }
            }
            string baseName = string.IsNullOrEmpty(bootPath) ? "boot"
                            : Path.GetFileNameWithoutExtension(bootPath.Trim().Trim('"'));
            if (string.IsNullOrEmpty(baseName)) baseName = "boot";
            string outName = baseName + (Mode == "restore" ? "-restore" : "-ksu") + ".img";
            bool any = false;
            any |= SetAuto("out", dir);                                  // v1 / 还原：输出目录
            any |= SetAuto("out_name", outName);                         // v1 / 还原：输出文件名
            any |= SetAuto("output", Path.Combine(dir, outName));        // v2：输出文件
            if (any)
                Log("已自动设置输出位置：" + Path.Combine(dir, outName), LogKind.Dim);
        }

        // 输出目录若不存在就创建出来
        void EnsureOutDir()
        {
            string dir = lastOutDir;
            if (string.IsNullOrEmpty(dir)) return;
            if (Directory.Exists(dir)) return;
            try
            {
                Directory.CreateDirectory(dir);
                Log("输出目录不存在，已自动创建：" + dir, LogKind.Dim);
            }
            catch (Exception ex)
            {
                Log("创建输出目录失败：" + ex.Message, LogKind.Warn);
            }
        }

        // 记下“哪些输出值是程序自己写的”，下次可安全覆盖；用户手改过的就保留
        readonly Dictionary<string, string> autoVals = new Dictionary<string, string>();

        bool SetAuto(string key, string value)
        {
            InputBox ib = fields.ContainsKey(key) ? fields[key] as InputBox : null;
            if (ib == null) return false;
            string cur = ib.Box.Text.Trim();
            bool isAuto = cur == "" || (autoVals.ContainsKey(key) && cur == autoVals[key]);
            if (!isAuto) return false;
            autoVals[key] = value;
            ib.Box.Text = value;
            return true;
        }

        void ChooseKsud()
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Filter = "可执行文件 (*.exe)|*.exe";
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    KsudPath = dlg.FileName;
                    kmis = QueryKmis();
                    RefreshKsudLabel();
                    RebuildForm();
                    Log("已切换 KSU：" + KsudPath, LogKind.Dim);
                    SaveSettings();
                }
            }
        }

        void OnDragEnterF(object s, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        void OnDragDropF(object s, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files == null || files.Length == 0) return;
            string file = files[0];
            if (!fields.ContainsKey("boot") || !(fields["boot"] is InputBox))
            {
                Log("当前页面没有 boot 镜像输入框，无法接收拖入的文件。", LogKind.Warn);
                return;
            }
            if (Directory.Exists(file))
            {
                Log("拖入的是文件夹，请拖入 boot.img / init_boot.img 文件。", LogKind.Warn);
                return;
            }
            ((InputBox)fields["boot"]).Box.Text = file;
            string name = FindField("boot") != null ? FindField("boot").Label : "boot 镜像";
            AutoFillOutput(file);
            Log("已把拖入的文件填入“" + name + "”：" + file, LogKind.Dim);
        }

        // ------------------------------------------------------------ 日志
        Color KindColor(int kind)
        {
            switch (kind)
            {
                case LogKind.Dim: return T.LogDim;
                case LogKind.Step: return T.LogStep;
                case LogKind.Ok: return T.LogOk;
                case LogKind.Warn: return T.LogWarn;
                case LogKind.Err: return T.LogErr;
                default: return T.LogFg;
            }
        }

        void Log(string text, int kind)
        {
            if (rtb == null) return;
            rtb.SelectionStart = rtb.TextLength;
            rtb.SelectionLength = 0;
            rtb.SelectionColor = KindColor(kind);
            rtb.AppendText(text + "\n");
            rtb.SelectionColor = T.LogFg;
            if (rtb.TextLength > 400000) { rtb.Select(0, 100000); rtb.SelectedText = ""; }
            rtb.SelectionStart = rtb.TextLength;
            rtb.ScrollToCaret();
        }

        void ClearLog()
        {
            rtb.Clear();
        }

        void CopyLog()
        {
            try
            {
                Clipboard.SetText(rtb.Text);
                SetStatus("日志已复制到剪贴板", T.Ok);
            }
            catch { }
        }

        void SetStatus(string text, Color dot)
        {
            statusLabel.Text = text;
            dotLabel.ForeColor = dot;
        }

        int Classify(string line)
        {
            string s = line.Trim();
            if (s.Length == 0) return LogKind.Dim;
            if (s.StartsWith("错误") || s.Contains("出错") || s.Contains("失败") || s.StartsWith("Error"))
                return LogKind.Err;
            if (s.StartsWith("由以下原因") || line.StartsWith("    ")) return LogKind.Err;
            if (s.StartsWith("- ") || s.StartsWith("* "))
                return (s.Contains("完成") || s.Contains("成功")) ? LogKind.Ok : LogKind.Step;
            if (s.Contains("完成") || s.Contains("成功")) return LogKind.Ok;
            if (s.Contains("警告")) return LogKind.Warn;
            return LogKind.Info;
        }

        void Drain()
        {
            bool end = false;
            int code = -1;
            while (logQueue.Count > 0)
            {
                object[] item;
                lock (logQueue) { item = logQueue.Dequeue(); }
                if (item == null) { end = true; continue; }
                if (item[0] == null) { end = true; continue; }
                if (item[0] is int) { code = (int)item[1]; continue; }
                string line = (string)item[0];
                if (line.Length > 0 && line[0] == '\0') Log(line.Substring(1), LogKind.Err);   // stderr
                else Log(line, Classify(line));
            }
            if (end) FinishRun(code);
        }

        // ------------------------------------------------------------ 运行
        Field FindField(string key)
        {
            foreach (var sec in SectionsFor(Mode))
                foreach (var f in sec.Fields)
                    if (f.Key == key) return f;
            return null;
        }

        string Val(string key)
        {
            if (fields.ContainsKey(key) && fields[key] is InputBox) return ((InputBox)fields[key]).Box.Text.Trim().Trim('"');
            return texts.ContainsKey(key) ? texts[key] : "";
        }

        bool Chk(string key)
        {
            if (fields.ContainsKey(key) && fields[key] is CheckBox) return ((CheckBox)fields[key]).Checked;
            return checks.ContainsKey(key) && checks[key];
        }

        List<string> MultiVal(string key)
        {
            var list = new List<string>();
            foreach (string part in Val(key).Replace("\r", "").Replace("\n", ",").Split(','))
            {
                string s = part.Trim();
                if (s != "") list.Add(s);
            }
            return list;
        }

        // 按 Windows 命令行规则加引号：内部双引号要转义，结尾的反斜杠要翻倍，
        // 否则 "D:\输出\" 里的 \" 会被解析成一个字面引号，路径就断在反斜杠上。
        static string Q(string a)
        {
            if (string.IsNullOrEmpty(a)) return "\"\"";
            if (a.IndexOf(' ') < 0 && a.IndexOf('\t') < 0 && a.IndexOf('"') < 0) return a;
            var sb = new StringBuilder("\"");
            int bs = 0;
            foreach (char c in a)
            {
                if (c == '\\') { sb.Append('\\'); bs++; }
                else if (c == '"') { sb.Append('\\', bs + 1).Append('"'); bs = 0; }
                else { sb.Append(c); bs = 0; }
            }
            if (bs > 0) sb.Append('\\', bs);
            sb.Append('"');
            return sb.ToString();
        }

        string[] BuildArgs()
        {
            if (KsudPath == "" || !File.Exists(KsudPath))
                throw new Exception("没有找到 KSU 程序，请点右上角“更换”选择 KSU 中文版 exe。");

            var a = new List<string>();
            string boot = Val("boot");
            if (boot == "") throw new Exception("请先选择 boot 镜像。");
            if (!File.Exists(boot)) throw new Exception("boot 镜像不存在：\n" + boot);

            if (Mode == "v1")
            {
                a.Add("boot-patch"); a.Add("--boot"); a.Add(boot);
                foreach (var pair in new[] { new[] { "kernel", "--kernel" }, new[] { "module", "--module" },
                                             new[] { "init", "--init" } })
                {
                    string v = Val(pair[0]);
                    if (v != "")
                    {
                        if (!File.Exists(v)) throw new Exception(pair[1] + " 指向的文件不存在：\n" + v);
                        a.Add(pair[1]); a.Add(v);
                    }
                }
                if (Val("out") != "") { a.Add("--out"); a.Add(Val("out")); lastOutDir = Val("out"); }
                else lastOutDir = Path.GetDirectoryName(boot);
                if (Val("out_name") != "") { a.Add("--out-name"); a.Add(Val("out_name")); }
                if (Val("kmi") != "") { a.Add("--kmi"); a.Add(Val("kmi")); }
                string arch = Val("arch");
                if (arch != "" && arch != "aarch64") { a.Add("--arch"); a.Add(arch); }
                if (Val("cmdline") != "") { a.Add("--cmdline"); a.Add(Val("cmdline")); }
                if (Chk("allow_shell")) a.Add("--allow-shell");
                if (Chk("no_custom_rc")) a.Add("--no-custom-rc");
                if (Chk("no_install")) a.Add("--no-install");
                if (Chk("enable_adbd")) a.Add("--enable-adbd");
                if (Chk("ramdisk")) a.Add("--ramdisk");
                foreach (string v in MultiVal("force_feature")) { a.Add("--force-feature"); a.Add(v); }
                foreach (string v in MultiVal("block_module")) { a.Add("--block-module"); a.Add(v); }
                foreach (string v in MultiVal("adb_debug_prop")) { a.Add("--adb-debug-prop"); a.Add(v); }
            }
            else if (Mode == "v2")
            {
                string output = Val("output");
                if (output == "") throw new Exception("请选择修补后的输出文件。");
                if (File.Exists(output) && !Chk("force"))
                    throw new Exception("输出文件已存在；请勾选“覆盖已存在的输出文件”，或换一个文件名。");
                a.Add("boot-patch-v2"); a.Add("--boot"); a.Add(boot); a.Add("--output"); a.Add(output);
                if (Val("module") != "")
                {
                    if (!File.Exists(Val("module"))) throw new Exception("--module 指向的文件不存在：\n" + Val("module"));
                    a.Add("--module"); a.Add(Val("module"));
                }
                if (Chk("force")) a.Add("--force");
                if (Chk("allow_shell")) a.Add("--allow-shell");
                if (Chk("no_custom_rc")) a.Add("--no-custom-rc");
                foreach (string v in MultiVal("force_feature")) { a.Add("--force-feature"); a.Add(v); }
                foreach (string v in MultiVal("block_module")) { a.Add("--block-module"); a.Add(v); }
                lastOutDir = Path.GetDirectoryName(output);
            }
            else
            {
                a.Add("boot-restore"); a.Add("--boot"); a.Add(boot);
                if (Val("out") != "") { a.Add("--out"); a.Add(Val("out")); lastOutDir = Val("out"); }
                else lastOutDir = Path.GetDirectoryName(boot);
                if (Val("out_name") != "") { a.Add("--out-name"); a.Add(Val("out_name")); }
            }
            return a.ToArray();
        }

        void StartRun()
        {
            if (running) return;
            string[] args;
            try { args = BuildArgs(); }
            catch (Exception ex)
            {
                Log(ex.Message, LogKind.Err);
                SetStatus("参数有误，请检查左侧表单", T.Danger);
                return;
            }
            SaveSettings();
            EnsureOutDir();      // 输出目录不存在就建出来

            var sb = new StringBuilder();
            foreach (string s in args) sb.Append(Q(s)).Append(' ');
            Log("", LogKind.Dim);
            Log(new string('─', 44), LogKind.Dim);
            Log("[" + DateTime.Now.ToString("HH:mm:ss") + "] $ " + Q(KsudPath) + " " + sb.ToString().TrimEnd(), LogKind.Dim);
            Log("", LogKind.Dim);

            var psi = new ProcessStartInfo(KsudPath, sb.ToString().TrimEnd())
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                WorkingDirectory = Path.GetDirectoryName(KsudPath)
            };
            try
            {
                proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
                proc.OutputDataReceived += delegate (object s, DataReceivedEventArgs e)
                {
                    if (e.Data != null) lock (logQueue) logQueue.Enqueue(new object[] { e.Data });
                };
                proc.ErrorDataReceived += delegate (object s, DataReceivedEventArgs e)
                {
                    if (e.Data != null) lock (logQueue) logQueue.Enqueue(new object[] { "\u0000" + e.Data });
                };
                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                Log("启动失败：" + ex.Message, LogKind.Err);
                FinishRun(-1);
                return;
            }

            running = true;
            startedAt = DateTime.Now;
            btnStart.Enabled = false;
            btnStop.Enabled = true;
            progress.Visible = true;
            SetStatus("正在修补…", T.Accent);
            spinner.Start();
            pump.Start();

            int pid = proc.Id;
            var runProc = proc;          // 捕获局部引用：FinishRun 会把字段置空并释放，线程不能再靠字段
            waiter = new Thread(delegate ()
            {
                int code = -1;
                try
                {
                    runProc.WaitForExit();
                    runProc.WaitForExit();     // 第二次调用等待异步输出读完，否则最后几行会丢
                    code = runProc.ExitCode;
                }
                catch { }
                lock (logQueue)
                {
                    logQueue.Enqueue(new object[] { -999, code });
                    logQueue.Enqueue(null);
                }
            });
            waiter.IsBackground = true;
            waiter.Start();
        }

        void StopRun()
        {
            if (!running || proc == null) return;
            SetStatus("正在停止…", T.Warn);
            try { proc.Kill(); } catch { }
        }

        void FinishRun(int code)
        {
            double secs = startedAt == DateTime.MinValue ? 0 : (DateTime.Now - startedAt).TotalSeconds;
            running = false;
            var finished = proc;
            proc = null;
            if (finished != null) { try { finished.Dispose(); } catch { } }    // 不释放每次运行都会漏一个进程句柄
            progress.Visible = false;
            spinner.Stop();
            pump.Stop();
            btnStart.Enabled = true;
            btnStop.Enabled = false;
            Log("", LogKind.Dim);
            if (code == 0)
            {
                Log("✔ 完成，用时 " + secs.ToString("0.0") + " 秒。", LogKind.Ok);
                SetStatus("完成 · 用时 " + secs.ToString("0.0") + " 秒", T.Ok);
            }
            else if (code < 0)
            {
                Log("✖ 已停止，用时 " + secs.ToString("0.0") + " 秒。", LogKind.Warn);
                SetStatus("已停止", T.Warn);
            }
            else
            {
                Log("✖ KSU 返回退出码 " + code + "，用时 " + secs.ToString("0.0") + " 秒。", LogKind.Err);
                SetStatus("失败 · 退出码 " + code, T.Danger);
            }
            Log(new string('─', 44), LogKind.Dim);
            startedAt = DateTime.MinValue;
        }

        void OpenOutDir()
        {
            string target = lastOutDir;
            if (string.IsNullOrEmpty(target))
            {
                if (Val("out") != "") target = Val("out");
                else if (Val("output") != "") target = Path.GetDirectoryName(Val("output"));
            }
            if (!string.IsNullOrEmpty(target) && Directory.Exists(target))
            {
                Process.Start(new ProcessStartInfo("explorer.exe", "\"" + target + "\"") { UseShellExecute = true });
            }
            else Log("还没有输出目录可打开。", LogKind.Warn);
        }

        bool dpiApplied;
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (!dpiApplied) { dpiApplied = true; ApplyDpiScale(); }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            // 启动时自动填值会让滚动面板跳到被填的控件上，这里把左列拉回顶部
            if (leftInner != null) leftInner.AutoScrollPosition = new Point(0, 0);
            if (rtb != null) rtb.Focus();
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (proc != null) { try { proc.Kill(); } catch { } }
            SaveSettings();
            base.OnFormClosing(e);
        }

        // ------------------------------------------------------------ 表单规格
        class Field
        {
            public string Key, Label, Hint, Kind, Flag;
            public bool Required;
            public string[] Values = new string[0];
        }

        class Section
        {
            public string Title = "", Desc = "";
            public List<Field> Fields = new List<Field>();
        }

        static Field Fd(string key, string label, string kind, string hint, bool required)
        {
            var f = new Field { Key = key, Label = label, Kind = kind, Hint = hint, Required = required };
            return f;
        }

        List<Section> SectionsFor(string mode)
        {
            var list = new List<Section>();
            if (mode == "v1")
            {
                var s1 = new Section
                {
                    Title = "输入文件",
                    Desc = "选择要修补的镜像；内核、模块、init 留空则使用 KSU 内置资产。"
                };
                s1.Fields.Add(Fd("boot", "boot 镜像", "file", "一般是从手机上备份出来的 boot.img 或 init_boot.img", true));
                s1.Fields.Add(Fd("kernel", "内核镜像（可选）", "file", "想替换的内核 Image，留空则保留原内核", false));
                s1.Fields.Add(Fd("module", "LKM 模块（可选）", "file", "指定 kernelsu.ko，留空按 KMI 自动使用内置模块", false));
                s1.Fields.Add(Fd("init", "init 文件（可选）", "file", "替换 ramdisk 中的 init，留空使用内置 ksuinit", false));
                list.Add(s1);

                var s2 = new Section { Title = "输出", Desc = "默认输出到本程序所在目录下的“输出”文件夹；文件名留空时使用 KSU 的默认规则。" };
                s2.Fields.Add(Fd("out", "输出目录（可选）", "dir", "留空则输出到程序目录\\输出", false));
                s2.Fields.Add(Fd("out_name", "输出文件名（可选）", "text", "例如 boot-ksu.img", false));
                list.Add(s2);

                var s3 = new Section { Title = "选项", Desc = "勾选即生效，对应 KSU boot-patch 的同名开关。" };
                var kmi = Fd("kmi", "KMI 版本", "combo", "指定后强制使用该 KMI 的内置模块；留空自动识别", false);
                s3.Fields.Add(kmi);
                var arch = Fd("arch", "架构", "combo", "", false);
                arch.Values = new[] { "aarch64", "x86_64", "arm", "riscv64" };
                s3.Fields.Add(arch);
                s3.Fields.Add(Fd("cmdline", "追加 cmdline", "text", "附加到 boot 镜像头的额外内核命令行", false));
                s3.Fields.Add(Fd("allow_shell", "始终允许 shell 获取 root", "check", "", false));
                s3.Fields.Add(Fd("no_custom_rc", "不加载自定义 rc", "check", "", false));
                s3.Fields.Add(Fd("no_install", "只改配置，不（重新）安装 KernelSU", "check", "对应 --no-install，仅修改 allow_shell 等配置", false));
                s3.Fields.Add(Fd("enable_adbd", "强制启用 adbd 并关闭认证", "check", "", false));
                s3.Fields.Add(Fd("ramdisk", "修补 ramdisk 而非 boot 镜像", "check", "用于 AVD 的 ramdisk", false));
                list.Add(s3);

                var s4 = new Section { Title = "内核特性（高级）", Desc = "启动时强制设定特性或屏蔽模块，多个值用逗号分隔。" };
                s4.Fields.Add(Fd("force_feature", "强制特性 FEATURE=VALUE", "text", "如 su_compat=0；被强制的特性用户空间无法再改", false));
                s4.Fields.Add(Fd("block_module", "屏蔽模块名", "text", "如 oplus_secure_guard", false));
                s4.Fields.Add(Fd("adb_debug_prop", "额外 adb_debug 属性", "text", "如 service.adb.root=1", false));
                list.Add(s4);
            }
            else if (mode == "v2")
            {
                var s1 = new Section
                {
                    Title = "输入与输出",
                    Desc = "始终作用于 boot 镜像，不会自动选择 init_boot 或 vendor_boot。"
                };
                s1.Fields.Add(Fd("boot", "源 boot 镜像", "file", "一般是从手机上备份出来的 boot.img", true));
                s1.Fields.Add(Fd("output", "输出文件", "save", "默认保存到程序目录\\输出；建议命名 boot-ksu.img", true));
                s1.Fields.Add(Fd("module", "kernelsu.ko（可选）", "file", "留空按 KMI 自动选择内置模块", false));
                list.Add(s1);

                var s2 = new Section { Title = "选项", Desc = "新版修补直接注入内核，无需再指定内核镜像。" };
                s2.Fields.Add(Fd("force", "覆盖已存在的输出文件", "check", "未勾选时输出文件已存在会直接报错", false));
                s2.Fields.Add(Fd("allow_shell", "始终允许 shell 获取 root", "check", "", false));
                s2.Fields.Add(Fd("no_custom_rc", "不加载自定义 rc", "check", "", false));
                s2.Fields.Add(Fd("force_feature", "强制特性 FEATURE=VALUE", "text", "如 su_compat=0", false));
                s2.Fields.Add(Fd("block_module", "屏蔽模块名", "text", "如 oplus_secure_guard", false));
                list.Add(s2);
            }
            else
            {
                var s1 = new Section { Title = "输入文件", Desc = "还原被 KernelSU 修补过的镜像，恢复成原厂 boot。" };
                s1.Fields.Add(Fd("boot", "boot 镜像", "file", "要还原的、已被 KernelSU 修补过的镜像", true));
                list.Add(s1);

                var s2 = new Section { Title = "输出", Desc = "默认输出到本程序所在目录下的“输出”文件夹。" };
                s2.Fields.Add(Fd("out", "输出目录（可选）", "dir", "留空则输出到程序目录\\输出", false));
                s2.Fields.Add(Fd("out_name", "输出文件名（可选）", "text", "例如 boot-restore.img", false));
                list.Add(s2);
            }
            return list;
        }
    }

    static class Dpi
    {
        [System.Runtime.InteropServices.DllImport("shcore.dll")]
        static extern int SetProcessDpiAwareness(int value);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool SetProcessDPIAware();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern uint GetDpiForWindow(IntPtr hwnd);

        public static float OfWindow(IntPtr h)
        {
            try
            {
                uint d = GetDpiForWindow(h);
                if (d >= 96 && d <= 480) return d;
            }
            catch { }
            return 96f;
        }

        public static void Enable()
        {
            try { if (SetProcessDpiAwareness(1) == 0) return; } catch { }
            try { SetProcessDPIAware(); } catch { }
        }
    }
    static class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                Dpi.Enable();
            }
            catch { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                try
                {
                    string log = Path.Combine(Path.GetTempPath(), "KSU-gui-crash.log");
                    File.WriteAllText(log, DateTime.Now.ToString() + "\r\n" + ex.ToString(), new UTF8Encoding(false));
                }
                catch { }
                MessageBox.Show(ex.ToString(), "启动失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
