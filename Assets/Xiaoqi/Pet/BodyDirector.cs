using UnityEngine;
using Xiaoqi.Host;

namespace Xiaoqi.Spike
{
    /// <summary>
    /// XQ: P1-4c 身体导演。负责「什么时候换、听谁的」。
    /// 优先级：local（悬停/摸/拖/窗口）> server（stage 意图，B7 接）> env（环境，P1 暂空）> random（兜底，8–40 秒）。
    /// 每个决定写一行 brain.log，带 src=。执行动作交给 IdleDirector。
    /// 这一版只有 local（拖拽结束、悬停）和 random；server 层留了 SetServerTier()，B7 接。
    /// [未编]
    /// </summary>
    public class BodyDirector : MonoBehaviour
    {
        public static BodyDirector I { get; private set; }

        public const string SrcLocal = "local", SrcServer = "server", SrcEnv = "env", SrcRandom = "random";

        private float _nextRandom;
        private string _lastSkip = "";
        // 验收用：命令行带 -xq-brain-fast 时随机间隔缩成 3–6 秒，20 秒冒烟里就能看到几次切换
        private static readonly bool Fast =
            System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-xq-brain-fast") >= 0;
        private static float RandGap() { return Fast ? Random.Range(3f, 6f) : Random.Range(8f, 40f); }
        private bool _dragWas;
        // 一个待办请求：优先级高的覆盖低的，超过 2 秒还换不了就丢弃（比如正在睡）
        private int _pendingPrio = -1;
        private string _pendingSrc, _pendingWhy;
        private float _pendingUntil;

        private static int Prio(string src)
        {
            switch (src) { case SrcLocal: return 3; case SrcServer: return 2; case SrcEnv: return 1; default: return 0; }
        }

        private void Awake()
        {
            I = this;
            _nextRandom = Time.unscaledTime + RandGap();
        }

        /// <summary>请求换一次待机。本地即时事件用 SrcLocal。</summary>
        public static void Request(string src, string why)
        {
            if (I == null) return;
            int p = Prio(src);
            if (p < I._pendingPrio) return;
            I._pendingPrio = p;
            I._pendingSrc = src;
            I._pendingWhy = why;
            I._pendingUntil = Time.unscaledTime + 2f;
        }

        /// <summary>B7 用：服务器意图决定档位（calm / normal）。只改档位，不直接换动作。</summary>
        public static void SetServerTier(string tier, string why)
        {
            var id = IdleDirector.I;
            if (id == null || id.Tier == tier) return;
            id.Tier = tier;
            BrainLog.Write(SrcServer, "tier", "tier=" + tier + " why=" + why);
        }

        private void Update()
        {
            var id = IdleDirector.I;
            if (id == null || !id.Bound) return;
            float now = Time.unscaledTime;

            // 拖拽刚结束：记一行 local，并让随机计时重新走，免得刚放下就换动作
            bool dragging = PetHost.I != null && PetHost.I.IsDragging;
            if (_dragWas && !dragging)
            {
                BrainLog.Write(SrcLocal, "drag_end", "next_random_in=8");
                _nextRandom = Mathf.Max(_nextRandom, now + 8f);
            }
            _dragWas = dragging;

            if (_pendingPrio >= 0)
            {
                if (now > _pendingUntil) { _pendingPrio = -1; }
                else if (id.CanSwitch(out _)) { RunSwitch(id, _pendingSrc, _pendingWhy); _pendingPrio = -1; return; }
            }

            if (now >= _nextRandom)
            {
                _nextRandom = now + RandGap();
                if (id.CanSwitch(out var reason)) RunSwitch(id, SrcRandom, "timer");
                else if (reason != _lastSkip) { _lastSkip = reason; BrainLog.Write(SrcRandom, "skip", "reason=" + reason); }
            }
        }

        private void RunSwitch(IdleDirector id, string src, string why)
        {
            int slot = id.Pick(out var cand);
            if (slot < 0) { BrainLog.Write(src, "skip", "reason=empty-pool"); return; }
            id.Play(slot, src, why, cand);
            if (src != SrcRandom) _nextRandom = Mathf.Max(_nextRandom, Time.unscaledTime + 8f);
        }
    }
}
