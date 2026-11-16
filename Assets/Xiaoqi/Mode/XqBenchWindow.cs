using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using Xiaoqi.Host;
using Debug = UnityEngine.Debug;

namespace Xiaoqi.Mode
{
    /// <summary>
    /// 工作台窗口（P3）：开一个独立的 Edge 应用窗口，网页是服务器的 /p/bench；她坐在窗口标题栏上并跟随；
    /// 关掉窗口（或 XqMode 切走）她回到进入前的位置和大小。
    ///
    /// 要点（都来自 G59-P3 与 FACTS-P3-2a）：
    /// - Edge 必须带独立 --user-data-dir，否则会并入已开着的 Edge、本进程立刻退出，拿不到窗口也判断不了关闭。
    /// - 位置不信 --window-position（缩放下是 DIP 还是物理像素没实测），窗口出来后用 SetWindowPos 按物理像素摆。
    /// - 坐窗口走 ME 现有机制：AvatarWindowHandler.XqSitOn（// XQ: 新增的一个公开入口），之后 ME 每帧自己跟随。
    /// - 用户把她拖离窗口后，松手 1.5 秒会再坐回去；窗口最小化 / 最大化时不坐（ME 自己会下窗），还原后坐回。
    /// </summary>
    public class XqBenchWindow : MonoBehaviour
    {
        private static XqBenchWindow _i;
        public static bool IsOpen { get { return _i != null && _i._open; } }
        public static IntPtr Hwnd { get { return _i != null ? _i._hwnd : IntPtr.Zero; } }

        public static bool Open() { return _i != null && _i.DoOpen(); }
        public static void Close() { if (_i != null) _i.DoClose(); }

