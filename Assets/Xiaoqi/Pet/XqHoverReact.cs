using UnityEngine;
using Xiaoqi.Host;

namespace Xiaoqi.Spike
{
    /// <summary>
    /// XQ: P1-4c 悬停反应。鼠标停在她身上 ≥ 0.5 秒 → 一个小表情（约 1.5 秒，走 XiaoqiFace 的尖峰通道）。
    /// 「看向鼠标」不用另做：ME 的 AvatarMouseTracking 一直在跟着鼠标，力度由设置页「看向的力度」管。
    /// 不播任何声音，不恢复 PetVoiceReactionHandler。冷却 20 秒。
    /// [未编]
    /// </summary>
    public class XqHoverReact : MonoBehaviour
    {
        private const float HoverSec = 0.5f;
        private const float CooldownSec = 20f;
        private const string Emotion = "开心";

        private float _since = -1f;
        private float _cool;

        private void Update()
        {
            var host = PetHost.I;
            if (host == null || !host.Ready) { _since = -1f; return; }
            if (Time.unscaledTime < _cool) return;
            if (host.IsDragging || PetPresence.IsAsleep || !host.AvatarVisible || MenuActions.IsMovementBlocked()
                || XiaoqiRuntime.IsQuiet())
            {
                _since = -1f;
                return;
            }

            var r = host.ScreenRect();
            if (r.height < 2f) { _since = -1f; return; }
            // 缩掉左右各 15%：包围盒比人宽，边上是空气
            var body = new Rect(r.x + r.width * 0.15f, r.y, r.width * 0.7f, r.height);
            bool over = body.Contains(new Vector2(Input.mousePosition.x, Input.mousePosition.y));
            if (!over) { _since = -1f; return; }

            if (_since < 0f) { _since = Time.unscaledTime; return; }
            if (Time.unscaledTime - _since < HoverSec) return;

            _since = -1f;
            _cool = Time.unscaledTime + CooldownSec;
            XiaoqiFace.DebugSpike(Emotion);
            BrainLog.Write(BodyDirector.SrcLocal, "hover", "hold=" + HoverSec.ToString("0.0") + " face=" + Emotion + " voice=none");
        }
    }
}
