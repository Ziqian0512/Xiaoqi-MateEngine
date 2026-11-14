using System.IO;
using SFB;
using UnityEngine;

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
        var h = SaveLoadHandler.Instance;
        if (h == null) return;
        var d = h.data;

        GUILayout.BeginArea(new Rect(40, 40, 560, Mathf.Min(760, Screen.height - 80)), GUI.skin.box);
        GUILayout.BeginHorizontal();
        GUILayout.Label("小祈设置", GUILayout.Height(28));
        if (GUILayout.Button("关闭", GUILayout.Width(64))) _open = false;
        GUILayout.EndHorizontal();
        _scroll = GUILayout.BeginScrollView(_scroll);

        GUILayout.Label("外观");
        d.avatarSize = Slider("大小", d.avatarSize, 0.5f, 2f);
        d.isTopmost = Toggle("置顶", d.isTopmost);
        d.startWithWindows = Toggle("开机自启", d.startWithWindows);
        d.fpsLimit = Mathf.RoundToInt(Slider("帧率", d.fpsLimit, 30, 144));
        d.graphicsQualityLevel = Mathf.RoundToInt(Slider("画质", d.graphicsQualityLevel, 0, 3));
        d.enableWindowSitting = Toggle("坐窗口", d.enableWindowSitting);
        d.enableLocomotion = Toggle("桌面上自己走", d.enableLocomotion);

        GUILayout.Space(8);
        GUILayout.Label("外出 / 气泡");
        GUILayout.BeginHorizontal();
        GUILayout.Label("外出时桌宠", GUILayout.Width(150));
        if (GUILayout.Button(XiaoqiRuntime.Config.pet_when_out == "hide" ? "隐藏" : "照常显示"))
            XiaoqiRuntime.Config.pet_when_out = XiaoqiRuntime.Config.pet_when_out == "hide" ? "show" : "hide";
        GUILayout.EndHorizontal();
        XiaoqiRuntime.Config.bubble = Toggle("气泡", XiaoqiRuntime.Config.bubble);
        XiaoqiRuntime.Dnd = Toggle("免打扰", XiaoqiRuntime.Dnd);
        XiaoqiRuntime.Config.dnd = XiaoqiRuntime.Dnd;
        var emotion = Toggle("情感线（测试）", XiaoqiRuntime.Config.emotion_line);
        if (emotion != XiaoqiRuntime.Config.emotion_line)
        {
            XiaoqiRuntime.Config.emotion_line = emotion;
            XiaoqiConfig.Save(XiaoqiRuntime.Config);
        }

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
            h.SaveToDisk();
            XiaoqiRunKey.Sync(d.startWithWindows);
            SaveLoadHandler.ApplyAllSettingsToAllAvatars();
            var loader = FindFirstObjectByType<VRMLoader>();
            if (loader != null && !string.IsNullOrEmpty(XiaoqiRuntime.Config.model_path))
                loader.LoadVRM(XiaoqiRuntime.Config.model_path);
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

    private static bool Toggle(string label, bool value)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(150));
        var v = GUILayout.Toggle(value, value ? "开" : "关");
        GUILayout.EndHorizontal();
        return v;
    }
}
