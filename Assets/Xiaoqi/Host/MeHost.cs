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
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int x, y; }
        [DllImport("user32.dll")] private static extern IntPtr MonitorFromPoint(POINT pt, uint flags);
        private const uint MONITOR_DEFAULTTONULL = 0;
        [StructLayout(LayoutKind.Sequential)] struct MONINFO {public int size;public RECT monitor,work;public uint flags;}
        [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern bool GetMonitorInfo(IntPtr monitor,ref MONINFO info);
        [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr window,out uint pid);

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
        private GameObject _uiHitBlocker;
        private MenuActions _uiHitMenu;
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
            if (_holdingSleep && _animator != null && !_animator.GetBool("IsSleeping"))
                _holdingSleep = false;
            if (_holdingSleep) MaybeZzz();
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

        int _sleepTicket;
        bool _holdingSleep;
        float _nextZzz = -1f;

        public void SetSleeping(bool on)
        {
            _sleepTicket++;
            if (!on)
            {
                _holdingSleep = false;
                ApplySleep(false);
                // XQ: B1 第二轮 #17 坐窗时睡的，醒来回到坐窗
                if (_sitSleep)
                {
                    _sitSleep = false;
                    if (_animator != null) _animator.SetBool("isWindowSit", true);
                    Debug.Log("XQ_SLEEP sit-restore");
                }
                return;
            }
            StartCoroutine(SleepWhenIdle(_sleepTicket));
        }

        bool _sitSleep;
        static bool SitExitOld { get { return Array.IndexOf(Environment.GetCommandLineArgs(), "-xq-sit-exit") >= 0; } }

        System.Collections.IEnumerator SleepWhenIdle(int ticket)
        {
            // XQ: B1 第二轮 #17 坐在窗口上点「睡一会」不再 ExitWindowSit()（那会解除吸附、她掉下窗）。
            // 只把动画机的 isWindowSit 放下（吸附还在，窗位置不动），让 Idle→Sleeping 的过渡能走；醒来再放回 isWindowSit。
            // 命令行 -xq-sit-exit 退回旧行为（先下窗再睡）。
            if (IsWindowSitting)
            {
                if (SitExitOld) ExitWindowSit();
                else if (_animator != null)
                {
                    _sitSleep = true;
                    _animator.SetBool("isWindowSit", false);
                    Debug.Log("XQ_SLEEP sit-keep window (isWindowSit off, snap kept)");
                }
            }
            if (IsDancing) StopDance();
            float until = Time.unscaledTime + 2f;
            while (Time.unscaledTime < until && ticket == _sleepTicket)
            {
                bool idle = _animator != null && _animator.GetCurrentAnimatorStateInfo(0).IsName("Idle");
                if (idle && !IsDragging && !IsWindowSitting) break;
                yield return null;
            }
            if (ticket != _sleepTicket) yield break;
            ApplySleep(true);
            _holdingSleep = true;
            MaybeZzz();
            Debug.Log("XQ_SLEEP hold idle=" + (_animator != null && _animator.GetCurrentAnimatorStateInfo(0).IsName("Idle")) + " sitSleep=" + _sitSleep);
            if (_sitSleep) { yield return new WaitForSecondsRealtime(1.2f); Debug.Log("XQ_SLEEP sit-check sleeping=" + (_animator != null && _animator.GetCurrentAnimatorStateInfo(0).IsName("Sleeping"))); }
        }

        void ApplySleep(bool on)
        {
            var sc = _animator != null ? _animator.GetComponent<AvatarSleepController>() : null;
            if (sc != null) sc.XqSetSleeping(on);
            else if (_animator != null) _animator.SetBool("IsSleeping", on);
        }

        void MaybeZzz()
        {
            if (Time.unscaledTime < _nextZzz) return;
            Xiaoqi.Spike.PetBubble.Enqueue("Zzz…", "");
            _nextZzz = Time.unscaledTime + 300f;
        }

        // ------------------------------------------------------------ window

        public bool IsWindowSitting => _animator != null && _animator.GetBool("isWindowSit");

        public bool AvatarVisible
        {
            get
            {
                if (_model == null) return false;
                if (_model.gameObject.activeInHierarchy) return true;
                foreach (Transform t in _model.transform)
                    if (t.gameObject.activeInHierarchy) return true;
                return false;
            }
        }

        public bool IsChibi
        {
            get
            {
                var chibi = _model != null ? _model.GetComponentInChildren<ChibiToggle>(true) : null;
                if (chibi == null) return false;
                var f = chibi.GetType().GetField("isChibi", AnyInstance);
                return f != null && (bool)f.GetValue(chibi);
            }
        }

        public void ExitWindowSit()
        {
            var h = FindFirstObjectByType<AvatarWindowHandler>(FindObjectsInactive.Include);
            if (h == null) { Debug.Log("XQ_HOST_MISS AvatarWindowHandler"); return; }
            h.ForceExitWindowSitting();
        }

        public void MoveToCenter()
        {
            var process=System.Diagnostics.Process.GetCurrentProcess();var hwnd=process.MainWindowHandle;
            if(hwnd==IntPtr.Zero)hwnd=GetActiveWindow();
            GetWindowThreadProcessId(hwnd,out uint pid);
            if(pid!=(uint)process.Id){Debug.LogWarning("XQ_HOST center missing own window");return;}
            if (hwnd == IntPtr.Zero) { Debug.Log("XQ_HOST_MISS window"); return; }
            if (!GetWindowRect(hwnd, out var wr)) return;
            var body=Xiaoqi.UI2.XqBodyAnchor.Stable();
            var center=new POINT {x=wr.left+Mathf.RoundToInt(body.center.x),y=wr.top+Screen.height-Mathf.RoundToInt(body.center.y)};
            var info=new MONINFO {size=Marshal.SizeOf<MONINFO>()};
            if(!GetMonitorInfo(MonitorFromPoint(center,2),ref info))return;
            var work=info.work;
            int w = wr.right - wr.left;
            int h = wr.bottom - wr.top;
            int x = body.height>8?work.left+(work.right-work.left)/2-Mathf.RoundToInt(body.center.x):work.left+(work.right-work.left-w)/2;
            int y = body.height>8?work.bottom-18-Screen.height+Mathf.RoundToInt(body.yMin):work.bottom-h;
            MoveWindow(hwnd, x, y, w, h, true);
            Debug.Log("XQ_HOST center " + x + "," + y + " " + w + "x" + h);
        }

        private void FixWindowSize()
        {
            var hwnd = FindWindow("UnityWndClass", null);
            if (hwnd == IntPtr.Zero) return;
            if (!GetWindowRect(hwnd, out var wr)) return;
            int w = Mathf.Max(wr.right - wr.left, 900);
            int h = Mathf.Max(wr.bottom - wr.top, 520);
            MoveWindow(hwnd, wr.left, wr.top, w, h, true);
            Debug.Log("XQ_HOST resize " + w + "x" + h);
        }

        /// <summary>窗口整个跑到屏幕外时拉回中间；正常在屏内不动。</summary>
        public void EnsureOnScreen()
        {
            var hwnd = FindWindow("UnityWndClass", null);
            Debug.Log("XQ_WATCH hwnd=" + hwnd);
            if (hwnd == IntPtr.Zero) return;
            if (!GetWindowRect(hwnd, out var wr)) return;
            Debug.Log("XQ_WATCH rect=" + wr.left + "," + wr.top + "," + wr.right + "," + wr.bottom);
            // Any connected monitor counts as on-screen (not just the primary work area).
            int cx = (wr.left + wr.right) / 2;
            int cy = (wr.top + wr.bottom) / 2;
            // XQ: B1 #10 原来拿「窗口中心」判断在不在屏内：窗口比一块屏还大(2558x1440)，她被拖到屏幕中线以上时窗口中心先出了屏，
            // 窗口保护就把整个窗口拉回底部中间，拖动时每 2 秒闪回一次。改成看「她的身体中心」：她还在某块屏里就不动。
            var pt = new POINT { x = cx, y = cy };
            bool inside = MonitorFromPoint(pt, MONITOR_DEFAULTTONULL) != IntPtr.Zero;
            if (!inside)
            {
                // 窗口中心不在任何屏里：只有「桌宠态、不在切换中、她的外框大小正常」才改看身体中心。
                // 近景/工作台镜头放大后外框远超屏幕（实测 h=4238），不能拿它判断（G68 回归：副屏窗口被拉回主屏）。
                var bodyR = PetHost.I != null ? PetHost.I.ScreenRect() : new Rect(0, 0, 0, 0);
                bool petNormal = Xiaoqi.Mode.XqMode.Current == Xiaoqi.Mode.XqView.Pet && !Xiaoqi.Mode.XqMode.Busy
                    && bodyR.width > 2f && bodyR.height > 8f && bodyR.height < Screen.height * 1.2f;
                if (petNormal)
                {
                    var bp = new POINT { x = wr.left + Mathf.RoundToInt(bodyR.center.x), y = wr.top + Mathf.RoundToInt(Screen.height - bodyR.center.y) };
                    inside = MonitorFromPoint(bp, MONITOR_DEFAULTTONULL) != IntPtr.Zero;
                }
                else if (Xiaoqi.Mode.XqMode.Current != Xiaoqi.Mode.XqView.Pet || Xiaoqi.Mode.XqMode.Busy)
                {
                    inside = true; // 非桌宠态/切换中：不动窗口
                }
            }
            bool tiny = (wr.right - wr.left) < 700 || (wr.bottom - wr.top) < 420;
            if (!inside || tiny)
            {
                Debug.Log("XQ_HOST fix window inside=" + inside + " tiny=" + tiny + " rect=" + wr.left + "," + wr.top);
                FixWindowSize();
                MoveToCenter();
            }
        }

        // ------------------------------------------------------------- dance

        private IEnumerator InitDances()
        {
            yield return null;
            var rac0 = _animator != null ? _animator.runtimeAnimatorController : null;
            if (rac0 != null)
            {
                var names = new System.Text.StringBuilder();
                foreach (var c in rac0.animationClips)
                    if (c != null && (c.name.ToLower().Contains("dance") || c.name.ToLower().Contains("idle")))
                        names.Append(c.name).Append(" | ");
                Debug.Log("XQ_CLIPS " + names);
            }
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

        private readonly List<HostAction> _actions = new List<HostAction>();
        private int _idleNext = -1;

        public IReadOnlyList<HostAction> Actions
        {
            get { BuildActions(); return _actions; }
        }

        private void BuildActions()
        {
            _actions.Clear();
            var chibi = _model != null ? _model.GetComponentInChildren<ChibiToggle>(true) : null;
            bool chibiOn = false;
            if (chibi != null)
            {
                var f = chibi.GetType().GetField("isChibi", AnyInstance);
                chibiOn = f != null && (bool)f.GetValue(chibi);
            }
            if (chibi != null) _actions.Add(new HostAction { Id = "chibi", Label = chibiOn ? "恢复大小" : "变小" });
            if (IsWindowSitting) _actions.Add(new HostAction { Id = "unsit", Label = "下窗" });
            _actions.Add(new HostAction { Id = "sit_toggle", Label = WindowSitEnabled ? "禁止坐窗口" : "允许坐窗口" });
            // 撤掉：见 G59-P1-2c。LocomotionEnabled / enableLocomotion 仍保留，菜单不再露出「自己走 / 停下脚步」。
            _actions.Add(new HostAction { Id = "pose_next", Label = "换个姿势" });
            for (int i = 0; i < 5; i++)
                _actions.Add(new HostAction { Id = "dance_" + i, Label = "舞 " + (i + 1) });
        }

        public bool RunAction(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            var guard = _animator != null ? _animator.GetComponent<AvatarAnimatorController>() : null;
            if (guard != null) guard.ignoreClickUntil = Time.unscaledTime + 0.35f;
            if (id == "chibi")
            {
                var chibi = _model != null ? _model.GetComponentInChildren<ChibiToggle>(true) : null;
                if (chibi == null) { Debug.Log("XQ_HOST_MISS ChibiToggle"); return false; }
                chibi.ToggleChibiMode();
                return true;
            }
            if (id == "unsit") { ExitWindowSit(); return true; }
            if (id == "sit_toggle") { bool on=!WindowSitEnabled; WindowSitEnabled=on; if(XiaoqiRuntime.Config!=null){XiaoqiRuntime.Config.window_sit=on;XiaoqiConfig.Save(XiaoqiRuntime.Config);} SaveSettings(); return true; }
            if (id == "walk_toggle")
            {
                LocomotionEnabled = !LocomotionEnabled;
                SaveSettings();
                var loco = Resources.FindObjectsOfTypeAll<AvatarLocomotionController>();
                Debug.Log("XQ_HOST walk=" + LocomotionEnabled + " comps=" + loco.Length
                          + (loco.Length > 0 ? " enabled=" + loco[0].EnableLocomotion : ""));
                return true;
            }
            if (id.StartsWith("dance_") && int.TryParse(id.Substring(6), out var di))
                return BeginHoldDance(di);
            if (id == "pose_next" && _animator != null)
            {
                var director=Xiaoqi.Spike.IdleDirector.I;
                if(director!=null&&director.Bound)
                {int slot=director.Pick(out var candidates);return slot>=0&&director.Play(slot,"manual","menu",candidates);}
                _idleNext = (_idleNext + 3) % 10;
                _animator.SetFloat("IdleIndex", _idleNext);
                _animator.SetBool("isIdle", true);
                Debug.Log("XQ_HOST idle=" + _idleNext);
                return true;
            }
            return false;
        }
        public bool IsDancing
        {
            get
            {
                if (_animator == null) return false;
                if (_animator.GetBool("isDancing")) return true;
                return _animator.GetBool("isCustomDancing");
            }
        }

        public bool PlayDance(string name)
        {
            if (!TryBuiltInDance(name, out int index)) return false;
            return BeginHoldDance(index);
        }

        public void StopDance()
        {
            var ac = AnimatorController();
            if (ac == null) return;
            ac.xqHoldDance = false;
            ac.forcedDance = false;
            ac.SetDancing(false);
            Debug.Log("XQ_HOST dance stop");
        }

        bool BeginHoldDance(int index)
        {
            var ac = AnimatorController();
            if (ac == null) { Debug.Log("XQ_HOST_MISS AvatarAnimatorController"); return false; }
            // XQ: hold through CheckForSound. enableDancing stays false and does not cancel this.
            // Menu close keeps IsMovementBlocked for the fade; Update skips that cancel while held.
            ac.ignoreClickUntil = Time.unscaledTime + 0.35f;
            ac.xqHoldDance = true;
            ac.StartDancing();
            ac.XqSetDanceIndex(index);
            Debug.Log("XQ_HOST hold dance=" + index + " isDancing=" + (_animator != null && _animator.GetBool("isDancing"))
                      + " idx=" + (_animator != null ? _animator.GetFloat("DanceIndex") : -1f));
            return true;
        }

        static bool TryBuiltInDance(string name, out int index)
        {
            index = -1;
            if (string.IsNullOrEmpty(name)) return false;
            var s = name.Trim();
            if (s.StartsWith("dance_") && int.TryParse(s.Substring(6), out var di) && di >= 0 && di < 5)
            {
                index = di;
                return true;
            }
            if (s.StartsWith("舞") && int.TryParse(s.Substring(1).Trim(), out var n) && n >= 1 && n <= 5)
            {
                index = n - 1;
                return true;
            }
            if (int.TryParse(s, out var plain) && plain >= 1 && plain <= 5)
            {
                index = plain - 1;
                return true;
            }
            return false;
        }

        AvatarAnimatorController AnimatorController()
        {
            if (_animator == null) return null;
            return _animator.GetComponent<AvatarAnimatorController>();
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

        public void SetUiBlocksMovement(bool on)
        {
            if (_uiHitBlocker == null)
            {
                _uiHitBlocker = new GameObject("XqUi2HitBlocker");
                _uiHitBlocker.transform.SetParent(transform, false);
                _uiHitBlocker.SetActive(false);
            }
            var ma = _uiHitMenu != null ? _uiHitMenu : FindFirstObjectByType<MenuActions>();
            if (ma != null && ma != _uiHitMenu)
            {
                _uiHitMenu = ma;
                bool found = false;
                var list = ma.menuEntries;
                for (int i = 0; i < list.Count; i++)
                    if (list[i] != null && list[i].menu == _uiHitBlocker) { found = true; break; }
                if (!found)
                    list.Add(new MenuEntry
                    {
                        menu = _uiHitBlocker,
                        blockMovement = true,
                        blockHandTracking = true,
                        blockReaction = true,
                        blockChibiMode = false
                    });
            }
            if (_uiHitBlocker.activeSelf != on) _uiHitBlocker.SetActive(on);
        }
    }
}
