using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Xiaoqi.UI2
{
    /// <summary>
    /// 近景的数据源：`/api/body/day` 与 `/api/body/feed`。
    /// 只读，不写任何东西；失败一律清空（卡片显示「还没有」），不保留旧数据。
    ///
    /// feed 的取法要留意：服务器按 id 升序返回、只给 id &gt; since 的、limit ≤ 50。
    /// 所以先不带 since 请求一次拿 last_id，再请求 since = max(0, last_id - 50) &amp; limit=50，
    /// 取到的是最新一窗；直接 since=0 只会拿到最早 50 条，7 天后「最新一句」就全是旧话。
    ///
    /// 轮询 3 秒（P2-5 要求改服务器后 ≤5 秒内卡片跟着变）。只在近景显示期间轮询。
    /// 日志里只打状态，不打服务器地址和令牌。
    /// </summary>
    public class XqCloseData : MonoBehaviour
    {
        [Serializable]
        class Envelope
        {
            public int last_id;
            public Item[] items;
        }

        [Serializable]
        class Item
        {
            public int id;
            public string ts;
            public string kind;
            public string channel;
            public string scope;
            public string text;
        }

        public static XqCloseData Inst { get; private set; }

        /// <summary>day 数据；null 表示这次没拿到（断线 / 解析失败 / 没配服务器）。</summary>
        public XqCloseDay Day { get; private set; }

        /// <summary>最新一句 kind=said 的话（服务器写的是 said）；null 表示没有或没拿到。</summary>
        public string Say { get; private set; }

        /// <summary>day 与 feed 两者是否都拿到了。卡片用它区分「还没拿到」与「拿到了但没有」。</summary>
        public bool Ready { get; private set; }

        /// <summary>数据变了就发一次，面板据此重画卡片。</summary>
        public event Action Changed;

        bool _on;
        Coroutine _loop;

        public static XqCloseData Ensure()
        {
            if (Inst != null) return Inst;
            var go = new GameObject("xq-close-data");
            UnityEngine.Object.DontDestroyOnLoad(go);
            Inst = go.AddComponent<XqCloseData>();
            return Inst;
        }

        public void Begin()
        {
            if (!isActiveAndEnabled) return;   // 未启用就别把 _on 卡成 true
            if (_on) return;
            _on = true;
            if (_loop != null) StopCoroutine(_loop);
            _loop = StartCoroutine(Loop());
        }

        public void End()
        {
            _on = false;
            if (_loop != null) StopCoroutine(_loop);
            _loop = null;
        }

        void OnDisable()
        {
            if (_loop != null) StopCoroutine(_loop);
            _loop = null;
        }

        IEnumerator Loop()
        {
            while (_on)
            {
                yield return FetchDay();
                yield return FetchFeed();
                Ready = Day != null || Say != null;
                Fire();

                float t = 0f;
                while (_on && t < 3f)
                {
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
        }

        void Fire()
        {
            try { if (Changed != null) Changed(); }
            catch (Exception e) { Debug.LogWarning("XQ_CLOSE data changed: " + e.Message); }
        }

        static XiaoqiConfig Cfg()
        {
            return XiaoqiRuntime.Config ?? XiaoqiConfig.LoadOrCreate();
        }

        static string Url(string path)
        {
            var cfg = Cfg();
            var server = (cfg.server ?? "").TrimEnd('/');
            if (string.IsNullOrEmpty(server)) return null;
            return server + path;
        }

        static void Auth(UnityWebRequest req)
        {
            var cfg = Cfg();
            if (!string.IsNullOrEmpty(cfg.token)) req.SetRequestHeader("X-Token", cfg.token);
        }

        // ---------------- day ----------------

        IEnumerator FetchDay()
        {
            // 与 XqTodayCard 同一套 -fake-day 开关，方便没服务器时看效果
            var args = Environment.GetCommandLineArgs();
            int fi = Array.IndexOf(args, "-fake-day");
            if (fi >= 0 && fi + 1 < args.Length && System.IO.File.Exists(args[fi + 1]))
            {
                XqCloseDay fake = null;
                try { fake = JsonUtility.FromJson<XqCloseDay>(System.IO.File.ReadAllText(args[fi + 1])); }
                catch (Exception e) { Debug.Log("XQ_CLOSE fake-day " + e.Message); }
                Day = fake;
                yield break;
            }

            var url = Url("/api/body/day");
            if (url == null) { Day = null; yield break; }

            using (var req = UnityWebRequest.Get(url))
            {
                Auth(req);
                req.timeout = 6;
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    Day = null;
                    Debug.Log("XQ_CLOSE day fail: " + req.responseCode);
                    yield break;
                }
                try { Day = JsonUtility.FromJson<XqCloseDay>(req.downloadHandler.text); }
                catch (Exception e) { Day = null; Debug.Log("XQ_CLOSE day parse: " + e.Message); }
            }
        }

        // ---------------- feed ----------------

        IEnumerator FetchFeed()
        {
            var url = Url("/api/body/feed");
            if (url == null) { Say = Day != null ? Day.say : null; yield break; }
            if (Day != null && !string.IsNullOrEmpty(Day.say)) { Say = Day.say; yield break; }

            int lastId = 0;
            bool ok = false;
            yield return Get(url, env =>
            {
                if (env == null) return;
                lastId = env.last_id;
                ok = true;
            });
            if (!ok) { Say = null; yield break; }

            int since = Mathf.Max(0, lastId - 50);
            string window = url + "?since=" + since + "&limit=50";
            string say = null;
            yield return Get(window, env =>
            {
                if (env == null || env.items == null) return;
                foreach (var it in env.items)
                {
                    if (it == null) continue;
                    if (it.kind != "said" && it.kind != "say") continue;              // 主人说的话是 kind='heard'，不要
                    if (string.IsNullOrEmpty(it.text)) continue;
                    if (!string.IsNullOrEmpty(it.scope) && it.scope != "private") continue;
                    say = it.text;                               // 升序，最后一条最新
                }
            });
            Say = say;
        }

        IEnumerator Get(string url, Action<Envelope> done)
        {
            using (var req = UnityWebRequest.Get(url))
            {
                Auth(req);
                req.timeout = 6;
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log("XQ_CLOSE feed fail: " + req.responseCode);
                    done(null);
                    yield break;
                }
                Envelope env = null;
                try { env = JsonUtility.FromJson<Envelope>(req.downloadHandler.text); }
                catch (Exception e) { Debug.Log("XQ_CLOSE feed parse: " + e.Message); }
                done(env);
            }
        }

        public void Shot(string fileName, float delaySec = 0f)
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-xq-close-shots");
            if (i < 0 || i + 1 >= args.Length) return;
            string dir = args[i + 1];
            if (string.IsNullOrEmpty(dir)) return;
            StartCoroutine(ShotCo(System.IO.Path.Combine(dir, fileName), delaySec));
        }

        IEnumerator ShotCo(string fullPath, float delaySec)
        {
            if (delaySec > 0f) yield return new WaitForSecondsRealtime(delaySec);
            yield return new WaitForEndOfFrame();
            try
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullPath));
                var tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
                tex.Apply();
                System.IO.File.WriteAllBytes(fullPath, tex.EncodeToPNG());
                Destroy(tex);
                Debug.Log("XQ_CLOSE shot " + fullPath + " " + Screen.width + "x" + Screen.height);
            }
            catch (Exception e) { Debug.LogWarning("XQ_CLOSE shot err: " + e.Message); }
        }
    }
}
