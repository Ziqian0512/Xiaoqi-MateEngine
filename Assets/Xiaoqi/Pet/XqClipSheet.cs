using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Xiaoqi.Spike
{
    // XQ: P1-B2 附带工具。只在命令行带 -xq-clipsheet 时启动，其他情况一行都不跑。
    // 作用：等模型加载好后，把动画控制器里每条剪辑采样 3 帧（开头/中间/接近结尾）渲染成 PNG，
    //       输出到 %LOCALAPPDATA%/Xiaoqi/clipsheet/<剪辑名>.png，并写 clips.csv。完事自动退出。
    // 为什么：WB 没有 Unity，看不到动画画面；要靠这些图把待机池分成 calm / normal。
    // [未编]：SampleAnimation 对 Humanoid 剪辑的行为、Camera.Render 在透明窗口下的表现都需要编译员确认。
    public class XqClipSheet : MonoBehaviour
    {
        private const int W = 320, H = 480;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-xq-clipsheet") < 0) return;
            var go = new GameObject("XqClipSheet");
            DontDestroyOnLoad(go);
            go.AddComponent<XqClipSheet>();
        }

        private static Animator FindAvatar()
        {
            foreach (var a in FindObjectsByType<Animator>(FindObjectsSortMode.None))
            {
                if (a == null || !a.gameObject.activeInHierarchy) continue;
                if (a.avatar == null || !a.isHuman) continue;
                if (a.runtimeAnimatorController == null) continue;
                if (a.GetComponentInChildren<SkinnedMeshRenderer>() == null) continue;
                return a;
            }
            return null;
        }

        private IEnumerator Start()
        {
            var outDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                      "Xiaoqi", "clipsheet");
            Directory.CreateDirectory(outDir);

            Animator an = null;
            float t0 = Time.unscaledTime;
            while (Time.unscaledTime - t0 < 120f)
            {
                an = FindAvatar();
                if (an != null) break;
                yield return new WaitForSeconds(1f);
            }
            if (an == null)
            {
                Debug.Log("XQ_CLIPSHEET fail no-avatar");
                Application.Quit();
                yield break;
            }
            yield return new WaitForSeconds(3f); // 让 ME 自己的初始化跑完

            var cam = Camera.main;
            if (cam == null) cam = FindFirstObjectByType<Camera>();
            if (cam == null)
            {
                Debug.Log("XQ_CLIPSHEET fail no-camera");
                Application.Quit();
                yield break;
            }

            var clips = new List<AnimationClip>();
            var seen = new HashSet<string>();
            foreach (var c in an.runtimeAnimatorController.animationClips)
                if (c != null && seen.Add(c.name)) clips.Add(c);
            Debug.Log("XQ_CLIPSHEET start avatar=" + an.name + " clips=" + clips.Count + " dir=" + outDir);

            an.enabled = false; // 之后完全由我们采样；程序退出，不用恢复
            var csv = new StringBuilder("name,length_s,loop,frames\n");
            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            var strip = new Texture2D(W * 3, H, TextureFormat.RGBA32, false);
            var oldTarget = cam.targetTexture;

            foreach (var clip in clips)
            {
                float len = Mathf.Max(0.05f, clip.length);
                float[] ts = { 0f, len * 0.5f, len * 0.95f };
                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        clip.SampleAnimation(an.gameObject, ts[i]);
                        cam.targetTexture = rt;
                        cam.Render();
                        var prev = RenderTexture.active;
                        RenderTexture.active = rt;
                        tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
                        tex.Apply();
                        RenderTexture.active = prev;
                        var px = tex.GetPixels32();
                        for (int k = 0; k < px.Length; k++) // 透明处垫浅灰，PNG 才看得清
                        {
                            float a = px[k].a / 255f;
                            px[k].r = (byte)(px[k].r * a + 217 * (1f - a));
                            px[k].g = (byte)(px[k].g * a + 217 * (1f - a));
                            px[k].b = (byte)(px[k].b * a + 217 * (1f - a));
                            px[k].a = 255;
                        }
                        tex.SetPixels32(px);
                        strip.SetPixels(W * i, 0, W, H, tex.GetPixels());
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning("XQ_CLIPSHEET frame " + clip.name + " #" + i + " " + e.Message);
                    }
                    yield return null;
                }
                cam.targetTexture = oldTarget;
                strip.Apply();
                var safe = clip.name.Replace('/', '_').Replace('\\', '_').Replace(' ', '_');
                File.WriteAllBytes(Path.Combine(outDir, safe + ".png"), strip.EncodeToPNG());
                csv.Append(clip.name).Append(',').Append(len.ToString("0.00")).Append(',')
                   .Append(clip.isLooping ? 1 : 0).Append(",3\n");
                Debug.Log("XQ_CLIPSHEET clip " + clip.name + " len=" + len.ToString("0.00"));
            }

            File.WriteAllText(Path.Combine(outDir, "clips.csv"), csv.ToString(), new UTF8Encoding(false));
            Debug.Log("XQ_CLIPSHEET done clips=" + clips.Count);
            Application.Quit();
        }
    }
}
