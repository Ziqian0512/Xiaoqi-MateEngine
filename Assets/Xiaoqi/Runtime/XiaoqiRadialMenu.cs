using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Xiaoqi's own radial menu. Replaces Mate Engine's pie menu: custom icon set,
/// Chinese labels, dark-glass look and Xiaoqi-only actions.
/// Right click the pet to open, right click / Esc / click outside to close.
/// </summary>
public class XiaoqiRadialMenu : MonoBehaviour
{
    private const int CanvasOrder = 320;

    private float _menuPx, _btnR, _btnD, _iconPx, _hubPx, _s;

    private static readonly Color Accent = new Color(0.949f, 0.651f, 0.722f, 1f);      // #F2A6B8
    private static readonly Color AccentSoft = new Color(0.949f, 0.651f, 0.722f, 0.16f);
    private static readonly Color IconIdle = new Color(0.965f, 0.933f, 0.905f, 0.95f);
    private static readonly Color IconHot = new Color(1f, 0.851f, 0.882f, 1f);
    private static readonly Color LabelIdle = new Color(0.90f, 0.858f, 0.884f, 0.95f);
    private static readonly Color LabelHot = new Color(1f, 0.878f, 0.906f, 1f);

    private class Item
    {
        public string Id;
        public string Label;
        public Action Run;
        public Func<bool> IsOn;
        public Sprite Icon;

        public RectTransform Root;
        public Image Bg;
        public Image Ring;
        public Image Glyph;
        public Text Caption;
        public Image Dot;
        public Vector2 BasePos;
        public float Hover;
        public float Appear;
    }

    private Item[] _items;
    private Canvas _canvas;
    private RectTransform _canvasRt;
    private RectTransform _menu;
    private RectTransform _menuRt;
    private CanvasGroup _group;
    private Image _disc;
    private Image _hairline;

    private bool _open;
    private bool _closing;
    private float _sinceOpen;
    private int _hover = -1;
    private int _pressed = -1;

    private Sprite _roundSprite, _ringSprite, _rimSprite, _dotSprite, _discSprite, _shadowSprite;
    private Font _font;
    private bool _ready;
    private bool _demo;
    private float _demoAt;
    private bool _shot;
    private string _shotPath;
    private bool _shotDone;

    private void Awake()
    {
        BuildSprites();
        BuildFont();
        BuildItems();
        BuildUi();

        _menu.gameObject.SetActive(false);
    }

    private void Start()
    {
        SuppressMateEngineMenu();
        RegisterBlocker();
        _demo = Array.IndexOf(Environment.GetCommandLineArgs(), "-menu-demo") >= 0;
        _shot = Array.IndexOf(Environment.GetCommandLineArgs(), "-menu-shot") >= 0;
        if (_shot) _demo = true;
        if (_demo) _demoAt = Time.unscaledTime + 2.5f;
        if (_shot) _shotPath = Path.Combine(Application.persistentDataPath, "menu-shot.png");
    }

    // ---------------------------------------------------------------- build

    private void BuildItems()
    {
        _items = new[]
        {
            new Item { Id = "today",    Label = "今天",  Run = () => Xiaoqi.Spike.TodayCard.DebugOpen(),        Icon = LoadIcon("today") },
            new Item { Id = "dnd",      Label = "免打扰", Run = ToggleDnd,     IsOn = () => XiaoqiRuntime.Dnd,  Icon = LoadIcon("dnd") },
            new Item { Id = "pin",      Label = "置顶",  Run = ToggleTopmost, IsOn = IsTopmost,                Icon = LoadIcon("pin") },
            new Item { Id = "settings", Label = "设置",  Run = XiaoqiSettings.Open,                              Icon = LoadIcon("settings") },
            new Item { Id = "face",     Label = "表情",  Run = Xiaoqi.Spike.EmotionLineTuner.Toggle,             Icon = LoadIcon("face") },
        };
    }

