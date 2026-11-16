using UnityEngine;
using Xiaoqi.Host;

namespace Xiaoqi.UI2
{
    /// <summary>
    /// 她在屏幕上的「稳定外框」。PetHost.ScreenRect() 是蒙皮网格包围盒，
    /// 待机动作一抬手、一歪头就变几十像素，直接拿它摆气泡、胶囊、菜单，它们就会跟着人漂，没法点。
    /// 这里做死区加平滑：原始外框的四条边都在死区里就不动；超出（换姿势、变小、被挪）才整体平滑挪过去。
    /// 同一帧多次调用返回同一个值。
    /// </summary>
    public static class XqBodyAnchor
    {
        const float Band = 0.08f;     // 死区 = 外框高度的 8%
        const float FollowRate = 9f;  // 越大越快贴上新位置

        static Rect _target;
        static Rect _shown;
        static bool _has;
        static int _frame = -1;

        public static Rect Stable()
        {
            if (_frame == Time.frameCount) return _shown;
            _frame = Time.frameCount;

            var raw = PetHost.I != null ? PetHost.I.ScreenRect() : new Rect(0, 0, 0, 0);
            if (raw.height < 8f || raw.width < 2f) { _has = false; _shown = raw; return _shown; }
            if (!_has) { _has = true; _target = _shown = raw; return _shown; }

            float band = Mathf.Max(6f, _target.height * Band);
            bool out_ = Mathf.Abs(raw.xMin - _target.xMin) > band
                     || Mathf.Abs(raw.xMax - _target.xMax) > band
                     || Mathf.Abs(raw.yMin - _target.yMin) > band
                     || Mathf.Abs(raw.yMax - _target.yMax) > band;
            if (out_) _target = raw;

            float k = 1f - Mathf.Exp(-FollowRate * Mathf.Max(0.001f, Time.unscaledDeltaTime));
            _shown = Rect.MinMaxRect(
                Mathf.Lerp(_shown.xMin, _target.xMin, k),
                Mathf.Lerp(_shown.yMin, _target.yMin, k),
                Mathf.Lerp(_shown.xMax, _target.xMax, k),
                Mathf.Lerp(_shown.yMax, _target.yMax, k));
            return _shown;
        }
    }
}
