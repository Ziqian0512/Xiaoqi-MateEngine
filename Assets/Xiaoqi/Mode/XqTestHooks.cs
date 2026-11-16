using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using Xiaoqi.Host;

namespace Xiaoqi.Mode
{
    /// <summary>
    /// 无人值守验收用的启动参数钩子（默认不启用，只在命令行带参数时才建）：
    ///   -xq-sit-test[=标题片段]   启动 12 秒后，把她直接坐到一个别的程序的可见窗口上（不靠拖拽），5 秒后再「下窗」。
    ///   -xq-close-test            启动 12 秒后 Pet→Close→Pet 连做 3 轮，每轮打窗口位置和她的位置，最后给出漂移量。
    ///   -xq-nap-test              启动 12 秒后调用「睡一会」(0.5 分钟)，6 秒后「叫醒」。
    /// 日志前缀 XQ_TEST。
    /// </summary>
    public class XqTestHooks : MonoBehaviour
    {
        [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc cb, IntPtr lp);
        delegate bool EnumProc(IntPtr h, IntPtr lp);
        [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
        [DllImport("user32.dll")] static extern bool IsIconic(IntPtr h);
        [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr h, out R r);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetWindowText(IntPtr h, StringBuilder sb, int n);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassName(IntPtr h, StringBuilder sb, int n);
        [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
        [StructLayout(LayoutKind.Sequential)] struct R { public int l, t, r, b; }
        [StructLayout(LayoutKind.Sequential)] struct MI { public int cb; public R mon; public R work; public uint flags; }
        [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "FindWindowW")] static extern IntPtr FindWindowW(string cls, string title);
        [DllImport("user32.dll")] static extern IntPtr MonitorFromWindow(IntPtr h, uint flags);
        [DllImport("user32.dll")] static extern bool GetMonitorInfo(IntPtr m, ref MI mi);
        [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int w, int hh, uint flags);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            string sit = null; bool nap = false, close = false, snap = false;
            foreach (var a in Environment.GetCommandLineArgs())
            {
                if (a == "-xq-sit-test") sit = "";
                else if (a.StartsWith("-xq-sit-test=")) sit = a.Substring("-xq-sit-test=".Length);
                else if (a == "-xq-nap-test") nap = true;
                else if (a == "-xq-close-test") close = true;
                else if (a == "-xq-snap-mode") snap = true; // XQ: 默认关；仅 R-3 位置快照验收启用
            }
            if (sit == null && !nap && !close && !snap) return;
            var go = new GameObject("XqTestHooks");
            DontDestroyOnLoad(go);
            var hooks = go.AddComponent<XqTestHooks>();
            if (snap) hooks.StartCoroutine(hooks.RunSnapMode());
            if (close) hooks.StartCoroutine(hooks.RunClose());
            else if (sit != null || nap) hooks.StartCoroutine(hooks.Run(sit, nap));
        }

        // XQ: P2 R-3 四种真实退出前后的位置快照与漂移统计（-xq-snap-mode 才启用）
        IEnumerator RunSnapMode()
        {
            while (PetHost.I == null || !PetHost.I.Ready) yield return null;
            var uh = FindWindowW("UnityWndClass", "小祈");
            if (uh == IntPtr.Zero) uh = FindWindowW("UnityWndClass", null);
            R r0 = default(R);
            Rect b0 = default(Rect);
            bool hasBefore = false;
            XqMode.OnModeChanged += (from, to) =>
            {
                if (from == XqView.Pet && to == XqView.Close)
                {
                    GetWindowRect(uh, out r0);
                    b0 = PetHost.I != null ? PetHost.I.ScreenRect() : default(Rect);
                    hasBefore = true;
                    Debug.Log("XQ_TEST snap before " + Snap(uh));
                }
                else if (from == XqView.Close && to == XqView.Pet && hasBefore)
                {
                    StartCoroutine(SnapExitAfterSettle(uh, r0, b0));
                }
            };
            XqMode.OnCloseReady += () => StartCoroutine(SnapCloseAfterSettle(uh));
        }

        IEnumerator SnapCloseAfterSettle(IntPtr uh)
        {
            yield return new WaitForSecondsRealtime(1.2f);
            Debug.Log("XQ_TEST snap close " + Snap(uh));
        }

