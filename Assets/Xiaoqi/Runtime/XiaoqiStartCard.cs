using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using XqStyle = Xiaoqi.UI.XqStyle;
using XqUi = Xiaoqi.UI.XqUi;

public class XiaoqiStartCard : MonoBehaviour
{
    [Serializable]
    private class Brand
    {
        public string name = "小祈";
        public string waking = "醒来中…";
        public string ok = "在呢";
        public string offline = "连不上服务器，先离线陪你";
        public float fade_in = 0.25f;
        public float ok_hold = 1.2f;
        public float offline_after = 8f;
        public float offline_hold = 3f;
        public float fade_out = 0.4f;
    }

    private Brand _brand;
    private CanvasGroup _group;
    private GameObject _canvasGo;
    private Text _status;
    private Image _dot;
    private bool _done;
    private float _start;

    private void Awake()
    {
        _brand = LoadBrand();
        Build();
        _start = Time.unscaledTime;
        StartCoroutine(Run());
    }

    private Brand LoadBrand()
    {
        var path = Path.Combine(Application.streamingAssetsPath, "Xiaoqi", "xiaoqi_brand.json");
        try
        {
            if (File.Exists(path))
                return JsonUtility.FromJson<Brand>(File.ReadAllText(path)) ?? new Brand();
        }
        catch (Exception e)
        {
            Debug.LogWarning("XiaoqiStartCard: " + e.Message);
        }
        return new Brand();
    }

    private void Build()
    {
        var canvasGo = new GameObject("XiaoqiStartCanvas");
        _canvasGo = canvasGo;
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        _group = canvasGo.AddComponent<CanvasGroup>();
        _group.alpha = 0f;
        _group.blocksRaycasts = false;

        var panel = new GameObject("Card", typeof(RectTransform));
        panel.transform.SetParent(canvasGo.transform, false);
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        float w = XqStyle.Fr(XqStyle.StartCardW);
        float h = XqStyle.Fr(XqStyle.StartCardH);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(0f, -XqStyle.Fr(XqStyle.StartCardTopY));
        XqUi.Surface(rt, w, h, XqStyle.Fr(XqStyle.SurfaceRadius),
            XqStyle.SurfaceFill, XqStyle.SurfaceLine, false);

        // 标题：左上 17/Ink，和卡片的标题层级一致。
        var title = XqUi.Label(panel.transform, _brand.name, XqStyle.FontStartTitle,
            XqStyle.StartTitleText, TextAnchor.MiddleLeft, XqStyle.StartTitleH);
        var trt = title.rectTransform;
        trt.anchorMin = trt.anchorMax = new Vector2(0f, 1f);
        trt.pivot = new Vector2(0f, 1f);
        trt.sizeDelta = new Vector2(w - XqStyle.Fr(XqStyle.StartCardPadX * 2f), XqStyle.Fr(XqStyle.StartTitleH));
        trt.anchoredPosition = new Vector2(XqStyle.Fr(XqStyle.StartCardPadX), -XqStyle.Fr(XqStyle.StartCardPadTop));

        // 状态行：6px 状态点 + 12/InkSoft 文字，贴卡底。
        var row = XqUi.Node(panel.transform, "StatusRow");
        row.anchorMin = new Vector2(0f, 0f);
        row.anchorMax = new Vector2(1f, 0f);
        row.pivot = new Vector2(0f, 0f);
        row.offsetMin = new Vector2(XqStyle.Fr(XqStyle.StartCardPadX), XqStyle.Fr(XqStyle.StartCardPadBottom));
        row.offsetMax = new Vector2(-XqStyle.Fr(XqStyle.StartCardPadX),
            XqStyle.Fr(XqStyle.StartCardPadBottom + XqStyle.StartStatusH));

        int d = Mathf.Max(4, XqStyle.Px(XqStyle.StartDotSize));
        var dotRt = XqUi.Node(row, "Dot");
        dotRt.anchorMin = dotRt.anchorMax = new Vector2(0f, 0.5f);
        dotRt.pivot = new Vector2(0f, 0.5f);
        dotRt.sizeDelta = new Vector2(d, d);
        dotRt.anchoredPosition = Vector2.zero;
        _dot = XqUi.Panel(dotRt, XqUi.Rounded(d, d, d * 0.5f, Color.white, Color.white, 0f),
            XqStyle.StartDotWaking);
        _dot.type = Image.Type.Simple;
        _dot.raycastTarget = false;

        _status = XqUi.Label(row, _brand.waking, XqStyle.FontStartStatus,
            XqStyle.StartStatusText, TextAnchor.MiddleLeft, XqStyle.StartStatusH);
        var srt = _status.rectTransform;
        srt.anchorMin = new Vector2(0f, 0f);
        srt.anchorMax = new Vector2(1f, 1f);
        srt.offsetMin = new Vector2(d + XqStyle.Fr(XqStyle.StartDotGap), 0f);
        srt.offsetMax = Vector2.zero;

        Debug.Log("XQ_START card w=" + w.ToString("0.0") + " h=" + h.ToString("0.0")
                  + " r=" + XqStyle.Fr(XqStyle.SurfaceRadius).ToString("0.0") + " dot=" + d);
    }

    private IEnumerator Run()
    {
        yield return Fade(0f, 1f, _brand.fade_in);
        while (!_done && Time.unscaledTime - _start < _brand.offline_after)
        {
            if (XiaoqiLink.Current != null)
            {
                _status.text = _brand.ok;
                if (_dot != null) _dot.color = XqStyle.StartDotOk;
                Debug.Log("XQ_START waking→ok " + (Time.unscaledTime - _start).ToString("0.0") + "s");
                yield return new WaitForSecondsRealtime(_brand.ok_hold);
                yield return Fade(1f, 0f, _brand.fade_out);
                if (_canvasGo != null) Destroy(_canvasGo);
                Destroy(this);
                yield break;
            }
            yield return null;
        }
        _status.text = _brand.offline;
        if (_dot != null) _dot.color = XqStyle.StartDotOffline;
        yield return new WaitForSecondsRealtime(_brand.offline_hold);
        yield return Fade(1f, 0f, _brand.fade_out);
        if (_canvasGo != null) Destroy(_canvasGo);
        Destroy(this);
    }

    private IEnumerator Fade(float from, float to, float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            _group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / Mathf.Max(0.01f, seconds)));
            yield return null;
        }
        _group.alpha = to;
    }
}


