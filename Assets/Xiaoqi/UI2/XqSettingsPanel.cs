using System;
using UnityEngine;
using UnityEngine.UIElements;
using Xiaoqi.Host;

namespace Xiaoqi.UI2
{
    /// <summary>
    /// P1-5 设置页。G74 W5 起改成右键玻璃菜单的第三级：菜单「设置」展开二级（6 个分类），
    /// 悬停/点分类 → 在二级旁边弹这张紧凑玻璃卡（只显示该分类的设置项）。
    /// 没有菜单时（托盘 / 近景 ⚙ 打开）退回独立卡：顶部多一排小标签可切分类。
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
        public static string[] PageNames { get { return Pages; } }
        public const float CardW = 380f;     // 卡宽（面板单位）

        static XqSettingsPanel _inst;
        static bool _ussAdded;

        public bool IsOpen { get; private set; }

        // XQ: B1 第二轮 #13/#14 静态入口：HUD / 菜单 / Sample 不用持有实例就能问「设置页开着没、鼠标在不在页上、刚点掉页的那一下要不要吞」。
        public static bool OpenNow { get { return _inst != null && _inst.IsOpen; } }
        public static bool OverNow { get { return _inst != null && _inst.IsOpen && _inst._over; } }
        static int _swallowFrame = -100;
        /// <summary>点页外关掉设置页的那一下点击（含按住期间）不能再穿到她身上（今天卡 / 拖动）。</summary>
        public static bool Swallow
        {
            get
            {
                if (_swallowFrame < 0) return false;
                if (Input.GetMouseButton(0) || Time.frameCount <= _swallowFrame + 3) return true;
                _swallowFrame = -100;
                return false;
            }
        }
        /// <summary>W5：菜单二级里悬停/点了某个分类 → 开（或切到）三级卡。</summary>
        public static void OpenCascade(int page)
        {
            if (_inst == null) _inst = new XqSettingsPanel();
            _inst.OpenSelf(true, page);
        }

        public static bool CascadeShown { get { return _inst != null && _inst.IsOpen && _inst._cascade; } }

        /// <summary>W5：菜单每帧调。l2 = 二级卡外框，row = 选中分类那一行，toRight = 三级放在二级右侧。全是面板单位。</summary>
        public static void CascadeTick(Rect l2, Rect row, bool toRight, float minX, float limitW, float limitH)
        {
            if (_inst == null || !_inst.IsOpen || !_inst._cascade) return;
            _inst.PlaceCascade(l2, row, toRight, minX, limitW, limitH);
        }

        /// <summary>开右键菜单前调：设置页和菜单互斥。</summary>
        public static void CloseIfOpen() { if (_inst != null && _inst.IsOpen) _inst.Close(); }

        /// <summary>HUD 每帧最先调。左键按在页外 → 关页并吞掉这一下，返回 true。页内/页外但页没开 → false。</summary>
        public static bool HandlePointer()
        {
            if (_inst == null || !_inst.IsOpen || _inst._panel == null || _inst._panel.panel == null) return false;
            return _inst.PointerSelf();
        }

        bool _over;

        bool PointerSelf()
        {
            var picked = _panel.panel.Pick(XqPanelXf.FromScreen(_panel.panel, Input.mousePosition));
            _over = Inside(_panel, picked);
            if (_needRelease)
            {
                if (Time.frameCount > _openFrame && !Input.GetMouseButton(0) && !Input.GetMouseButtonDown(0)) _needRelease = false;
                return _over && Input.GetMouseButtonDown(0);
            }
            if (Input.GetMouseButtonDown(0))
            {
                if (_over) return true; // 点在页里：页自己的控件处理，HUD 别当成点她
                if (_cascade && XqGlassMenu.OverCardNow) return false; // W5：点的是菜单（换分类 / 点别的行），交给菜单
                bool wasCascade = _cascade;
                Close();
                if (wasCascade) XqGlassMenu.CloseCurrent("blank");      // W5：点卡外空白：三级 + 菜单一起关
                _swallowFrame = Time.frameCount;
                return true;
            }
            return false;
        }

        VisualElement _host;
        VisualElement _panel;
        VisualElement[] _pageViews;
        Label[] _tabs;
        Label _title;
        VisualElement _hTabs;
        bool _cascade;
        float _cascT0;
        float _placedH = -1f;
        bool _cascLogged;
        int _page = 1;
        bool _built;
        bool _placed;
        bool _scheduled;
        bool _applied;
        bool _needRelease;
        int _openFrame = -10;