        IEnumerator SnapExitAfterSettle(IntPtr uh, R r0, Rect b0)
        {
            while (XqMode.Busy) yield return null;
            yield return new WaitForSecondsRealtime(1.0f);
            R r1; GetWindowRect(uh, out r1);
            var b1 = PetHost.I != null ? PetHost.I.ScreenRect() : default(Rect);
            Debug.Log("XQ_TEST snap after " + Snap(uh));
            Debug.Log("XQ_TEST snap drift win=" + (r1.l - r0.l) + "," + (r1.t - r0.t) + " size=" + ((r1.r - r1.l) - (r0.r - r0.l)) + "," + ((r1.b - r1.t) - (r0.b - r0.t))
                + " bodyCx=" + (b1.center.x - b0.center.x).ToString("0") + " bodyBottom=" + (b1.yMin - b0.yMin).ToString("0") + " bodyH=" + (b1.height - b0.height).ToString("0"));
        }

        static string Snap(IntPtr uh)
        {
            R ur; GetWindowRect(uh, out ur);
            var b = PetHost.I != null ? PetHost.I.ScreenRect() : default(Rect);
            return "win=" + ur.l + "," + ur.t + " " + (ur.r - ur.l) + "x" + (ur.b - ur.t)
                + " body=" + b.xMin.ToString("0") + ".." + b.xMax.ToString("0") + " y=" + b.yMin.ToString("0") + ".." + b.yMax.ToString("0")
                + " h=" + b.height.ToString("0") + " mode=" + XqMode.Current;
        }

        IEnumerator RunClose()
        {
            yield return new WaitForSecondsRealtime(12f);
            var uh = FindWindowW("UnityWndClass", "小祈");
            string before = Snap(uh);
            Debug.Log("XQ_TEST close before " + before);
            R r0; GetWindowRect(uh, out r0);
            var b0 = PetHost.I != null ? PetHost.I.ScreenRect() : default(Rect);
            for (int i = 1; i <= 3; i++)
            {
                bool ok = XqMode.Set(XqView.Close);
                Debug.Log("XQ_TEST close round=" + i + " enter accepted=" + ok);
                float t0 = Time.unscaledTime;
                while (XqMode.Busy && Time.unscaledTime - t0 < 6f) yield return null;
                Debug.Log("XQ_TEST close round=" + i + " entered in=" + (Time.unscaledTime - t0).ToString("0.00") + "s " + Snap(uh));
                yield return new WaitForSecondsRealtime(4f);
                Debug.Log("XQ_TEST close round=" + i + " hold4s " + Snap(uh));
                ok = XqMode.Set(XqView.Pet);
                t0 = Time.unscaledTime;
                while (XqMode.Busy && Time.unscaledTime - t0 < 6f) yield return null;
                yield return new WaitForSecondsRealtime(1f);
                Debug.Log("XQ_TEST close round=" + i + " exited accepted=" + ok + " " + Snap(uh));
            }
            R r1; GetWindowRect(uh, out r1);
            var b1 = PetHost.I != null ? PetHost.I.ScreenRect() : default(Rect);
            Debug.Log("XQ_TEST close drift win=" + (r1.l - r0.l) + "," + (r1.t - r0.t) + " size=" + ((r1.r - r1.l) - (r0.r - r0.l)) + "," + ((r1.b - r1.t) - (r0.b - r0.t))
                + " bodyCx=" + (b1.center.x - b0.center.x).ToString("0") + " bodyBottom=" + (b1.yMin - b0.yMin).ToString("0") + " bodyH=" + (b1.height - b0.height).ToString("0"));
        }

        IEnumerator Run(string sit, bool nap)
        {
            yield return new WaitForSecondsRealtime(12f);
            if (nap)
            {
                Debug.Log("XQ_TEST nap start");
                Xiaoqi.Spike.PetPresence.Nap(0.5f);
                yield return new WaitForSecondsRealtime(3f);
                Debug.Log("XQ_TEST nap asleep=" + Xiaoqi.Spike.PetPresence.IsAsleep);
                yield return new WaitForSecondsRealtime(3f);
                Xiaoqi.Spike.PetPresence.Wake();
                Debug.Log("XQ_TEST nap woke asleep=" + Xiaoqi.Spike.PetPresence.IsAsleep);
            }
            if (sit != null)
            {
                var h = FindTarget(sit);
                var sitter = FindFirstObjectByType<AvatarWindowHandler>(FindObjectsInactive.Include);
                if (h == IntPtr.Zero || sitter == null) { Debug.Log("XQ_TEST sit no-target hwnd=" + h + " handler=" + (sitter != null)); yield break; }
                var uh = FindWindowW("UnityWndClass", "小祈");
                MoveTargetLow(h, uh);
                yield return new WaitForSecondsRealtime(0.5f);
                bool ok = sitter.XqSitOn(h, 0.5f);
                Debug.Log("XQ_TEST sit ok=" + ok + (ok ? "" : " why=" + sitter.XqLastFail) + " sitting=" + (PetHost.I != null && PetHost.I.IsWindowSitting));
                for (int i = 1; i <= 5; i++)
                {
                    yield return new WaitForSecondsRealtime(1f);
                    if (i == 1 || i == 3 || i == 5) Geo(h, uh, "t=" + i);
                }
                Debug.Log("XQ_TEST sit after5s snapped=" + (sitter.XqSnappedHwnd == h) + " sitting=" + (PetHost.I != null && PetHost.I.IsWindowSitting));
                if (nap) yield break;
                PetHost.I?.ExitWindowSit();
                yield return new WaitForSecondsRealtime(1f);
                Debug.Log("XQ_TEST sit exit sitting=" + (PetHost.I != null && PetHost.I.IsWindowSitting));
            }
        }

