using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;

public class XiaoqiModeController : MonoBehaviour
{
    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] private static extern bool GetWindowRect(System.IntPtr hWnd, out RECT r);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(System.IntPtr hWnd, System.IntPtr after,
        int x, int y, int cx, int cy, uint flags);
    private RECT _petRect;
    private bool _hasPetRect;

    private static XiaoqiModeController _instance;
    private static float _alpha;
    private static Texture2D _black;
    private readonly List<GameObject> _hiddenRoots = new List<GameObject>();
    private Scene _petScene;

    private void Awake()
    {
        _instance = this;
        if (_black == null)
        {
            _black = new Texture2D(1, 1);
            _black.SetPixel(0, 0, Color.black);
            _black.Apply();
        }
        _petScene = SceneManager.GetActiveScene();
        var hwnd = Process.GetCurrentProcess().MainWindowHandle;
        _hasPetRect = GetWindowRect(hwnd, out _petRect);
    }

    private static void Resize(int w, int h, RECT r)
    {
        var hwnd = Process.GetCurrentProcess().MainWindowHandle;
        SetWindowPos(hwnd, System.IntPtr.Zero, r.Left, r.Top, w, h, 0x0004 | 0x0010);
    }

    public static void EnterRoom()
    {
        if (_instance != null) _instance.StartCoroutine(_instance.EnterRoutine());
    }

    public static void ExitRoom()
    {
        if (_instance != null) _instance.StartCoroutine(_instance.ExitRoutine());
    }

    private static bool KeepRoot(GameObject go)
    {
        var n = go.name;
        return n.Contains("WindowAPI") || n.Contains("UniWindow") || n.Contains("XiaoqiSystems")
               || n.Contains("SteamAPI");
    }

    private IEnumerator EnterRoutine()
    {
        if (_petScene.IsValid() && _petScene.name != "Mate Engine Main") yield break;
        yield return Fade(0f, 1f, 0.25f);

        _petScene = SceneManager.GetActiveScene();
        _hiddenRoots.Clear();
        foreach (var go in _petScene.GetRootGameObjects())
        {
            if (!go.activeSelf || KeepRoot(go) || go.name == "Main Camera") continue;
            go.SetActive(false);
            _hiddenRoots.Add(go);
        }

        var load = SceneManager.LoadSceneAsync("XiaoqiRoom", LoadSceneMode.Additive);
        if (load != null) yield return load;
        var room = SceneManager.GetSceneByName("XiaoqiRoom");
        if (room.IsValid()) SceneManager.SetActiveScene(room);
        if (_hasPetRect) Resize(1400, 900, _petRect);

        yield return Fade(1f, 0f, 0.35f);
    }

    private IEnumerator ExitRoutine()
    {
        yield return Fade(0f, 1f, 0.25f);

        var room = SceneManager.GetSceneByName("XiaoqiRoom");
        if (room.IsValid() && room.isLoaded)
        {
            var unload = SceneManager.UnloadSceneAsync(room);
            if (unload != null) yield return unload;
        }

        if (_hasPetRect)
            Resize(_petRect.Right - _petRect.Left, _petRect.Bottom - _petRect.Top, _petRect);
        if (_petScene.IsValid()) SceneManager.SetActiveScene(_petScene);
        foreach (var go in _hiddenRoots)
            if (go != null) go.SetActive(true);
        _hiddenRoots.Clear();

        yield return Fade(1f, 0f, 0.35f);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            _alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }
        _alpha = to;
    }

    private void OnGUI()
    {
        if (_alpha <= 0.001f || _black == null) return;
        var old = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, _alpha);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _black);
        GUI.color = old;
    }
}