        // 连接页：订阅 XiaoqiLink.OnStage 记最近一次收到的时间，用来判断「已连接」
        XqSettingsRows.StatusRow _status;
        bool _stageHooked;
        bool _gotStage;
        float _lastStageAt;
        float _statusAt;

        public static void Open()
        {
            if (_inst == null) _inst = new XqSettingsPanel();
            _inst.OpenSelf(false, -1);
        }

        void OpenSelf(bool cascade, int page)
        {
            if (!_built && !Build()) return;
            if (IsOpen && _cascade == cascade && cascade)
            {
                if (page != _page) ShowPage(page);   // 三级里切分类：不重播入场
                return;
            }
            ApplyThemeClass(XqUi2Sample.CurrentTheme); // XQ: B1 第二轮 #14 主题以 XqUi2Sample 为准（它读命令行 -xq-theme），不再只读 cfg
            _cascade = cascade;
            _hTabs.style.display = cascade ? DisplayStyle.None : DisplayStyle.Flex;
            IsOpen = true;
            _needRelease = true; // XQ: B1 #9 打开它的那一下点击（菜单「设置」/近景 ⚙）还按着，等抬起之后才认「点页外关闭」
            _openFrame = Time.frameCount;
            _host.style.display = DisplayStyle.Flex;
            _host.BringToFront();            // W5b：近景里别被装饰卡压住
            _cascT0 = Time.unscaledTime; _cascLogged = false; _placedH = -1f;
            _panel.style.opacity = 0f;       // 量到高度、摆好位置之后再显出来，不在 (0,0) 闪一下
            _placed = false;
            ShowPage(page >= 0 ? page : _page);
            if (!_scheduled && _panel != null)
            {
                // TODO(编译员)：确认 IVisualElementScheduler（VisualElement.schedule.Execute(Action).Every(ms)，
                // UI Toolkit 标准 API，本仓库无先例）。用它代替每帧 Update，避免另建 MonoBehaviour。
                _panel.schedule.Execute(Tick).Every(16);
                _scheduled = true;
            }
            PetHost.I?.SetUiBlocksMovement(true);
            // 第一次打开时把存盘值重新应用一遍（§4 D4：不为列表单独再建 Update 循环）
            if (!_applied)
            {
                _applied = true;
                XqSettingsApply.ApplyAll(XiaoqiRuntime.Config);
            }
            Debug.Log("XQ_SET2 open cascade=" + cascade);
        }

        public void Close()
        {
            IsOpen = false;
            _cascade = false;
            if (_host != null) _host.style.display = DisplayStyle.None;
            PetHost.I?.SetUiBlocksMovement(false);
            Debug.Log("XQ_SET2 close");
        }

