using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Xiaoqi.Host;

namespace Xiaoqi.Spike
{
    /// <summary>
    /// XQ: P1-4b 待机导演。负责「怎么换」：选哪条、淡入、记日志。「什么时候换」归 BodyDirector。
    /// - 停掉 ME 的 12 秒顺序轮换（IDLE_SWITCH_TIME 设成 1e9，它是 public 字段，不改 ME 源码）。
    /// - 换待机 = XqPoseBlend 记旧姿势 → IdleIndex 立刻跳到目标 → LateUpdate 把旧姿势淡成新姿势（0.4–0.8 秒）。
    ///   不再让 IdleIndex 用 3 秒线性滑过中间编号。
    /// - 池：混合树（Idle→Female）阈值 0..19 共 20 个槽（19 与 0 同为 PET_IDLE），ME 原本只用前 10 个。
    ///   默认仍用前 10 个（PoolMax=10，和 ME 一直在播的一致）；动画拼图看过后再决定放开到 19。
    /// - 同一条 90 秒内不重复，最近 3 条不重复。
    /// 槽位 → 剪辑名 来自 AvatarAnimatorControllerV2 1.controller 的离线解析（见 FACTS.md §8）。
    /// [未编]
    /// </summary>
    [DefaultExecutionOrder(5000)]
    public class IdleDirector : MonoBehaviour
    {
        public static IdleDirector I { get; private set; }

        public static readonly string[] SlotNames =
        {
            "PET_IDLE", "PET_IDLE_UPDATE2_01", "PET_IDLE_UPDATE2_02", "PET_IDLE_UPDATE2_03", "PET_IDLE_UPDATE2_04",
            "PET_IDLE_2", "PET_IDLE_14", "PET_IDLE_4", "PET_IDLE_3", "PET_IDLE_5",
            "PET_IDLE_15", "PET_IDLE_13", "PET_IDLE_9", "PET_IDLE_12", "PET_IDLE_6",
            "PET_IDLE_10", "PET_IDLE_11", "PET_IDLE_7", "PET_IDLE_8",
        };

        /// <summary>池大小：只用槽位 0..PoolMax-1。ME 原值 10。</summary>
        public static int PoolMax = 10;
        /// <summary>安静档的槽位（B2 档位）；null 或空 = 不分档，全池同权。拼图出来后填。</summary>
        public static int[] CalmSlots = null;

        private const float NoRepeatSec = 90f;
        private const int NoRepeatLast = 3;

        private static readonly int IdleIndexParam = Animator.StringToHash("IdleIndex");

        private Animator _anim;
        private AvatarAnimatorController _ctl;
        private readonly XqPoseBlend _blend = new XqPoseBlend();
        private readonly Dictionary<int, float> _lastAt = new Dictionary<int, float>();
        private readonly List<int> _recent = new List<int>();
        private int _cur = -1;
        private float _nextBind;
        private string _tier = "normal";

        public string Tier { get => _tier; set => _tier = string.IsNullOrEmpty(value) ? "normal" : value; }
        public int Current => _cur;
        public bool Bound => _anim != null && _ctl != null;

        private void Awake() { I = this; }

        private void Update()
        {
            if (Time.unscaledTime >= _nextBind)
            {
                _nextBind = Time.unscaledTime + 1f;
                TryBind();
            }
        }

        private void LateUpdate()
        {
            if (!_blend.Active) return;
            // 拖拽、跳舞、睡觉、离开待机状态：直接停，别再把姿势往旧的拉
            if (_ctl == null || _ctl.isDragging || _ctl.isDancing || !InIdleState()) { _blend.Cancel(); return; }
            _blend.Apply(Time.unscaledDeltaTime);
        }

