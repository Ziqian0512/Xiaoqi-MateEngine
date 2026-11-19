using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using Xiaoqi.Host;

namespace Xiaoqi.Mode
{
    public enum XqView { Pet = 0, Close = 1, Bench = 2 }

    /// <summary>
    /// 桌宠 / 近景 / 工作台 三态切换器。只管「切到哪个视图」，不持有数据、不做决策。
    ///
    /// 用法：
    ///   XqMode.Set(XqView.Close);            // 任何入口（菜单、方钮、热键）都调这一个
    ///   XqMode.OnModeChanged += (from, to) => ...;      // 开始切换时触发
    ///   XqMode.OnCloseReady / OnCloseLeaving            // 近景镜头到位 / 开始退出（近景界面在这两处出现和收起）
    ///
    /// 过渡中（镜头 / 窗口还在动）再调 Set 会被忽略并写日志，避免半途打架。
    /// 启动参数 -xq-mode close|bench 用于无人值守测试（PetHost 就绪 2 秒后切一次）。
    /// </summary>
    public class XqMode : MonoBehaviour
    {
        public static XqView Current = XqView.Pet;
        public static bool Busy { get; private set; }
        public static event Action<XqView, XqView> OnModeChanged;
        public static event Action OnCloseReady;
        public static event Action OnCloseLeaving;

        /// <summary>她开始放大后多久触发 OnCloseReady（卡片开始飞出）。</summary>
        public const float CardsDelaySec = 0.45f;

        private static XqMode _i;
        private bool _autoDone;
        private bool _warmDone;
        private readonly bool[] _keyDown = new bool[3];

        [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int vKey);
        private const int VK_CONTROL = 0x11, VK_MENU = 0x12;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (_i != null) return;
            var go = new GameObject("XqMode");
            DontDestroyOnLoad(go);
            _i = go.AddComponent<XqMode>();
            go.AddComponent<XqBenchWindow>();
            // 近景界面（DeepSeek 的 XqClosePanel）：镜头开始放大 0.45 秒后出现，开始退出时收起
            OnCloseReady += Xiaoqi.UI2.XqClosePanel.Show;
            OnCloseLeaving += Xiaoqi.UI2.XqClosePanel.Hide;
        }

        /// <summary>工作台入口是否可用（P2 通过前默认灰）。</summary>
        public static bool BenchAvailable { get { return XqModeConfig.I.bench_enabled; } }

        /// <summary>G74 W1：工作台走 Unity 原生面板（默认）；-xq-bench-web 才开旧的 Edge 网页窗口。</summary>
        public static bool BenchNative { get { return !XqModeConfig.I.bench_web; } }

        public static bool Set(XqView to)
        {
            if (_i == null) { Debug.Log("XQ_MODE not booted"); return false; }
            if (to == Current) return true;
            if (Busy) { Debug.Log("XQ_MODE busy, ignore " + to); return false; }
            if (to == XqView.Bench && !BenchAvailable) { Debug.Log("XQ_MODE bench disabled"); return false; }
            if (PetHost.I == null || !PetHost.I.Ready) { Debug.Log("XQ_MODE host not ready"); return false; }
            _i.StartCoroutine(_i.Transition(Current, to));
            return true;
        }

        /// <summary>工作台窗口被用户关掉（XqBenchWindow 调）：已经关了，直接回桌宠。</summary>
        internal static void BenchClosedByUser()
        {
            if (Current != XqView.Bench || Busy || _i == null) return;
            _i.StartCoroutine(_i.Transition(XqView.Bench, XqView.Pet));
        }

