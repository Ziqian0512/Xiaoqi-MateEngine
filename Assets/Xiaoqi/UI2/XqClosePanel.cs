using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Xiaoqi.Mode;

namespace Xiaoqi.UI2
{
    /// <summary>
    /// P2-3 近景界面（G59-P2 §3.2 / §3.3 / §5）。对外只暴露 Show() / Hide() / IsShown。
    ///
    /// 3a：左侧竖栏（药丸）+ 底部名牌 + 右下两个方钮（桌宠可点、工作台灰）。
    /// 3b：四张卡片 + 圆环（在 XqClosePanel.Cards.cs）。
    ///
    /// 几条硬约束：
    /// - 根容器 pickingMode=Ignore，只有竖栏/名牌/方钮/卡片自己 Position。
    ///   近景是 ME 的大屏模式、窗口铺满整块显示器，透明处能不能穿透是别的机制管的；
    ///   根节点不设 Position 就不会挡住它们。
    /// - 几何量全部来自 XqCloseLayout（一处常量），Build 时每个元素过 InFaceZone()，
    ///   违反就打 XQ_CLOSE violate &lt;名字&gt;。
    /// - 竖栏圆点的点击（今天/她说过的话/待办/设置）与 ♥ 退出在 P2-5 接，3a 只搭样子。
    /// - 面板可能挂在 XqUi2Sample 的 UIDocument 上，它的 PanelSettings.scale 会随她身高变，
    ///   所以像素量要用 RuntimePanelUtils.ScreenToPanel 换到面板坐标，不能直接写物理像素。
    ///
    /// 本文件只放骨架（显示/隐藏、构建入口、定位）；竖栏/名牌/方钮的搭法与脸区自检在
    /// XqClosePanel.Controls.cs，四张卡与圆环在 XqClosePanel.Cards.cs，
    /// 同一个 partial 类，拆开只为单文件不超 300 行。
    /// </summary>
    public partial class XqClosePanel
    {
        static XqClosePanel _inst;
        static bool _ussAdded;

        struct Slot
        {
            public string name;
            public float left, top, wStage, hStage;
        }

        bool _built;
        bool _shown;

        VisualElement _host;
        VisualElement _rail;
        VisualElement _plate;
        VisualElement _btnPet;
        VisualElement _btnBench;
        VisualElement _tipBench;

        readonly List<Slot> _slots = new List<Slot>();

        public static bool IsShown { get { return _inst != null && _inst._shown; } }

        public static void Show()
        {
            if (_inst == null) _inst = new XqClosePanel();
            try { _inst.ShowSelf(); }
            catch (Exception e) { Debug.LogWarning("XQ_CLOSE show: " + e.Message); }
        }

        public static void Hide()
        {
            if (_inst == null) return;
            try { _inst.HideSelf(); }
            catch (Exception e) { Debug.LogWarning("XQ_CLOSE hide: " + e.Message); }
        }

        // ---------------- 显示 / 隐藏 ----------------

        void ShowSelf()
        {
            if (!_built && !Build()) return;
            _shown = true;
            _host.style.display = DisplayStyle.Flex;
            Place();
            BindData();
            if (_data != null) _data.Begin();
            RefreshNext();
            RefreshEnergy(XiaoqiLink.Current);
            RefreshSay();
            StartEnter();
            Pair("open");
        }

        void HideSelf()
        {
            _shown = false;
            if (_host == null) return;
            if (_host.style.display == DisplayStyle.None) return;
            UnbindData();
            StartExit();
            Pair("close");
        }

        /// <summary>
        /// 根尺寸变化（换分辨率、PanelSettings.scale 随她身高变）时重排。
        /// 用 GeometryChangedEvent 而不是定时轮询：host 铺满根节点，根一变它必然收到事件。
        /// </summary>
        void OnHostGeometryChanged(GeometryChangedEvent e)
        {
            if (!_shown) return;
            try
            {
                Place();
                AssertFaceZone();
            }
            catch (Exception ex) { Debug.LogWarning("XQ_CLOSE relayout: " + ex.Message); }
        }

