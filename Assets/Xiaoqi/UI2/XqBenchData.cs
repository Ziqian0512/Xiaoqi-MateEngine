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

        /// <summary>每页要的接口（页号 0=此刻 1=手账 2=记忆 3=成长 4=Brain轻控）。</summary>
        public static string[] KeysOf(int page)
        {
            switch (page)
            {
                case 0: return new[] { "state", "life" };
                case 1: return new[] { "life", "diary", "today" };
                case 2: return new[] { "memory", "today" };
                case 3: return new[] { "growth" };
                case 4: return new[] { "state" };
            }
            return new string[0];
        }

        /// <summary>刷新间隔（秒）；0 = 只在进页 / 点重试时取一次。</summary>
        static float EveryOf(int page) { return (page == 0 || page == 1) ? 60f : 0f; }

        readonly Dictionary<string, JToken> _data = new Dictionary<string, JToken>();
        readonly Dictionary<string, DateTime> _stamp = new Dictionary<string, DateTime>();
        readonly Dictionary<string, bool> _fail = new Dictionary<string, bool>();
        readonly HashSet<string> _inflight = new HashSet<string>();
        Coroutine _loop;
        int _page = -1;
        string _fakeDir;
        bool _fakeChecked;
        bool _prefetched;

        public static string CacheDir { get { return Path.Combine(Application.dataPath, "..", "xq_bench_cache"); } }

        // ---------------- 对外 ----------------

        public bool Loading(int page)
        {
            foreach (var k in KeysOf(page))
                if (_inflight.Contains(k)) return true;
            return false;
        }

        /// <summary>该页是否全部接口都失败且没有缓存。</summary>
        public bool Offline(int page)
        {
            var keys = KeysOf(page);
            if (keys.Length == 0) return false;
            bool anyFail = false;
            foreach (var k in keys)
            {
                bool f;
                if (_fail.TryGetValue(k, out f) && f) anyFail = true;
            }
            return anyFail;
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
            if (page < 0 || page > 4) return;
            _page = page;
            foreach (var k in KeysOf(page)) Get(k);   // Stale-While-Revalidate：先把已有缓存秒刷进内存
            _loop = StartCoroutine(Loop(page));
        }

        public void PrefetchAll()
        {
            if (!isActiveAndEnabled || _prefetched) return;
            _prefetched = true;
            string[] all = { "state", "life", "diary", "today", "memory", "growth" };
            foreach (var k in all) Get(k);
            for (int i = 0; i < all.Length; i++)
                StartCoroutine(FetchOne(_page, all[i]));
        }

        public void Refresh()
        {
            if (_page >= 0) Open(_page);
        }

        public void Stop()
        {
            if (_loop != null) StopCoroutine(_loop);
            _loop = null;
            _page = -1;
        }

        public void TriggerBrainAction(string act, Action<string> cb)
        {
            if (!isActiveAndEnabled) { cb?.Invoke("不可用"); return; }
            StartCoroutine(BrainActionCo(act, cb));
        }

        IEnumerator BrainActionCo(string act, Action<string> cb)
        {
            var cfg = XiaoqiRuntime.Config ?? XiaoqiConfig.LoadOrCreate();
            string server = cfg != null ? (cfg.server ?? "").TrimEnd('/') : "";
            if (string.IsNullOrEmpty(server)) { cb?.Invoke("未配服务器"); yield break; }
            XqHttp.Resp resp = default(XqHttp.Resp);
            yield return XqHttp.PostJson(server + "/api/self/run", cfg.token, "{\"action\":\"" + act + "\"}", 6, r => resp = r);
            if (resp.ok)
            {
                Debug.Log("XQ_BRAIN action=" + act + " ok");
                cb?.Invoke("已排队：" + act);
            }
            else
            {
                Debug.Log("XQ_BRAIN action=" + act + " code=" + resp.code);
                cb?.Invoke("触发失败 " + resp.code);
            }
        }

        void OnDisable() { if (_loop != null) StopCoroutine(_loop); _loop = null; }

        // ---------------- 并发取数 ----------------

        IEnumerator Loop(int page)
        {
            float every = EveryOf(page);
            while (_page == page)
            {
                var keys = KeysOf(page);
                for (int i = 0; i < keys.Length; i++)
                    StartCoroutine(FetchOne(page, keys[i]));
                Fire();
                if (every <= 0f) yield break;
                float t = 0f;
                while (_page == page && t < every) { t += Time.unscaledDeltaTime; yield return null; }
            }
        }

        IEnumerator FetchOne(int page, string key)
        {
            if (!_inflight.Add(key)) yield break;
            try
            {
                bool unused = false;
                yield return Fetch(key, r => unused = r);
            }
            finally
            {
                _inflight.Remove(key);
            }
            if (_page == page || (_page >= 0 && Array.IndexOf(KeysOf(_page), key) >= 0)) Fire();
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
            int timeout = 15;
            XqHttp.Resp resp = default(XqHttp.Resp);
            yield return XqHttp.Get(url, cfg.token, timeout, r => resp = r);
            if (resp.ok)
            {
                Accept(key, resp.text, true, t0, "net");
                connFail(false);
            }
            else
            {
                SetFail(key, true);
                Debug.Log("XQ_BENCHDATA fetch key=" + key + " ok=n code=" + resp.code + " err=" + resp.error
                    + " ms=" + ((Time.realtimeSinceStartup - t0) * 1000f).ToString("0"));
                connFail(resp.connError);
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
