using System;
using SFB;
using UnityEngine;
using UnityEngine.UI;
using Xiaoqi.Host;
using Xiaoqi.UI;

public class XiaoqiSettings : MonoBehaviour
{
    private static XiaoqiSettings _instance;
    private static bool _open;

    private Canvas _canvas;
    private GameObject _go;
    private RectTransform _card;
    private RectTransform _content;
    private float _cardW;
    private float _contentW;

    private void Awake()
    {
        _instance = this;
        Build();
        _go.SetActive(false);
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-demo-settings") >= 0)
            StartCoroutine(DemoOpen());
    }

    private System.Collections.IEnumerator DemoOpen()
    {
        yield return new WaitForSecondsRealtime(2.5f);
        Open();
    }

    public static void Open()
    {
        if (_instance == null) _instance = FindFirstObjectByType<XiaoqiSettings>(FindObjectsInactive.Include);
        if (_instance == null) { Debug.Log("XQ_SET no instance"); return; }
        _open = true;
        _instance._go.SetActive(true);
        _instance.Rebuild();
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-debug-settings") >= 0)
        {
            var dbg = new GameObject("Dbg", typeof(RectTransform));
            dbg.transform.SetParent(_instance._canvas.transform, false);
            var rt = (RectTransform)dbg.transform;
            rt.anchorMin = new Vector2(0.1f, 0.1f);
            rt.anchorMax = new Vector2(0.4f, 0.4f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = dbg.AddComponent<UnityEngine.UI.Image>();
            img.color = Color.red;

            var dbg2 = new GameObject("DbgCard", typeof(RectTransform));
            dbg2.transform.SetParent(_instance._card, false);
            var rt2 = (RectTransform)dbg2.transform;
            rt2.anchorMin = rt2.anchorMax = new Vector2(0.5f, 0.5f);
            rt2.sizeDelta = new Vector2(80f, 80f);
            dbg2.AddComponent<UnityEngine.UI.Image>().color = Color.green;
        }
        Debug.Log("XQ_SET open active=" + _instance._go.activeInHierarchy + " card=" + _instance._card.rect
                  + " pos=" + _instance._card.anchoredPosition
                  + " canvas=" + ((RectTransform)_instance._canvas.transform).rect
                  + " scale=" + _instance._canvas.scaleFactor);
    }

    public void Close()
    {
        _open = false;
        _go.SetActive(false);
    }

    private void Update()
    {
        if (_open && !ShotMode() && Xiaoqi.Host.PetHost.I != null && !Xiaoqi.Host.PetHost.I.AvatarVisible) Close();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && _open && !ShotMode()) Close();
    }

    private static bool ShotMode()
    {
        var a = Environment.GetCommandLineArgs();
        return Array.IndexOf(a, "-menu-shot") >= 0 || Array.IndexOf(a, "-demo-settings") >= 0;
    }

    private void Build()
    {
        var canvasGo = new GameObject("XqSettingsCanvas");
        canvasGo.transform.SetParent(transform, false);
        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 340;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;
        canvasGo.AddComponent<GraphicRaycaster>();

        _go = new GameObject("XqSettings", typeof(RectTransform));
        _go.transform.SetParent(canvasGo.transform, false);
        _card = (RectTransform)_go.transform;
        _card.anchorMin = _card.anchorMax = new Vector2(0.5f, 0.5f);
        _card.pivot = new Vector2(0.5f, 0.5f);
        _cardW = XqStyle.Fr(420f);
        _contentW = _cardW - XqStyle.Fr(40f);
        _card.sizeDelta = new Vector2(_cardW, XqStyle.Fr(520f));
        _card.anchoredPosition = Vector2.zero;

        var bg = _card.gameObject.AddComponent<Image>();
        bg.color = XqStyle.Paper;
        bg.raycastTarget = true;

        var title = XqUi.Label(_card, "设置", XqStyle.FontCardTitle, XqStyle.Ink, TextAnchor.MiddleLeft, 26f);
        var trt = title.rectTransform;
        trt.anchorMin = trt.anchorMax = new Vector2(0f, 1f);
        trt.pivot = new Vector2(0f, 1f);
        trt.sizeDelta = new Vector2(XqStyle.Fr(160f), XqStyle.Fr(26f));
        trt.anchoredPosition = new Vector2(XqStyle.Fr(18f), -XqStyle.Fr(14f));

        var close = XqUi.IconButton(_card, "×", Close);
        var crt = close.GetComponent<RectTransform>();
        crt.anchorMin = crt.anchorMax = new Vector2(1f, 1f);
        crt.pivot = new Vector2(1f, 1f);
        crt.anchoredPosition = new Vector2(-XqStyle.Fr(10f), -XqStyle.Fr(8f));

        var scrollGo = new GameObject("Scroll", typeof(RectTransform));
        scrollGo.transform.SetParent(_card, false);
        var srt = (RectTransform)scrollGo.transform;
        srt.anchorMin = new Vector2(0f, 0f);
        srt.anchorMax = new Vector2(1f, 1f);
        srt.offsetMin = new Vector2(XqStyle.Fr(14f), XqStyle.Fr(14f));
        srt.offsetMax = new Vector2(-XqStyle.Fr(14f), -XqStyle.Fr(48f));

        var viewport = new GameObject("Viewport", typeof(RectTransform));
        viewport.transform.SetParent(scrollGo.transform, false);
        var vrt = (RectTransform)viewport.transform;
        vrt.anchorMin = Vector2.zero;
        vrt.anchorMax = Vector2.one;
        vrt.offsetMin = Vector2.zero;
        vrt.offsetMax = Vector2.zero;
        viewport.AddComponent<RectMask2D>();

        _content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
        _content.transform.SetParent(viewport.transform, false);
        _content.anchorMin = new Vector2(0f, 1f);
        _content.anchorMax = new Vector2(1f, 1f);
        _content.pivot = new Vector2(0f, 1f);
        _content.sizeDelta = new Vector2(0f, 0f);

        var vlg = _content.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = XqStyle.Fr(8f);
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        var fit = _content.gameObject.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = scrollGo.AddComponent<ScrollRect>();
        scroll.content = _content;
        scroll.viewport = vrt;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = XqStyle.Fr(28f);
    }