        // ---------------- 构建 ----------------

        bool Build()
        {
            var doc = PickDoc();
            var root = doc != null ? doc.rootVisualElement : null;
            if (root == null) { Debug.Log("XQ_CLOSE no UIDocument"); return false; }
            Debug.Log("XQ_CLOSE doc=" + doc.name);

            if (!_ussAdded)
            {
                var uss = Resources.Load<StyleSheet>("XqClose");
                if (uss == null) Debug.Log("XQ_CLOSE no uss");
                else { root.styleSheets.Add(uss); _ussAdded = true; }
            }

            _host = new VisualElement { name = "xq-close-host", pickingMode = PickingMode.Ignore };
            _host.style.position = Position.Absolute;
            _host.style.left = 0;
            _host.style.top = 0;
            _host.style.right = 0;
            _host.style.bottom = 0;
            _host.style.display = DisplayStyle.None;
            _host.AddToClassList("xq-root");
            _host.AddToClassList(ThemeClass(ThemeFromArgs()));
            _host.RegisterCallback<GeometryChangedEvent>(OnHostGeometryChanged);

            BuildRail();
            BuildPlate();
            BuildButtons();
            BuildCards();
            AnimRegister();
            WireRail();

            root.Add(_host);
            _built = true;
            AssertFaceZone();
            return true;
        }

        void Add(string name, float left, float top, float wStage, float hStage)
        {
            _slots.Add(new Slot { name = name, left = left, top = top, wStage = wStage, hStage = hStage });
        }

        // ---------------- 定位 ----------------

        /// <summary>
        /// 按当前面板尺寸摆位置。left/top 用屏幕百分比，宽高用「样式稿 px × u」，
        /// u = 面板高 / 540（G59-P1 §4.3）。面板坐标由 ScreenToPanel 换出来，
        /// 这样 PanelSettings.scale 变化时物理尺寸仍然正确。
        /// </summary>
        void Place()
        {
            var panel = _host != null ? _host.panel : null;
            if (panel == null) return;
            Vector2 a = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(0f, 0f));
            Vector2 b = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(Screen.width, Screen.height));
            float pw = Mathf.Abs(b.x - a.x);
            float ph = Mathf.Abs(b.y - a.y);
            if (pw < 8f || ph < 8f) return;

            float u = ph / XqCloseLayout.Stage;
            _u = u;
            // XQ: B1 第二轮 #14 稿子按 540 高画的，1080 屏上 u≈2 → 左栏 124px 宽、字 32px，巨大。
            // 控件（左栏/名牌/方钮）面板单位上限 1.3，卡片 1.6：位置仍按屏幕百分比，只是尺寸不再随屏高倍增。
            float uc = Mathf.Min(u, 1.3f);
            float ucard = Mathf.Min(u, 1.6f);
            PlaceOne(_rail, XqCloseLayout.RailL, XqCloseLayout.RailT,
                XqCloseLayout.RailW, XqCloseLayout.RailH, pw, ph, uc);
            PlaceOne(_plate, XqCloseLayout.PlateL, XqCloseLayout.PlateT,
                XqCloseLayout.PlateW, XqCloseLayout.PlateH, pw, ph, uc);
            PlaceOne(_btnPet, XqCloseLayout.BtnPetL, XqCloseLayout.BtnTop,
                XqCloseLayout.BtnSize, XqCloseLayout.BtnSize, pw, ph, uc);
            PlaceOne(_btnBench, XqCloseLayout.BtnBenchL, XqCloseLayout.BtnTop,
                XqCloseLayout.BtnSize, XqCloseLayout.BtnSize, pw, ph, uc);
            PlaceOne(_cardNext, XqCloseLayout.NextL, XqCloseLayout.NextT,
                XqCloseLayout.NextW, XqCloseLayout.NextH, pw, ph, ucard);
            PlaceOne(_cardEnergy, XqCloseLayout.EnergyL, XqCloseLayout.EnergyT,
                XqCloseLayout.EnergyW, XqCloseLayout.EnergyH, pw, ph, ucard);
            PlaceOne(_cardSay, XqCloseLayout.SayL, XqCloseLayout.SayT,
                XqCloseLayout.SayW, XqCloseLayout.SayH, pw, ph, ucard);
            PlaceOne(_cardTiny, XqCloseLayout.TinyL, XqCloseLayout.TinyT,
                XqCloseLayout.TinyW, XqCloseLayout.TinyH, pw, ph, ucard);
            PlaceOne(_ringFocus, XqCloseLayout.RingL, XqCloseLayout.RingT,
                XqCloseLayout.RingSize, XqCloseLayout.RingSize, pw, ph, ucard);

