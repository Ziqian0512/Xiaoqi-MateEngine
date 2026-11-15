using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using Xiaoqi.Host;
using Xiaoqi.Spike;

namespace Xiaoqi.UI2
{
    /// <summary>
    /// P1-1 最小样品：气泡（真三角尾巴）+ 药丸 + 方钮，锚在 HeadAnchor 旁边。
    /// 点击穿透不改 UniWindowController。屏幕叠加的 UIDocument 由
    /// UIElementsRuntimeUtility 挂在 Canvas.externRenderOverlaysBefore 上，
    /// 和 uGUI 同一条 overlay，发生在 WaitForEndOfFrame 之前。
    /// HitTestCoroutine 在那一帧末尾 ReadPixels，GetOnOpaquePixel 用 alpha ≥ 0.1
    /// （Mate Engine Main 的 hitTestType = Opacity）区分样品和透明区。
    /// 6000.2.6f2 的 PanelSettings 没有公开 renderMode。targetTexture 为空时画到屏幕
    /// （手册里 Target Texture 属于 Screen Space Overlay，设了才改去贴图）。
    /// clearColor 保持关闭，避免整屏清成不透明。
    /// 换蓝色主题：命令行 -xq-theme blue，或 XqUi2Sample.SetTheme("blue")。
    /// 指针在样品上时经 MeHost.SetUiBlocksMovement 挡住「任意左键即拖动」，不改穿透判断。
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class XqUi2Sample : MonoBehaviour
    {
        const float RefHeight = 364f;
        const float FontPx = 16f;
        const string Pink = "xq-theme-pink";
        const string Blue = "xq-theme-blue";
        const string Violet = "xq-theme-violet";

        static readonly CustomStyleProperty<Color> AccentColor = new CustomStyleProperty<Color>("--xq-accent");
        static XqUi2Sample _instance;

        [SerializeField] PanelSettings panel;
        [SerializeField] StyleSheet uss;
        [SerializeField] Font font;
        [SerializeField] GameObject documentPrefab;

        PanelSettings _runtimePanel;
        UIDocument _doc;
        VisualElement _root;
        VisualElement _sample;
        VisualElement _bubble;
        VisualElement _pill;
        VisualElement _btn;
        XqTail _tail;
        string _theme = "pink";
        bool _fading;
        bool _logged;
        HeadAnchor _head;
        float _nextHeadFind;

        public static void SetTheme(string theme)
        {
            if (_instance == null)
                _instance = FindFirstObjectByType<XqUi2Sample>(FindObjectsInactive.Include);
            if (_instance == null) return;
            _instance.ApplyTheme(theme);
        }

        void Awake()
        {
            _instance = this;
            _theme = ThemeFromArgs();
            ResolveAssets();
            if (panel == null || uss == null || font == null)
            {
                Debug.LogError("XQ_UI2 missing panel=" + (panel != null) + " uss=" + (uss != null) + " font=" + (font != null));
                enabled = false;
                return;
            }

            _runtimePanel = Instantiate(panel);
            _runtimePanel.name = "XqPanelSettingsRuntime";
            _runtimePanel.scaleMode = PanelScaleMode.ConstantPixelSize;
            _runtimePanel.clearColor = false;
            _runtimePanel.colorClearValue = Color.clear;
            _runtimePanel.targetTexture = null;
            _runtimePanel.sortingOrder = 360;

            var go = SpawnDocument();
            _doc = go.GetComponent<UIDocument>();
            if (_doc == null) _doc = go.AddComponent<UIDocument>();
            _doc.visualTreeAsset = null;
            _doc.sortingOrder = 360;
            _doc.panelSettings = _runtimePanel;

            _root = _doc.rootVisualElement;
            _root.Clear();
            _root.pickingMode = PickingMode.Ignore;
            _root.style.backgroundColor = new Color(0f, 0f, 0f, 0f);
            _root.style.unityFont = font;
            _root.styleSheets.Add(uss);
            _root.AddToClassList("xq-root");
            ApplyTheme(_theme);
            Build(_root);
        }

        void OnDestroy()
        {
            if (_instance == this) _instance = null;
            if (_runtimePanel != null) Destroy(_runtimePanel);
        }

        void LateUpdate()
        {
            if (_root == null || _sample == null || _runtimePanel == null) return;
            if (_root.panel == null) return;
            if (_tail != null && _root.customStyle.TryGetValue(AccentColor, out var accent))
                _tail.SetColor(accent);

            bool hidden = PetHost.I != null && !PetHost.I.AvatarVisible;
            if (hidden || !TryHead(out var screen))
            {
                _sample.style.display = DisplayStyle.None;
                return;
            }
            _sample.style.display = DisplayStyle.Flex;

            float bodyH = 0f;
            if (PetHost.I != null) bodyH = PetHost.I.ScreenRect().height;
            float scale = bodyH >= 8f ? Mathf.Clamp(bodyH / RefHeight, 0.8f, 2f) : 1f;
            if (Mathf.Abs(_runtimePanel.scale - scale) > 0.01f)
                _runtimePanel.scale = scale;

            float w = _sample.worldBound.width;
            float h = _sample.worldBound.height;
            if (float.IsNaN(w) || w < 2f || float.IsNaN(h) || h < 2f)
            {
                _sample.style.left = -10000f;
                return;
            }

            var p = RuntimePanelUtils.ScreenToPanel(_root.panel, screen);
            float gap = 28f;
            float left = p.x + gap;
            float top = p.y - 16f;
            float limit = _root.worldBound.width;
            bool onLeft = left + w > limit - 8f;
            if (onLeft) left = p.x - gap - w;
            if (limit > 8f)
            {
                left = Mathf.Clamp(left, 8f, Mathf.Max(8f, limit - w - 8f));
                float limitH = _root.worldBound.height;
                top = Mathf.Clamp(top, 8f, Mathf.Max(8f, limitH - h - 8f));
            }
            _sample.style.left = left;
            _sample.style.top = top;
            _tail.PointLeft = !onLeft;

            if (!_logged && bodyH >= 8f)
            {
                _logged = true;
                float fontPx = FontPx * _runtimePanel.scale;
                Debug.Log("XQ_UI2 on theme=" + _theme
                    + " class=" + ThemeClass(_theme)
                    + " scale=" + _runtimePanel.scale.ToString("0.00")
                    + " bodyH=" + bodyH.ToString("0")
                    + " fontPx=" + fontPx.ToString("0.0")
                    + " scaleMode=" + _runtimePanel.scaleMode
                    + " clearColor=" + _runtimePanel.clearColor
                    + " hook=GetOnOpaquePixel");
                if (fontPx < 12f) Debug.LogWarning("XQ_UI2 font below 12px");
            }
        }

        void Update()
        {
            if (_root == null || _root.panel == null)
            {
                PetHost.I?.SetUiBlocksMovement(false);
                return;
            }
            bool over = Hit(_sample);
            PetHost.I?.SetUiBlocksMovement(over);
            if (!Input.GetMouseButtonDown(0)) return;
            if (!_fading && Hit(_bubble)) FadeBubble();
            else if (Hit(_pill)) Debug.Log("XQ_UI2 click pill");
            else if (Hit(_btn)) Debug.Log("XQ_UI2 click button");
        }

        GameObject SpawnDocument()
        {
            if (documentPrefab != null && documentPrefab.GetComponent<UIDocument>() != null)
            {
                var inst = Instantiate(documentPrefab, transform);
                inst.name = "XqUi2Document";
                return inst;
            }
            var go = new GameObject("XqUi2Document");
            go.transform.SetParent(transform, false);
            return go;
        }

        void Build(VisualElement root)
        {
            _sample = new VisualElement { name = "xq-sample", pickingMode = PickingMode.Ignore };
            _sample.AddToClassList("xq-sample");

            _bubble = new VisualElement { name = "xq-bubble", pickingMode = PickingMode.Ignore };
            _bubble.AddToClassList("xq-bubble");
            _tail = new XqTail();
            _tail.AddToClassList("xq-tail");
            var bubbleBody = Body(false);
            bubbleBody.RegisterCallback<ClickEvent>(_ => FadeBubble());
            _tail.RegisterCallback<ClickEvent>(_ => FadeBubble());
            _bubble.Add(_tail);
            _bubble.Add(bubbleBody);

            var row = new VisualElement { name = "xq-row", pickingMode = PickingMode.Ignore };
            row.AddToClassList("xq-row");
            _pill = new VisualElement { name = "xq-pill", pickingMode = PickingMode.Ignore };
            _pill.AddToClassList("xq-pill");
            _pill.Add(Body(false));
            _btn = new VisualElement { name = "xq-btn", pickingMode = PickingMode.Ignore };
            _btn.AddToClassList("xq-btn");
            _btn.Add(Body(true));
            row.Add(_pill);
            row.Add(_btn);

            _sample.Add(_bubble);
            _sample.Add(row);
            root.Add(_sample);
            _sample.style.display = DisplayStyle.None;
        }

        static VisualElement Body(bool emph)
        {
            var body = new VisualElement { name = "xq-body" };
            body.AddToClassList("xq-body");
            var glass = new VisualElement();
            glass.AddToClassList("xq-glass");
            if (emph) glass.AddToClassList("xq-glass-emph");
            var hi = new VisualElement();
            hi.AddToClassList("xq-hi");
            var label = new Label("测试");
            label.AddToClassList("xq-label");
            body.Add(glass);
            body.Add(hi);
            body.Add(label);
            return body;
        }

        void FadeBubble()
        {
            if (_fading || _bubble == null) return;
            _fading = true;
            Debug.Log("XQ_UI2 click bubble -> fade");
            StartCoroutine(Fade());
        }

        IEnumerator Fade()
        {
            const float dur = 0.2f;
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                if (_bubble != null) _bubble.style.opacity = Mathf.Clamp01(1f - t / dur);
                yield return null;
            }
            if (_bubble == null) yield break;
            _bubble.style.opacity = 0f;
            _bubble.style.display = DisplayStyle.None;
            _bubble.pickingMode = PickingMode.Ignore;
            if (_tail != null) _tail.pickingMode = PickingMode.Ignore;
        }

