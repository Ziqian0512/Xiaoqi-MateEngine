using System.IO;
using SFB;
using UnityEngine;
using Xiaoqi.Host;

public class XiaoqiSettings : MonoBehaviour
{
    private static XiaoqiSettings _instance;
    private static bool _open;
    private Vector2 _scroll;

    private void Awake()
    {
        _instance = this;
    }

    public static void Open()
    {
        if (_instance == null)
            _instance = FindFirstObjectByType<XiaoqiSettings>();
        _open = _instance != null;
    }

    private void OnGUI()
    {
        if (!_open) return;
        var host = PetHost.I;
        if (host == null) return;

        GUILayout.BeginArea(new Rect(40, 40, 560, Mathf.Min(760, Screen.height - 80)), GUI.skin.box);
        GUILayout.BeginHorizontal();
        GUILayout.Label("小祈设置", GUILayout.Height(28));
        if (GUILayout.Button("关闭", GUILayout.Width(64))) _open = false;
        GUILayout.EndHorizontal();
        _scroll = GUILayout.BeginScrollView(_scroll);

        GUILayout.Label("外观");
        var size = Slider("大小", host.Size, 0.5f, 2f);
        if (!Mathf.Approximately(size, host.Size)) host.Size = size;
        SetToggle("置顶", host.Topmost, v => host.Topmost = v);
        SetToggle("开机自启", host.StartWithWindows, v => host.StartWithWindows = v);
        var fps = Mathf.RoundToInt(Slider("帧率", host.Fps, 30, 144));
        if (fps != host.Fps) host.Fps = fps;
        var quality = Mathf.RoundToInt(Slider("画质", host.Quality, 0, 3));
        if (quality != host.Quality) host.Quality = quality;
        SetToggle("坐窗口", host.WindowSitEnabled, v => host.WindowSitEnabled = v);
        SetToggle("桌面上自己走", host.LocomotionEnabled, v => host.LocomotionEnabled = v);

        GUILayout.Space(8);
        GUILayout.Label("外出 / 气泡");
        GUILayout.BeginHorizontal();
        GUILayout.Label("外出时桌宠", GUILayout.Width(150));
        if (GUILayout.Button(XiaoqiRuntime.Config.pet_when_out == "hide" ? "隐藏" : "照常显示"))
            XiaoqiRuntime.Config.pet_when_out = XiaoqiRuntime.Config.pet_when_out == "hide" ? "show" : "hide";
        GUILayout.EndHorizontal();
        SetToggle("气泡", XiaoqiRuntime.Config.bubble, v => XiaoqiRuntime.Config.bubble = v);
        SetToggle("免打扰", XiaoqiRuntime.Dnd, v => { XiaoqiRuntime.Dnd = v; XiaoqiRuntime.Config.dnd = v; });
        SetToggle("情感线（测试）", XiaoqiRuntime.Config.emotion_line, v =>
        {
            XiaoqiRuntime.Config.emotion_line = v;
            XiaoqiConfig.Save(XiaoqiRuntime.Config);
        });

        GUILayout.Space(8);
        GUILayout.Label("连接");
        GUILayout.BeginHorizontal();
        GUILayout.Label("服务器", GUILayout.Width(80));
        XiaoqiRuntime.Config.server = GUILayout.TextField(XiaoqiRuntime.Config.server ?? "");
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("令牌", GUILayout.Width(80));
        XiaoqiRuntime.Config.token = GUILayout.PasswordField(XiaoqiRuntime.Config.token ?? "", '*');
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("模型", GUILayout.Width(80));
        XiaoqiRuntime.Config.model_path = GUILayout.TextField(XiaoqiRuntime.Config.model_path ?? "");
        if (GUILayout.Button("选择", GUILayout.Width(64)))
        {
            var paths = StandaloneFileBrowser.OpenFilePanel("选择 VRM", "", new[] { new ExtensionFilter("VRM", "vrm") }, false);
            if (paths.Length > 0) XiaoqiRuntime.Config.model_path = paths[0];
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(8);
        if (GUILayout.Button("保存并应用"))
        {
            host.SaveSettings();
            if (!string.IsNullOrEmpty(XiaoqiRuntime.Config.model_path))
                host.LoadModel(XiaoqiRuntime.Config.model_path);
            XiaoqiConfig.Save(XiaoqiRuntime.Config);
        }

        GUILayout.Space(12);
        GUILayout.Label("小祈 · 桌面");
        GUILayout.Label("右键她可以打开圆盘菜单。");

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private static float Slider(string label, float value, float min, float max)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(150));
        var v = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(220));
        GUILayout.Label(v.ToString("0.00"), GUILayout.Width(60));
        GUILayout.EndHorizontal();
        return v;
    }

    private static void SetToggle(string label, bool value, System.Action<bool> set)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(150));
        bool v = GUILayout.Toggle(value, value ? "开" : "关");
        GUILayout.EndHorizontal();
        if (v != value) set(v);
    }
}
