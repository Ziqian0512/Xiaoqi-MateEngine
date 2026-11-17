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
        static float _outSince = -1f;

        public static Rect Stable()
        {
            if (_frame == Time.frameCount) return _shown;
            _frame = Time.frameCount;

            var raw = PetHost.I != null ? PetHost.I.ScreenRect() : new Rect(0, 0, 0, 0);
            if (raw.height < 8f || raw.width < 2f) { _has = false; _shown = raw; return _shown; }
            if (!_has) { _has = true; _target = _shown = raw; return _shown; }

            // XQ(WB): 死区上限按屏高封顶。近景镜头放大后外框高 4000+px，8% 就是 340px，会迟钝到几乎不跟。
            float band = Mathf.Clamp(_target.height * Band, 6f, Mathf.Max(24f, Screen.height * 0.04f));
            bool out_ = Mathf.Abs(raw.xMin - _target.xMin) > band
                     || Mathf.Abs(raw.xMax - _target.xMax) > band
                     || Mathf.Abs(raw.yMin - _target.yMin) > band
                     || Mathf.Abs(raw.yMax - _target.yMax) > band;
            // XQ(WB): 抖动衰减——超出死区必须持续 0.25 秒才换目标，抬一下手、歪一下头这种一闪而过的不算。
            if (!out_) _outSince = -1f;
            else
            {
                if (_outSince < 0f) _outSince = Time.unscaledTime;
                if (Time.unscaledTime - _outSince >= 0.25f) { _target = raw; _outSince = -1f; }
            }

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
