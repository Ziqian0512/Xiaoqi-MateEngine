using System.Linq;
using UnityEngine;
using Xamin;

public class XiaoqiRadialMenu : MonoBehaviour
{
    private bool _done;

    private void Update()
    {
        if (_done) return;
        var selectors = FindObjectsByType<CircleSelector>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (selectors == null || selectors.Length == 0) return;
        _done = true;

        // 第二组（Smoothie/Cake）隐藏，主组保留。
        for (int i = 1; i < selectors.Length; i++)
            selectors[i].gameObject.SetActive(false);

        var main = selectors[0];
        if (main.Buttons == null) return;
        foreach (var go in main.Buttons)
        {
            if (go == null) continue;
            var b = go.GetComponent<Xamin.Button>();
            if (b == null) continue;
            b.action.RemoveAllListeners();
            string n = go.name;
            switch (n)
            {
                case "Settings":
                    b.action.AddListener(XiaoqiSettings.Open);
                    break;
                case "Chat":
                    b.action.AddListener(Xiaoqi.Spike.TodayCard.DebugOpen);
                    break;
                case "Blendshapes":
                    b.action.AddListener(Xiaoqi.Spike.EmotionLineTuner.Toggle);
                    break;
                case "Chibi":
                    b.action.AddListener(ToggleTopmost);
                    break;
                default:
                    go.SetActive(false);
                    break;
            }
        }
        Debug.Log("XQ_RADIAL configured buttons=" + main.Buttons.Count);
    }

    private static void ToggleTopmost()
    {
        var h = SaveLoadHandler.Instance;
        if (h == null || h.data == null) return;
        h.data.isTopmost = !h.data.isTopmost;
        h.SaveToDisk();
        SaveLoadHandler.ApplyAllSettingsToAllAvatars();
    }
}
