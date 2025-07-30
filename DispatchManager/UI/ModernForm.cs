using DispatchManager;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public class ModernForm : Form
{
    // ----------------------
    // Public Properties
    // ----------------------
    public Size CustomMinimumSize { get; set; } = new Size(800, 600);
    public Size CustomMaximumSize { get; set; } = Size.Empty;
    public bool ShowBorder { get; set; } = true;
    public int BorderThickness { get; set; } = 1;

    public Color TitleBarColor
    {
        get => panelTop.BackColor;
        set
        {
            panelTop.BackColor = value;
            btnClose.BackColor = value;
            btnMinimize.BackColor = value;
            btnMaximize.BackColor = value;
        }
    }
    public Color ButtonHoverColor { get; set; } = Color.DodgerBlue;
    public Color CloseButtonHoverColor { get; set; } = Color.Red;
    public Color ButtonForeColor { get; set; } = Color.White;
    public Color BorderColor { get; set; } = Color.Gray;

    // Expose the TitleBar panel safely
    public Panel TitleBarPanel => panelTop;
    public int TitleBarHeight => panelTop.Height;

    // Expose the buttons safely
    public int CloseButtonLeft => btnClose.Left;
    public int MinimizeButtonLeft => btnMinimize.Left;

    // ----------------------
    // Controls
    // ----------------------
    private Panel panelTop;
    private Button btnClose;
    private Button btnMinimize;
    private Button btnMaximize;
    private System.Windows.Forms.Label lblLoggedInUser;


    // ----------------------
    // Dragging Logic (WinAPI)
    // ----------------------
    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();
    [DllImport("user32.dll")]
    private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
    private const int WM_NCLBUTTONDOWN = 0xA1;
    private const int HTCAPTION = 0x2;

    // ----------------------
    // Constructor
    // ----------------------
    public ModernForm()
    {
        this.FormBorderStyle = FormBorderStyle.None;
        this.DoubleBuffered = true;
        this.MinimumSize = CustomMinimumSize;

        this.SetStyle(ControlStyles.ResizeRedraw |
              ControlStyles.AllPaintingInWmPaint |
              ControlStyles.UserPaint |
              ControlStyles.OptimizedDoubleBuffer, true);

        this.UpdateStyles();

        // Title Panel
        panelTop = new Panel
        {
            Dock = DockStyle.Top,
            Height = 25,
            BackColor = Color.CornflowerBlue
        };
        panelTop.MouseDown += PanelTop_MouseDown;
        this.Controls.Add(panelTop);

        // Buttons
        btnClose = CreateTitleButton();
        btnClose.Paint += BtnClose_Paint;
        btnClose.MouseEnter += (s, e) => btnClose.BackColor = CloseButtonHoverColor;
        btnClose.MouseLeave += (s, e) => btnClose.BackColor = TitleBarColor;
        btnClose.Click += (s, e) => this.Close();
        btnClose.FlatAppearance.BorderSize = 0;
        panelTop.Controls.Add(btnClose);

        btnMaximize = CreateTitleButton();
        btnMaximize.Paint += BtnMaximize_Paint;
        btnMaximize.MouseEnter += (s, e) => btnMaximize.BackColor = ButtonHoverColor;
        btnMaximize.MouseLeave += (s, e) => btnMaximize.BackColor = TitleBarColor;
        btnMaximize.Click += BtnMaximize_Click;
        btnMaximize.FlatAppearance.BorderSize = 0;
        panelTop.Controls.Add(btnMaximize);

        btnMinimize = CreateTitleButton();
        btnMinimize.Paint += BtnMinimize_Paint;
        btnMinimize.MouseEnter += (s, e) => btnMinimize.BackColor = ButtonHoverColor;
        btnMinimize.MouseLeave += (s, e) => btnMinimize.BackColor = TitleBarColor;
        btnMinimize.Click += (s, e) => this.WindowState = FormWindowState.Minimized;
        btnMinimize.FlatAppearance.BorderSize = 0;
        panelTop.Controls.Add(btnMinimize);

        // Logged In User Label
        lblLoggedInUser = new System.Windows.Forms.Label
        {
            AutoSize = true,
            Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular),
            ForeColor = System.Drawing.Color.White,
            Text = Session.CurrentFullName
        };
        panelTop.Controls.Add(lblLoggedInUser);

        PositionButtons();
        this.Resize += ModernForm_Resize;
    }

    // ----------------------
    // Create Title Buttons
    // ----------------------
    private Button CreateTitleButton()
    {
        return new Button
        {
            Width = 25,
            Height = 25,
            FlatStyle = FlatStyle.Flat,
            Text = "",
            BackColor = Color.CornflowerBlue,
            ForeColor = ButtonForeColor,
            TabStop = false
        };
    }
  
    // ----------------------
    // Reposition Buttons on Resize
    // ----------------------
    private void ModernForm_Resize(object sender, EventArgs e)
    {
        // Fix grey border and misaligned buttons when maximized
        if (this.WindowState == FormWindowState.Maximized)
        {
            this.Padding = new Padding(0); // Remove default padding for maximized state
            panelTop.Width = this.ClientSize.Width; // Adjust title bar width
        }
        else
        {
            this.Padding = new Padding(0);
            panelTop.Width = this.ClientSize.Width; // Keep width in sync
        }

        PositionButtons(); // Update button positions

        this.MinimumSize = CustomMinimumSize;
        if (CustomMaximumSize != Size.Empty)
            this.MaximumSize = CustomMaximumSize;
    }


    private void PositionButtons()
    {
        int rightEdge = this.ClientSize.Width;

        btnClose.Location = new Point(rightEdge - btnClose.Width, 0);
        btnMaximize.Location = new Point(rightEdge - btnClose.Width - btnMaximize.Width, 0);
        btnMinimize.Location = new Point(rightEdge - btnClose.Width - btnMaximize.Width - btnMinimize.Width, 0);

        if (lblLoggedInUser != null)
        {
            int padding = 5;
            lblLoggedInUser.Location = new Point(
                btnMinimize.Left - lblLoggedInUser.Width - padding,
                (panelTop.Height - lblLoggedInUser.Height) / 2
            );
        }
    }


    // ----------------------
    // Paint Events
    // ----------------------
    private void BtnClose_Paint(object sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using (Pen pen = new Pen(ButtonForeColor, 1))
        {
            int padding = 8;
            g.DrawLine(pen, padding, padding, btnClose.Width - padding, btnClose.Height - padding);
            g.DrawLine(pen, btnClose.Width - padding, padding, padding, btnClose.Height - padding);
        }
    }

    private void BtnMinimize_Paint(object sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        using (Pen pen = new Pen(ButtonForeColor, 1))
        {
            int y = btnMinimize.Height / 2 + 5;
            g.DrawLine(pen, 8, y, btnMinimize.Width - 8, y);
        }
    }

    private void BtnMaximize_Paint(object sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        using (Pen pen = new Pen(ButtonForeColor, 1))
        {
            int padding = 8;
            g.DrawRectangle(pen, padding, padding, btnMaximize.Width - padding * 2, btnMaximize.Height - padding * 2);
        }
    }

    // ----------------------
    // Maximize Logic
    // ----------------------
    private void BtnMaximize_Click(object sender, EventArgs e)
    {
        this.WindowState = (this.WindowState == FormWindowState.Maximized)
            ? FormWindowState.Normal
            : FormWindowState.Maximized;
    }

    // ----------------------
    // Dragging Logic
    // ----------------------
    private void PanelTop_MouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            ReleaseCapture();
            SendMessage(this.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
        }
    }

    // ----------------------
    // Resizing Logic (Edges)
    // ----------------------
    protected override void WndProc(ref Message m)
    {
        const int WM_NCHITTEST = 0x84;
        const int HTCLIENT = 1;
        const int HTLEFT = 10;
        const int HTRIGHT = 11;
        const int HTTOP = 12;
        const int HTTOPLEFT = 13;
        const int HTTOPRIGHT = 14;
        const int HTBOTTOM = 15;
        const int HTBOTTOMLEFT = 16;
        const int HTBOTTOMRIGHT = 17;

        if (m.Msg == WM_NCHITTEST)
        {
            base.WndProc(ref m);
            if ((int)m.Result == HTCLIENT)
            {
                var cursor = this.PointToClient(Cursor.Position);
                int resizeArea = 8;
                if (cursor.X < resizeArea && cursor.Y < resizeArea)
                    m.Result = (IntPtr)HTTOPLEFT;
                else if (cursor.X > this.Width - resizeArea && cursor.Y < resizeArea)
                    m.Result = (IntPtr)HTTOPRIGHT;
                else if (cursor.X < resizeArea && cursor.Y > this.Height - resizeArea)
                    m.Result = (IntPtr)HTBOTTOMLEFT;
                else if (cursor.X > this.Width - resizeArea && cursor.Y > this.Height - resizeArea)
                    m.Result = (IntPtr)HTBOTTOMRIGHT;
                else if (cursor.X < resizeArea)
                    m.Result = (IntPtr)HTLEFT;
                else if (cursor.X > this.Width - resizeArea)
                    m.Result = (IntPtr)HTRIGHT;
                else if (cursor.Y < resizeArea)
                    m.Result = (IntPtr)HTTOP;
                else if (cursor.Y > this.Height - resizeArea)
                    m.Result = (IntPtr)HTBOTTOM;
            }
            return;
        }
        base.WndProc(ref m);
    }

    // ----------------------
    // Border Painting
    // ----------------------
    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.Clear(this.BackColor);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (ShowBorder)
        {
            using (Pen borderPen = new Pen(BorderColor, BorderThickness))
            {
                int offset = BorderThickness / 2;
                e.Graphics.DrawRectangle(
                    borderPen,
                    offset,
                    offset,
                    this.ClientSize.Width - BorderThickness,
                    this.ClientSize.Height - BorderThickness
                );
            }
        }
    }

}
