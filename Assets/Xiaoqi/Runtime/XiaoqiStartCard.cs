using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

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

        var panel = new GameObject("Card");
        panel.transform.SetParent(canvasGo.transform, false);
        var bg = panel.AddComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 0.92f);
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(320, 88);
        rt.anchoredPosition = new Vector2(0, 120);

        var title = new GameObject("Name");
        title.transform.SetParent(panel.transform, false);
        var t = title.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 18;
        t.color = new Color(0.15f, 0.15f, 0.18f);
        t.text = _brand.name;
        var trt = title.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0f, 0.5f);
        trt.anchorMax = new Vector2(1f, 1f);
        trt.offsetMin = new Vector2(76, 0);
        trt.offsetMax = new Vector2(-12, -14);

        var status = new GameObject("Status");
        status.transform.SetParent(panel.transform, false);
        _status = status.AddComponent<Text>();
        _status.font = t.font;
        _status.fontSize = 13;
        _status.color = new Color(0.42f, 0.42f, 0.46f);
        _status.text = _brand.waking;
        var srt = status.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(0f, 0f);
        srt.anchorMax = new Vector2(1f, 0.5f);
        srt.offsetMin = new Vector2(76, 12);
        srt.offsetMax = new Vector2(-12, 0);
    }

    private IEnumerator Run()
    {
        yield return Fade(0f, 1f, _brand.fade_in);
        while (!_done && Time.unscaledTime - _start < _brand.offline_after)
        {
            if (XiaoqiLink.Current != null)
            {
                _status.text = _brand.ok;
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


