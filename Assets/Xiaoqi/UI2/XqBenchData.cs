using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Xiaoqi.UI2
{
    /// <summary>
    /// G74 W6 工作台页签（她 / 生活 / 记忆 / 成长）的数据源。契约见主库 docs/BENCH-API.md（dsh/bench-api @ 0f89e5fd）。
    /// 只读 GET，不写任何东西；不校验令牌（控制台同源接口），但配了 token 就照常带 X-Token（无害）。
    /// 连不上：保留上一次的内容（先内存、再磁盘缓存 xq_bench_cache/&lt;key&gt;.json），并把该页标记为 Offline，面板亮「本地服务没开 · 重试」。
    /// 日志只打 key / 结果 / 耗时，不打服务器地址和令牌。
    /// 调试：-xq-bench-fake [目录]  读 &lt;目录&gt;/&lt;key&gt;.json 当作服务器返回（缺省目录 StreamingAssets/Xiaoqi/bench_fake）。
    /// </summary>
    public class XqBenchData : MonoBehaviour
    {
        public static XqBenchData Inst { get; private set; }

        public static XqBenchData Ensure()
        {
            if (Inst != null) return Inst;
            var go = new GameObject("xq-bench-data");
            UnityEngine.Object.DontDestroyOnLoad(go);
            Inst = go.AddComponent<XqBenchData>();
            return Inst;
        }

        /// <summary>有数据到手 / 失败状态变了就发一次。</summary>
        public event Action Changed;

        static readonly Dictionary<string, string> Paths = new Dictionary<string, string>
        {
            { "state", "/api/state" },
            { "life", "/api/life" },
            { "diary", "/api/g40/diary" },
            { "library", "/api/cards/library" },
            { "pools", "/api/g40/pools" },
            { "about", "/api/g40/about" },
            { "memory", "/api/memory" },
            { "today", "/api/today" },
            { "growth", "/api/growth" },
        };

        /// <summary>每页要的接口（页号 1=她 2=生活 3=记忆 4=成长）。</summary>
        public static string[] KeysOf(int page)
        {
            switch (page)
            {
                case 1: return new[] { "state", "life" };
                case 2: return new[] { "life", "diary", "library", "pools", "about" };
                case 3: return new[] { "memory", "today" };
                case 4: return new[] { "growth" };
            }
            return new string[0];
        }

        /// <summary>刷新间隔（秒）；0 = 只在进页 / 点重试时取一次。</summary>
        static float EveryOf(int page) { return (page == 1 || page == 2) ? 60f : 0f; }

        readonly Dictionary<string, JToken> _data = new Dictionary<string, JToken>();
        readonly Dictionary<string, DateTime> _stamp = new Dictionary<string, DateTime>();
        readonly Dictionary<string, bool> _fail = new Dictionary<string, bool>();
        Coroutine _loop;
        int _page;
        string _fakeDir;
        bool _fakeChecked;

        public static string CacheDir { get { return Path.Combine(Application.dataPath, "..", "xq_bench_cache"); } }

        // ---------------- 对外 ----------------

        /// <summary>该页是否有接口最近一次失败（没试过算没失败）。</summary>
        public bool Offline(int page)
        {
            foreach (var k in KeysOf(page))
            {
                bool f;
                if (_fail.TryGetValue(k, out f) && f) return true;
            }
            return false;
        }

        /// <summary>取某接口的最新内容；没有就读磁盘缓存；都没有返回 null。</summary>
        public JToken Get(string key)
        {
            JToken t;
            if (_data.TryGetValue(key, out t)) return t;
            t = LoadDisk(key);
            if (t != null) _data[key] = t;
            return t;
        }

        public DateTime StampOf(string key)
        {
            DateTime d;
            return _stamp.TryGetValue(key, out d) ? d : DateTime.MinValue;
        }

        public void Open(int page)
        {
            if (!isActiveAndEnabled) return;
            Stop();
            if (page < 1 || page > 4) return;
            _page = page;
            _loop = StartCoroutine(Loop(page));
        }

        public void Refresh()
        {
            if (_page >= 1) Open(_page);
        }

        public void Stop()
        {
            if (_loop != null) StopCoroutine(_loop);
            _loop = null;
            _page = 0;
        }

        void OnDisable() { if (_loop != null) StopCoroutine(_loop); _loop = null; }

        // ---------------- 取数 ----------------

        IEnumerator Loop(int page)
        {
            float every = EveryOf(page);
            while (_page == page)
            {
                bool refused = false;
                foreach (var key in KeysOf(page))
                {
                    if (refused) { SetFail(key, true); continue; }   // 前一个已经连不上，别再各等 5 秒
                    bool conn = false;
                    yield return Fetch(key, r => conn = r);
                    refused = conn;
                }
                Fire();
                if (every <= 0f) yield break;
                float t = 0f;
                while (_page == page && t < every) { t += Time.unscaledDeltaTime; yield return null; }
            }
        }

        void Fire()
        {
            try { if (Changed != null) Changed(); }
            catch (Exception e) { Debug.LogWarning("XQ_BENCHDATA changed: " + e.Message); }
        }

        void SetFail(string key, bool f) { _fail[key] = f; }

        string FakeDir()
        {
            if (_fakeChecked) return _fakeDir;
            _fakeChecked = true;
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-xq-bench-fake");
            if (i < 0) return null;
            string d = (i + 1 < args.Length && !args[i + 1].StartsWith("-")) ? args[i + 1] : null;
            if (string.IsNullOrEmpty(d)) d = Path.Combine(Application.streamingAssetsPath, "Xiaoqi", "bench_fake");
            _fakeDir = d;
            Debug.Log("XQ_BENCHDATA fake dir=" + d);
            return d;
        }

        /// <summary>done(true) = 连接层失败（拒绝 / 超时 / 没配服务器）。</summary>
        IEnumerator Fetch(string key, Action<bool> connFail)
        {
            float t0 = Time.realtimeSinceStartup;
            string fake = FakeDir();
            if (fake != null)
            {
                string fp = Path.Combine(fake, key + ".json");
                if (File.Exists(fp)) Accept(key, File.ReadAllText(fp), false, t0, "fake");
                else { SetFail(key, true); Debug.Log("XQ_BENCHDATA fetch key=" + key + " ok=n fake-missing"); }
                connFail(false);
                yield break;
            }

            var cfg = XiaoqiRuntime.Config ?? XiaoqiConfig.LoadOrCreate();
            string server = (cfg.server ?? "").TrimEnd('/');
            if (string.IsNullOrEmpty(server))
            {
                SetFail(key, true);
                Debug.Log("XQ_BENCHDATA fetch key=" + key + " ok=n no-server");
                connFail(true);
                yield break;
            }

            string url = server + Paths[key];
            using (var req = UnityWebRequest.Get(url))
            {
                req.timeout = (key == "memory" || key == "library" || key == "pools" || key == "diary") ? 10 : 5;
                if (!string.IsNullOrEmpty(cfg.token)) req.SetRequestHeader("X-Token", cfg.token);
                yield return req.SendWebRequest();
                bool ok = req.result == UnityWebRequest.Result.Success && req.responseCode == 200;
                if (ok)
                {
                    Accept(key, req.downloadHandler.text, true, t0, "net");
                    connFail(false);
                }
                else
                {
                    SetFail(key, true);
                    bool conn = req.result == UnityWebRequest.Result.ConnectionError;
                    Debug.Log("XQ_BENCHDATA fetch key=" + key + " ok=n result=" + req.result + " code=" + req.responseCode
                        + " ms=" + ((Time.realtimeSinceStartup - t0) * 1000f).ToString("0"));
                    connFail(conn);
                }
            }
        }

        void Accept(string key, string text, bool saveDisk, float t0, string src)
        {
            JToken tok = null;
            try { tok = JToken.Parse(text); }
            catch (Exception e) { Debug.Log("XQ_BENCHDATA fetch key=" + key + " ok=n parse " + e.Message); }
            if (tok == null || tok.Type == JTokenType.Null) { SetFail(key, true); return; }
            _data[key] = tok;
            _stamp[key] = DateTime.Now;
            SetFail(key, false);
            if (saveDisk) SaveDisk(key, text);
            Debug.Log("XQ_BENCHDATA fetch key=" + key + " ok=y src=" + src + " bytes=" + text.Length + " ms=" + ((Time.realtimeSinceStartup - t0) * 1000f).ToString("0"));
        }

        // ---------------- 磁盘缓存 ----------------

        static string CachePath(string key) { return Path.Combine(CacheDir, key + ".json"); }

        static void SaveDisk(string key, string text)
        {
            try
            {
                Directory.CreateDirectory(CacheDir);
                File.WriteAllText(CachePath(key), text);
            }
            catch (Exception e) { Debug.Log("XQ_BENCHDATA cache save " + key + " " + e.Message); }
        }

        JToken LoadDisk(string key)
        {
            try
            {
                string p = CachePath(key);
                if (!File.Exists(p)) return null;
                var t = JToken.Parse(File.ReadAllText(p));
                _stamp[key] = File.GetLastWriteTime(p);
                Debug.Log("XQ_BENCHDATA cache load key=" + key);
                return t;
            }
            catch (Exception e) { Debug.Log("XQ_BENCHDATA cache load " + key + " " + e.Message); return null; }
        }
    }
}