        bool Build()
        {
            var doc = PickDoc();
            var root = doc != null ? doc.rootVisualElement : null;
            if (root == null) { Debug.Log("XQ_SET2 no UIDocument"); return false; }
            Debug.Log("XQ_SET2 doc=" + doc.name);

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
            _panel.AddToClassList("xq-st-card");
            _panel.style.width = CardW;
            _panel.style.height = 380f;
            _panel.AddToClassList(ThemeClass(cfg != null ? cfg.theme : "pink"));
            var shadow = new VisualElement { pickingMode = PickingMode.Ignore };
            shadow.AddToClassList("xq-shadow");
            var glass = new VisualElement { pickingMode = PickingMode.Ignore };
            glass.AddToClassList("xq-st-glass");
            var hi = new VisualElement { pickingMode = PickingMode.Ignore };
            hi.AddToClassList("xq-hi");

            var frame = new VisualElement { pickingMode = PickingMode.Ignore };
            frame.AddToClassList("xq-st-frame2");
            var head = new VisualElement { pickingMode = PickingMode.Ignore };
            head.AddToClassList("xq-st-head");
            _title = new Label("设置");
            _title.AddToClassList("xq-st-title");
            _title.pickingMode = PickingMode.Ignore;
            head.Add(_title);
            var tabHost = new VisualElement { pickingMode = PickingMode.Ignore };
            tabHost.AddToClassList("xq-st-htabs");
            _hTabs = tabHost;
            head.Add(tabHost);
            frame.Add(head);
            var pageScroll = new ScrollView(ScrollViewMode.Vertical) { pickingMode = PickingMode.Position };
            pageScroll.style.flexGrow = 1f;
            pageScroll.style.flexShrink = 1f;
            pageScroll.verticalScrollerVisibility = ScrollerVisibility.Auto;
            pageScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            var pageHost = new VisualElement { pickingMode = PickingMode.Ignore };
            pageHost.AddToClassList("xq-st-pages");

            _tabs = new Label[Pages.Length];
            _pageViews = new VisualElement[Pages.Length];
            for (int i = 0; i < Pages.Length; i++)
            {
                int idx = i;
                var t = new Label(Pages[i]);
                t.AddToClassList("xq-st-htab");
                t.pickingMode = PickingMode.Position;
                t.RegisterCallback<ClickEvent>(e => { e.StopPropagation(); ShowPage(idx); });
                _tabs[i] = t;
                tabHost.Add(t);

                var p = new VisualElement { pickingMode = PickingMode.Ignore };
                p.AddToClassList("xq-st-page");
                _pageViews[i] = p;
                pageHost.Add(p);
            }

            pageScroll.Add(pageHost);
            frame.Add(pageScroll);
            _panel.Add(shadow);
            _panel.Add(glass);
            _panel.Add(hi);
            _panel.Add(frame);
            _host.Add(_panel);
            root.Add(_host);

            // D1 做「行为」页；D2 做「外观」「显示」；D3 做「声音」「连接」「关于」。全部有页，不再有空页。
            BuildLookPage(_pageViews[0]);
            BuildBehaviorPage(_pageViews[1]);
            BuildDisplayPage(_pageViews[2]);
            BuildSoundPage(_pageViews[3]);
            BuildConnectPage(_pageViews[4]);
            BuildAboutPage(_pageViews[5]);

            _built = true;
            return true;
        }

        /// <summary>
        /// 场景里可能不止一个 UIDocument。优先挑挂 XqUi2Sample 的、再挑名字含 Xq/Xiaoqi 的，
        /// 都没有才退回第一个，避免面板挂到别的面板根上出不来。
        /// </summary>
        static UIDocument PickDoc()
        {
            var docs = UnityEngine.Object.FindObjectsByType<UIDocument>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (docs == null || docs.Length == 0) return null;

            // 1) 挂在 XqUi2Sample 下面的（它的 XqUi2Document 子物体）
            var sample = UnityEngine.Object.FindFirstObjectByType<XqUi2Sample>(FindObjectsInactive.Include);
            if (sample != null)
            {
                foreach (var d in docs)
                    if (d != null && Under(d.transform, sample.transform)) return d;
            }
            // 2) 名字含 Xq / Xiaoqi（不分大小写）
            foreach (var d in docs)
            {
                if (d == null) continue;
                var n = d.name ?? "";
                if (n.IndexOf("Xq", StringComparison.OrdinalIgnoreCase) >= 0) return d;
                if (n.IndexOf("Xiaoqi", StringComparison.OrdinalIgnoreCase) >= 0) return d;
            }
            // 3) 退回第一个
            return docs[0];
        }

        /// <summary>顺着 parent 往上找，判断 t 是否在 root 之下（不用 GetComponentInParent）。</summary>
        static bool Under(Transform t, Transform root)
        {
            for (var p = t; p != null; p = p.parent)
                if (p == root) return true;
            return false;
        }

        /// <summary>行为页 7 行，默认值照 G59-P1 §6，一个不改。</summary>
        void BuildBehaviorPage(VisualElement page)
        {
            var cfg = XiaoqiRuntime.Config;

            // 1 坐在窗口上（默认开）。读 cfg：宿主现值可能已被 XiaoqiDefaults 复位
            page.Add(XqSettingsRows.Toggle("坐在窗口上",
                cfg != null ? cfg.window_sit : (PetHost.I != null && PetHost.I.WindowSitEnabled),
                v => Bind(XqSettingsApply.SetWindowSit, (c, x) => c.window_sit = x, "window_sit", v)));

            // 2 看向鼠标（开）
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
                Bind<float>(null, (c, v) => c.look_strength = v / 100f, "look_strength", f);
            };
            page.Add(look.Root);

