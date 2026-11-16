using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Xiaoqi.Mode
{
    /// <summary>
    /// 把「她被摸了 / 切到近景 / 开了工作台」这类客户端事件报给服务器（POST /api/body/event）。
    /// 攒 5 条或 10 秒发一次；失败静默丢弃，不重试堆积；服务器返回 404（接口还没上线）就只写本地日志，之后不再发。
    /// 不触发任何回复，只落库。
    /// </summary>
    public class XqEventSink : MonoBehaviour
    {
        [Serializable]
        private class Ev { public string kind; public string detail; public string ts; }

        private const int BatchCount = 5;
        private const float BatchSec = 10f;
        private const int Cap = 20;           // 队列最多留 20 条，满了丢最老的，防堆积

        private static XqEventSink _i;
        private readonly List<Ev> _q = new List<Ev>();
        private float _firstAt = -1f;
        private bool _sending;
        private bool _disabled;               // 服务器没这个接口

        public static void Report(string kind, string detail)
        {
            if (_i == null)
            {
                var go = new GameObject("XqEventSink");
                DontDestroyOnLoad(go);
                _i = go.AddComponent<XqEventSink>();
            }
            _i.Add(kind, detail);
        }

        private void Add(string kind, string detail)
        {
            if (detail == null) detail = "";
            if (detail.Length > 80) detail = detail.Substring(0, 80);
            Debug.Log("XQ_EVENT " + kind + " " + detail + (_disabled ? " (local only)" : ""));
            if (_disabled) return;
            if (_q.Count >= Cap) _q.RemoveAt(0);
            _q.Add(new Ev { kind = kind, detail = detail, ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") });
            if (_firstAt < 0f) _firstAt = Time.unscaledTime;
        }

        private void Update()
        {
            if (_sending || _disabled || _q.Count == 0) return;
            if (_q.Count >= BatchCount || Time.unscaledTime - _firstAt >= BatchSec)
                StartCoroutine(Flush());
        }

        private IEnumerator Flush()
        {
            _sending = true;
            var batch = new List<Ev>(_q);
            _q.Clear();
            _firstAt = -1f;

            var cfg = XiaoqiRuntime.Config;
            string server = cfg != null ? (cfg.server ?? "").TrimEnd('/') : "";
            if (server.Length == 0) { _sending = false; yield break; }

            foreach (var ev in batch)
            {
                var body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(ev));
                using (var req = new UnityWebRequest(server + "/api/body/event", "POST"))
                {
                    req.uploadHandler = new UploadHandlerRaw(body);
                    req.downloadHandler = new DownloadHandlerBuffer();
                    req.SetRequestHeader("Content-Type", "application/json");
                    if (!string.IsNullOrEmpty(cfg.token)) req.SetRequestHeader("X-Token", cfg.token);
                    req.timeout = 5;
                    yield return req.SendWebRequest();
                    if (req.responseCode == 404)
                    {
                        _disabled = true;
                        Debug.Log("XQ_EVENT server has no /api/body/event (404), local log only");
                        break;
                    }
                    if (req.result != UnityWebRequest.Result.Success)
                        Debug.Log("XQ_EVENT drop " + ev.kind + " code=" + req.responseCode); // 静默丢弃，不重试
                }
            }
            _sending = false;
        }
    }
}