        // ---- Win32 ----
        [StructLayout(LayoutKind.Sequential)] private struct RECT { public int left, top, right, bottom; }
        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO { public int cbSize; public RECT rcMonitor, rcWork; public uint dwFlags; }
        private delegate bool EnumProc(IntPtr h, IntPtr lp);
        [DllImport("user32.dll")] private static extern bool EnumWindows(EnumProc cb, IntPtr lp);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr h, StringBuilder sb, int n);
        [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr h);
        [DllImport("user32.dll")] private static extern int GetWindowTextLength(IntPtr h);
        [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr h, out RECT r);
        [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int w, int hh, uint flags);
        [DllImport("user32.dll")] private static extern bool MoveWindow(IntPtr h, int x, int y, int w, int hh, bool repaint);
        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr h);
        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr h, int cmd);
        [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr h);
        [DllImport("user32.dll")] private static extern bool IsZoomed(IntPtr h);
        [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr h);
        [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr h);
        [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr h, uint flags);
        [DllImport("user32.dll")] private static extern bool GetMonitorInfo(IntPtr mon, ref MONITORINFO mi);
        private const uint SWP_NOZORDER = 0x4, SWP_NOACTIVATE = 0x10;
        private const uint MONITOR_DEFAULTTONEAREST = 2;
        private const int SW_RESTORE = 9;

        // ---- 状态 ----
        private Process _proc;
        private IntPtr _hwnd = IntPtr.Zero;
        private bool _open;
        private bool _closingByUs;
        private bool _attaching;
        private float _lastSit = -10f;
        private float _nextCheck;

        // 进入前的状态（退出时原样恢复）
        private RECT _savedRect;
        private bool _savedRectOk;
        private float _savedSize = -1f;
        private bool _savedSitEnabled;
        private bool _sitFlagChanged;

        private IntPtr _unityHwnd = IntPtr.Zero;
        private AvatarWindowHandler _sit;

        private void Awake()
        {
            _i = this;
        }

        private IntPtr UnityHwnd()
        {
            if (_unityHwnd == IntPtr.Zero) _unityHwnd = Process.GetCurrentProcess().MainWindowHandle;
            return _unityHwnd;
        }

        private static string FindEdge()
        {
            var cfgPath = XqModeConfig.I.edge_path;
            if (!string.IsNullOrEmpty(cfgPath) && File.Exists(cfgPath)) return cfgPath;
            string[] roots =
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            };
            foreach (var r in roots)
            {
                if (string.IsNullOrEmpty(r)) continue;
                var p = Path.Combine(r, "Microsoft", "Edge", "Application", "msedge.exe");
                if (File.Exists(p)) return p;
            }
            return null;
        }

        private static void Fail(string log)
        {
            Debug.Log("XQ_BENCH fail: " + log);
            Xiaoqi.Spike.PetBubble.Enqueue("打不开工作台", "");
        }

        // =============== 打开 ===============
        private bool DoOpen()
        {
            if (_open && _proc != null && !_proc.HasExited)
            {
                if (_hwnd != IntPtr.Zero) { if (IsIconic(_hwnd)) ShowWindow(_hwnd, SW_RESTORE); SetForegroundWindow(_hwnd); }
                return true; // 已开着 = 前置，不重复开
            }
            if (_attaching) return true;

            var edge = FindEdge();
            if (edge == null) { Fail("msedge.exe not found"); return false; }

            var cfg = XiaoqiRuntime.Config;
            string server = cfg != null ? (cfg.server ?? "").TrimEnd('/') : "";
            if (server.Length == 0) { Fail("no server in config"); return false; }
            string token = cfg.token ?? "";
            var mc = XqModeConfig.I;
            string url = server + "/p/bench#t=" + token + "&theme=" + mc.bench_theme;

            // 她所在的显示器（物理像素）
            RECT mon = new RECT { left = 0, top = 0, right = Screen.currentResolution.width, bottom = Screen.currentResolution.height };
            var uh = UnityHwnd();
            if (uh != IntPtr.Zero)
            {
                var m = new MONITORINFO { cbSize = Marshal.SizeOf(typeof(MONITORINFO)) };
                if (GetMonitorInfo(MonitorFromWindow(uh, MONITOR_DEFAULTTONEAREST), ref m)) mon = m.rcMonitor;
                _savedRectOk = GetWindowRect(uh, out _savedRect);
            }
            int mw = mon.right - mon.left, mh = mon.bottom - mon.top;
            int w = Mathf.Max(mc.bench_min_w, Mathf.RoundToInt(mw * mc.bench_w_pct));
            int h = Mathf.Max(mc.bench_min_h, Mathf.RoundToInt(mh * mc.bench_h_pct));
            int x = mon.left + Mathf.RoundToInt(mw * mc.bench_left_pct);
            int y = mon.top + Mathf.RoundToInt(mh * mc.bench_top_pct);

            string profile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Xiaoqi", "edge-profile");
            try { Directory.CreateDirectory(profile); } catch (Exception) { }

            var psi = new ProcessStartInfo
            {
                FileName = edge,
                Arguments = "--app=\"" + url + "\" --user-data-dir=\"" + profile + "\""
                    + " --window-position=" + x + "," + y + " --window-size=" + w + "," + h
                    + " --no-first-run --no-default-browser-check",
                UseShellExecute = false,
            };
            try { _proc = Process.Start(psi); }
            catch (Exception e) { Fail("start: " + e.Message); return false; }
            if (_proc == null) { Fail("start returned null"); return false; }

            Debug.Log("XQ_BENCH start pid=" + _proc.Id + " want=" + x + "," + y + " " + w + "x" + h + " (token hidden)");
            _closingByUs = false;
            _attaching = true;
            StartCoroutine(Attach(x, y, w, h));
            return true;
        }

        private IEnumerator Attach(int x, int y, int w, int h)
        {
            float t0 = Time.realtimeSinceStartup;
            IntPtr hwnd = IntPtr.Zero;
            while (Time.realtimeSinceStartup - t0 < 5f)
            {
                try
                {
                    if (_proc.HasExited) break;
                    _proc.Refresh();
                    hwnd = _proc.MainWindowHandle;
                }
                catch (Exception) { }
                if (hwnd == IntPtr.Zero) hwnd = FindByPid(_proc.Id);
                if (hwnd != IntPtr.Zero) break;
                yield return new WaitForSecondsRealtime(0.1f);
            }
            _attaching = false;

            if (_closingByUs) { KillProc(); yield break; } // 还没摆好就被切走了

            if (hwnd == IntPtr.Zero)
            {
                Fail("no window within 5s");
                KillProc();
                XqMode.BenchClosedByUser();
                yield break;
            }

            _hwnd = hwnd;
            // 最小尺寸 900x560 是 DIP（G59-P3 §3.2、§4.3），换成物理像素：175% 下 900 DIP = 1575 px
            uint dpi = 0;
            try { dpi = GetDpiForWindow(hwnd); } catch (Exception) { }
            float scale = dpi > 0 ? dpi / 96f : 1f;
            var mc0 = XqModeConfig.I;
            w = Mathf.Max(w, Mathf.RoundToInt(mc0.bench_min_w * scale));
            h = Mathf.Max(h, Mathf.RoundToInt(mc0.bench_min_h * scale));
            Debug.Log("XQ_BENCH dpi=" + dpi + " scale=" + scale.ToString("0.00") + " size=" + w + "x" + h);
            // 按物理像素摆（不信 --window-position；实测值记在日志里，给 FACTS 第 6 条用）
            RECT before; GetWindowRect(hwnd, out before);
            SetWindowPos(hwnd, IntPtr.Zero, x, y, w, h, SWP_NOZORDER | SWP_NOACTIVATE);
            RECT after; GetWindowRect(hwnd, out after);
            Debug.Log("XQ_BENCH placed before=" + Fmt(before) + " after=" + Fmt(after) + " want=" + x + "," + y + " " + w + "x" + h);
            SetForegroundWindow(hwnd);

            var mc = XqModeConfig.I;
            if (mc.bench_pet_size > 0f && PetHost.I != null)
            {
                _savedSize = PetHost.I.Size;
                PetHost.I.Size = mc.bench_pet_size;
            }
            if (SaveLoadHandler.Instance != null && !SaveLoadHandler.Instance.data.enableWindowSitting)
            {
                _savedSitEnabled = false; _sitFlagChanged = true;
                SaveLoadHandler.Instance.data.enableWindowSitting = true; // 只改内存，不 Save；退出时还原
            }

            yield return new WaitForSecondsRealtime(0.3f);
            _open = true;
            TrySit(true);
            XqEventSink.Report("window", "bench_open");
        }

        private static string Fmt(RECT r) { return r.left + "," + r.top + " " + (r.right - r.left) + "x" + (r.bottom - r.top); }

        private static IntPtr FindByPid(int pid)
        {
            IntPtr found = IntPtr.Zero;
            var sb = new StringBuilder(64);
            EnumWindows((h, lp) =>
            {
                uint p; GetWindowThreadProcessId(h, out p);
                if (p != (uint)pid || !IsWindowVisible(h) || GetWindowTextLength(h) == 0) return true;
                sb.Length = 0; GetClassName(h, sb, 64);
                if (sb.ToString() != "Chrome_WidgetWin_1") return true;
                found = h;
                return false;
            }, IntPtr.Zero);
            return found;
        }

        // =============== 坐上去 / 跟随 ===============
        private float SitFraction()
        {
            var mc = XqModeConfig.I;
            RECT r; if (!GetWindowRect(_hwnd, out r)) return mc.bench_sit_pct;
            int w = Mathf.Max(1, r.right - r.left);
            float f = w < 900 ? 0.45f : mc.bench_sit_pct;
            float maxF = (w - mc.bench_sit_right_gap) / (float)w; // 不压右上角三个系统按钮
            return Mathf.Clamp(Mathf.Min(f, maxF), 0.2f, 0.9f);
        }

        private void TrySit(bool log)
        {
            if (_sit == null) _sit = FindFirstObjectByType<AvatarWindowHandler>(FindObjectsInactive.Include);
            if (_sit == null) { if (log) Debug.Log("XQ_HOST_MISS AvatarWindowHandler"); return; }
            if (_hwnd == IntPtr.Zero || !IsWindow(_hwnd)) return;
            _lastSit = Time.unscaledTime;
            bool ok = _sit.XqSitOn(_hwnd, SitFraction());
            if (log) Debug.Log("XQ_BENCH sit ok=" + ok + " frac=" + SitFraction().ToString("0.00"));
        }

        private void Update()
        {
            if (!_open || _proc == null) return;
            if (Time.unscaledTime < _nextCheck) return;
            _nextCheck = Time.unscaledTime + 0.25f;

            bool exited;
            try { exited = _proc.HasExited; } catch (Exception) { exited = true; }
            if (exited) { OnClosed("process exited"); return; }

            // 被拖离 / 被 ME 放弃后，坐回去（最小化、最大化时不坐）
            if (_sit != null && _sit.XqSnappedHwnd != _hwnd
                && PetHost.I != null && !PetHost.I.IsDragging
                && !IsIconic(_hwnd) && !IsZoomed(_hwnd)
                && Time.unscaledTime - _lastSit > 1.5f)
            {
                TrySit(false);
            }
        }

        // =============== 关闭 ===============
        private void OnClosed(string why)
        {
            Debug.Log("XQ_BENCH closed: " + why);
            bool byUs = _closingByUs;
            Restore();
            XqEventSink.Report("window", "bench_close");
            if (!byUs) XqMode.BenchClosedByUser();
        }

        private void DoClose()
        {
            if (!_open && _proc == null) return;
            _closingByUs = true;
            try
            {
                if (_proc != null && !_proc.HasExited)
                {
                    _proc.CloseMainWindow();
                    StartCoroutine(KillLater(_proc));
                }
            }
            catch (Exception) { }
            if (_open) OnClosed("closed by mode");
        }

        private IEnumerator KillLater(Process p)
        {
            yield return new WaitForSecondsRealtime(1.0f);
            try { if (p != null && !p.HasExited) p.Kill(); } catch (Exception) { }
        }

        private void KillProc()
        {
            try { if (_proc != null && !_proc.HasExited) _proc.Kill(); } catch (Exception) { }
        }

        private void Restore()
        {
            _open = false;
            _hwnd = IntPtr.Zero;
            if (_sit != null) _sit.XqSitOff();
            if (_sitFlagChanged && SaveLoadHandler.Instance != null)
                SaveLoadHandler.Instance.data.enableWindowSitting = _savedSitEnabled;
            _sitFlagChanged = false;
            if (_savedSize > 0f && PetHost.I != null) PetHost.I.Size = _savedSize;
            _savedSize = -1f;
            var uh = UnityHwnd();
            if (_savedRectOk && uh != IntPtr.Zero)
            {
                MoveWindow(uh, _savedRect.left, _savedRect.top, _savedRect.right - _savedRect.left, _savedRect.bottom - _savedRect.top, true);
                Debug.Log("XQ_BENCH restore pet window " + Fmt(_savedRect));
            }
            _savedRectOk = false;
            _proc = null;
        }

        private void OnApplicationQuit()
        {
            KillProc();
        }
    }
}
