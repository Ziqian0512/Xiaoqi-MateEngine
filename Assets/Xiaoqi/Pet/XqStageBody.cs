using UnityEngine;
using Xiaoqi.Host;

namespace Xiaoqi.Spike
{
    /// <summary>
    /// XQ: P1-7 服务器意图 → 桌宠身体。只读 /api/body/stage 的 where / pose / body.sleepy / phone.typing，按
    /// INTENT-MAP-DRAFT 的映射（主人 11-16 已确认 6-1…6-5）做三件事：
    ///   1. 定待机池档位 calm / normal，交给 BodyDirector.SetServerTier（只改权重，不直接播剪辑；睡姿仍归 PetPresence）。
    ///   2. 给头顶标签一个 dorm 时的文字（sit_write / sit_eat / lie_phone → 当前这段的名字，缺素材的降级）。
    ///   3. phone.typing 从 false 变 true 时出一个「正在打字…」小气泡（6-5：要）。
    ///   4. 困意高时自己睡着（6-3：要）：sleepy ≥ sleepy_hi 持续 hold 秒、人在宿舍、没被碰过、没在拖/跳/菜单，
    ///      就 Nap(self_sleep_min)；醒后冷却 self_sleep_cooldown_min 分钟；困意回落到 sleepy_hi-30 以下就醒。
    ///      叫醒入口已有：拖她、菜单「叫醒她」。
    /// 不做：spot/prop、WalkLeft/Right、isEating、躲边（6-1 留着、6-2 不用）。受 emotion_line 总开关管。
    /// 日志：stdout 的 XQ_STAGE，和 brain.log 里 src=server 的 evt=stage / evt=self_sleep / evt=typing。
    /// [未编]
    /// </summary>
    public class XqStageBody : MonoBehaviour
    {
        public static XqStageBody I { get; private set; }

        private EmotionLineConfig _cfg;
        private StageData _stage;
        private float _stageAt = -1f;
        private string _key = "";

        private bool _typingWas;
        private float _lastTypingBubble = -999f;

        private float _hiSince = -1f;
        private float _lastTouch = -999f;
        private float _cooldownUntil;
        private bool _asleepWas;
        private bool _selfNap;
        private float _nextTick;

        private void Awake() { I = this; }

        private void Start()
        {
            _cfg = EmotionLineConfig.Load();
            // 验收用：-xq-brain-fast 时把「自己睡着」的等待缩短（只改内存，不落盘），冒烟里一分钟内能看到
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-xq-brain-fast") >= 0)
            {
                _cfg.body.self_sleep_hold_sec = 8f;
                _cfg.body.self_sleep_idle_sec = 3f;
                _cfg.body.self_sleep_min = 0.4f;
                _cfg.body.self_sleep_cooldown_min = 0.5f;
            }
            XiaoqiLink.OnStage += OnStage;
            if (XiaoqiLink.Current != null) OnStage(XiaoqiLink.Current);
        }

        private void OnDestroy() { XiaoqiLink.OnStage -= OnStage; }

        /// <summary>dorm 时头顶标签：缺素材的三个 pose 写「在干嘛」；其余为空。PetPresence 调用。</summary>
        public static string DormLabel(StageData s)
        {
            if (s == null || s.where != "dorm") return "";
            if (s.pose == "sit_write" || s.pose == "sit_eat" || s.pose == "lie_phone")
                return s.seg != null && !string.IsNullOrEmpty(s.seg.name) ? s.seg.name : "";
            return "";
        }

        /// <summary>算档位。外出和缺素材的 pose、困意高 → calm；其余 normal。</summary>
        public static string TierFor(StageData s, float sleepyHi)
        {
            if (s == null) return "normal";
            if (s.where != "dorm") return "calm";
            switch (s.pose)
            {
                case "sleep":
                case "sit_write":
                case "sit_eat":
                case "lie_phone":
                case "stand_window":
                    return "calm";
            }
            if (s.body != null && s.body.sleepy >= sleepyHi) return "calm";
            return "normal"; // sit_idle / stand_tidy / 其他（6-2：洗漱不躲边，用普通待机）
        }

