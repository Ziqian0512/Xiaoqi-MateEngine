using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Xiaoqi.UI2
{
    /// <summary>
    /// XqClosePanel 的 P2-4 部分：延展（进入）与退出动画，按 G59-P2 §4。
    ///
    /// 进入：每个元素从「靠近她」的起始偏移飞到目标位，淡入 + 缩放 0.5→1 + 位移，
    ///       各 0.7 秒，依次错开 90 毫秒，缓动 cubic-bezier(.2,.9,.3,1.15)；
    ///       全部到位后接 7 秒周期的上下浮动。
    /// 退出：全部同时反向收回，0.35 秒，不错开。
    ///
    /// 位移用 style.translate、缩放用 style.scale、淡入用 style.opacity——
    /// 这三个和 XqPetHud.ApplyChip / Place 里是同一套写法，不改 left/top，
    /// 所以动画不会跟 Place() 的位置计算打架。
    /// ⑧⑨ 两个方钮在 §4 表里没有起始偏移，只跟名牌一起淡入，不臆造位移。
    /// </summary>
    public partial class XqClosePanel
    {
        struct AnimItem
        {
            public VisualElement el;
            public int order;
            public Vector2 off;
        }

        readonly List<AnimItem> _anim = new List<AnimItem>();
        int _animMode;      // 0 停、1 进入中、2 已到位（浮动）、3 退出中
        float _animT;
        float _u = 1f;
        bool _scheduled;
        bool _benchHover;
        int _lastClickFrame = -1;
        static CloseEscWatcher _escWatcher;

        // XQ: P2 R-3 先于 XqMode.Update 捕获近景 Esc 退出并打 XQ_CLOSE exit by=esc（不改 XqMode.cs）
        [DefaultExecutionOrder(-100)]
        sealed class CloseEscWatcher : MonoBehaviour
        {
            void Update()
            {
                if (_inst != null && _inst._shown && !Xiaoqi.Mode.XqMode.Busy
                    && Xiaoqi.Mode.XqMode.Current == Xiaoqi.Mode.XqView.Close
                    && Input.GetKeyDown(KeyCode.Escape))
                {
                    Debug.Log("XQ_CLOSE exit by=esc");
                }
            }
        }

        public static bool OverAny()
        {
            return _inst != null && _inst._shown && _inst.OverAnySelf();
        }

        bool OverAnySelf()
        {
            return OverEl(_rail) || OverEl(_plate) || OverEl(_btnPet) || OverEl(_btnBench)
                || OverEl(_cardNext) || OverEl(_cardEnergy) || OverEl(_cardSay) || OverEl(_cardTiny) || OverEl(_ringFocus);
        }

        bool OverEl(VisualElement el)
        {
            if (el == null || _host == null || _host.panel == null) return false;
            if (el.resolvedStyle.display == DisplayStyle.None || !el.visible) return false;
            var r = el.worldBound;
            if (float.IsNaN(r.x) || r.width < 1f || r.height < 1f) return false;
            var m = Input.mousePosition;
            var p0 = RuntimePanelUtils.ScreenToPanel(_host.panel, m);
            float sc = Mathf.Max(0.1f, Screen.height / Mathf.Max(1f, _host.worldBound.height));
            var p1 = new Vector2(m.x / sc, (Screen.height - m.y) / sc);
            return r.Contains(p0) || r.Contains(p1);
        }

        void TickPointer()
        {
            if (!_shown || _host == null || _host.panel == null) return;
            bool hb = OverEl(_btnBench);
            if (hb != _benchHover)
            {
                _benchHover = hb;
                if (_tipBench != null) _tipBench.EnableInClassList("on", hb);
                Debug.Log("XQ_CLOSE bench-hover=" + hb);
            }
            if (Input.GetMouseButtonDown(0) && _lastClickFrame != Time.frameCount)
            {
                if (OverEl(_btnPet)) { _lastClickFrame = Time.frameCount; Debug.Log("XQ_CLOSE btn pet"); Debug.Log("XQ_CLOSE exit by=btn"); Xiaoqi.Mode.XqMode.Set(Xiaoqi.Mode.XqView.Pet); return; }
                if (OverEl(_ringFocus)) { _lastClickFrame = Time.frameCount; Pair("ring placeholder"); return; }
                if (OverEl(DotByWord("♥"))) { _lastClickFrame = Time.frameCount; Pair("rail pet"); Debug.Log("XQ_CLOSE exit by=rail"); Xiaoqi.Mode.XqMode.Set(Xiaoqi.Mode.XqView.Pet); return; }
                if (OverEl(DotByWord("今"))) { _lastClickFrame = Time.frameCount; Focus(_cardNext); return; }
                if (OverEl(DotByWord("话"))) { _lastClickFrame = Time.frameCount; Focus(_cardSay); return; }
                if (OverEl(DotByWord("待"))) { _lastClickFrame = Time.frameCount; Pair("todo placeholder"); return; }
                if (OverEl(DotByWord("设"))) { _lastClickFrame = Time.frameCount; Pair("settings open"); XiaoqiSettings.Open(); return; }
            }
        }

        void AnimAdd(VisualElement el, int order, Vector2 off)
        {
            if (el == null) return;
            _anim.Add(new AnimItem { el = el, order = order, off = off });
        }

        /// <summary>按 §4 的表登记 7 个有位移的元素，再挂上两个方钮（无位移）。</summary>
        void AnimRegister()
        {
            AnimAdd(_rail, 0, XqCloseAnim.Offsets[0]);
            AnimAdd(_cardNext, 1, XqCloseAnim.Offsets[1]);
            AnimAdd(_cardEnergy, 2, XqCloseAnim.Offsets[2]);
            AnimAdd(_cardSay, 3, XqCloseAnim.Offsets[3]);
            AnimAdd(_cardTiny, 4, XqCloseAnim.Offsets[4]);
            AnimAdd(_ringFocus, 5, XqCloseAnim.Offsets[5]);
            AnimAdd(_plate, 6, XqCloseAnim.Offsets[6]);
            AnimAdd(_btnPet, 6, Vector2.zero);
            AnimAdd(_btnBench, 6, Vector2.zero);
        }

        void StartEnter()
        {
            _animMode = 1;
            _animT = 0f;
            _focusOn = false;
            _focusCard = null;
            _focusT = 1f;
            _focusK = 1f;
            for (int i = 0; i < _anim.Count; i++)
            {
                var el = _anim[i].el;
                el.style.opacity = 0f;
                el.style.scale = new Scale(Vector3.one * XqCloseAnim.EnterScaleFrom);
                Vector2 v = _anim[i].off * _u;
                el.style.translate = new Translate(v.x, v.y, 0f);
            }
            Hook();
            Pair("anim enter");
        }

        void StartExit()
        {
            _animMode = 3;
            _animT = 0f;
            Hook();
            Pair("anim exit");
        }

        void Hook()
        {
            if (_escWatcher == null)
            {
                var go = new GameObject("XqCloseEscWatcher");
                UnityEngine.Object.DontDestroyOnLoad(go);
                _escWatcher = go.AddComponent<CloseEscWatcher>();
            }
            if (_scheduled || _host == null) return;
            // TODO(编译员)：VisualElement.schedule.Execute(Action).Every(ms) 是 UI Toolkit 标准
            // 调度器，但本仓库没有先例（XqSettingsPanel 同样只是写在纸上未编译）。若调度器不可用，
            // 等价替换：建一个隐藏的 MonoBehaviour，在 Update() 里调用本文件的 TickAnim()。
            _host.schedule.Execute(TickAnim).Every(16);
            _scheduled = true;
        }

        void TickAnim()
        {
            if (_animMode == 0) return;
            if (_host == null || _host.panel == null) return;

            TickPointer();
            _animT += Time.unscaledDeltaTime;

            if (_animMode == 1)
            {
                ApplyEnter(_animT);
                float total = EnterTotal();
                if (_animT >= total)
                {
                    _animMode = 2;
                    Pair("anim settled");
                    if (_data != null)
                    {
                        _data.Shot("settled.png", 0.4f);
                        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-xq-close-ui-test") >= 0)
                            _data.StartCoroutine(RunUiTest());
                    }
                }
            }
            else if (_animMode == 2)
            {
                ApplyBob();
            }
            else if (_animMode == 3)
            {
                ApplyExit(_animT);
                if (_animT >= XqCloseAnim.ExitDur)
                {
                    _animMode = 0;
                    if (_host != null) _host.style.display = DisplayStyle.None;
                    ResetStyles();
                    Pair("anim done");
                }
            }
            if (_animMode == 2) TickFocus();
        }

        float EnterTotal()
        {
            return XqCloseAnim.EnterBase
                + (XqCloseAnim.Count - 1) * XqCloseAnim.EnterStagger
                + XqCloseAnim.EnterDur;
        }

        void ApplyEnter(float t)
        {
            for (int i = 0; i < _anim.Count; i++)
            {
                var a = _anim[i];
                if (a.el == null) continue;
                float p = Mathf.Clamp01((t - XqCloseAnim.Start(a.order)) / XqCloseAnim.EnterDur);
                float e = XqCloseAnim.Enter(p);
                float op = XqCloseAnim.EnterFade > 0f
                    ? Mathf.Clamp01(p / XqCloseAnim.EnterFade) : 1f;
                a.el.style.opacity = op;
                a.el.style.scale = new Scale(Vector3.one
                    * Mathf.Lerp(XqCloseAnim.EnterScaleFrom, 1f, e));
                Vector2 v = a.off * ((1f - e) * _u);
                a.el.style.translate = new Translate(v.x, v.y, 0f);
            }
        }

        /// <summary>到位后的 7 秒上下浮动。相位按出场顺序错开，免得整块界面齐步走。</summary>
        void ApplyBob()
        {
            float phase = Time.unscaledTime * (Mathf.PI * 2f / XqCloseAnim.BobPeriod);
            for (int i = 0; i < _anim.Count; i++)
            {
                var a = _anim[i];
                if (a.el == null) continue;
                float bob = Mathf.Sin(phase + a.order * XqCloseAnim.BobPhaseStep)
                    * XqCloseAnim.BobAmp * _u;
                // 聚焦系数只作用在目标卡上（P2-5 的 1.04 倍）
                float k = (a.el == _focusCard) ? _focusK : 1f;
                a.el.style.opacity = 1f;
                a.el.style.scale = new Scale(Vector3.one * k);
                a.el.style.translate = new Translate(0f, bob, 0f);
            }
        }

        void ApplyExit(float t)
        {
            float p = Mathf.Clamp01(t / XqCloseAnim.ExitDur);
            float e = XqCloseAnim.Exit(p);
            for (int i = 0; i < _anim.Count; i++)
            {
                var a = _anim[i];
                if (a.el == null) continue;
                a.el.style.opacity = 1f - e;
                a.el.style.scale = new Scale(Vector3.one
                    * Mathf.Lerp(1f, XqCloseAnim.ExitScaleTo, e));
                Vector2 v = a.off * (XqCloseAnim.ExitTravel * e * _u);
                a.el.style.translate = new Translate(v.x, v.y, 0f);
            }
        }

        /// <summary>回零，免得下一次进来还残留上一轮的透明度/缩放/位移。</summary>
        void ResetStyles()
        {
            for (int i = 0; i < _anim.Count; i++)
            {
                var el = _anim[i].el;
                if (el == null) continue;
                el.style.opacity = 1f;
                el.style.scale = new Scale(Vector3.one);
                el.style.translate = new Translate(0f, 0f, 0f);
            }
        }

        System.Collections.IEnumerator RunUiTest()
        {
            yield return new WaitForSecondsRealtime(1.0f);
            if (_tipBench != null) { _tipBench.EnableInClassList("on", true); Debug.Log("XQ_CLOSE bench-hover=True"); }
            if (_data != null) _data.Shot("tip-bench.png", 0.15f);
            yield return new WaitForSecondsRealtime(0.4f);
            if (_tipBench != null) { _tipBench.EnableInClassList("on", false); Debug.Log("XQ_CLOSE bench-hover=False"); }

            Focus(_cardNext);
            if (_data != null) _data.Shot("focus-next-on.png", 0.35f);
            yield return new WaitForSecondsRealtime(0.55f);
            Focus(_cardNext);
            yield return new WaitForSecondsRealtime(0.35f);

            Focus(_cardSay);
            if (_data != null) _data.Shot("focus-say-on.png", 0.35f);
            yield return new WaitForSecondsRealtime(0.55f);
            Focus(_cardSay);
            yield return new WaitForSecondsRealtime(0.35f);

            Pair("todo placeholder");
            Pair("settings not in this branch");
            Pair("ring placeholder");
            yield return new WaitForSecondsRealtime(0.3f);

            var a = Environment.GetCommandLineArgs();
            if (Array.IndexOf(a, "-xq-exit-heart") >= 0)
            {
                Pair("rail pet");
                Pair("exit by=rail");
                Xiaoqi.Mode.XqMode.Set(Xiaoqi.Mode.XqView.Pet);
            }
            else if (Array.IndexOf(a, "-xq-exit-btn") >= 0)
            {
                Debug.Log("XQ_CLOSE btn pet");
                Debug.Log("XQ_CLOSE exit by=btn");
                Xiaoqi.Mode.XqMode.Set(Xiaoqi.Mode.XqView.Pet);
            }
        }
    }
}
