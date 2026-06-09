using System.Drawing;
using System.Drawing.Drawing2D;
using System.Net;
using System.Text;

namespace WorkStatusLight;

public class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly WorkStatusLightForm _form;
    private HttpListener? _httpListener;
    private Thread? _httpThread;

    public TrayApplicationContext()
    {
        _form = new WorkStatusLightForm();

        _trayIcon = new NotifyIcon
        {
            Icon = CreateTrayIcon(),
            Visible = true,
            Text = "工作状态指示"
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add("显示(&S)", null, (_, _) => ShowForm());
        menu.Items.Add("隐藏(&H)", null, (_, _) => _form.Hide());
        menu.Items.Add("关于(&A)", null, (_, _) => ShowAbout());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("退出(&X)", null, (_, _) => ExitApp());
        _trayIcon.ContextMenuStrip = menu;
        _trayIcon.DoubleClick += (_, _) =>
        {
            if (_form.Visible) _form.Hide();
            else ShowForm();
        };

        StartHttp();
        ShowForm();
    }

    private void ShowForm()
    {
        _form.Show();
        _form.BringToFront();
    }

    private static Icon CreateTrayIcon()
    {
        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            using var bgPath = new GraphicsPath();
            int d = 10;
            var r = new Rectangle(2, 2, 28, 28);
            bgPath.AddArc(r.X, r.Y, d, d, 180, 90);
            bgPath.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            bgPath.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            bgPath.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            bgPath.CloseFigure();
            using (var bg = new SolidBrush(Color.Black))
                g.FillPath(bg, bgPath);

            using (var br = new SolidBrush(Color.FromArgb(220, 40, 40)))
                g.FillEllipse(br, 8, 8, 16, 16);
        }
        return Icon.FromHandle(bmp.GetHicon());
    }

    private void ShowAbout()
    {
        var sb = new StringBuilder();
        sb.AppendLine("交通信号灯 v2.0");
        sb.AppendLine();
        sb.AppendLine("作者：潍坊辉越软件 (www.nextdreaminc.cn)");
        sb.AppendLine();
        sb.AppendLine("════════════════════════════════");
        sb.AppendLine("  Curl 控制指令说明");
        sb.AppendLine("════════════════════════════════");
        sb.AppendLine();
        sb.AppendLine("【红灯常亮】");
        sb.AppendLine("  curl http://localhost:8800/red");
        sb.AppendLine();
        sb.AppendLine("【红灯闪烁】");
        sb.AppendLine("  curl http://localhost:8800/red-blink");
        sb.AppendLine();
        sb.AppendLine("【黄灯常亮】");
        sb.AppendLine("  curl http://localhost:8800/yellow");
        sb.AppendLine();
        sb.AppendLine("【黄灯闪烁】");
        sb.AppendLine("  curl http://localhost:8800/yellow-blink");
        sb.AppendLine();
        sb.AppendLine("【绿灯常亮】");
        sb.AppendLine("  curl http://localhost:8800/green");
        sb.AppendLine();
        sb.AppendLine("【绿灯闪烁】");
        sb.AppendLine("  curl http://localhost:8800/green-blink");
        sb.AppendLine();
        sb.AppendLine("【全熄灭】");
        sb.AppendLine("  curl http://localhost:8800/off");
        sb.AppendLine();
        sb.AppendLine("【查询当前状态】");
        sb.AppendLine("  curl http://localhost:8800/status");
        sb.AppendLine();
        sb.AppendLine("════════════════════════════════");
        sb.AppendLine("  注意事项");
        sb.AppendLine("════════════════════════════════");
        sb.AppendLine("· 启动动画：红→黄→绿 各0.5秒");
        sb.AppendLine("· 动画结束后默认绿灯常亮");
        sb.AppendLine("· 窗口可拖拽移动位置");

        MessageBox.Show(sb.ToString(), "关于 - 交通信号灯",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void StartHttp()
    {
        try
        {
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add("http://localhost:8800/");
            _httpListener.Start();

            _httpThread = new Thread(HttpLoop) { IsBackground = true };
            _httpThread.Start();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"HTTP 服务启动失败：{ex.Message}\n请检查 8800 端口是否被占用。",
                            "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void HttpLoop()
    {
        while (_httpListener is { IsListening: true })
        {
            try
            {
                var ctx = _httpListener.GetContext();
                HandleRequest(ctx);
            }
            catch (HttpListenerException) { break; }
            catch { /* ignore */ }
        }
    }

    private void HandleRequest(HttpListenerContext ctx)
    {
        var path = ctx.Request.Url?.AbsolutePath.TrimStart('/').ToLowerInvariant() ?? "";
        string json;
        int code = 200;

        switch (path)
        {
            case "red":
                _form.SetState(WorkStatusLightState.Red);
                json = """{"status":"ok","light":"red"}""";
                break;
            case "red-blink":
                _form.SetState(WorkStatusLightState.RedBlink);
                json = """{"status":"ok","light":"red-blink"}""";
                break;
            case "yellow":
                _form.SetState(WorkStatusLightState.Yellow);
                json = """{"status":"ok","light":"yellow"}""";
                break;
            case "yellow-blink":
                _form.SetState(WorkStatusLightState.YellowBlink);
                json = """{"status":"ok","light":"yellow-blink"}""";
                break;
            case "green":
                _form.SetState(WorkStatusLightState.Green);
                json = """{"status":"ok","light":"green"}""";
                break;
            case "green-blink":
                _form.SetState(WorkStatusLightState.GreenBlink);
                json = """{"status":"ok","light":"green-blink"}""";
                break;
            case "off":
                _form.SetState(WorkStatusLightState.Off);
                json = """{"status":"ok","light":"off"}""";
                break;
            case "status":
                json = $$"""{"status":"ok","light":"{{_form.CurrentState.ToString().ToLowerInvariant()}}"}""";
                break;
            default:
                code = 404;
                json = """{"status":"error","message":"Use /red, /red-blink, /yellow, /yellow-blink, /green, /green-blink, /off, or /status"}""";
                break;
        }

        ctx.Response.StatusCode = code;
        ctx.Response.ContentType = "application/json";
        ctx.Response.Headers.Add("Access-Control-Allow-Origin", "*");
        var buf = Encoding.UTF8.GetBytes(json);
        ctx.Response.ContentLength64 = buf.Length;
        ctx.Response.OutputStream.Write(buf, 0, buf.Length);
        ctx.Response.Close();
    }

    private void ExitApp()
    {
        try { _httpListener?.Stop(); _httpListener?.Close(); } catch { }
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _form.Close();
        _form.Dispose();
        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _trayIcon.Dispose();
            _form.Dispose();
            _httpListener?.Close();
        }
        base.Dispose(disposing);
    }
}