        /// <summary>把目标窗口挪到她所在显示器的下 40% 处：窗口太靠上时她的身体会伸出屏幕上沿，截图看不全也点不到。</summary>
        static void MoveTargetLow(IntPtr h, IntPtr uh)
        {
            try
            {
                if (uh == IntPtr.Zero) return;
                var mon = MonitorFromWindow(uh, 2);
                var mi = new MI { cb = Marshal.SizeOf(typeof(MI)) };
                if (!GetMonitorInfo(mon, ref mi)) return;
                R r; if (!GetWindowRect(h, out r)) return;
                int w = r.r - r.l;
                int nx = mi.work.l + Mathf.Max(0, ((mi.work.r - mi.work.l) - w) / 2);
                int ny = mi.work.t + Mathf.RoundToInt((mi.work.b - mi.work.t) * 0.6f);
                SetWindowPos(h, IntPtr.Zero, nx, ny, 0, 0, 0x1 | 0x4 | 0x10);
                Debug.Log("XQ_TEST sit move-target to " + nx + "," + ny + " monitor work=" + mi.work.l + "," + mi.work.t + ".." + mi.work.r + "," + mi.work.b);
            }
            catch (Exception e) { Debug.Log("XQ_TEST sit move-target failed " + e.Message); }
        }

        static void Geo(IntPtr h, IntPtr uh, string tag)
        {
            try
            {
                R tr, ur; GetWindowRect(h, out tr); GetWindowRect(uh, out ur);
                var b = PetHost.I != null ? PetHost.I.ScreenRect() : default(Rect);
                float top = ur.t + (Screen.height - b.yMax), bot = ur.t + (Screen.height - b.yMin), cx = ur.l + b.center.x;
                Debug.Log("XQ_TEST sit geo " + tag + " unity=" + ur.l + "," + ur.t + " " + (ur.r - ur.l) + "x" + (ur.b - ur.t)
                    + " target=" + tr.l + "," + tr.t + " " + (tr.r - tr.l) + "x" + (tr.b - tr.t)
                    + " bodyDesk y=" + top.ToString("0") + ".." + bot.ToString("0") + " cx=" + cx.ToString("0")
                    + " feetMinusTargetTop=" + (bot - tr.t).ToString("0"));
            }
            catch (Exception e) { Debug.Log("XQ_TEST sit geo failed " + e.Message); }
        }

        static IntPtr FindTarget(string frag)
        {
            IntPtr best = IntPtr.Zero; long bestArea = 0;
            uint me = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;
            EnumWindows((h, lp) =>
            {
                if (!IsWindowVisible(h) || IsIconic(h)) return true;
                uint pid; GetWindowThreadProcessId(h, out pid);
                if (pid == me) return true;
                var t = new StringBuilder(256); GetWindowText(h, t, 256);
                if (t.Length == 0) return true;
                var c = new StringBuilder(128); GetClassName(h, c, 128);
                string cs = c.ToString();
                if (cs == "Progman" || cs == "Shell_TrayWnd" || cs == "WorkerW") return true;
                R r; if (!GetWindowRect(h, out r)) return true;
                int w = r.r - r.l, hh = r.b - r.t;
                if (w < 300 || hh < 150) return true;
                if (frag.Length > 0 && t.ToString().IndexOf(frag, StringComparison.OrdinalIgnoreCase) < 0) return true;
                long area = (long)w * hh;
                if (area > bestArea) { bestArea = area; best = h; }
                return true;
            }, IntPtr.Zero);
            if (best != IntPtr.Zero)
            {
                var t = new StringBuilder(256); GetWindowText(best, t, 256);
                R r; GetWindowRect(best, out r);
                Debug.Log("XQ_TEST sit target hwnd=" + best + " title='" + t + "' rect=" + r.l + "," + r.t + " " + (r.r - r.l) + "x" + (r.b - r.t));
            }
            return best;
        }
    }
}
