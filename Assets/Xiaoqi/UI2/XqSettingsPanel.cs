using System;
using UnityEngine;
using UnityEngine.UIElements;
using Xiaoqi.Host;

namespace Xiaoqi.UI2
{
    /// <summary>
    /// P1-5 设置页。玻璃面板（G59-P1 §4.2）+ 左分类栏 6 项 + 右内容区。
    /// 挂到现有 UIDocument 的 rootVisualElement 下；样式 Resources.Load&lt;StyleSheet&gt;("XqSettings")。
    /// 尺寸先按物理像素算（屏高 70%，至少 600），再用 RuntimePanelUtils.ScreenToPanel 换算，
    /// PanelSettings.scale 变化时物理尺寸不变。
    /// 指针在面板上挡拖动；Esc 或点面板外空白关闭。
    /// </summary>
    public class XqSettingsPanel
    {
        const string Pink = "xq-theme-pink";
        const string Blue = "xq-theme-blue";
        const string Violet = "xq-theme-violet";
        static readonly string[] Pages = { "外观", "行为", "显示", "声音", "连接", "关于" };

        static XqSettingsPanel _inst;
        static bool _ussAdded;

        public bool IsOpen { get; private set; }

        VisualElement _host;
        VisualElement _panel;
        VisualElement[] _pageViews;
        Label[] _tabs;
        int _page = 1;
        bool _built;
        bool _placed;
        bool _scheduled;

        public static void Open()
        {
            if (_inst == null) _inst = new XqSettingsPanel();
            _inst.OpenSelf();
        }

        void OpenSelf()
        {
            if (!_built && !Build()) return;
            IsOpen = true;
            _host.style.display = DisplayStyle.Flex;
            Place();
            ShowPage(_page);
            if (!_scheduled && _panel != null)
            {
                // TODO(编译员)：确认 IVisualElementScheduler（VisualElement.schedule.Execute(Action).Every(ms)，
                // UI Toolkit 标准 API，本仓库无先例）。用它代替每帧 Update，避免另建 MonoBehaviour。
                _panel.schedule.Execute(Tick).Every(16);
                _scheduled = true;
            }
            PetHost.I?.SetUiBlocksMovement(true);
            Debug.Log("XQ_SET2 open");
        }

        public void Close()
        {
            IsOpen = false;
            if (_host != null) _host.style.display = DisplayStyle.None;
            PetHost.I?.SetUiBlocksMovement(false);
            Debug.Log("XQ_SET2 close");
        }