        private IEnumerator Transition(XqView from, XqView to)
        {
            Busy = true;
            Debug.Log("XQ_MODE " + from + " -> " + to);
            XqEventSink.Report("view", to.ToString().ToLowerInvariant());
            try { if (OnModeChanged != null) OnModeChanged(from, to); }
            catch (Exception e) { Debug.LogWarning("XQ_MODE listener: " + e.Message); }

            // 离开 from
            if (from == XqView.Close)
            {
                try { if (OnCloseLeaving != null) OnCloseLeaving(); }
                catch (Exception e) { Debug.LogWarning("XQ_MODE leaving listener: " + e.Message); }
                if (to == XqView.Bench && BenchNative)
                {
                    // 近景 → 原生工作台：同一个大屏窗口，不退出，只换镜头
                    yield return new WaitForSecondsRealtime(0.35f);
                }
                else
                {
                    XqCloseCamera.BeginExit();
                    yield return XqCloseCamera.Settle();
                }
            }
            else if (from == XqView.Bench)
            {
                if (BenchNative)
                {
                    Xiaoqi.UI2.XqBenchPanel.Hide();
                    if (to == XqView.Close)
                    {
                        XqCloseCamera.Retarget(false, 0f, 0f, 0f);
                        yield return new WaitForSecondsRealtime(0.5f);
                    }
                    else
                    {
                        XqCloseCamera.BeginExit();
                        yield return XqCloseCamera.Settle();
                    }
                }
                else
                {
                    XqBenchWindow.Close();
                    yield return new WaitForSecondsRealtime(0.2f);
                }
            }

            Current = to;

            // 进入 to
            if (to == XqView.Close)
            {
                if (!XqCloseCamera.BeginEnter())
                {
                    Debug.Log("XQ_MODE close failed, back to Pet");
                    Current = XqView.Pet;
                }
                else
                {
                    // G59-P2 §4：她开始放大约 0.45 秒后卡片开始飞出；镜头走完 SettleSec 才算切换结束
                    yield return new WaitForSecondsRealtime(CardsDelaySec);
                    try { if (OnCloseReady != null) OnCloseReady(); }
                    catch (Exception e) { Debug.LogWarning("XQ_MODE ready listener: " + e.Message); }
                    yield return new WaitForSecondsRealtime(Mathf.Max(0f, XqCloseCamera.SettleSec - CardsDelaySec));
                }
            }
            else if (to == XqView.Bench)
            {
                if (BenchNative)
                {
                    float pf = 0f, pvx = 0f, pvy = 0f;
                    bool ok;
                    string path;
                    if (from == XqView.Close)
                    {
                        path = "from-close";
                        Xiaoqi.UI2.XqBenchPanel.PetTarget(out pf, out pvx, out pvy);
                        ok = XqCloseCamera.Retarget(true, pf, pvx, pvy);
                    }
                    else
                    {
                        // 桌宠直接进工作台 = 先走「进近景」那条已验证的路（只换镜头不出卡片），窗口铺满后再按真实屏幕尺寸算落点
                        path = "pet-direct";
                        ok = XqCloseCamera.BeginEnter();
                        if (ok)
                        {
                            yield return new WaitForSecondsRealtime(XqCloseCamera.SettleSec);
                            Xiaoqi.UI2.XqBenchPanel.PetTarget(out pf, out pvx, out pvy);
                            ok = XqCloseCamera.Retarget(true, pf, pvx, pvy);
                        }
                    }
                    Debug.Log("XQ_MODE bench path=" + path + " ok=" + ok + " screen=" + Screen.width + "x" + Screen.height
                        + " target frac=" + pf.ToString("0.00") + " eye=(" + pvx.ToString("0.00") + "," + pvy.ToString("0.00") + ")");
                    if (!ok)
                    {
                        Debug.Log("XQ_MODE bench(native) failed, back to Pet");
                        Current = XqView.Pet;
                    }
                    else
                    {
                        Xiaoqi.UI2.XqBenchPanel.ApplyTilt();
                        // 从桌宠来：等 ME 的窗口铺满 + 镜头淡入（约 0.9 秒）再出面板；从近景来：窗口早就铺满了
                        float showAt = 0.5f;
                        yield return new WaitForSecondsRealtime(showAt);
                        Xiaoqi.UI2.XqBenchPanel.Show();
                        Debug.Log("XQ_MODE bench shown=" + Xiaoqi.UI2.XqBenchPanel.IsShown + " path=" + path);
                        yield return new WaitForSecondsRealtime(Mathf.Max(0f, XqCloseCamera.SettleSec - showAt) + 0.1f);
                    }
                }
                else if (!XqBenchWindow.Open())
                {
                    Debug.Log("XQ_MODE bench failed, back to Pet");
                    Current = XqView.Pet;
                }
            }
            Busy = false;
        }

        private void Update()
        {
            var cfg = XqModeConfig.I;

            // G74 W1：宿主就绪 3 秒后把工作台面板预建好（隐藏），点「工作台」时不用现场构建
            if (!_warmDone && BenchNative && PetHost.I != null && PetHost.I.Ready && Time.realtimeSinceStartup > 3f)
            {
                _warmDone = true;
                Xiaoqi.UI2.XqBenchPanel.Warm();
            }

            // 无人值守测试：-xq-mode close|bench
            if (!_autoDone && PetHost.I != null && PetHost.I.Ready && Time.realtimeSinceStartup > 5f)
            {
                _autoDone = true;
                var m = XqModeConfig.ArgValue("-xq-mode");
                if (m == "close") Set(XqView.Close);
                else if (m == "bench") Set(XqView.Bench);
            }

            // Esc：近景 → 桌宠；工作台 → 近景（走 Unity 自己的输入，只有她拿着焦点时才算）
            if (!Busy && Input.GetKeyDown(KeyCode.Escape))
            {
                if (Current == XqView.Bench && Xiaoqi.UI2.XqBenchPanel.DismissPhotoOverlay()) return;
                if (Current == XqView.Close) Set(XqView.Pet);
                else if (Current == XqView.Bench) Set(XqView.Close);
            }

            // Ctrl+Alt+1/2/3：全局键，默认关
            if (cfg.hotkeys)
            {
                bool mod = (GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0 && (GetAsyncKeyState(VK_MENU) & 0x8000) != 0;
                for (int n = 0; n < 3; n++)
                {
                    bool down = mod && (GetAsyncKeyState(0x31 + n) & 0x8000) != 0;
                    if (down && !_keyDown[n]) Set((XqView)n);
                    _keyDown[n] = down;
                }
            }
        }
    }
}
