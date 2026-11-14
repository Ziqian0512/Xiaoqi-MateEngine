using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Reflection;
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
            Debug.Log("XQ_SHOT_Awake args=" + args.Length + " index=" + i);
            if (i < 0 || i + 1 >= args.Length) { enabled = false; return; }
            _dir = args[i + 1];
            Directory.CreateDirectory(_dir);
            if (XiaoqiRuntime.Config == null) XiaoqiRuntime.Config = XiaoqiConfig.LoadOrCreate();
            XiaoqiRuntime.Config.emotion_line = true;
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            yield return new WaitForSecondsRealtime(4f);
            if (XiaoqiRuntime.Config != null) XiaoqiRuntime.Config.emotion_line = true;
            _head = FindFirstObjectByType<HeadAnchor>(FindObjectsInactive.Include);
            var readout = new StringBuilder();
            string[] emos = { "开心", "得意", "害羞", "委屈", "生气", "担心", "想念", "失望" };
            for (int i = 0; i < emos.Length; i++)
            {
                XiaoqiFace.DebugBase(emos[i], 0.6f);
                yield return new WaitForSecondsRealtime(0.7f);
                yield return Capture(string.Format("{0:D2}_{1}_底色.png", i * 2 + 1, emos[i]), readout, emos[i], "base");
                XiaoqiFace.DebugSpike(emos[i]);
                yield return new WaitForSecondsRealtime(0.8f);
                yield return Capture(string.Format("{0:D2}_{1}_冒一下.png", i * 2 + 2, emos[i]), readout, emos[i], "spike");
            }
            File.WriteAllText(Path.Combine(_dir, "readout.txt"), readout.ToString());
            Debug.Log("XQ_SHOTS done " + _dir);
            yield return new WaitForSecondsRealtime(0.5f);
            Application.Quit();
        }

        private IEnumerator Capture(string name, StringBuilder readout, string emo, string phase)
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
            var loader = FindFirstObjectByType<VRMLoader>(FindObjectsInactive.Include);
            var model = loader != null ? loader.GetCurrentModel() : null;
            var uni = model != null ? model.GetComponent<UniversalBlendshapes>() : null;
            var line = new StringBuilder(name + " " + emo + " " + phase);
            if (uni != null)
            {
                var t = uni.GetType();
                foreach (var k in new[] { "Joy", "Angry", "Sorrow", "Fun", "LookDown", "Blink", "O" })
                {
                    var f = t.GetField(k);
                    line.Append(" " + k + "=" + (f != null ? ((float)f.GetValue(uni)).ToString("0.00") : "?"));
                }
            }
            readout.AppendLine(line.ToString());
        }
    }
}