    private RectTransform Row(float height)
    {
        var rt = new GameObject("Row", typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(_content, false);
        var le = rt.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = XqStyle.Fr(height);
        le.minHeight = XqStyle.Fr(height);
        return rt;
    }

    private void RowLabel(RectTransform row, string text)
    {
        var t = XqUi.Label(row, text, XqStyle.FontBody, XqStyle.Body, TextAnchor.MiddleLeft, 20f);
        var trt = t.rectTransform;
        trt.anchorMin = trt.anchorMax = new Vector2(0f, 1f);
        trt.pivot = new Vector2(0f, 1f);
        trt.sizeDelta = new Vector2(XqStyle.Fr(120f), XqStyle.Fr(22f));
        trt.anchoredPosition = new Vector2(0f, -XqStyle.Fr(2f));
    }

    private void Place(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x, y);
    }

    private void Section(string title)
    {
        var rt = Row(18f);
        var t = XqUi.Label(rt, title, XqStyle.FontSmall, XqStyle.Muted, TextAnchor.MiddleLeft, 16f);
        var trt = t.rectTransform;
        trt.anchorMin = trt.anchorMax = new Vector2(0f, 1f);
        trt.pivot = new Vector2(0f, 1f);
        trt.sizeDelta = new Vector2(_contentW, XqStyle.Fr(16f));
        trt.anchoredPosition = Vector2.zero;
    }

    private void Rebuild()
    {
        for (int i = _content.childCount - 1; i >= 0; i--)
            Destroy(_content.GetChild(i).gameObject);

        var host = PetHost.I;
        if (host == null) return;

        _card.sizeDelta = new Vector2(_cardW, Mathf.Min(XqStyle.Fr(520f), Screen.height * 0.8f));

        Section("外观");
        AddSlider("大小", 0.5f, 2f, host.Size, "0.00", v => host.Size = v);
        AddSwitch("置顶", host.Topmost, v => host.Topmost = v);
        AddSegmented("画质", new[] { "低", "中", "高", "最高" }, Mathf.Clamp(host.Quality, 0, 3), i => host.Quality = i);
        int[] fpsOpts = { 30, 60, 90, 144 };
        int fpsIdx = Nearest(fpsOpts, host.Fps);
        AddSegmented("帧率", new[] { "30", "60", "90", "144" }, fpsIdx, i => host.Fps = fpsOpts[i]);
        AddSwitch("坐窗口", host.WindowSitEnabled, v => host.WindowSitEnabled = v);
        AddSwitch("桌面上自己走", host.LocomotionEnabled, v => host.LocomotionEnabled = v);

        Section("陪伴");
        bool hide = XiaoqiRuntime.Config.pet_when_out == "hide";
        AddSegmented("外出时", new[] { "照常显示", "隐藏" }, hide ? 1 : 0, i =>
        {
            XiaoqiRuntime.Config.pet_when_out = i == 1 ? "hide" : "show";
            XiaoqiConfig.Save(XiaoqiRuntime.Config);
        });
        AddSwitch("气泡", XiaoqiRuntime.Config.bubble, v => { XiaoqiRuntime.Config.bubble = v; XiaoqiConfig.Save(XiaoqiRuntime.Config); });
        AddSwitch("情感线（测试）", XiaoqiRuntime.Config.emotion_line, v => { XiaoqiRuntime.Config.emotion_line = v; XiaoqiConfig.Save(XiaoqiRuntime.Config); });
        int[] naps = { 10, 20, 40 };
        int napIdx = Nearest(naps, XiaoqiRuntime.Config.nap_min);
        AddSegmented("睡一会", new[] { "10 分钟", "20 分钟", "40 分钟" }, napIdx, i =>
        {
            XiaoqiRuntime.Config.nap_min = naps[i];
            XiaoqiConfig.Save(XiaoqiRuntime.Config);
        });
        AddSwitch("开机自启", host.StartWithWindows, v => host.StartWithWindows = v);

        Section("连接");
        AddInput("服务器", XiaoqiRuntime.Config.server ?? "", false, v => XiaoqiRuntime.Config.server = v);
        AddInput("令牌", XiaoqiRuntime.Config.token ?? "", true, v => XiaoqiRuntime.Config.token = v);
        AddModelRow();

        var saveRow = Row(34f);
        var save = XqUi.Capsule(saveRow, "保存并重连", null);
        Place(save.Root, 0f, 0f, XqStyle.Fr(150f), XqStyle.Fr(32f));
        save.Bg.sprite = XqUi.Pill(Mathf.RoundToInt(XqStyle.Fr(150f)), Mathf.RoundToInt(XqStyle.Fr(32f)), XqStyle.PinkFill, XqStyle.PinkStrong);
        save.Bg.type = Image.Type.Simple;
        save.Text.color = XqStyle.PinkStrong;
        save.Hover.OnClick = () =>
        {
            host.SaveSettings();
            if (!string.IsNullOrEmpty(XiaoqiRuntime.Config.model_path))
                host.LoadModel(XiaoqiRuntime.Config.model_path);
            XiaoqiConfig.Save(XiaoqiRuntime.Config);
            Debug.Log("XQ_SET save");
        };

        Section("关于");
        var aboutRow = Row(30f);
        var ver = XqUi.Label(aboutRow, "小祈 " + Application.version, XqStyle.FontSmall, XqStyle.Body, TextAnchor.MiddleLeft, 20f);
        Place(ver.rectTransform, 0f, 0f, XqStyle.Fr(200f), XqStyle.Fr(20f));
        var aboutBtn = XqUi.Capsule(aboutRow, "关于与许可证", null);
        Place(aboutBtn.Root, _contentW - XqStyle.Fr(130f), 0f, XqStyle.Fr(130f), XqStyle.Fr(30f));
        aboutBtn.Hover.OnClick = () => { Close(); XiaoqiAbout.Show(); };
    }