            // 5 放音乐时自己跳（默认关）。读 cfg：宿主现值可能已被 XiaoqiDefaults 复位
            page.Add(XqSettingsRows.Toggle("放音乐时自己跳",
                cfg != null ? cfg.dance_on_music : XqSettingsApply.GetDancing(),
                v => Bind(XqSettingsApply.SetDancing, (c, x) => c.dance_on_music = x, "dance_on_music", v)));

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

        /// <summary>外观页 5 行（照 G59-P1 §6）。</summary>
        void BuildLookPage(VisualElement page)
        {
            var cfg = XiaoqiRuntime.Config;
            var host = PetHost.I;

            // 1 大小：滑条，现值，双击回 1.00（ME 的 avatarSize 默认值）
            var size = XqSettingsRows.Slider("大小", 0.5f, 2f,
                host != null ? host.Size : 1f, v => { if (PetHost.I != null) PetHost.I.Size = v; }, 1f);
            size.Format = "0.00";
            size.Refresh();
            // 松手 / 双击回默认时才落盘，拖动过程中不写盘
            size.OnCommit = () => { SaveHost(); Bind<float>(null, null, "size", size.Value); };
            page.Add(size.Root);

            // 2 画质：G59-P1 §6 只列低/中/高；ME 的 graphicsQualityLevel 允许 0-3，
            //    第 4 档（最高）不在表里，不画。超出的值只显示到「高」，不动宿主。
            int q = host != null ? Mathf.Clamp(host.Quality, 0, 2) : 1;
            page.Add(XqSettingsRows.Dropdown("画质", new[] { "低", "中", "高" }, q,
                i => { Bind<int>(v => { if (PetHost.I != null) PetHost.I.Quality = v; }, null, "quality", i); SaveHost(); }));

            // 3 帧率：现值，选项照旧设置页（30/60/90/144）
            int[] fpsOpts = { 30, 60, 90, 144 };
            int fps = host != null ? host.Fps : 60;
            int fpsIdx = 0;
            for (int i = 0; i < fpsOpts.Length; i++)
                if (Mathf.Abs(fpsOpts[i] - fps) < Mathf.Abs(fpsOpts[fpsIdx] - fps)) fpsIdx = i;
            page.Add(XqSettingsRows.Dropdown("帧率", new[] { "30", "60", "90", "144" }, fpsIdx,
                i => { Bind<int>(v => { if (PetHost.I != null) PetHost.I.Fps = v; }, null, "fps", fpsOpts[i]); SaveHost(); }));

            // 4 主题色：3 个色点，粉 / 蓝 / 紫，默认粉
            var dotColors = new[]
            {
                new Color(1f, 0.56f, 0.72f, 1f),  // #FF8FB8 粉
                new Color(0.35f, 0.66f, 1f, 1f),  // #5AA8FF 蓝
                new Color(0.69f, 0.52f, 1f, 1f),  // #B184FF 紫
            };
            string theme = cfg != null ? cfg.theme : "pink";
            int themeIdx = theme == "blue" ? 1 : (theme == "violet" || theme == "purple") ? 2 : 0;
            page.Add(XqSettingsRows.Dots("主题色", new[] { "粉", "蓝", "紫" }, dotColors, themeIdx,
                i =>
                {
                    var name = i == 1 ? "blue" : i == 2 ? "violet" : "pink";
                    XqUi2Sample.SetTheme(name);
                    ApplyThemeClass(name);
                    Bind<string>(null, (c, v) => c.theme = v, "theme", name);
                }));

            // 5 模型：一行内放路径灰字 + 「重新加载」按钮
            string path = cfg != null ? cfg.model_path : "";
            string shortPath = string.IsNullOrEmpty(path) ? "" : System.IO.Path.GetFileName(path);   // W5b：整条路径在窄卡里把标签挤成竖排
            page.Add(XqSettingsRows.Button("模型", shortPath, "重新加载", () =>
            {
                var p = XiaoqiRuntime.Config != null ? XiaoqiRuntime.Config.model_path : null;
                if (string.IsNullOrEmpty(p)) { Debug.Log("XQ_SET2 miss model_path"); return; }
                if (PetHost.I == null) { Debug.Log("XQ_SET2 miss PetHost"); return; }
                PetHost.I.LoadModel(p);
                Debug.Log("XQ_SET2 set model=reload");
            }));
        }

