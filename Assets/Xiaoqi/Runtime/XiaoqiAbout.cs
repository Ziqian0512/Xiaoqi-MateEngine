using System.IO;
using UnityEngine;

public class XiaoqiAbout : MonoBehaviour
{
    private static XiaoqiAbout _instance;
    private static bool _open;
    private static string _license = "";

    public static void Show()
    {
        if (_instance == null)
        {
            var go = new GameObject("XiaoqiAbout");
            Object.DontDestroyOnLoad(go);
            _instance = go.AddComponent<XiaoqiAbout>();
        }
        Xiaoqi.Spike.TodayCard.DebugClose();
        _open = true;
    }

    public static void Hide() { _open = false; }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) _open = false;
    }

    private void OnGUI()
    {
        if (!_open) return;
        if (string.IsNullOrEmpty(_license))
        {
            var p = Path.Combine(Application.streamingAssetsPath, "Xiaoqi", "MATE-ENGINE-LICENSE.txt");
            _license = File.Exists(p) ? File.ReadAllText(p) : "Mate Engine license missing.";
        }
        var r = new Rect((Screen.width - 520) / 2f, (Screen.height - 420) / 2f, 520, 420);
        GUI.Box(r, "关于小祈");
        GUILayout.BeginArea(new Rect(r.x + 16, r.y + 28, r.width - 32, r.height - 44));
        GUILayout.Label("小祈");
        GUILayout.Label("基于 Mate Engine 开发");
        GUILayout.Label("https://github.com/shinyflvre/Mate-Engine");
        GUILayout.Space(8);
        GUILayout.Label("Mate Engine 版权与许可证原文：");
        GUILayout.TextArea(_license, GUILayout.ExpandHeight(true));
        if (GUILayout.Button("关闭")) _open = false;
        GUILayout.EndArea();
    }
}