    private void BuildUi()
    {
        // pixel-perfect sizing: menu takes ~56% of the smaller screen side
        _menuPx = Mathf.Clamp(Mathf.Min(Screen.width, Screen.height) * 0.56f, 300f, 560f);
        _s = _menuPx / 400f;
        _btnR = 128f * _s;
        _btnD = 86f * _s;
        _iconPx = 44f * _s;
        _hubPx = 100f * _s;

        var canvasGo = new GameObject("XiaoqiMenuCanvas");
        canvasGo.transform.SetParent(transform, false);
        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = CanvasOrder;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;
        canvasGo.AddComponent<GraphicRaycaster>();
        _canvasRt = canvasGo.GetComponent<RectTransform>();

        var menuGo = new GameObject("Menu", typeof(RectTransform));
        menuGo.transform.SetParent(canvasGo.transform, false);
        _menu = (RectTransform)menuGo.transform;
        _menuRt = menuGo.GetComponent<RectTransform>();
        _menuRt.anchorMin = _menuRt.anchorMax = new Vector2(0.5f, 0.5f);
        _menuRt.pivot = new Vector2(0.5f, 0.5f);
        _menuRt.sizeDelta = new Vector2(_menuPx, _menuPx);
        _menuRt.anchoredPosition = Vector2.zero;
        _group = menuGo.AddComponent<CanvasGroup>();
        _group.blocksRaycasts = true;
        _group.interactable = true;

        var shadow = NewImage("Shadow", _menuRt, _shadowSprite, new Vector2(520f * _s, 520f * _s), new Color(0f, 0f, 0f, 0.55f));
        shadow.raycastTarget = false;
        shadow.rectTransform.anchoredPosition = new Vector2(0f, -12f * _s);

        _disc = NewImage("Disc", _menuRt, _discSprite, new Vector2(_menuPx, _menuPx), Color.white);
        _disc.raycastTarget = true;

        _hairline = NewImage("Hairline", _menuRt, _rimSprite, new Vector2(_menuPx, _menuPx), new Color(1f, 1f, 1f, 0.14f));
        _hairline.raycastTarget = false;

        // center hub
        var hub = NewImage("Hub", _menuRt, _ringSprite, new Vector2(_hubPx, _hubPx), new Color(1f, 1f, 1f, 0.16f));
        hub.raycastTarget = false;
        var name = NewText("Name", _menuRt, "小祈", Mathf.RoundToInt(28f * _s), new Color(0.98f, 0.941f, 0.918f, 0.94f));
        name.rectTransform.sizeDelta = new Vector2(140f * _s, 44f * _s);
        name.rectTransform.anchoredPosition = Vector2.zero;

        for (int i = 0; i < _items.Length; i++)
        {
            var it = _items[i];
            float ang = (90f - i * (360f / _items.Length)) * Mathf.Deg2Rad;
            var pos = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * _btnR;
            it.BasePos = pos;

            var rt = new GameObject("Item_" + it.Id, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(_menuRt, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(_btnD, _btnD);
            rt.anchoredPosition = pos;
            it.Root = rt;

            it.Bg = NewImage("Bg", rt, _roundSprite, new Vector2(_btnD, _btnD), new Color(1f, 1f, 1f, 0.07f));
            it.Bg.raycastTarget = true;

            it.Ring = NewImage("Ring", rt, _ringSprite, new Vector2(_btnD + 2f * _s, _btnD + 2f * _s), new Color(Accent.r, Accent.g, Accent.b, 0f));
            it.Ring.raycastTarget = false;

            if (it.Icon != null)
            {
                it.Glyph = NewImage("Glyph", rt, it.Icon, new Vector2(_iconPx, _iconPx), IconIdle);
                it.Glyph.preserveAspect = true;
                it.Glyph.raycastTarget = false;
                it.Glyph.rectTransform.anchoredPosition = new Vector2(0f, 10f * _s);
            }

            it.Caption = NewText("Caption", rt, it.Label, Mathf.Max(10, Mathf.RoundToInt(17f * _s)), LabelIdle);
            it.Caption.rectTransform.sizeDelta = new Vector2(_btnD + 10f * _s, 20f * _s);
            it.Caption.rectTransform.anchoredPosition = new Vector2(0f, -24f * _s);

            it.Dot = NewImage("Dot", rt, _dotSprite, new Vector2(9f * _s, 9f * _s), Accent);
            it.Dot.raycastTarget = false;
            it.Dot.rectTransform.anchoredPosition = new Vector2(28f * _s, 28f * _s);
            it.Dot.gameObject.SetActive(false);
        }
        _ready = true;
    }

    private Image NewImage(string name, Transform parent, Sprite sprite, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.type = Image.Type.Simple;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        return img;
    }

    private Text NewText(string name, Transform parent, string text, int size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = _font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        return t;
    }

    private void BuildFont()
    {
        try
        {
            _font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei UI", "Microsoft YaHei", "Segoe UI" }, 16);
        }
        catch (Exception e)
        {
            Debug.LogWarning("XQ_MENU font: " + e.Message);
        }
        if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private Sprite LoadIcon(string id)
    {
        try
        {
            var path = Path.Combine(Application.streamingAssetsPath, "Xiaoqi", "menu", "icons", id + ".png");
            if (!File.Exists(path))
            {
                Debug.LogWarning("XQ_MENU icon missing " + path);
                return null;
            }
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(File.ReadAllBytes(path));
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }
        catch (Exception e)
        {
            Debug.LogWarning("XQ_MENU icon " + id + ": " + e.Message);
            return null;
        }
    }

    private void BuildSprites()
    {
        _discSprite = MakeDisc(512);
        _shadowSprite = MakeShadow(512);
        _roundSprite = MakeRound(128);
        _ringSprite = MakeRing(128, 3f);
        _rimSprite = MakeRing(512, 4f);
        _dotSprite = MakeDot(32);
    }

    private static Sprite Finish(Texture2D tex, int size)
    {
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        tex.Apply(false, true);
        return sprite;
    }

    private static Sprite MakeDisc(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var px = new Color32[size * size];
        float c = (size - 1) * 0.5f;
        float R = size * 0.5f - 2f;
        Color inner = new Color(0.192f, 0.157f, 0.227f, 0.965f);
        Color outer = new Color(0.098f, 0.078f, 0.122f, 0.945f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - c, dy = y - c;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                int idx = y * size + x;
                if (d > R + 2f) { px[idx] = new Color32(0, 0, 0, 0); continue; }
                float edge = Mathf.Clamp01((R - d) / 1.6f);
                float t = Mathf.Clamp01(d / R);
                float lift = 0.09f * Mathf.Clamp01(1f - Mathf.Sqrt((dx + R * 0.35f) * (dx + R * 0.35f) + (dy + R * 0.45f) * (dy + R * 0.45f)) / (R * 1.05f));
                Color col = Color.Lerp(inner, outer, t);
                float a = Mathf.Lerp(inner.a, outer.a, t) * edge;
                px[idx] = new Color32(
                    (byte)(Mathf.Clamp01(col.r + lift) * 255f),
                    (byte)(Mathf.Clamp01(col.g + lift) * 255f),
                    (byte)(Mathf.Clamp01(col.b + lift) * 255f),
                    (byte)(Mathf.Clamp01(a) * 255f));
            }
        }
        tex.SetPixels32(px);
        return Finish(tex, size);
    }

    private static Sprite MakeShadow(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var px = new Color32[size * size];
        float c = (size - 1) * 0.5f;
        float R = size * 0.5f - 10f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - c, dy = y - c;
                float d = Mathf.Sqrt(dx * dx + dy * dy) / R;
                float a = d >= 1f ? 0f : Mathf.Pow(1f - d, 2.4f) * 0.62f;
                px[y * size + x] = new Color32(0, 0, 0, (byte)(a * 255f));
            }
        }
        tex.SetPixels32(px);
        return Finish(tex, size);
    }

