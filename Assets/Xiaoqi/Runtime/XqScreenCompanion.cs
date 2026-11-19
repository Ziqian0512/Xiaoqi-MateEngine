using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// 未来多模态看图分析的扩展接口（预留：以后接本地/云端 VLM 截图理解）。
/// </summary>
public interface IXqVisionAnalyzer
{
    IEnumerator AnalyzeScreen(Action<string> onComment);
}

/// <summary>
/// 陪我看屏幕：读取当前前台窗口标题与进程名（跳过隐私词与自身窗口），
/// 推送到云端 /api/screen/push（见 relay/screen.py），并把结果反馈给界面气泡/提示。
/// </summary>
public static class XqScreenCompanion
{
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    static readonly string[] PrivacyWords = { "密码", "登录", "password", "1password", "keepass", "银行", "bank", "支付" };
    static string _lastTitle = "";
    static string _lastApp = "";

    public static bool TryCaptureForeground(out string title, out string app)
    {
        title = "";
        app = "";
        try
        {
            var fg = GetForegroundWindow();
            if (fg == IntPtr.Zero) return false;
            GetWindowThreadProcessId(fg, out uint pid);
            if (pid == (uint)Process.GetCurrentProcess().Id)
            {
                title = _lastTitle;
                app = _lastApp;
                return !string.IsNullOrEmpty(title);
            }
            var sb = new StringBuilder(256);
            GetWindowText(fg, sb, sb.Capacity);
            title = (sb.ToString() ?? "").Trim();
            try
            {
                using (var proc = Process.GetProcessById((int)pid))
                    app = proc.ProcessName ?? "";
            }
            catch (Exception) { }
            if (!string.IsNullOrEmpty(title) && !IsPrivate(title, app))
            {
                _lastTitle = title;
                _lastApp = app;
            }
            if (IsPrivate(title, app)) title = "[隐私窗口已跳过]";
            return !string.IsNullOrEmpty(title);
        }
        catch (Exception) { return false; }
    }

    static bool IsPrivate(string title, string app)
    {
        string hay = ((title ?? "") + " " + (app ?? "")).ToLowerInvariant();
        for (int i = 0; i < PrivacyWords.Length; i++)
            if (hay.Contains(PrivacyWords[i])) return true;
        return false;
    }

    public static void PushNow(MonoBehaviour host, Action<string> done)
    {
        if (host == null) { done?.Invoke("没有协程宿主"); return; }
        host.StartCoroutine(PushCo(done));
    }

    static IEnumerator PushCo(Action<string> done)
    {
        var cfg = XiaoqiRuntime.Config ?? XiaoqiConfig.LoadOrCreate();
        string server = cfg != null ? (cfg.server ?? "").TrimEnd('/') : "";
        if (string.IsNullOrEmpty(server)) { done?.Invoke("未配置服务器"); yield break; }

        string title, app;
        if (!TryCaptureForeground(out title, out app))
        {
            title = "桌面空闲";
            app = "explorer";
        }
        var payload = new JObject
        {
            ["title"] = title,
            ["app"] = app,
            ["idle_sec"] = 0
        };
        XqHttp.Resp resp = default(XqHttp.Resp);
        yield return XqHttp.PostJson(server + "/api/screen/push", cfg.token, payload.ToString(Newtonsoft.Json.Formatting.None), 6, r => resp = r);
        if (!resp.ok)
        {
            done?.Invoke("推送失败 HTTP " + resp.code);
            yield break;
        }
        try
        {
            var j = JObject.Parse(resp.text);
            bool on = j["on"] != null && (bool)j["on"];
            string msg = on
                ? ("小祈已看到：" + (title.Length > 18 ? title.Substring(0, 18) + "…" : title))
                : "已推过，但云端屏幕陪伴总开关未开";
            UnityEngine.Debug.Log("XQ_SCREEN push ok on=" + on + " app=" + app);
            done?.Invoke(msg);
        }
        catch (Exception)
        {
            done?.Invoke("小祈已收到当前窗口");
        }
    }
}
