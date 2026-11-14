using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Xiaoqi.Spike;

public class XiaoqiFeed : MonoBehaviour
{
    private int _since = -1;
    private BubbleUI _bubble;

    [Serializable]
    private class Envelope
    {
        public FeedNow now;
        public FeedItem[] items;
        public int last_id;
        public FeedRules rules;
    }

    [Serializable]
    private class FeedNow
    {
        public string doing;
        public string posture;
    }

    [Serializable]
    private class FeedItem
    {
        public int id;
        public string ts;
        public string kind;
        public string channel;
        public string scope;
        public string text;
        public string label;
    }

    [Serializable]
    private class FeedRules
    {
        public bool bubble_text;
        public bool show_private_text;
        public bool show_group_text;
    }

    private void Start()
    {
        StartCoroutine(Poll());
    }

    private IEnumerator Poll()
    {
        while (true)
        {
            var cfg = XiaoqiRuntime.Config ?? XiaoqiConfig.LoadOrCreate();
            var baseUrl = (cfg.server ?? "").TrimEnd('/') + "/api/body/feed";
            var url = _since < 0 ? baseUrl : baseUrl + "?since=" + _since;
            using (var req = UnityWebRequest.Get(url))
            {
                if (!string.IsNullOrEmpty(cfg.token)) req.SetRequestHeader("X-Token", cfg.token);
                req.timeout = 5;
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var env = JsonUtility.FromJson<Envelope>(req.downloadHandler.text);
                        if (env != null)
                        {
                            _since = env.last_id;
                            var allow = cfg.bubble && !XiaoqiRuntime.Dnd
                                        && (env.rules == null || env.rules.show_private_text);
                            if (allow && env.items != null)
                                foreach (var item in env.items)
                                {
                                    if (string.IsNullOrEmpty(item.text)) continue;
                                    if (!string.IsNullOrEmpty(item.scope) && item.scope != "private") continue;
                                    Show(item.text);
                                }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning("XiaoqiFeed parse: " + e.Message);
                    }
                }
            }
            yield return new WaitForSeconds(3f);
        }
    }

    private void Show(string text)
    {
        if (_bubble == null) _bubble = FindFirstObjectByType<BubbleUI>(FindObjectsInactive.Include);
        if (_bubble != null) _bubble.Show(text);
    }
}