        bool Build()
        {
            var doc = UnityEngine.Object.FindFirstObjectByType<UIDocument>();
            var root = doc != null ? doc.rootVisualElement : null;
            if (root == null) { Debug.Log("XQ_SET2 no UIDocument"); return false; }

            if (!_ussAdded)
            {
                var uss = Resources.Load<StyleSheet>("XqSettings");
                if (uss == null) Debug.Log("XQ_SET2 no uss");
                else { root.styleSheets.Add(uss); _ussAdded = true; }
            }

            var cfg = XiaoqiRuntime.Config;
            _host = new VisualElement { name = "xq-settings-host", pickingMode = PickingMode.Ignore };
            _host.style.position = Position.Absolute;
            _host.style.left = 0;
            _host.style.top = 0;
            _host.style.right = 0;
            _host.style.bottom = 0;
            _host.style.display = DisplayStyle.None;

            _panel = new VisualElement { name = "xq-settings", pickingMode = PickingMode.Position };
            _panel.AddToClassList("xq-st-panel");
            _panel.AddToClassList(ThemeClass(cfg != null ? cfg.theme : "pink"));
            var shadow = new VisualElement { pickingMode = PickingMode.Ignore };
            shadow.AddToClassList("xq-shadow");
            var glass = new VisualElement { pickingMode = PickingMode.Ignore };
            glass.AddToClassList("xq-st-glass");
            var hi = new VisualElement { pickingMode = PickingMode.Ignore };
            hi.AddToClassList("xq-hi");

            var frame = new VisualElement { pickingMode = PickingMode.Ignore };
            frame.AddToClassList("xq-st-frame");
            var title = new Label("设置");
            title.AddToClassList("xq-st-title");
            title.pickingMode = PickingMode.Ignore;
            frame.Add(title);

            var body = new VisualElement { pickingMode = PickingMode.Ignore };
            body.AddToClassList("xq-st-body");
            var tabHost = new VisualElement { pickingMode = PickingMode.Ignore };
            tabHost.AddToClassList("xq-st-tabs");
            var pageHost = new VisualElement { pickingMode = PickingMode.Ignore };
            pageHost.AddToClassList("xq-st-pages");

            _tabs = new Label[Pages.Length];
            _pageViews = new VisualElement[Pages.Length];
            for (int i = 0; i < Pages.Length; i++)
            {
                int idx = i;
                var t = new Label(Pages[i]);
                t.AddToClassList("xq-st-tab");
                t.pickingMode = PickingMode.Position;
                t.RegisterCallback<ClickEvent>(e => { e.StopPropagation(); ShowPage(idx); });
                _tabs[i] = t;
                tabHost.Add(t);

                var p = new VisualElement { pickingMode = PickingMode.Ignore };
                p.AddToClassList("xq-st-page");
                _pageViews[i] = p;
                pageHost.Add(p);
            }

            body.Add(tabHost);
            body.Add(pageHost);
            frame.Add(body);
            _panel.Add(shadow);
            _panel.Add(glass);
            _panel.Add(hi);
            _panel.Add(frame);
            _host.Add(_panel);
            root.Add(_host);

            // D1 只做「行为」页；其余 5 页一行灰字，不报错
            for (int i = 0; i < _pageViews.Length; i++)
            {
                if (i == 1) continue;
                var t = new Label("下一步做");
                t.AddToClassList("xq-st-empty");
                t.pickingMode = PickingMode.Ignore;
                _pageViews[i].Add(t);
            }
            BuildBehaviorPage(_pageViews[1]);

            _built = true;
            return true;
        }

        /// <summary>行为页 7 行，默认值照 G59-P1 §6，一个不改。</summary>
        void BuildBehaviorPage(VisualElement page)
        {
            var cfg = XiaoqiRuntime.Config;

            // 1 坐在窗口上（开）
            page.Add(XqSettingsRows.Toggle("坐在窗口上",
                PetHost.I != null && PetHost.I.WindowSitEnabled,
                v => Bind(XqSettingsApply.SetWindowSit, null, "window_sit", v)));

            // 2 坐姿高度微调：ME 无对应字段，整行灰掉，只存盘不连宿主
            var sit = XqSettingsRows.Slider("坐姿高度微调", -50f, 50f,
                cfg != null ? cfg.sit_offset : 0f, null, 0f);
            sit.OnCommit = () => Bind<float>(null, (c, v) => c.sit_offset = v, "sit_offset", sit.Value);
            sit.Root.SetEnabled(false);
            sit.Root.AddToClassList("xq-st-row-off");
            XqSettingsRows.Hint(sit.Root, "暂未接入");
            page.Add(sit.Root);

            // 3 看向鼠标（开）
            page.Add(XqSettingsRows.Toggle("看向鼠标",
                cfg == null || cfg.look_at_mouse,
                v => Bind(XqSettingsApply.SetLookAtMouse,
                    (c, x) => { c.look_at_mouse = x; }, "look_at_mouse", v)));

            // 4 看向的力度（100%）：拖动即时生效，松手落盘
            var look = XqSettingsRows.Slider("看向的力度", 0f, 100f,
                (cfg != null ? cfg.look_strength : 1f) * 100f,
                v => XqSettingsApply.SetLookStrength(v / 100f), 100f);
            look.Suffix = "%";
            look.Refresh();
            look.OnCommit = () =>
            {
                float f = look.Value;
                XqSettingsApply.SetLookStrength(f / 100f);
                Bind<float>(null, (c, v) => c.look_strength = v / 100f, "look_strength", f);
            };
            page.Add(look.Root);

            // 5 放音乐时自己跳（默认关，G55 的 XiaoqiDefaults 写的 false）
            page.Add(XqSettingsRows.Toggle("放音乐时自己跳",
                XqSettingsApply.GetDancing(),
                v => Bind(XqSettingsApply.SetDancing, null, "enableDancing", v)));

            // 6 多久不动就睡着（默认关）
            int idle = cfg != null ? cfg.idle_sleep_min : 0;
            int idx = idle == 1 ? 1 : idle == 3 ? 2 : idle == 5 ? 3 : idle == 10 ? 4 : 0;
            page.Add(XqSettingsRows.Dropdown("多久不动就睡着",
                new[] { "关", "1 分钟", "3 分钟", "5 分钟", "10 分钟" }, idx,
                i => Bind(XqSettingsApply.SetIdleSleep, (c, v) => { c.idle_sleep_min = v; },
                    "idle_sleep_min", i == 1 ? 1 : i == 2 ? 3 : i == 3 ? 5 : i == 4 ? 10 : 0)));

            // 7 双击摸头（开）
            page.Add(XqSettingsRows.Toggle("双击摸头",
                cfg == null || cfg.pat_double,
                v => Bind(XqSettingsApply.SetPatDouble,
                    (c, x) => { c.pat_double = x; }, "pat_double", v)));
        }

