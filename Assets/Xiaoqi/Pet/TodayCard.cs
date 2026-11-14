using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Xiaoqi.Spike
{
    public class TodayCard : MonoBehaviour
    {
        [Serializable] private class DayData
        {
            public int v; public string date; public string now; public int now_min;
            public Seg[] segs; public Mark[] marks; public string mood; public Body body;
        }
        [Serializable] private class Seg { public int a, b; public string start, end, name, act, where; }
        [Serializable] private class Mark { public string at; public int min; public string emo; public float delta; public string note, src; }
        [Serializable] private class Body { public float sleepy, stamina, social; }

        private bool _open;
        private DayData _data;
        private string _error;
        private Rect _card = new Rect(0, 0, 420, 420);
        private float _nextRefresh;
        private Vector2 _downPos;
        private float _downAt;
        private bool _downOnPet;
        private AvatarAnimatorController _avatar;
        private float _mouseOutsideAt = -1f;

        private void Update()
        {
            if (!XiaoqiRuntime.Config.emotion_line) return;
            if (_avatar == null) _avatar = FindFirstObjectByType<AvatarAnimatorController>(FindObjectsInactive.Include);

            if (Input.GetMouseButtonDown(0))
            {
                _downPos = Input.mousePosition;
                _downAt = Time.unscaledTime;
                _downOnPet = HitPet(Input.mousePosition);
            }
            if (Input.GetMouseButtonUp(0))
            {
                var dt = Time.unscaledTime - _downAt;
                var moved = Vector2.Distance(Input.mousePosition, _downPos);
                bool dragging = _avatar != null && _avatar.isDragging;
                bool click = _downOnPet && dt <= 0.3f && moved <= 6f && !dragging;
                if (click)
                {
                    if (_open) Close();
                    else Open();
                }
            }
            if (_open && Input.GetKeyDown(KeyCode.Escape)) Close();
            if (_open && _data == null && Time.unscaledTime >= _nextRefresh) StartCoroutine(Fetch());
            if (_open && _data != null && Time.unscaledTime >= _nextRefresh) StartCoroutine(Fetch());

            if (_open && _card.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y)))
                _mouseOutsideAt = -1f;
            else if (_open && _mouseOutsideAt < 0f) _mouseOutsideAt = Time.unscaledTime;
            if (_open && _mouseOutsideAt > 0f && Time.unscaledTime - _mouseOutsideAt > 10f) Close();
        }

        public static void DebugOpen()
        {
            var c = FindFirstObjectByType<TodayCard>(FindObjectsInactive.Include);
            if (c != null) c.Open();
        }

        private void Open()
        {
            _open = true;
            _data = null;
            _error = null;
            _nextRefresh = 0f;
            _mouseOutsideAt = -1f;
        }

        private void Close() { _open = false; }

        private IEnumerator Fetch()
        {
            _nextRefresh = Time.unscaledTime + 30f;
            var args = Environment.GetCommandLineArgs();
            var fi = Array.IndexOf(args, "-fake-day");
            if (fi >= 0 && fi + 1 < args.Length && System.IO.File.Exists(args[fi + 1]))
            {
                try { _data = JsonUtility.FromJson<DayData>(System.IO.File.ReadAllText(args[fi + 1])); }
                catch (Exception e) { _error = e.Message; }
                yield break;
            }
            var cfg = XiaoqiRuntime.Config;
            var url = (cfg.server ?? "").TrimEnd('/') + "/api/body/day";
            using (var req = UnityWebRequest.Get(url))
            {
                if (!string.IsNullOrEmpty(cfg.token)) req.SetRequestHeader("X-Token", cfg.token);
                req.timeout = 6;
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    try { _data = JsonUtility.FromJson<DayData>(req.downloadHandler.text); _error = null; }
                    catch (Exception e) { _error = e.Message; }
                }
                else _error = "连不上";
            }
        }

        private bool HitPet(Vector3 screen)
        {
            var loader = FindFirstObjectByType<VRMLoader>(FindObjectsInactive.Include);
            var model = loader != null ? loader.GetCurrentModel() : null;
            if (model == null) return false;
            var cam = Camera.main;
            if (cam == null) return false;
            var b = new Bounds();
            bool first = true;
            foreach (var r in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (first) { b = r.bounds; first = false; }
                else b.Encapsulate(r.bounds);
            }
            if (first) return false;
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            foreach (var x in new[] { b.min.x, b.max.x })
                foreach (var y in new[] { b.min.y, b.max.y })
                    foreach (var z in new[] { b.min.z, b.max.z })
                    {
                        var p = cam.WorldToScreenPoint(new Vector3(x, y, z));
                        min = Vector2.Min(min, new Vector2(p.x, p.y));
                        max = Vector2.Max(max, new Vector2(p.x, p.y));
                    }
            return new Rect(min, max - min).Contains(new Vector2(screen.x, screen.y));
        }

        private void OnGUI()
        {
            if (!_open) return;
            var w = 420f;
            var h = Mathf.Min(460f, Screen.height - 80f);
            _card = new Rect(Screen.width - w - 30f, 40f, w, h);
            GUI.Box(_card, "今天");
            GUILayout.BeginArea(new Rect(_card.x + 14, _card.y + 28, _card.width - 28, _card.height - 40));
            if (!string.IsNullOrEmpty(_error)) GUILayout.Label(_error);
            if (_data != null)
            {
                GUILayout.Label(_data.date + "   心情 " + (_data.mood ?? ""));
                GUILayout.Space(4);
                GUILayout.Label("日程");
                foreach (var s in _data.segs ?? new Seg[0])
                    GUILayout.Label(s.start + "–" + s.end + "  " + s.name);
                GUILayout.Space(4);
                GUILayout.Label("情绪点");
                var marks = _data.marks ?? new Mark[0];
                if (marks.Length == 0) GUILayout.Label("今天还没有什么特别的");
                for (int i = marks.Length - 1, n = 0; i >= 0 && n < 6; i--, n++)
                    GUILayout.Label(marks[i].at + "  " + marks[i].emo + "  " + marks[i].note);
                GUILayout.Space(4);
                if (_data.body != null)
                    GUILayout.Label("困意 " + _data.body.sleepy.ToString("0") + "   体力 "
                        + _data.body.stamina.ToString("0") + "   社交 " + _data.body.social.ToString("0"));
                GUILayout.Space(6);
                if (GUILayout.Button("关闭")) Close();
            }
            GUILayout.EndArea();
        }
    }
}