        void ApplyTheme(string theme)
        {
            _theme = Normalize(theme);
            if (_root == null) return;
            _root.RemoveFromClassList(Pink);
            _root.RemoveFromClassList(Blue);
            _root.RemoveFromClassList(Violet);
            _root.AddToClassList(ThemeClass(_theme));
            Debug.Log("XQ_UI2 theme=" + _theme + " class=" + ThemeClass(_theme));
        }

        bool Hit(VisualElement target)
        {
            if (target == null || _root == null || _root.panel == null) return false;
            var picked = _root.panel.Pick(RuntimePanelUtils.ScreenToPanel(_root.panel, Input.mousePosition));
            for (var e = picked; e != null; e = e.parent)
                if (e == target) return true;
            return false;
        }

        bool TryHead(out Vector2 screen)
        {
            screen = default;
            if (_head == null && Time.unscaledTime >= _nextHeadFind)
            {
                _nextHeadFind = Time.unscaledTime + 0.5f;
                _head = FindFirstObjectByType<HeadAnchor>(FindObjectsInactive.Include);
            }
            if (_head == null) return false;
            screen = _head.ScreenPoint;
            return screen.sqrMagnitude > 1f;
        }

        void ResolveAssets()
        {
#if UNITY_EDITOR
            if (panel == null)
                panel = UnityEditor.AssetDatabase.LoadAssetAtPath<PanelSettings>(XqUi2Paths.Panel);
            if (uss == null)
                uss = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(XqUi2Paths.Uss);
            if (font == null)
                font = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>(XqUi2Paths.Font);
            if (documentPrefab == null)
                documentPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(XqUi2Paths.Document);
#endif
        }