        /// <summary>显示页 5 行（照 G59-P1 §6；「她在哪块屏」现在不做，整行不画）。</summary>
        void BuildDisplayPage(VisualElement page)
        {
            var cfg = XiaoqiRuntime.Config;
            var host = PetHost.I;

            // 1 始终置顶：开
            page.Add(XqSettingsRows.Toggle("始终置顶",
                host != null ? host.Topmost : (cfg == null || cfg.topmost),
                v =>
                {
                    Bind<bool>(x => { if (PetHost.I != null) PetHost.I.Topmost = x; },
                        (c, x) => c.topmost = x, "topmost", v);
                    SaveHost();
                }));

            // 2 全屏或游戏时躲开：开。XqFullscreenHide 每秒读 cfg，改了即生效
            page.Add(XqSettingsRows.Toggle("全屏或游戏时躲开",
                cfg == null || cfg.hide_on_fullscreen,
                v => Bind<bool>(null, (c, x) => c.hide_on_fullscreen = x, "hide_on_fullscreen", v)));

            // 3 身边小胶囊：常驻 / 淡出 / 全关，默认淡出
            string chips = cfg != null ? cfg.chips : "fade";
            int chipsIdx = chips == "stay" ? 0 : chips == "off" ? 2 : 1;
            page.Add(XqSettingsRows.Dropdown("身边小胶囊",
                new[] { "常驻", "淡出", "全关" }, chipsIdx,
                i => Bind<string>(null, (c, v) => c.chips = v, "chips",
                    i == 0 ? "stay" : i == 2 ? "off" : "fade")));

            // 4 她说话的气泡：开
            page.Add(XqSettingsRows.Toggle("她说话的气泡",
                cfg == null || cfg.bubble,
                v => Bind<bool>(null, (c, x) => c.bubble = x, "bubble", v)));

            // 5 她外出时：选项文字照 G55 原样，默认读现值不改
            bool hide = cfg != null && cfg.pet_when_out == "hide";
            page.Add(XqSettingsRows.Dropdown("她外出时",
                new[] { "照常显示", "隐藏" }, hide ? 1 : 0,
                i => Bind<string>(null, (c, v) => c.pet_when_out = v, "pet_when_out",
                    i == 1 ? "hide" : "show")));
        }

        /// <summary>声音页 2 行：静音、总音量。默认关 / 100%。</summary>
        void BuildSoundPage(VisualElement page)
        {
            var cfg = XiaoqiRuntime.Config;

            // 1 静音：关
            page.Add(XqSettingsRows.Toggle("静音",
                cfg != null && cfg.muted,
                v => Bind<bool>(XqSettingsApply.SetMuted, (c, x) => c.muted = x, "muted", v)));

            // 2 总音量：0-100%，拖动即时应用（不打日志），松手调 CommitVolume 落盘并打一行
            var vol = XqSettingsRows.Slider("总音量", 0f, 100f,
                (cfg != null ? cfg.volume : 1f) * 100f,
                v => XqSettingsApply.SetVolume(v / 100f), 100f);
            vol.Suffix = "%";
            vol.Refresh();
            vol.OnCommit = () => XqSettingsApply.CommitVolume(vol.Value / 100f);
            page.Add(vol.Root);

            // 3 V6 按住说话热键 & 自动语音回复
            var hotkeys = new[] { "N", "V", "B", "Space" };
            int hkIdx = Array.IndexOf(hotkeys, cfg != null && !string.IsNullOrEmpty(cfg.voice_hotkey) ? cfg.voice_hotkey : "N");
            if (hkIdx < 0) hkIdx = 0;
            page.Add(XqSettingsRows.Dropdown("按住说话热键", hotkeys, hkIdx, idx =>
            {
                if (XiaoqiRuntime.Config == null || idx < 0 || idx >= hotkeys.Length) return;
                XiaoqiRuntime.Config.voice_hotkey = hotkeys[idx];
                XiaoqiConfig.Save(XiaoqiRuntime.Config);
            }));

            page.Add(XqSettingsRows.Toggle("自动语音回复",
                cfg == null || cfg.voice_tts_auto,
                v =>
                {
                    if (XiaoqiRuntime.Config == null) return;
                    XiaoqiRuntime.Config.voice_tts_auto = v;
                    XiaoqiConfig.Save(XiaoqiRuntime.Config);
                }));

            // 打开面板时按 cfg 应用一次，避免别人改过 AudioListener 后这里显示与听感不一致
            XqSettingsApply.ApplyVolume();
        }

