using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class XiaoqiBoot
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string className, string windowName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool SetWindowText(IntPtr hWnd, string text);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (SceneManager.GetActiveScene().name != "Mate Engine Main") return;
        RenameWindow();
        XiaoqiHide.Apply();
        var prefab = Resources.Load<GameObject>("XiaoqiSystems");
        if (prefab == null)
        {
            Debug.LogError("XiaoqiBoot: Resources/XiaoqiSystems.prefab missing");
            return;
        }
        var go = UnityEngine.Object.Instantiate(prefab);
        go.name = "XiaoqiSystems";
        UnityEngine.Object.DontDestroyOnLoad(go);
    }

    private static void RenameWindow()
    {
        try
        {
            var hwnd = FindWindow("UnityWndClass", null);
            if (hwnd == IntPtr.Zero) hwnd = FindWindow(null, Application.productName);
            if (hwnd == IntPtr.Zero) return;
            SetWindowText(hwnd, "小祈");
        }
        catch (Exception e)
        {
            Debug.LogWarning("XiaoqiBoot title: " + e.Message);
        }
    }
}