        static string ThemeFromArgs()
        {
            var a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length; i++)
            {
                var s = a[i];
                if ((s == "-xq-theme" || s == "--xq-theme") && i + 1 < a.Length)
                    return Normalize(a[i + 1]);
                const string a1 = "-xq-theme=";
                const string a2 = "--xq-theme=";
                if (s.StartsWith(a1)) return Normalize(s.Substring(a1.Length));
                if (s.StartsWith(a2)) return Normalize(s.Substring(a2.Length));
            }
            return "pink";
        }

        static string Normalize(string theme)
        {
            if (string.IsNullOrEmpty(theme)) return "pink";
            theme = theme.Trim().ToLowerInvariant();
            if (theme == "blue" || theme == Blue) return "blue";
            if (theme == "violet" || theme == "purple" || theme == Violet) return "violet";
            return "pink";
        }

        static string ThemeClass(string theme)
        {
            if (theme == "blue") return Blue;
            if (theme == "violet") return Violet;
            return Pink;
        }

        sealed class XqTail : VisualElement
        {
            Color _color = new Color(1f, 0.56f, 0.72f, 1f);
            bool _left = true;

            public XqTail()
            {
                generateVisualContent += Draw;
                pickingMode = PickingMode.Position;
                RegisterCallback<GeometryChangedEvent>(_ => MarkDirtyRepaint());
                RegisterCallback<CustomStyleResolvedEvent>(_ =>
                {
                    if (customStyle.TryGetValue(AccentColor, out var c))
                        SetColor(c);
                });
            }

            public bool PointLeft
            {
                get => _left;
                set
                {
                    if (_left == value) return;
                    _left = value;
                    EnableInClassList("xq-point-right", !value);
                    MarkDirtyRepaint();
                }
            }

            public void SetColor(Color color)
            {
                if (_color == color) return;
                _color = color;
                MarkDirtyRepaint();
            }

            void Draw(MeshGenerationContext ctx)
            {
                float w = contentRect.width;
                float h = contentRect.height;
                if (w < 1f || h < 1f) return;
                var p = ctx.painter2D;
                p.fillColor = _color;
                p.BeginPath();
                if (_left)
                {
                    p.MoveTo(new Vector2(0f, h * 0.5f));
                    p.LineTo(new Vector2(w, 0f));
                    p.LineTo(new Vector2(w, h));
                }
                else
                {
                    p.MoveTo(new Vector2(w, h * 0.5f));
                    p.LineTo(new Vector2(0f, 0f));
                    p.LineTo(new Vector2(0f, h));
                }
                p.ClosePath();
                p.Fill();
            }
        }
    }
}