        /// <summary>
        /// 连接页 5 行（照 G59-P1 §6）。不改 XiaoqiLink，只订阅它的 OnStage 记最近一次收到的时间。
        /// 判定：Time.unscaledTime - _lastStageAt &lt; 3 × poll_sec + 5 算已连接。
        /// </summary>
        void BuildConnectPage(VisualElement page)
        {
            var cfg = XiaoqiRuntime.Config;

            // 1 连接状态：灯 + 文字 + 「测试」按钮（只打日志 + 刷新判断，不发请求）
            _status = XqSettingsRows.Status("连接状态", "测试", () =>
            {
                Debug.Log("XQ_SET2 test");
                RefreshStatus();
            });
            page.Add(_status.Root);
            if (!_stageHooked)
            {
                XiaoqiLink.OnStage += OnStageTick;
                _stageHooked = true;
            }
            RefreshStatus();

            // 2 服务器地址：日志里只打长度，不打地址（任务书 §5.10）
            page.Add(XqSettingsRows.Input("服务器地址",
                cfg != null ? cfg.server : "",
                false,
                v =>
                {
                    try
                    {
                        var cur = XiaoqiRuntime.Config;
                        if (cur == null) { Debug.Log("XQ_SET2 miss cfg"); return; }
                        cur.server = v ?? "";
                        XiaoqiConfig.Save(cur);
                        Debug.Log("XQ_SET2 set server len=" + cur.server.Length);
                    }
                    catch (Exception e) { Debug.LogWarning("XQ_SET2 " + e.Message); }
                }).Root);

            // 3 令牌：密码框 + 「粘贴」。日志里永远不打印令牌，只打长度
            page.Add(XqSettingsRows.Input("令牌",
                cfg != null ? cfg.token : "",
                true,
                v =>
                {
                    try
                    {
                        var cur = XiaoqiRuntime.Config;
                        if (cur == null) { Debug.Log("XQ_SET2 miss cfg"); return; }
                        cur.token = v ?? "";
                        XiaoqiConfig.Save(cur);
                        Debug.Log("XQ_SET2 set token len=" + cur.token.Length);
                    }
                    catch (Exception e) { Debug.LogWarning("XQ_SET2 " + e.Message); }
                },
                "粘贴", row =>
                {
                    // TODO(编译员)：确认 GUIUtility.systemCopyBuffer（Unity 标准 API，本仓库无先例）
                    var s = GUIUtility.systemCopyBuffer;
                    if (string.IsNullOrEmpty(s)) { Debug.Log("XQ_SET2 paste empty"); return; }
                    row.SetText(s);
                    var cur = XiaoqiRuntime.Config;
                    if (cur == null) { Debug.Log("XQ_SET2 miss cfg"); return; }
                    cur.token = row.Value;
                    XiaoqiConfig.Save(cur);
                    Debug.Log("XQ_SET2 set token len=" + row.Value.Length);
                }).Root);

            // 4 情感线：关
            page.Add(XqSettingsRows.Toggle("情感线",
                cfg != null && cfg.emotion_line,
                v => Bind<bool>(null, (c, x) => c.emotion_line = x, "emotion_line", v)));

            // 5 开机自启：关（现值由宿主注册表决定）
            page.Add(XqSettingsRows.Toggle("开机自启",
                PetHost.I != null && PetHost.I.StartWithWindows,
                v => Bind<bool>(x => { if (PetHost.I != null) PetHost.I.StartWithWindows = x; },
                    null, "start_with_windows", v)));
        }

        /// <summary>关于页 4 行：版本、ME 署名、模型授权、打开日志目录。</summary>
        void BuildAboutPage(VisualElement page)
        {
            // 1 版本 / 提交号：读不到就写「开发版」
            string ver = Application.version;
            if (string.IsNullOrEmpty(ver)) ver = "开发版";
            page.Add(XqSettingsRows.Text("版本 / 提交号", ver));

            // 2 ME 署名与许可：「查看」调现有 XiaoqiAbout.Show()，不重写内容
            page.Add(XqSettingsRows.Button("ME 署名与许可", "", "查看", () =>
            {
                XiaoqiAbout.Show();
                Debug.Log("XQ_SET2 about");
            }));

            // 3 模型授权：一两行文字
            page.Add(XqSettingsRows.Note("16.vrm：主人购买，授权说明见 docs/ASSETS.md"));

            // 4 打开日志目录
            page.Add(XqSettingsRows.Button("打开日志目录", "", "打开", OpenLogDir));
        }

