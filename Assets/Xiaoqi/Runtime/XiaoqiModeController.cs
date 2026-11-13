using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class XiaoqiModeController : MonoBehaviour
{
    private static XiaoqiModeController _instance;
    private static float _alpha;
    private static Texture2D _black;

    private void Awake()
    {
        _instance = this;
        if (_black == null)
        {
            _black = new Texture2D(1, 1);
            _black.SetPixel(0, 0, Color.black);
            _black.Apply();
        }
    }

    public static void EnterRoom()
    {
        if (_instance != null) _instance.StartCoroutine(_instance.Swap("XiaoqiRoom"));
    }

    public static void ExitRoom()
    {
        if (_instance != null) _instance.StartCoroutine(_instance.Swap("Mate Engine Main"));
    }

    private IEnumerator Swap(string scene)
    {
        yield return Fade(0f, 1f, 0.25f);
        SceneManager.LoadScene(scene);
        yield return null;
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
