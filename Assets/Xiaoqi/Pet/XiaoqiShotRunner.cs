using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Xiaoqi.Spike
{
    public class XiaoqiShotRunner : MonoBehaviour
    {
        private string _dir;
        private HeadAnchor _head;

        private void Awake()
        {
            var args = Environment.GetCommandLineArgs();
            var i = Array.IndexOf(args, "-shots-pet");
            if (i < 0 || i + 1 >= args.Length) { enabled = false; return; }
            _dir = args[i + 1];
            Directory.CreateDirectory(_dir);
            XiaoqiRuntime.Config.emotion_line = true;
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            yield return new WaitForSecondsRealtime(4f);
            _head = FindFirstObjectByType<HeadAnchor>(FindObjectsInactive.Include);
            string[] emos = { "开心", "得意", "害羞", "委屈", "生气", "担心", "想念", "失望" };
            for (int i = 0; i < emos.Length; i++)
            {
                XiaoqiFace.DebugSpike(emos[i]);
                yield return new WaitForSecondsRealtime(0.8f);
                yield return Capture(string.Format("{0:D2}_{1}.png", i + 1, emos[i]));
            }
            Debug.Log("XQ_SHOTS done " + _dir);
            yield return new WaitForSecondsRealtime(0.5f);
            Application.Quit();
        }

        private IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            var p = _head != null ? _head.ScreenPoint : new Vector2(Screen.width / 2f, Screen.height / 2f);
            int size = 512;
            int x = Mathf.Clamp((int)p.x - size / 2, 0, Mathf.Max(0, Screen.width - size));
            int y = Mathf.Clamp((int)p.y - size / 2, 0, Mathf.Max(0, Screen.height - size));
            var tex = new Texture2D(size, size, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(x, y, size, size), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(_dir, name), tex.EncodeToPNG());
            Destroy(tex);
        }
    }
}