        static void OpenLogDir()
        {
            try
            {
                var dir = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Xiaoqi");
                if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
                System.Diagnostics.Process.Start(dir);
                Debug.Log("XQ_SET2 logdir=" + dir);
            }
            catch (Exception e)
            {
                Debug.LogWarning("XQ_SET2 logdir " + e.Message);
            }
        }

        /// <summary>收到一次 stage 就记下时间，用来判断「已连接」。</summary>
        void OnStageTick(StageData s)
        {
            _lastStageAt = Time.unscaledTime;
            _gotStage = true;
            if (IsOpen) RefreshStatus();
        }

        /// <summary>3 × poll_sec + 5 秒内收到过算已连接；从没收到过是未知。</summary>
        void RefreshStatus()
        {
            if (_status == null) return;
            if (!_gotStage) { _status.Set("未知", 0); return; }
            var cfg = XiaoqiRuntime.Config;
            int poll = cfg != null ? Mathf.Max(1, cfg.poll_sec) : 2;
            bool ok = Time.unscaledTime - _lastStageAt < 3f * poll + 5f;
            _status.Set(ok ? "已连接" : "连不上", ok ? 1 : 2);
        }

        /// <summary>
        /// 落盘一次宿主设置（Handler.SaveToDisk + ApplyAll）。
        /// 只在「提交」时调：滑条拖动每帧调会频繁写盘。
        /// </summary>
        static void SaveHost()
        {
            try
            {
                var h = PetHost.I;
                if (h == null) { Debug.Log("XQ_SET2 miss PetHost"); return; }
                h.SaveSettings();
            }
            catch (Exception e)
            {
                Debug.LogWarning("XQ_SET2 " + e.Message);
            }
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
                if (_tabs[k] != null) _tabs[k].EnableInClassList("xq-st-htab-on", k == _page);
            if (_title != null) _title.text = _cascade ? Pages[_page] : "设置";
            Debug.Log("XQ_SET2 page " + Pages[_page]);
        }

        static float Hei(VisualElement el, float fb)
        {
            float h = el.resolvedStyle.height;
            return float.IsNaN(h) || h < 8f ? fb : h;
        }

        static float Ease(float t) { t = Mathf.Clamp01(t); return 1f - Mathf.Pow(1f - t, 3f); }

        /// <summary>三级卡：贴着二级卡外侧，顶对齐选中的那一行；不出可用区（minX / limitW / limitH 由菜单算好传来）。</summary>
        void PlaceCascade(Rect l2, Rect row, bool toRight, float minX, float limitW, float limitH)
        {
            if (_panel == null || _panel.panel == null) return;
            float hMeasured = _panel.resolvedStyle.height;
            if (float.IsNaN(hMeasured) || hMeasured < 8f) return;      // 还没布局出来，先不显示
            const float gap = 12f;
            float w = CardW, h = hMeasured;
            float x = toRight ? l2.xMax + gap : l2.xMin - gap - w;
            bool squeeze = x < minX || x + w > limitW - 8f;
            x = Mathf.Clamp(x, minX, Mathf.Max(minX, limitW - w - 8f));
            float rowTop = (!float.IsNaN(row.y) && row.y > 0f) ? row.y - 6f : l2.y;
            float y = Mathf.Clamp(rowTop, 8f, Mathf.Max(8f, limitH - h - 8f));
            float k = Ease((Time.unscaledTime - _cascT0) / 0.18f);
            _panel.style.left = x;
            _panel.style.top = y;
            _panel.style.opacity = k;
            _panel.style.translate = new Translate((1f - k) * 10f * (toRight ? -1f : 1f), 0f, 0f);
            _placed = true;
            if (!_cascLogged)
            {
                _cascLogged = true;
                Debug.Log("XQ_SET2 cascade place page=" + Pages[_page] + " x=" + x.ToString("0") + " y=" + y.ToString("0") + " size=" + w.ToString("0") + "x" + h.ToString("0")
                    + " side=" + (toRight ? "R" : "L") + " limit=" + limitW.ToString("0") + "x" + limitH.ToString("0") + (squeeze ? " squeeze" : ""));
            }
        }