            PlaceRail(uc);
            PlacePlate(uc);
            PlaceButtons(uc);
            PlaceCards(ucard);
        }

        static void PlaceOne(VisualElement el, float left, float top,
            float wStage, float hStage, float pw, float ph, float u)
        {
            if (el == null) return;
            el.style.left = left * pw;
            el.style.top = top * ph;
            el.style.width = wStage * u;
            el.style.height = hStage * u;
        }

        // ---------------- 工具 ----------------

        /// <summary>
        /// 场景里可能不止一个 UIDocument。优先挑挂 XqUi2Sample 的、再挑名字含 Xq/Xiaoqi 的，
        /// 都没有才退回第一个（与 b4-deepseek 设置页同一套挑选规则）。
        /// </summary>
        static UIDocument PickDoc()
        {
            var docs = UnityEngine.Object.FindObjectsByType<UIDocument>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (docs == null || docs.Length == 0) return null;

            var sample = UnityEngine.Object.FindFirstObjectByType<XqUi2Sample>(FindObjectsInactive.Include);
            if (sample != null)
            {
                foreach (var d in docs)
                    if (d != null && Under(d.transform, sample.transform)) return d;
            }
            foreach (var d in docs)
            {
                if (d == null) continue;
                var n = d.name ?? "";
                if (n.IndexOf("Xq", StringComparison.OrdinalIgnoreCase) >= 0) return d;
                if (n.IndexOf("Xiaoqi", StringComparison.OrdinalIgnoreCase) >= 0) return d;
            }
            return docs[0];
        }

        static bool Under(Transform t, Transform root)
        {
            for (var p = t; p != null; p = p.parent)
                if (p == root) return true;
            return false;
        }

        static string ThemeClass(string theme)
        {
            if (string.IsNullOrEmpty(theme)) return "xq-theme-pink";
            theme = theme.Trim().ToLowerInvariant();
            if (theme == "blue") return "xq-theme-blue";
            if (theme == "violet" || theme == "purple") return "xq-theme-violet";
            return "xq-theme-pink";
        }

        /// <summary>
        /// 主题与 XqUi2Sample 同源：命令行 -xq-theme blue / --xq-theme=violet，缺省粉。
        /// 不从 XiaoqiConfig 读，是因为配置里没有 theme 字段（2026-11 该文件现状）。
        /// </summary>
        static string ThemeFromArgs()
        {
            var a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length; i++)
            {
                var s = a[i];
                if ((s == "-xq-theme" || s == "--xq-theme") && i + 1 < a.Length)
                    return a[i + 1] ?? "";
                if (s.StartsWith("-xq-theme=")) return s.Substring("-xq-theme=".Length);
                if (s.StartsWith("--xq-theme=")) return s.Substring("--xq-theme=".Length);
            }
            return "pink";
        }

        static void Pair(string what)
        {
            Debug.Log("XQ_CLOSE " + what);
        }
    }
}
