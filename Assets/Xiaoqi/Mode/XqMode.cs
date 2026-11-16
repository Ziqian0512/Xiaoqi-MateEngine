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
                XqCloseCamera.BeginExit();
                yield return XqCloseCamera.Settle();
            }
            else if (from == XqView.Bench)
            {
                XqBenchWindow.Close();
                yield return new WaitForSecondsRealtime(0.2f);
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
                if (!XqBenchWindow.Open())
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
