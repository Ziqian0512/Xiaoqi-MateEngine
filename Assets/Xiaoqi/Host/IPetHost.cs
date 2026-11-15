using System;
using System.Collections.Generic;
using UnityEngine;

namespace Xiaoqi.Host
{
    /// <summary>一句话表情帧，对应 Mate Engine 的 7 个表情通道。</summary>
    public struct FaceFrame
    {
        public float joy, angry, sorrow, fun, lookDown, blink, o;
    }

    /// <summary>宿主提供的一个可执行动作（让她… 子列用）。</summary>
    public class HostAction
    {
        public string Id;
        public string Label;
    }

    /// <summary>
    /// 小祈代码与底层（现在是 Mate Engine）之间的唯一接口。
    /// 换底子时只重写 MeHost，菜单/表情/气泡/今天卡/设置都不用改。
    /// </summary>
    public interface IPetHost
    {
        bool Ready { get; }
        Transform Avatar { get; }
        Animator Animator { get; }
        event Action AvatarChanged;

        bool IsDragging { get; }
        Rect ScreenRect();
        void SetFace(FaceFrame f);
        void SetMouth(float open);
        void LoadModel(string path);
        void SetSleeping(bool on);

        bool IsWindowSitting { get; }
        bool IsChibi { get; }
        void ExitWindowSit();
        void MoveToCenter();

        IReadOnlyList<string> Dances { get; }
        IReadOnlyList<HostAction> Actions { get; }
        bool RunAction(string id);
        bool IsDancing { get; }
        bool PlayDance(string name);
        void StopDance();

        float Size { get; set; }
        bool Topmost { get; set; }
        int Quality { get; set; }
        int Fps { get; set; }
        bool WindowSitEnabled { get; set; }
        bool LocomotionEnabled { get; set; }
        bool StartWithWindows { get; set; }
        void SaveSettings();

        void SuppressHostMenu();
        void RegisterMenuBlocker(Func<bool> isOpen);
    }

    public static class PetHost
    {
        public static IPetHost I;
    }
}
