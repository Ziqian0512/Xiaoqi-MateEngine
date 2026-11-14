using System;
using System.Collections.Generic;
using UnityEngine;
using VRM;

namespace Xiaoqi.Spike
{
    public class XiaoqiFace : MonoBehaviour
    {
        private static readonly string[] Keys = { "Joy", "Angry", "Sorrow", "Fun", "LookDown", "Blink", "O" };
        private EmotionLineConfig _cfg;
        private GameObject _model;
        private Animator _animator;
        private UniversalBlendshapes _uni;
        private VRMBlendShapeProxy _proxy;
        private StageData _stage;
        private float _nextFind;
        private float _nextUniCheck;
        private readonly Dictionary<string, float> _cur = new Dictionary<string, float>();
        private readonly Dictionary<string, float> _target = new Dictionary<string, float>();
        private string _roseAt = "";
        private bool _firstStage = true;
        private string _spikeName = "";
        private float _spikeStart = -1f;
        private bool _asleep;
        private float _yawnAt = -1f;
        private float _nextYawn;

        private void Start()
        {
            _cfg = EmotionLineConfig.Load();
            foreach (var k in Keys) { _cur[k] = 0f; _target[k] = 0f; }
            XiaoqiLink.OnStage += OnStage;
            if (XiaoqiLink.Current != null) OnStage(XiaoqiLink.Current);
            _nextYawn = Time.unscaledTime + UnityEngine.Random.Range(_cfg.body.yawn_every_min[0], _cfg.body.yawn_every_min[1]) * 60f;
        }

        private void OnDestroy() { XiaoqiLink.OnStage -= OnStage; }

        private void SetModel(GameObject model)
        {
            if (model == null || model == _model) return;
            _model = model;
            _uni = null;
            _proxy = null;
            _animator = null;
            _nextUniCheck = 0f;
        }

        private void OnStage(StageData s)
        {
            if (s == null) return;
            _stage = s;
            _asleep = s.where == "dorm" && s.pose == "sleep";
            var rose = s.face != null ? s.face.rose_at : "";
            if (_firstStage) { _firstStage = false; _roseAt = rose; return; }
            if (!string.IsNullOrEmpty(rose) && rose != _roseAt)
            {
                _roseAt = rose;
                float age = 0f;
                try { age = (float)(DateTime.Now - DateTime.Parse(rose)).TotalSeconds; } catch { }
                if (age <= _cfg.face.spike_max_age_sec)
                {
                    _spikeName = s.face.rose_name;
                    _spikeStart = Time.unscaledTime;
                    Debug.Log("XQ_SPIKE name=" + _spikeName + " at=" + rose + " note=" + s.face.rose_note);
                }
            }
        }

        private void Update()
        {
            if (!XiaoqiRuntime.Config.emotion_line) return;
            if (Time.unscaledTime >= _nextFind && _model == null)
            {
                _nextFind = Time.unscaledTime + 1f;
                var loader = FindFirstObjectByType<VRMLoader>(FindObjectsInactive.Include);
                if (loader != null) SetModel(loader.GetCurrentModel());
            }
            if (_model == null || _stage == null) return;

            if (Time.unscaledTime >= _nextUniCheck)
            {
                _nextUniCheck = Time.unscaledTime + 1f;
                _uni = _model.GetComponent<UniversalBlendshapes>();
                if (_uni == null) _uni = _model.GetComponentInChildren<UniversalBlendshapes>();
                _proxy = _model.GetComponent<VRMBlendShapeProxy>();
                if (_proxy == null) _proxy = _model.GetComponentInChildren<VRMBlendShapeProxy>();
                _animator = _model.GetComponentInChildren<Animator>();
            }

            foreach (var k in Keys) _target[k] = 0f;

            if (_asleep)
            {
                _target["Blink"] = 1f;
            }
            else
            {
                ApplyBase();
                ApplySpike();
                ApplyBody();
            }

            float dt = Time.unscaledDeltaTime;
            foreach (var k in Keys)
                _cur[k] = Mathf.MoveTowards(_cur[k], Mathf.Clamp01(_target[k]), dt / Mathf.Max(0.01f, _cfg.face.blend_sec));
            ApplyToModel();
        }

        private void ApplyBase()
        {
            var f = _stage.face;
            if (f == null || string.IsNullOrEmpty(f.top) || f.level < _cfg.face.base_min_level) return;
            AddMap(f.top, f.level * _cfg.face.base_gain, baseOnly: true);
        }

        private void ApplySpike()
        {
            if (_spikeStart < 0f || string.IsNullOrEmpty(_spikeName)) return;
            float age = Time.unscaledTime - _spikeStart;
            float w = 1f;
            if (age > _cfg.face.spike_hold)
                w = 1f - Mathf.Clamp01((age - _cfg.face.spike_hold) / Mathf.Max(0.01f, _cfg.face.spike_fade));
            if (w <= 0f) { _spikeStart = -1f; return; }
            AddMap(_spikeName, _cfg.face.spike_gain * w, baseOnly: false);
        }

        private void ApplyBody()
        {
            var b = _stage.body;
            if (b == null) return;
            if (b.sleepy >= _cfg.body.sleepy_hi)
            {
                _target["Blink"] = Mathf.Max(_target["Blink"], _cfg.body.sleepy_lid);
                if (Time.unscaledTime >= _nextYawn && (_spikeStart < 0f))
                {
                    _yawnAt = Time.unscaledTime;
                    _nextYawn = Time.unscaledTime + UnityEngine.Random.Range(_cfg.body.yawn_every_min[0], _cfg.body.yawn_every_min[1]) * 60f;
                    Debug.Log("XQ_YAWN sleepy=" + b.sleepy.ToString("0.0"));
                }
                if (_yawnAt >= 0f)
                {
                    float p = (Time.unscaledTime - _yawnAt) / Mathf.Max(0.01f, _cfg.body.yawn_sec);
                    float tri = p < 0.5f ? p * 2f : (1f - p) * 2f;
                    if (p >= 1f) _yawnAt = -1f;
                    else foreach (var kv in _cfg.body.yawn) Add(kv.Key, kv.Value * tri);
                }
            }
            if (b.stamina <= _cfg.body.tired_lo)
                foreach (var kv in _cfg.body.tired_add) Add(kv.Key, kv.Value);
        }

        private void AddMap(string emotion, float scale, bool baseOnly)
        {
            if (!_cfg.face.map.TryGetValue(emotion, out var m)) return;
            foreach (var kv in m)
            {
                SetMax(kv.Key, kv.Value * scale);
            }
        }

        private void SetMax(string key, float v)
        {
            if (!_target.ContainsKey(key)) return;
            _target[key] = Mathf.Max(_target[key], v);
        }

        private void Add(string key, float v)
        {
            if (!_target.ContainsKey(key)) return;
            _target[key] = Mathf.Clamp01(_target[key] + v);
        }

        private void ApplyToModel()
        {
            if (_uni != null)
            {
                var t = _uni.GetType();
                foreach (var k in Keys)
                {
                    var f = t.GetField(k);
                    if (f != null) f.SetValue(_uni, _cur[k]);
                }
                return;
            }
            if (_proxy != null)
            {
                foreach (var k in Keys)
                {
                    if (Enum.TryParse(k, out BlendShapePreset p))
                        _proxy.SetValue(BlendShapeKey.CreateFromPreset(p), _cur[k]);
                }
                _proxy.Apply();
            }
        }
    }
}

