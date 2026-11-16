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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            string sit = null; bool nap = false;
            foreach (var a in Environment.GetCommandLineArgs())
            {
                if (a == "-xq-sit-test") sit = "";
                else if (a.StartsWith("-xq-sit-test=")) sit = a.Substring("-xq-sit-test=".Length);
                else if (a == "-xq-nap-test") nap = true;
            }
            if (sit == null && !nap) return;
            var go = new GameObject("XqTestHooks");
            DontDestroyOnLoad(go);
            var hooks = go.AddComponent<XqTestHooks>();
            hooks.StartCoroutine(hooks.Run(sit, nap));
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
                bool ok = sitter.XqSitOn(h, 0.5f);
                Debug.Log("XQ_TEST sit ok=" + ok + (ok ? "" : " why=" + sitter.XqLastFail) + " sitting=" + (PetHost.I != null && PetHost.I.IsWindowSitting));
                yield return new WaitForSecondsRealtime(5f);
                Debug.Log("XQ_TEST sit after5s snapped=" + (sitter.XqSnappedHwnd == h) + " sitting=" + (PetHost.I != null && PetHost.I.IsWindowSitting));
                if (nap) yield break;
                PetHost.I?.ExitWindowSit();
                yield return new WaitForSecondsRealtime(1f);
                Debug.Log("XQ_TEST sit exit sitting=" + (PetHost.I != null && PetHost.I.IsWindowSitting));
            }
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
