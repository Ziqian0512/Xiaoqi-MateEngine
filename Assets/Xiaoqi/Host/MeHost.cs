using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using CustomDancePlayer;
using UnityEngine;
using VRM;
using Xamin;

namespace Xiaoqi.Host
{
    /// <summary>
    /// IPetHost 的 Mate Engine 实现。所有对 ME 类的直接引用都收在这个文件里
    /// （白名单见 G55 6.5）。任何一步反射找不到成员：打 XQ_HOST_MISS，返回空/不改原样。
    /// </summary>
    public class MeHost : MonoBehaviour, IPetHost
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int left, top, right, bottom; }

        [DllImport("user32.dll")] private static extern IntPtr GetActiveWindow();
        [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hWnd, out RECT r);
        [DllImport("user32.dll")] private static extern bool MoveWindow(IntPtr hWnd, int x, int y, int w, int h, bool repaint);
        [DllImport("user32.dll")] private static extern bool SystemParametersInfo(uint action, uint param, ref RECT r, uint winIni);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr FindWindow(string cls, string title);

        private const uint SPI_GETWORKAREA = 0x0030;
        private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static MeHost Instance { get; private set; }

        private GameObject _model;
        private Animator _animator;
        private VRMBlendShapeProxy _proxy;
        private UniversalBlendshapes _uni;
        private float _nextScan;
        private float _nextUniCheck;
        private FaceFrame _face;
        private bool _hasFace;

        private readonly List<string> _dances = new List<string>();
        private AvatarDanceHandler _dance;
        private FieldInfo _entriesField;
        private MethodInfo _loadAllMi;
        private MethodInfo _clickMi;
        private Type _entryType;
        private FieldInfo _entryIdField;

        private Func<bool> _menuBlocker;
        private GameObject _blockerGo;
        private bool _menuSuppressed;

        public bool Ready => _model != null && _animator != null;
        public Transform Avatar => _model != null ? _model.transform : null;
        public Animator Animator => _animator;
        public event Action AvatarChanged;

        private void Awake()
        {
            Instance = this;
            PetHost.I = this;
        }

        private void Start()
        {
            Scan();
            StartCoroutine(InitDances());
        }

        private void Update()
        {
            if (Time.unscaledTime >= _nextScan)
            {
                _nextScan = Time.unscaledTime + 1f;
                Scan();
            }
            if (_blockerGo != null)
            {
                bool on = _menuBlocker != null && _menuBlocker();
                if (_blockerGo.activeSelf != on) _blockerGo.SetActive(on);
            }
        }

        private void LateUpdate()
        {
            if (_hasFace && _uni == null) ApplyProxyFace();
        }

        // ------------------------------------------------------------ avatar

        private void Scan()
        {
            var loader = FindFirstObjectByType<VRMLoader>(FindObjectsInactive.Include);
            var model = loader != null ? loader.GetCurrentModel() : null;
            if (model == null) return;
            if (model != _model)
            {
                _model = model;
                _animator = model.GetComponentInChildren<Animator>(true);
                _proxy = model.GetComponentInChildren<VRMBlendShapeProxy>(true);
                _uni = null;
                _hasFace = false;
                Debug.Log("XQ_HOST avatar " + model.name + " animator=" + (_animator != null) + " proxy=" + (_proxy != null));
                AvatarChanged?.Invoke();
            }
            if (Time.unscaledTime >= _nextUniCheck)
            {
                _nextUniCheck = Time.unscaledTime + 1f;
                _uni = model.GetComponentInChildren<UniversalBlendshapes>(true);
            }
        }

        public bool IsDragging => _animator != null && _animator.GetBool("isDragging");

        public Rect ScreenRect()
        {
            var cam = Camera.main;
            if (_model == null || cam == null) return new Rect(0, 0, 0, 0);
            var b = new Bounds();
            bool first = true;
            foreach (var r in _model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (first) { b = r.bounds; first = false; }
                else b.Encapsulate(r.bounds);
            }
            if (first) return new Rect(0, 0, 0, 0);
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
            return new Rect(min, max - min);
        }

        public void SetFace(FaceFrame f)
        {
            _face = f;
            _hasFace = true;
            if (_uni == null)
            {
                var model = _model;
                if (model != null) _uni = model.GetComponentInChildren<UniversalBlendshapes>(true);
            }
            if (_uni != null)
            {
                _uni.Joy = f.joy;
                _uni.Angry = f.angry;
                _uni.Sorrow = f.sorrow;
                _uni.Fun = f.fun;
                _uni.LookDown = f.lookDown;
                _uni.Blink = f.blink;
                _uni.O = f.o;
            }
        }

        private void ApplyProxyFace()
        {
            if (_proxy == null) return;
            Set("Joy", _face.joy); Set("Angry", _face.angry); Set("Sorrow", _face.sorrow);
            Set("Fun", _face.fun); Set("LookDown", _face.lookDown); Set("Blink", _face.blink); Set("O", _face.o);
            _proxy.Apply();
        }

        private void Set(string key, float v)
        {
            if (Enum.TryParse(key, out BlendShapePreset p))
                _proxy.SetValue(BlendShapeKey.CreateFromPreset(p), v);
        }

        public void SetMouth(float open)
        {
            if (_uni != null)
            {
                var t = _uni.GetType();
                foreach (var k in new[] { "A", "I", "U", "O" }) t.GetField(k)?.SetValue(_uni, 0f);
                var pick = new[] { "A", "I", "U", "O" }[UnityEngine.Random.Range(0, 4)];
                t.GetField(pick)?.SetValue(_uni, open);
                return;
            }
            if (_proxy != null)
            {
                foreach (var p in new[] { BlendShapePreset.A, BlendShapePreset.I, BlendShapePreset.U, BlendShapePreset.O })
                    _proxy.SetValue(BlendShapeKey.CreateFromPreset(p), 0f);
                _proxy.SetValue(BlendShapeKey.CreateFromPreset(BlendShapePreset.A), open);
                _proxy.Apply();
            }
        }

        public void LoadModel(string path)
        {
            var loader = FindFirstObjectByType<VRMLoader>(FindObjectsInactive.Include);
            if (loader == null) { Debug.Log("XQ_HOST_MISS VRMLoader"); return; }
            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
            {
                Debug.Log("XQ_HOST model missing " + path);
                return;
            }
            loader.LoadVRM(path);
        }

        public void SetSleeping(bool on)
        {            if (_animator == null) return;
            foreach (var p in _animator.parameters)
            {
                if (p.type == AnimatorControllerParameterType.Bool && p.name == "IsSleeping")
                {
                    _animator.SetBool("IsSleeping", on);
                    return;
                }
            }
        }

        // ------------------------------------------------------------ window

        public bool IsWindowSitting => _animator != null && _animator.GetBool("isWindowSit");

        public void ExitWindowSit()
        {
            var h = FindFirstObjectByType<AvatarWindowHandler>(FindObjectsInactive.Include);
            if (h == null) { Debug.Log("XQ_HOST_MISS AvatarWindowHandler"); return; }
            h.ForceExitWindowSitting();
        }

        public void MoveToCenter()
        {
            var hwnd = GetActiveWindow();
            if (hwnd == IntPtr.Zero) hwnd = FindWindow("UnityWndClass", null);
            if (hwnd == IntPtr.Zero) { Debug.Log("XQ_HOST_MISS window"); return; }
            if (!GetWindowRect(hwnd, out var wr)) return;
            var work = new RECT();
            if (!SystemParametersInfo(SPI_GETWORKAREA, 0, ref work, 0)) return;
            int w = wr.right - wr.left;
            int h = wr.bottom - wr.top;
            int x = work.left + (work.right - work.left - w) / 2;
            int y = work.bottom - h;
            MoveWindow(hwnd, x, y, w, h, true);
            Debug.Log("XQ_HOST center " + x + "," + y + " " + w + "x" + h);
        }

        // ------------------------------------------------------------- dance

        private IEnumerator InitDances()
        {
            yield return null;
            yield return null;
            _dance = FindFirstObjectByType<AvatarDanceHandler>(FindObjectsInactive.Include);
            if (_dance == null) { Debug.Log("XQ_HOST_MISS AvatarDanceHandler"); yield break; }

            var t = _dance.GetType();
            _entriesField = t.GetField("entries", AnyInstance);
            _loadAllMi = t.GetMethod("LoadAllSources", AnyInstance);
            _clickMi = t.GetMethod("OnListItemClicked", AnyInstance);
            if (_entriesField == null) Debug.Log("XQ_HOST_MISS entries");
            if (_loadAllMi == null) Debug.Log("XQ_HOST_MISS LoadAllSources");
            if (_clickMi == null) Debug.Log("XQ_HOST_MISS OnListItemClicked");

            var sub = t.GetField("streamingSubfolder", AnyInstance);
            if (sub != null) sub.SetValue(_dance, "Xiaoqi/Dances");
            else Debug.Log("XQ_HOST_MISS streamingSubfolder");

            try { _loadAllMi?.Invoke(_dance, null); }
            catch (Exception e) { Debug.Log("XQ_HOST dance load: " + e.Message); }

            RefreshDances();
        }

        private void RefreshDances()
        {
            _dances.Clear();
            if (_dance == null || _entriesField == null) return;
            if (!(_entriesField.GetValue(_dance) is IList list)) return;
            if (list.Count > 0)
            {
                _entryType = list[0].GetType();
                _entryIdField = _entryType.GetField("id", AnyInstance);
            }
            foreach (var e in list)
            {
                var id = _entryIdField?.GetValue(e) as string;
                if (!string.IsNullOrEmpty(id)) _dances.Add(id);
            }
            Debug.Log("XQ_HOST dances=" + _dances.Count);
        }

        public IReadOnlyList<string> Dances => _dances;
        public bool IsDancing => _animator != null && _animator.GetBool("isCustomDancing");

        public bool PlayDance(string name)
        {
            if (_dance == null || _clickMi == null || _entriesField == null) return false;
            if (!(_entriesField.GetValue(_dance) is IList list)) return false;
            for (int i = 0; i < list.Count; i++)
            {
                var id = _entryIdField?.GetValue(list[i]) as string;
                if (string.Equals(id, name, StringComparison.OrdinalIgnoreCase))
                {
                    try { _clickMi.Invoke(_dance, new object[] { i }); return true; }
                    catch (Exception e) { Debug.Log("XQ_HOST dance play: " + e.Message); return false; }
                }
            }
            return false;
        }

        public void StopDance()
        {
            if (_dance == null) return;
            try { _dance.StopPlay(); }
            catch (Exception e) { Debug.Log("XQ_HOST dance stop: " + e.Message); }
        }

        // ---------------------------------------------------------- settings

        private static SaveLoadHandler Handler => SaveLoadHandler.Instance;

        public float Size
        {
            get => Handler != null ? Handler.data.avatarSize : 1f;
            set
            {
                if (Handler == null) return;
                Handler.data.avatarSize = Mathf.Clamp(value, 0.5f, 2f);
                SyncScaleController(Handler.data.avatarSize);
                SaveLoadHandler.ApplyAllSettingsToAllAvatars();
            }
        }

        /// <summary>ME 的 AvatarScaleController 每帧把自己的 targetSize 写回 data，必须同步，否则设置会被覆盖。</summary>
        private static void SyncScaleController(float value)
        {
            foreach (var c in Resources.FindObjectsOfTypeAll<AvatarScaleController>())
            {
                if (c == null) continue;
                var t = c.GetType();
                if (t.GetField("avatarSizeSlider", AnyInstance)?.GetValue(c) is UnityEngine.UI.Slider s)
                    s.SetValueWithoutNotify(value);
                t.GetField("targetSize", AnyInstance)?.SetValue(c, value);
            }
        }

        public bool Topmost
        {
            get => Handler != null && Handler.data.isTopmost;
            set { if (Handler == null) return; Handler.data.isTopmost = value; SaveLoadHandler.ApplyAllSettingsToAllAvatars(); }
        }

        public int Quality
        {
            get => Handler != null ? Handler.data.graphicsQualityLevel : 1;
            set
            {
                if (Handler == null) return;
                Handler.data.graphicsQualityLevel = Mathf.Clamp(value, 0, 3);
                QualitySettings.SetQualityLevel(Handler.data.graphicsQualityLevel, true);
                SaveLoadHandler.ApplyAllSettingsToAllAvatars();
            }
        }

        public int Fps
        {
            get => Handler != null ? Handler.data.fpsLimit : 60;
            set { if (Handler == null) return; Handler.data.fpsLimit = Mathf.Clamp(value, 30, 144); Application.targetFrameRate = Handler.data.fpsLimit; }
        }

        public bool WindowSitEnabled
        {
            get => Handler != null && Handler.data.enableWindowSitting;
            set { if (Handler == null) return; Handler.data.enableWindowSitting = value; SaveLoadHandler.ApplyAllSettingsToAllAvatars(); }
        }

        public bool LocomotionEnabled
        {
            get => Handler != null && Handler.data.enableLocomotion;
            set { if (Handler == null) return; Handler.data.enableLocomotion = value; SaveLoadHandler.ApplyAllSettingsToAllAvatars(); }
        }

        public bool StartWithWindows
        {
            get => Handler != null && Handler.data.startWithWindows;
            set
            {
                if (Handler == null) return;
                Handler.data.startWithWindows = value;
                XiaoqiRunKey.Sync(value);
            }
        }

        public void SaveSettings()
        {
            if (Handler == null) return;
            Handler.SaveToDisk();
            SaveLoadHandler.ApplyAllSettingsToAllAvatars();
        }

        // ------------------------------------------------------------- menus

        public void SuppressHostMenu()
        {
            if (_menuSuppressed) return;
            _menuSuppressed = true;
            foreach (var sel in FindObjectsByType<CircleSelector>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (sel == null) continue;
                sel.transform.localScale = Vector3.zero;
                sel.gameObject.SetActive(false);
            }
            foreach (var ma in FindObjectsByType<MenuActions>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (ma == null) continue;
                ma.radialMenuKey = KeyCode.None;
            }
            Debug.Log("XQ_HOST me-menu suppressed");
        }

        public void RegisterMenuBlocker(Func<bool> isOpen)
        {
            _menuBlocker = isOpen;
            if (_blockerGo == null)
            {
                _blockerGo = new GameObject("XqMenuBlocker");
                _blockerGo.SetActive(false);
            }
            var ma = FindFirstObjectByType<MenuActions>();
            if (ma == null) { Debug.Log("XQ_HOST_MISS MenuActions"); return; }
            foreach (var e in ma.menuEntries)
                if (e != null && e.menu == _blockerGo) return;
            ma.menuEntries.Add(new MenuEntry
            {
                menu = _blockerGo,
                blockMovement = true,
                blockHandTracking = true,
                blockReaction = true,
                blockChibiMode = false
            });
        }
    }
}
