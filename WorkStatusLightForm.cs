using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace WorkStatusLight;

public enum WorkStatusLightState
{
    Red,
    RedBlink,
    Yellow,
    YellowBlink,
    Green,
    GreenBlink,
    Off
}

public class WorkStatusLightForm : Form
{
    private WorkStatusLightState _state = WorkStatusLightState.Off;
    private bool _blinkVisible = true;
    private readonly System.Windows.Forms.Timer _blinkTimer;
    private Point _dragOffset;
    private bool _dragging;
    private bool _isAnimating;

    private readonly Image _defaultImg;
    private readonly Image _redImg;
    private readonly Image _greenImg;
    private readonly Image _yellowImg;

    public WorkStatusLightForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        ClientSize = new Size(150, 57);
        ShowInTaskbar = false;

        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.DoubleBuffer, true);

        var assetsDir = Path.Combine(AppContext.BaseDirectory, "assets");
        _defaultImg = Image.FromFile(Path.Combine(assetsDir, "default.png"));
        _redImg = Image.FromFile(Path.Combine(assetsDir, "red.png"));
        _greenImg = Image.FromFile(Path.Combine(assetsDir, "green.png"));
        _yellowImg = Image.FromFile(Path.Combine(assetsDir, "yellow.png"));

        PositionOnScreen();

        _blinkTimer = new System.Windows.Forms.Timer { Interval = 500 };
        _blinkTimer.Tick += (_, _) =>
        {
            if (IsBlinking)
            {
                _blinkVisible = !_blinkVisible;
                RefreshLayered();
            }
        };
        _blinkTimer.Start();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        PlayStartupAnimation();
    }

    private bool IsBlinking => _state is WorkStatusLightState.RedBlink
                                            or WorkStatusLightState.YellowBlink
                                            or WorkStatusLightState.GreenBlink;

    private void PlayStartupAnimation()
    {
        _isAnimating = true;
        ApplyState(WorkStatusLightState.Red);
        var step = 0;
        var animTimer = new System.Windows.Forms.Timer { Interval = 500 };
        animTimer.Tick += (_, _) =>
        {
            step++;
            switch (step)
            {
                case 1:
                    ApplyState(WorkStatusLightState.Yellow);
                    break;
                case 2:
                    ApplyState(WorkStatusLightState.Green);
                    break;
                case 3:
                    animTimer.Stop();
                    animTimer.Dispose();
                    _isAnimating = false;
                    break;
            }
        };
        animTimer.Start();
    }

    private void ApplyState(WorkStatusLightState state)
    {
        _state = state;
        _blinkVisible = true;
        RefreshLayered();
    }

    private void PositionOnScreen()
    {
        var screen = Screen.PrimaryScreen?.WorkingArea ?? Screen.AllScreens[0].WorkingArea;
        Location = new Point(screen.Right - Width - 30, screen.Top + 50);
    }

    public void SetState(WorkStatusLightState state)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => SetState(state));
            return;
        }
        if (_isAnimating) return;
        ApplyState(state);
    }

    public WorkStatusLightState CurrentState => _state;

    private Image GetImageForCurrentState()
    {
        if (IsBlinking && !_blinkVisible)
            return _defaultImg;

        return _state switch
        {
            WorkStatusLightState.Red or WorkStatusLightState.RedBlink => _redImg,
            WorkStatusLightState.Yellow or WorkStatusLightState.YellowBlink => _yellowImg,
            WorkStatusLightState.Green or WorkStatusLightState.GreenBlink => _greenImg,
            _ => _defaultImg,
        };
    }

    private void RefreshLayered()
    {
        if (!IsHandleCreated) return;

        var img = GetImageForCurrentState();

        using var bmp = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.Transparent);
            g.DrawImage(img, 0, 0, Width, Height);
        }

        var screenPt = PointToScreen(Point.Empty);
        var hdcSrc = NativeMethods.CreateCompatibleDC(IntPtr.Zero);
        var hBmp = bmp.GetHbitmap(Color.FromArgb(0));
        var oldBmp = NativeMethods.SelectObject(hdcSrc, hBmp);

        var pptDst = new NativeMethods.POINT { x = screenPt.X, y = screenPt.Y };
        var size = new NativeMethods.SIZE { cx = Width, cy = Height };
        var pptSrc = new NativeMethods.POINT { x = 0, y = 0 };
        var blend = new NativeMethods.BLENDFUNCTION
        {
            BlendOp = 0,
            BlendFlags = 0,
            SourceConstantAlpha = 255,
            AlphaFormat = 1
        };

        NativeMethods.UpdateLayeredWindow(Handle, IntPtr.Zero, ref pptDst, ref size,
            hdcSrc, ref pptSrc, 0, ref blend, 2);

        NativeMethods.SelectObject(hdcSrc, oldBmp);
        NativeMethods.DeleteObject(hBmp);
        NativeMethods.DeleteDC(hdcSrc);
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= 0x00080000;
            return cp;
        }
    }

    protected override void OnPaint(PaintEventArgs e) { }

    protected override void OnMove(EventArgs e)
    {
        base.OnMove(e);
        if (IsHandleCreated) RefreshLayered();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left)
        {
            _dragging = true;
            _dragOffset = e.Location;
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_dragging)
            Location = new Point(Location.X + e.X - _dragOffset.X,
                                 Location.Y + e.Y - _dragOffset.Y);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _dragging = false;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _blinkTimer.Dispose();
            _defaultImg.Dispose();
            _redImg.Dispose();
            _greenImg.Dispose();
            _yellowImg.Dispose();
        }
        base.Dispose(disposing);
    }
}

internal static class NativeMethods
{
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool UpdateLayeredWindow(
        IntPtr hwnd, IntPtr hdcDst, ref POINT pptDst, ref SIZE psize,
        IntPtr hdcSrc, ref POINT pptSrc, int crKey, ref BLENDFUNCTION pblend, int dwFlags);

    [DllImport("gdi32.dll")]
    public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    public static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    public static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

    [DllImport("gdi32.dll")]
    public static extern bool DeleteObject(IntPtr hObject);

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT { public int x, y; }

    [StructLayout(LayoutKind.Sequential)]
    public struct SIZE { public int cx, cy; }

    [StructLayout(LayoutKind.Sequential)]
    public struct BLENDFUNCTION
    {
        public byte BlendOp, BlendFlags, SourceConstantAlpha, AlphaFormat;
    }
}
