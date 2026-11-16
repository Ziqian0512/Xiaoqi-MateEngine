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

        // 连接页：订阅 XiaoqiLink.OnStage 记最近一次收到的时间，用来判断「已连接」
        XqSettingsRows.StatusRow _status;
        bool _stageHooked;
        bool _gotStage;
        float _lastStageAt;
        float _statusAt;

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
            page.Add(XqSettingsRows.Button("模型", path, "重新加载", () =>
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

            // 2 总音量：0-100%，拖动即时应用，松手落盘
            var vol = XqSettingsRows.Slider("总音量", 0f, 100f,
                (cfg != null ? cfg.volume : 1f) * 100f,
                v => XqSettingsApply.SetVolume(v / 100f), 100f);
            vol.Suffix = "%";
            vol.Refresh();
            vol.OnCommit = () => Bind<float>(null, (c, v) => c.volume = v / 100f, "volume", vol.Value / 100f);
            page.Add(vol.Root);

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
            // 连接状态每半秒刷一次（跟 Tick 的 16ms 节流分开，别每帧改文字）
            if (Time.unscaledTime - _statusAt > 0.5f) { _statusAt = Time.unscaledTime; RefreshStatus(); }
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