        private void OnStage(StageData s)
        {
            if (s == null || XiaoqiRuntime.Config == null || !XiaoqiRuntime.Config.emotion_line) return;
            if (_cfg == null) _cfg = EmotionLineConfig.Load();
            _stage = s;
            _stageAt = Time.unscaledTime;

            string tier = TierFor(s, _cfg.body.sleepy_hi);
            string key = s.where + "/" + s.pose + "/" + tier;
            if (key != _key)
            {
                _key = key;
                string label = DormLabel(s);
                BrainLog.Write(BodyDirector.SrcServer, "stage",
                    "where=" + s.where + " pose=" + s.pose + " tier=" + tier + " label=" + (label == "" ? "-" : label)
                    + " sleepy=" + (s.body != null ? s.body.sleepy.ToString("0") : "-"));
                Debug.Log("XQ_STAGE pose=" + s.pose + " where=" + s.where + " tier=" + tier);
            }
            BodyDirector.SetServerTier(tier, "stage " + s.where + "/" + s.pose);

            // 打字小气泡：只在 false→true 的边沿出，60 秒内不重复；睡着/安静/外出不出
            bool typing = s.phone != null && s.phone.typing;
            if (typing && !_typingWas
                && Time.unscaledTime - _lastTypingBubble >= 60f
                && !PetPresence.IsAsleep && !XiaoqiRuntime.IsQuiet() && s.where == "dorm")
            {
                _lastTypingBubble = Time.unscaledTime;
                PetBubble.Enqueue("正在打字…", "");
                BrainLog.Write(BodyDirector.SrcServer, "typing", "bubble=1");
            }
            _typingWas = typing;
        }

        private void Update()
        {
            var host = PetHost.I;
            if (host == null || !host.Ready || _cfg == null) return;
            float now = Time.unscaledTime;

            // 「碰过她」：拖、按着鼠标、鼠标停在身体上。自己睡着之前要安静 self_sleep_idle_sec 秒。
            if (host.IsDragging || Input.GetMouseButton(0)) _lastTouch = now;
            else
            {
                var r = host.ScreenRect();
                if (r.height >= 2f)
                {
                    var body = new Rect(r.x + r.width * 0.15f, r.y, r.width * 0.7f, r.height);
                    if (body.Contains(new Vector2(Input.mousePosition.x, Input.mousePosition.y))) _lastTouch = now;
                }
            }

            if (now < _nextTick) return;
            _nextTick = now + 1f;

            // 醒来后开冷却（不管是谁叫醒的：拖、菜单、小睡到点、服务器 pose 变了）
            bool asleep = PetPresence.IsAsleep;
            if (_asleepWas && !asleep)
            {
                _cooldownUntil = now + _cfg.body.self_sleep_cooldown_min * 60f;
                _hiSince = -1f;
                if (_selfNap) BrainLog.Write(BodyDirector.SrcServer, "self_sleep", "wake cooldown_min=" + _cfg.body.self_sleep_cooldown_min);
                _selfNap = false;
            }
            _asleepWas = asleep;

            bool on = _cfg.body.self_sleep && XiaoqiRuntime.Config != null && XiaoqiRuntime.Config.emotion_line;
            // 服务器很久没更新（断线）就别凭旧数据睡
            bool fresh = _stage != null && now - _stageAt <= 30f;
            float sleepy = _stage != null && _stage.body != null ? _stage.body.sleepy : 0f;

            // 自己睡着之后：困意明显回落就醒
            if (_selfNap && asleep && fresh && sleepy < _cfg.body.sleepy_hi - 30f)
            {
                BrainLog.Write(BodyDirector.SrcServer, "self_sleep", "wake reason=sleepy_low sleepy=" + sleepy.ToString("0"));
                PetPresence.Wake();
                return;
            }

            bool inDorm = fresh && _stage.where == "dorm" && _stage.pose != "sleep";
            if (!on || !inDorm || asleep || sleepy < _cfg.body.sleepy_hi) { _hiSince = -1f; return; }
            if (_hiSince < 0f) _hiSince = now;
            if (now - _hiSince < _cfg.body.self_sleep_hold_sec) return;
            if (now < _cooldownUntil) return;
            if (now - _lastTouch < _cfg.body.self_sleep_idle_sec) return;
            if (host.IsDragging || host.IsDancing || host.IsWindowSitting || !host.AvatarVisible) return;
            if (MenuActions.IsMovementBlocked()) return;

            _selfNap = true;
            BrainLog.Write(BodyDirector.SrcServer, "self_sleep",
                "start sleepy=" + sleepy.ToString("0") + " hold=" + _cfg.body.self_sleep_hold_sec.ToString("0")
                + " min=" + _cfg.body.self_sleep_min);
            PetPresence.Nap(_cfg.body.self_sleep_min);
            if (!PetPresence.IsAsleep) _selfNap = false; // PetPresence 还没就绪
        }
    }
}
