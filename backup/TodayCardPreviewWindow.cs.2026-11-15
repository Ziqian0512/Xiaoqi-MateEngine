// XQ: G57 V1 —— UI Toolkit 预览窗（只在编辑器里跑，不进游戏）。
// 主人玩游戏时不去点菜单：外部放 .xq-trigger 文件，XqAutoPilot 调这里的 Open()。
// 所有步骤写 .xq-log.txt，免得异常被吞掉看不见。
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Xiaoqi.UI2
{
    public class TodayCardPreviewWindow : EditorWindow
    {
        const string LogPath = @"E:\dsh-pet-projects\dspet-main\.xq-log.txt";

        static void Log(string msg)
        {
            try { File.AppendAllText(LogPath, DateTime.Now.ToString("HH:mm:ss") + "  " + msg + "\n"); }
            catch { /* ignore */ }
            Debug.Log("[XQ] " + msg);
        }

        [MenuItem("Xiaoqi/UI2/预览今日卡")]
        public static void Open()
        {
            Log("Open() 进入");
            try
            {
                foreach (var old in Resources.FindObjectsOfTypeAll<TodayCardPreviewWindow>())
                {
                    if (old != null) { old.Close(); Log("关掉旧预览窗"); }
                }
                AssetDatabase.ImportAsset("Assets/Xiaoqi/Resources/UI2/xq.uss", ImportAssetOptions.ForceUpdate);
                Log("USS 已强制重导");

                var win = CreateInstance<TodayCardPreviewWindow>();
                win.titleContent = new GUIContent("小祈 · UI Toolkit");
                win.minSize = new Vector2(420, 560);
                win.position = new Rect(0, 0, 470, 640);
                win.ShowUtility();
                Log("ShowUtility() 完成，position=" + win.position);
                win.Rebuild();
                Log("Rebuild() 返回");
            }
            catch (Exception e)
            {
                Log("异常: " + e);
            }
        }

        VisualElement _root;

        void OnEnable() => Rebuild();

        public void Rebuild()
        {
            Log("Rebuild() 进入");
            rootVisualElement.Clear();
            rootVisualElement.style.backgroundColor = new Color(0.10f, 0.11f, 0.13f);

            var bg = new VisualElement();
            bg.style.position = Position.Absolute;
            bg.style.left = 0; bg.style.top = 0; bg.style.right = 0; bg.style.bottom = 0;
            bg.style.backgroundColor = new Color(0.16f, 0.18f, 0.22f);
            rootVisualElement.Add(bg);

            var uxml = Resources.Load<VisualTreeAsset>("UI2/TodayCard");
            if (uxml == null)
            {
                Log("找不到 Assets/Xiaoqi/Resources/UI2/TodayCard.uxml");
                rootVisualElement.Add(new Label("找不到 TodayCard.uxml"));
                return;
            }
            Log("UXML 已加载");

            var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Xiaoqi/Resources/UI2/xq.uss")
                        ?? Resources.Load<StyleSheet>("UI2/xq");
            Log("StyleSheet=" + (sheet != null ? sheet.name : "null"));

            _root = uxml.Instantiate();
            if (sheet != null && !_root.styleSheets.Contains(sheet)) _root.styleSheets.Add(sheet);
            Log("sheetsOnRoot=" + _root.styleSheets.count);

            _root.style.position = Position.Absolute;
            _root.style.left = 24;
            _root.style.top = 24;
            rootVisualElement.Add(_root);

            var bubbleUxml = Resources.Load<VisualTreeAsset>("UI2/ChatBubble");
            if (bubbleUxml != null)
            {
                var b = bubbleUxml.Instantiate();
                if (sheet != null && !b.styleSheets.Contains(sheet)) b.styleSheets.Add(sheet);
                b.style.position = Position.Absolute;
                b.style.left = 24;
                b.style.top = 330;
                rootVisualElement.Add(b);
            }

            _root.Q<Label>("today-clock").text = "22:20";
            _root.Q<Label>("today-line").text = "今天你玩了三个小时游戏，我只在旁边看着。";
            _root.Q<Label>("today-doing").text = "坐在窗口上晃脚";
            _root.Q<Label>("today-mood").text = "有点想被理";

            var close = _root.Q<Button>("today-close");
            if (close != null)
            {
                close.clicked += () =>
                {
                    _root.style.opacity = 0f;
                    _root.schedule.Execute(() => _root.style.display = DisplayStyle.None).StartingIn(200);
                    Log("点到了「收起」");
                };
            }
            Log("Rebuild() 完成");
        }
    }
}