    private static Sprite MakeRound(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var px = new Color32[size * size];
        float c = (size - 1) * 0.5f;
        float R = size * 0.5f - 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                float a = Mathf.Clamp01((R - d) / 1.4f);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }
        tex.SetPixels32(px);
        return Finish(tex, size);
    }

    private static Sprite MakeRing(int size, float width)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var px = new Color32[size * size];
        float c = (size - 1) * 0.5f;
        float R = size * 0.5f - width;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                float a = Mathf.Clamp01((width - Mathf.Abs(d - R)) / 1.2f);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }
        tex.SetPixels32(px);
        return Finish(tex, size);
    }

    private static Sprite MakeDot(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var px = new Color32[size * size];
        float c = (size - 1) * 0.5f;
        float R = size * 0.5f - 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                float a = Mathf.Clamp01((R - d) / 1.4f);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }
        tex.SetPixels32(px);
        return Finish(tex, size);
    }

    // ------------------------------------------------------------- ME clean

    private void SuppressMateEngineMenu()
    {
        foreach (var sel in FindObjectsByType<Xamin.CircleSelector>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (sel == null) continue;
            sel.transform.localScale = Vector3.zero;
            sel.gameObject.SetActive(false);
        }
        foreach (var ma in FindObjectsByType<MenuActions>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (ma == null) continue;
            ma.radialMenuKey = KeyCode.None;
        }
        Debug.Log("XQ_MENU me-radial suppressed");
    }

    private void RegisterBlocker()
    {
        var ma = FindFirstObjectByType<MenuActions>();
        if (ma == null) return;
        ma.menuEntries.Add(new MenuEntry
        {
            menu = _menu.gameObject,
            blockMovement = true,
            blockHandTracking = true,
            blockReaction = true,
            blockChibiMode = false
        });
    }

    // ---------------------------------------------------------------- input

    private void Update()
    {
        if (!_ready || _closing) return;

        if (!_open)
        {
            if (_demo && Time.unscaledTime >= _demoAt) { _demo = false; OpenAt(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)); return; }
            if (Input.GetMouseButtonDown(1)) Open();
            return;
        }

        _sinceOpen += Time.unscaledDeltaTime;

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
        {
            Close();
            return;
        }

        var mouse = (Vector2)Input.mousePosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_menuRt, mouse, null, out var local);
        _hover = HitTest(local);

        if (Input.GetMouseButtonDown(0))
        {
            _pressed = _hover;
            Debug.Log("XQ_MENU down local=" + local + " hover=" + _hover);
            if (_hover < 0) { Close(); return; }
        }
        if (Input.GetMouseButtonUp(0))
        {
            int hit = _hover;
            int was = _pressed;
            _pressed = -1;
            Debug.Log("XQ_MENU up hit=" + hit + " pressed=" + was);
            if (hit >= 0 && hit == was)
            {
                var item = _items[hit];
                Close();
                try { item.Run?.Invoke(); }
                catch (Exception e) { Debug.LogWarning("XQ_MENU action " + item.Id + ": " + e.Message); }
                return;
            }
        }

        Animate();
        ApplyHover();
    }

    private int HitTest(Vector2 local)
    {
        float max = _btnD * 0.5f + 3f;
        for (int i = 0; i < _items.Length; i++)
        {
            if ((local - _items[i].BasePos).sqrMagnitude <= max * max) return i;
        }
        return -1;
    }

    private void Animate()
    {
        float t = Mathf.Clamp01(_sinceOpen / 0.18f);
        float ease = 1f - Mathf.Pow(1f - t, 3f);
        _group.alpha = ease;
        _menuRt.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, ease);

        for (int i = 0; i < _items.Length; i++)
        {
            float d = Mathf.Clamp01((_sinceOpen - i * 0.022f) / 0.16f);
            float e = 1f - Mathf.Pow(1f - d, 3f);
            _items[i].Appear = e;
            _items[i].Root.localScale = Vector3.one * Mathf.Lerp(0.78f, 1f, e);
        }
    }

    private void ApplyHover()
    {
        for (int i = 0; i < _items.Length; i++)
        {
            var it = _items[i];
            float target = (i == _hover && _sinceOpen > 0.12f) ? 1f : 0f;
            it.Hover = Mathf.MoveTowards(it.Hover, target, Time.unscaledDeltaTime * 9f);
            float h = it.Hover * it.Appear;

            it.Root.localScale = Vector3.one * (1f + 0.06f * h);
            it.Bg.color = Color.Lerp(new Color(1f, 1f, 1f, 0.085f), new Color(Accent.r, Accent.g, Accent.b, 0.20f), h);
            it.Ring.color = Color.Lerp(new Color(1f, 1f, 1f, 0.13f), new Color(Accent.r, Accent.g, Accent.b, 0.95f), h);
            if (it.Glyph != null) it.Glyph.color = Color.Lerp(IconIdle, IconHot, h);
            it.Caption.color = Color.Lerp(LabelIdle, LabelHot, h);
            it.Dot.gameObject.SetActive(it.IsOn != null && it.IsOn());
            it.Dot.color = Color.Lerp(new Color(Accent.r, Accent.g, Accent.b, 0.75f), Accent, h);
        }
    }

    // ---------------------------------------------------------------- state

    private void Open()
    {
        OpenAt(Input.mousePosition);
    }

    private void OpenAt(Vector2 screenPoint)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRt, screenPoint, null, out var local);
        var half = _canvasRt.rect.size * 0.5f;
        if (half.x < 10f) half = new Vector2(Screen.width, Screen.height) * 0.5f / Mathf.Max(0.01f, _canvas.scaleFactor);
        float pad = _menuPx * 0.5f + 18f;
        local.x = Mathf.Clamp(local.x, -half.x + pad, half.x - pad);
        local.y = Mathf.Clamp(local.y, -half.y + pad, half.y - pad);
        _menuRt.anchoredPosition = local;

        _menu.gameObject.SetActive(true);
        _menu.SetAsLastSibling();
        _open = true;
        _closing = false;
        _sinceOpen = 0f;
        _hover = -1;
        _pressed = -1;
        _group.alpha = 0f;
        _menuRt.localScale = Vector3.one * 0.92f;
        ApplyHover();
        Debug.Log("XQ_MENU open screen=" + Screen.width + "x" + Screen.height
                  + " canvas=" + _canvasRt.rect.size + " scale=" + _canvas.scaleFactor
                  + " pos=" + _menuRt.anchoredPosition);
        if (_shot && !_shotDone)
        {
            _shotDone = true;
            StartCoroutine(ShotRoutine());
        }
    }

    private IEnumerator ShotRoutine()
    {
        yield return new WaitForSecondsRealtime(0.9f);
        ScreenCapture.CaptureScreenshot(_shotPath);
        Debug.Log("XQ_MENU shot " + _shotPath);
        yield return new WaitForSecondsRealtime(0.8f);
        Application.Quit();
    }

    private void Close()
    {
        if (!_open || _closing) return;
        _closing = true;
        _open = false;
        _hover = -1;
        _pressed = -1;
        StartCoroutine(CloseRoutine());
    }

    private IEnumerator CloseRoutine()
    {
        float t = 0f;
        float startAlpha = _group.alpha;
        while (t < 0.12f)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / 0.12f);
            _group.alpha = Mathf.Lerp(startAlpha, 0f, k);
            _menuRt.localScale = Vector3.one * Mathf.Lerp(1f, 0.94f, k);
            yield return null;
        }
        _menu.gameObject.SetActive(false);
        _group.alpha = 0f;
        _closing = false;
        Debug.Log("XQ_MENU close");
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (_shot || _demo) return;
        if (!hasFocus && _open) Close();
    }

    // -------------------------------------------------------------- actions

    private static bool IsTopmost()
    {
        var h = SaveLoadHandler.Instance;
        return h != null && h.data != null && h.data.isTopmost;
    }

    private static void ToggleTopmost()
    {
        var h = SaveLoadHandler.Instance;
        if (h == null || h.data == null) return;
        h.data.isTopmost = !h.data.isTopmost;
        h.SaveToDisk();
        SaveLoadHandler.ApplyAllSettingsToAllAvatars();
        Debug.Log("XQ_MENU topmost=" + h.data.isTopmost);
    }

    private static void ToggleDnd()
    {
        XiaoqiRuntime.Dnd = !XiaoqiRuntime.Dnd;
        if (XiaoqiRuntime.Config != null)
        {
            XiaoqiRuntime.Config.dnd = XiaoqiRuntime.Dnd;
            XiaoqiConfig.Save(XiaoqiRuntime.Config);
        }
        Debug.Log("XQ_MENU dnd=" + XiaoqiRuntime.Dnd);
    }
}
