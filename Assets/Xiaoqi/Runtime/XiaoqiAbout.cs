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
    private XqUi.CardSkin _skin;
    private float _cardW;
    private float _fitH = 240f;

    public static void Show()
    {
        if (_instance == null) _instance = FindFirstObjectByType<XiaoqiAbout>(FindObjectsInactive.Include);
        if (_instance == null) return;
        _instance.EnsureBuilt();
        _instance.Fit();
        _open = true;
        _instance._go.SetActive(true);
        Debug.Log("XQ_ABOUT show active=" + _instance._go.activeInHierarchy + " card=" + _instance._card.rect);
    }

    private void Update()
    {
        if (_open && Xiaoqi.Host.PetHost.I != null && !Xiaoqi.Host.PetHost.I.AvatarVisible) Hide();
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
        _cardW = XqStyle.Fr(420f);
        _card.anchoredPosition = Vector2.zero;

        // 主题 D：和今天卡/设置卡同一套卡面与标题区。
        _skin = XqUi.CardBase(_card);
        XqUi.CardHeader(_card, "小祈", Close);

        var ver = XqUi.Label(_card, Application.version, XqStyle.FontSmall, XqStyle.TextTertiary,
            TextAnchor.MiddleRight, 18f);
        var vrt = ver.rectTransform;
        vrt.anchorMin = vrt.anchorMax = new Vector2(1f, 1f);
        vrt.pivot = new Vector2(1f, 1f);
        vrt.sizeDelta = new Vector2(XqStyle.Fr(120f), XqStyle.Fr(18f));
        vrt.anchoredPosition = new Vector2(-XqStyle.Fr(XqStyle.CardHeaderPadX + 28f),
            -XqStyle.Fr((XqStyle.CardHeaderH - 18f) * 0.5f));

        string text = "本程序的桌面部分基于 Mate Engine（作者 Shinymoon）修改，按 Mate Engine 许可证 v2.1 分发："
                      + "不得用于商业用途；修改后的源码按同一许可证公开在 github.com/Ziqian0512/Xiaoqi-MateEngine。";
        float bodyW = _cardW - XqStyle.Fr(XqStyle.CardBodyPadX * 2f);
        var body = XqUi.Label(_card, text, 13, XqStyle.Body, TextAnchor.UpperLeft, 60f);
        var brt = body.rectTransform;
        brt.anchorMin = brt.anchorMax = new Vector2(0f, 1f);
        brt.pivot = new Vector2(0f, 1f);
        brt.sizeDelta = new Vector2(bodyW, XqStyle.Fr(60f));
        brt.anchoredPosition = new Vector2(XqStyle.Fr(XqStyle.CardBodyPadX),
            -XqStyle.Fr(XqStyle.CardHeaderH + XqStyle.CardBodyPadTop));
        float bodyH = Mathf.Max(XqStyle.Fr(36f), body.preferredHeight);

        float y = -(XqStyle.Fr(XqStyle.CardHeaderH + XqStyle.CardBodyPadTop) + bodyH + XqStyle.Fr(12f));
        float bw = XqStyle.Fr(216f);
        float bh = XqStyle.Fr(XqStyle.CardButtonH);

        string license = Path.Combine(Application.streamingAssetsPath, "Xiaoqi", "MATE-ENGINE-LICENSE.txt");
        AddButton("查看许可证全文", bw, bh, ref y, () => OpenFile(license));

        string llama = Path.Combine(Application.streamingAssetsPath, "LLAMA 3.2 COMMUNITY LICENSE.txt");
        if (File.Exists(llama)) AddButton("查看 LLAMA 3.2 许可证", bw, bh, ref y, () => OpenFile(llama));

        _fitH = -y + XqStyle.Fr(XqStyle.CardBodyPadBottom - XqStyle.CardButtonGap);
        Fit();
    }

    /// <summary>主题 D：卡高按内容算，内容再多也不超过屏幕 85%；不再写死 400。</summary>
    private void Fit()
    {
        if (_card == null) return;
        float h = Mathf.Min(_fitH, Screen.height * 0.85f);
        _card.sizeDelta = new Vector2(_cardW, h);
        XqUi.CardSize(_skin, _cardW, h);
    }

    private void AddButton(string label, float w, float h, ref float y, Action onClick)
    {
        var cap = XqUi.CardButton(_card, label, w, h, onClick, false);
        var rt = cap.Root;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(XqStyle.Fr(XqStyle.CardBodyPadX), y);
        y -= h + XqStyle.Fr(XqStyle.CardButtonGap);
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
