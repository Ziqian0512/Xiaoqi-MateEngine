using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class XiaoqiRoomAnimator
{
    private const string ControllerPath = "Assets/Xiaoqi/_Private/Resources/Animations/XiaoqiRoom.controller";
    private const string AnimRoot = "Assets/MATE ENGINE - Animations";

    public static void Setup()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            Debug.LogWarning("XiaoqiRoomAnimator: controller missing " + ControllerPath);
            return;
        }
        var machine = controller.layers[0].stateMachine;
        Add(machine, "Sleep", AnimRoot + "/PET_SLEEPING/PET_SLEEPING.anim");
        Add(machine, "LiePhone", AnimRoot + "/PET_SITTING/ME_02/WINDOW_LAY_10.anim");
        Add(machine, "SitWrite", AnimRoot + "/PET_SITTING/ME_02/PET_SIT_01.anim");
        Add(machine, "SitEat", AnimRoot + "/PET_SITTING/ME_02/PET_SIT_02.anim");
        Add(machine, "StandWindow", AnimRoot + "/PET_POSE/PET_POSE_1.anim");
        Add(machine, "StandTidy", AnimRoot + "/PET_POSE/PET_POSE_2.anim");
        AssetDatabase.SaveAssets();
        Debug.Log("XIAOQI_ROOM_ANIMATOR states added");
    }

    private static void Add(AnimatorStateMachine machine, string name, string clipPath)
    {
        foreach (var child in machine.states)
            if (child.state.name == name) return;
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null)
        {
            Debug.LogWarning("XiaoqiRoomAnimator: clip missing " + clipPath);
            return;
        }
        var state = machine.AddState(name);
        state.motion = clip;
    }
}
