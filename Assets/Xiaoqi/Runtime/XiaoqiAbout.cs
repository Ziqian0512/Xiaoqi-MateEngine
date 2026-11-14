using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Xiaoqi.UI;

public class XiaoqiAbout : MonoBehaviour
{
    private static XiaoqiAbout _instance;
    private static bool _open;

    private Canvas _canvas;
    private GameObject _go;
    private RectTransform _card;

    public static void Show()
    {
        if (_instance == null) _instance = FindFirstObjectByType<XiaoqiAbout>(FindObjectsInactive.Include);
        if (_instance == null) return;
        _instance.EnsureBuilt();
        _instance._card.sizeDelta = new Vector2(_instance._card.sizeDelta.x,
            Mathf.Min(XqStyle.Fr(400f), Screen.height * 0.85f));
        _open = true;
        _instance._go.SetActive(true);
        Debug.Log("XQ_ABOUT show active=" + _instance._go.activeInHierarchy + " card=" + _instance._card.rect);
    }

    public static void Hide()
    {
        _open = false;
        if (_instance != null && _instance._go != null) _instance._go.SetActive(false);
    }

    public void Close() => Hide();

    private void Awake()
    {
        if (Array.IndexOf(System.Environment.GetCommandLineArgs(), "-demo-about") >= 0)
            StartCoroutine(DemoOpen());
    }

    private System.Collections.IEnumerator DemoOpen()
    {
        yield return new WaitForSecondsRealtime(2.5f);
        Show();
    }

    private void EnsureBuilt()
    {
        if (_go != null) return;
        var canvasGo = new GameObject("XqAboutCanvas");
        canvasGo.transform.SetParent(transform, false);
        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 345;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;
        canvasGo.AddComponent<GraphicRaycaster>();

        _go = new GameObject("XqAboutCard", typeof(RectTransform));
        _go.transform.SetParent(canvasGo.transform, false);
        _card = (RectTransform)_go.transform;
        _card.anchorMin = _card.anchorMax = new Vector2(0.5f, 0.5f);
        _card.pivot = new Vector2(0.5f, 0.5f);
        float w = XqStyle.Fr(420f);
        float h = Mathf.Min(XqStyle.Fr(400f), Screen.height * 0.85f);
        _card.sizeDelta = new Vector2(w, h);
        _card.anchoredPosition = Vector2.zero;

        var bg = _card.gameObject.AddComponent<Image>();
        bg.color = XqStyle.Paper;

        var title = XqUi.Label(_card, "小祈", XqStyle.FontCardTitle, XqStyle.Ink, TextAnchor.MiddleLeft, 26f);
        var trt = title.rectTransform;
        trt.anchorMin = trt.anchorMax = new Vector2(0f, 1f);
        trt.pivot = new Vector2(0f, 1f);
        trt.sizeDelta = new Vector2(XqStyle.Fr(200f), XqStyle.Fr(26f));
        trt.anchoredPosition = new Vector2(XqStyle.Fr(18f), -XqStyle.Fr(14f));

        var ver = XqUi.Label(_card, Application.version, XqStyle.FontSmall, XqStyle.Muted, TextAnchor.MiddleRight, 18f);
        var vrt = ver.rectTransform;
        vrt.anchorMin = vrt.anchorMax = new Vector2(1f, 1f);
        vrt.pivot = new Vector2(1f, 1f);
        vrt.sizeDelta = new Vector2(XqStyle.Fr(120f), XqStyle.Fr(18f));
        vrt.anchoredPosition = new Vector2(-XqStyle.Fr(46f), -XqStyle.Fr(18f));

        var close = XqUi.IconButton(_card, "×", Close);
        var crt = close.GetComponent<RectTransform>();
        crt.anchorMin = crt.anchorMax = new Vector2(1f, 1f);
        crt.pivot = new Vector2(1f, 1f);
        crt.anchoredPosition = new Vector2(-XqStyle.Fr(10f), -XqStyle.Fr(8f));

        string text = "本程序的桌面部分基于 Mate Engine（作者 Shinymoon）修改，按 Mate Engine 许可证 v2.1 分发："
                      + "不得用于商业用途；修改后的源码按同一许可证公开在 github.com/Ziqian0512/Xiaoqi-MateEngine。";
        var body = XqUi.Label(_card, text, 13, XqStyle.Body, TextAnchor.UpperLeft, 140f);
        var brt = body.rectTransform;
        brt.anchorMin = brt.anchorMax = new Vector2(0f, 1f);
        brt.pivot = new Vector2(0f, 1f);
        brt.sizeDelta = new Vector2(w - XqStyle.Fr(36f), XqStyle.Fr(140f));
        brt.anchoredPosition = new Vector2(XqStyle.Fr(18f), -XqStyle.Fr(52f));

        float y = -XqStyle.Fr(200f);
        string license = Path.Combine(Application.streamingAssetsPath, "Xiaoqi", "MATE-ENGINE-LICENSE.txt");
        AddButton("查看许可证全文", ref y, () => OpenFile(license));

        string llama = Path.Combine(Application.streamingAssetsPath, "LLAMA 3.2 COMMUNITY LICENSE.txt");
        if (File.Exists(llama)) AddButton("查看 LLAMA 3.2 许可证", ref y, () => OpenFile(llama));
    }

    private void AddButton(string label, ref float y, Action onClick)
    {
        var cap = XqUi.Capsule(_card, label, null);
        float w = XqStyle.Fr(200f);
        var rt = cap.Root;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(w, XqStyle.Fr(32f));
        rt.anchoredPosition = new Vector2(XqStyle.Fr(18f), y);
        cap.Bg.sprite = XqUi.Pill(Mathf.RoundToInt(w), Mathf.RoundToInt(XqStyle.Fr(32f)), XqStyle.Paper, XqStyle.PinkLine);
        cap.Bg.type = Image.Type.Simple;
        cap.Hover.OnClick = onClick;
        y -= XqStyle.Fr(42f);
    }

    private static void OpenFile(string path)
    {
        if (!File.Exists(path))
        {
            Debug.Log("XQ_ABOUT missing " + path);
            return;
        }
        Application.OpenURL("file:///" + path.Replace("\\", "/"));
    }
}