        private void TryBind()
        {
            var host = PetHost.I;
            if (host == null || !host.Ready) return;
            var a = host.Animator;
            if (a == null) return;
            if (a == _anim && _ctl != null) return;
            _anim = a;
            _ctl = a.GetComponent<AvatarAnimatorController>();
            if (_ctl == null) { Debug.Log("XQ_IDLE_MISS AvatarAnimatorController"); return; }
            _ctl.IDLE_SWITCH_TIME = 1e9f; // 停掉 ME 的 12 秒轮换
            _cur = Mathf.Clamp(Mathf.RoundToInt(_anim.GetFloat(IdleIndexParam)), 0, SlotNames.Length - 1);
            _blend.Cancel();
            Debug.Log("XQ_IDLE bound anim=" + a.name + " cur=" + _cur + " pool=" + PoolMax + " switch_time=1e9");
        }

        private bool InIdleState()
        {
            return _anim != null && _anim.GetCurrentAnimatorStateInfo(0).IsName("Idle") && !_anim.IsInTransition(0);
        }

        /// <summary>现在能不能换待机：只有在「站着待机」的时候才换。</summary>
        public bool CanSwitch(out string reason)
        {
            reason = "";
            if (!Bound) { reason = "unbound"; return false; }
            if (_ctl.isDragging) { reason = "dragging"; return false; }
            if (_ctl.isDancing || _ctl.xqHoldDance || _ctl.forcedDance) { reason = "dancing"; return false; }
            if (PetPresence.IsAsleep || _anim.GetBool("IsSleeping")) { reason = "asleep"; return false; }
            if (PetHost.I != null && (PetHost.I.IsWindowSitting || !PetHost.I.AvatarVisible)) { reason = "sit-or-hidden"; return false; }
            if (MenuActions.IsMovementBlocked()) { reason = "menu"; return false; }
            if (!InIdleState()) { reason = "not-idle-state"; return false; }
            if (_blend.Active) { reason = "fading"; return false; }
            return true;
        }

        /// <summary>从池里挑下一条。返回 -1 = 没得挑。cand 用于写日志。</summary>
        public int Pick(out string cand)
        {
            var sb = new StringBuilder();
            float now = Time.unscaledTime;
            var pool = new List<int>();
            for (int i = 0; i < Mathf.Min(PoolMax, SlotNames.Length); i++)
            {
                if (i == _cur) continue;
                if (_recent.Contains(i)) continue;
                pool.Add(i);
            }
            var fresh = pool.FindAll(i => !_lastAt.TryGetValue(i, out var t) || now - t >= NoRepeatSec);
            if (fresh.Count > 0) pool = fresh; // 90 秒内播过的优先排除；全都播过就退而求其次，只守「最近 3 条」
            if (pool.Count == 0) { cand = ""; return -1; }

            bool useCalm = _tier == "calm" && CalmSlots != null && CalmSlots.Length > 0;
            int best = -1;
            float bestScore = -1f;
            foreach (var i in pool)
            {
                float s = 1f + Random.value; // 1..2
                if (useCalm) s *= System.Array.IndexOf(CalmSlots, i) >= 0 ? 3f : 0.4f;
                if (sb.Length > 0) sb.Append('|');
                sb.Append(i).Append(':').Append(SlotNames[i]).Append('(').Append(s.ToString("0.0")).Append(')');
                if (s > bestScore) { bestScore = s; best = i; }
            }
            cand = sb.ToString();
            return best;
        }

        /// <summary>执行一次换待机。调用方负责先问 CanSwitch。</summary>
        public bool Play(int slot, string src, string why, string cand)
        {
            if (!Bound || slot < 0 || slot >= SlotNames.Length) return false;
            float fade = Random.Range(0.4f, 0.8f);
            bool blended = _blend.Begin(_anim, fade); // 必须在改参数之前
            _anim.SetFloat(IdleIndexParam, slot);
            int from = _cur;
            _cur = slot;
            _lastAt[slot] = Time.unscaledTime;
            _recent.Add(slot);
            while (_recent.Count > NoRepeatLast) _recent.RemoveAt(0);
            BrainLog.Write(src, "idle",
                "from=" + from + " pick=" + slot + ":" + SlotNames[slot]
                + " fade=" + (blended ? fade.ToString("0.00") : "none")
                + " tier=" + _tier + " cand=" + (string.IsNullOrEmpty(cand) ? "-" : cand) + " why=" + why);
            return true;
        }
    }
}