        void Place()
        {
            if (_panel == null || _host == null || _panel.panel == null) return;
            if (_cascade) return;
            _placed = true;
            _placedH = 380f;
            // V6：固定紧凑高度 380px，垂直贴近小祈身体中心，切页签永不跳动。
            var body = XqBodyAnchor.Stable();
            var bc = body.height >= 8f ? body.center : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            var wa = XqMonitor.WorkArea(bc);
            float dpi = XqMonitor.UiScale(bc);
            float upp = Mathf.Max(0.01f, XqPanelXf.UnitsPerPx(_panel.panel));
            float h = Mathf.Min(_placedH / upp, wa.height - 24f);
            float w = Mathf.Min(CardW / upp, wa.width - 24f);
            const float gap = 16f;
            float targetY = body.height >= 8f ? body.center.y : wa.center.y;
            float cx, cy = Mathf.Clamp(targetY, wa.yMin + h * 0.5f + 12f, wa.yMax - h * 0.5f - 12f);
            string side;
            float roomL = body.height >= 8f ? body.xMin - wa.xMin : 0f;
            float roomR = body.height >= 8f ? wa.xMax - body.xMax : 0f;
            if (body.height < 8f || (roomL < w + gap && roomR < w + gap)) { cx = wa.center.x; side = "center"; }
            else if (roomR >= roomL) { cx = body.xMax + gap + w * 0.5f; side = "R"; }
            else { cx = body.xMin - gap - w * 0.5f; side = "L"; }
            cx = Mathf.Clamp(cx, wa.xMin + w * 0.5f + 12f, wa.xMax - w * 0.5f - 12f);
            Debug.Log("XQ_SET2 place side=" + side + " wa=" + wa.xMin.ToString("0") + "," + wa.yMin.ToString("0") + " " + wa.width.ToString("0") + "x" + wa.height.ToString("0") + " size=" + w.ToString("0") + "x" + h.ToString("0") + " dpi=" + dpi.ToString("0.00"));
            var a = XqPanelXf.FromScreen(_panel.panel, new Vector2(cx - w * 0.5f, cy + h * 0.5f)); // XQ: B1 #4
            var b = XqPanelXf.FromScreen(_panel.panel, new Vector2(cx + w * 0.5f, cy - h * 0.5f));
            _panel.style.left = a.x;
            _panel.style.top = a.y;
            _panel.style.opacity = 1f;
            _panel.style.translate = new Translate(0f, 0f, 0f);
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
            if (!_cascade && !_placed) Place();   // V6：固定高度 380px，首次放置后不再随页签跳动
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                bool wasCascade = _cascade;
                Close();
                if (wasCascade) XqGlassMenu.CloseCurrent("esc");   // W5：Esc 统一关（三级 + 菜单）
                return;
            }
            // 连接状态每半秒刷一次（跟 Tick 的 16ms 节流分开，别每帧改文字）
            if (Time.unscaledTime - _statusAt > 0.5f) { _statusAt = Time.unscaledTime; RefreshStatus(); }
            // XQ: B1 第二轮 #13 点页外关闭 / 挡拖动都挪到 HandlePointer（HUD 每帧最先调）和 XqUi2Sample.Update（统一设挡板）。
            // 之前这里和 Sample.Update 两头各自 SetUiBlocksMovement，谁后跑谁说了算，Sample 每帧把它改回 false，点设置页里面就漏到她身上。
            var picked = _panel.panel.Pick(XqPanelXf.FromScreen(_panel.panel, Input.mousePosition));
            _over = Inside(_panel, picked);
        }

        static string ThemeClass(string theme)
        {
            if (string.IsNullOrEmpty(theme)) return Pink;
            var t = theme.Trim().ToLowerInvariant();
            if (t == "blue") return Blue;
            if (t == "violet" || t == "purple") return Violet;
            return Pink;
        }

        /// <summary>面板根跟着换主题 class（颜色变量都在根 class 上）。</summary>
        void ApplyThemeClass(string theme)
        {
            if (_panel == null) return;
            _panel.RemoveFromClassList(Pink);
            _panel.RemoveFromClassList(Blue);
            _panel.RemoveFromClassList(Violet);
            _panel.AddToClassList(ThemeClass(theme));
        }
    }
}