    private static int Nearest(int[] options, int value)
    {
        int best = 0;
        for (int i = 1; i < options.Length; i++)
            if (Mathf.Abs(options[i] - value) < Mathf.Abs(options[best] - value)) best = i;
        return best;
    }

    private void AddSlider(string label, float min, float max, float value, string fmt, Action<float> set)
    {
        var row = Row(32f);
        RowLabel(row, label);
        var view = XqUi.SliderRow(row, min, max, value, fmt, v => set(v));
        Place(view.Root, XqStyle.Fr(126f), 0f, _contentW - XqStyle.Fr(126f), XqStyle.Fr(30f));
    }

    private void AddSwitch(string label, bool value, Action<bool> set)
    {
        var row = Row(32f);
        RowLabel(row, label);
        var sw = XqUi.Switch(row, value, v => set(v));
        Place(sw.Root, XqStyle.Fr(126f), 0f, XqStyle.Fr(44f), XqStyle.Fr(28f));
    }

    private void AddSegmented(string label, string[] options, int index, Action<int> set)
    {
        var row = Row(36f);
        RowLabel(row, label);
        var seg = XqUi.Segmented(row, options, index, i => set(i));
        Place(seg.Root, XqStyle.Fr(126f), 0f, _contentW - XqStyle.Fr(126f), XqStyle.Fr(36f));
    }

    private void AddInput(string label, string value, bool password, Action<string> set)
    {
        var row = Row(34f);
        RowLabel(row, label);
        var input = XqUi.TextInput(row, value, password, v => set(v));
        Place(input.Root, XqStyle.Fr(126f), 0f, _contentW - XqStyle.Fr(126f), XqStyle.Fr(32f));
    }

    private void AddModelRow()
    {
        var row = Row(34f);
        RowLabel(row, "模型");
        var input = XqUi.TextInput(row, XiaoqiRuntime.Config.model_path ?? "", false, v => XiaoqiRuntime.Config.model_path = v);
        Place(input.Root, XqStyle.Fr(126f), 0f, _contentW - XqStyle.Fr(126f) - XqStyle.Fr(70f), XqStyle.Fr(32f));
        var pick = XqUi.Capsule(row, "选择", null);
        Place(pick.Root, _contentW - XqStyle.Fr(64f), 0f, XqStyle.Fr(64f), XqStyle.Fr(32f));
        pick.Hover.OnClick = () =>
        {
            var paths = StandaloneFileBrowser.OpenFilePanel("选择 VRM", "", new[] { new ExtensionFilter("VRM", "vrm") }, false);
            if (paths.Length > 0)
            {
                XiaoqiRuntime.Config.model_path = paths[0];
                input.Field.text = paths[0];
            }
        };
    }
}