        /// <summary>改一项：连宿主 → 写 cfg → 存盘 → 打一行验收日志。</summary>
        static void Bind<T>(Action<T> apply, Action<XiaoqiConfig, T> store, string key, T value)
        {
            try
            {
                apply?.Invoke(value);
                var cfg = XiaoqiRuntime.Config;
                if (cfg != null)
                {
                    store?.Invoke(cfg, value);
                    XiaoqiConfig.Save(cfg);
                }
                Debug.Log("XQ_SET2 set " + key + "=" + value);
            }
            catch (Exception e)
            {
                Debug.LogWarning("XQ_SET2 " + e.Message);
            }
        }

        void ShowPage(int i)
        {
            _page = Mathf.Clamp(i, 0, Pages.Length - 1);
            for (int k = 0; k < _pageViews.Length; k++)
                if (_pageViews[k] != null)
                    _pageViews[k].style.display = k == _page ? DisplayStyle.Flex : DisplayStyle.None;
            for (int k = 0; k < _tabs.Length; k++)
                if (_tabs[k] != null) _tabs[k].EnableInClassList("xq-st-tab-on", k == _page);
            Debug.Log("XQ_SET2 page " + Pages[_page]);
        }

        void Place()
        {
            if (_panel == null || _host == null || _panel.panel == null) return;
            _placed = true;
            float h = Mathf.Max(600f, Screen.height * 0.7f);
            h = Mathf.Min(h, Screen.height - 20f);
            float w = h * 610f / 492f;
            float maxW = Screen.width - 20f;
            if (w > maxW) { w = maxW; h = w * 492f / 610f; }
            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;
            var a = RuntimePanelUtils.ScreenToPanel(_panel.panel, new Vector2(cx - w * 0.5f, cy + h * 0.5f));
            var b = RuntimePanelUtils.ScreenToPanel(_panel.panel, new Vector2(cx + w * 0.5f, cy - h * 0.5f));
            _panel.style.left = a.x;
            _panel.style.top = a.y;
            _panel.style.width = Mathf.Abs(b.x - a.x);
            _panel.style.height = Mathf.Abs(b.y - a.y);
        }

        static bool Inside(VisualElement target, VisualElement picked)
        {
            for (var e = picked; e != null; e = e.parent)
                if (e == target) return true;
            return false;
        }

        void Tick()
        {
            if (!IsOpen || _panel == null || _panel.panel == null) return;
            if (!_placed) Place();
            if (Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }
            var picked = _panel.panel.Pick(
                RuntimePanelUtils.ScreenToPanel(_panel.panel, Input.mousePosition));
            bool over = Inside(_panel, picked);
            PetHost.I?.SetUiBlocksMovement(over);
            if (!over && Input.GetMouseButtonDown(0)) Close();
        }

        static string ThemeClass(string theme)
        {
            if (string.IsNullOrEmpty(theme)) return Pink;
            var t = theme.Trim().ToLowerInvariant();
            if (t == "blue") return Blue;
            if (t == "violet" || t == "purple") return Violet;
            return Pink;
        }
    }
}
