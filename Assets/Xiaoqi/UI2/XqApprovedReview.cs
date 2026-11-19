using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;
using Xiaoqi.Host;
using Xiaoqi.Mode;

namespace Xiaoqi.UI2
{
    // Explicit opt-in capture hook; does nothing in ordinary runs and never sends chat messages.
    internal sealed class XqApprovedReview : MonoBehaviour
    {
        string _directory;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, "-xq-approved-review");
            if (index < 0 || index + 1 >= args.Length) return;
            var go = new GameObject("XqApprovedReview"); DontDestroyOnLoad(go);
            var runner = go.AddComponent<XqApprovedReview>(); runner._directory = args[index + 1]; runner.StartCoroutine(runner.Run());
        }
        IEnumerator Run()
        {
            float deadline = Time.realtimeSinceStartup + 75f;
            while ((PetHost.I == null || !PetHost.I.Ready || Time.realtimeSinceStartup < 10f) && Time.realtimeSinceStartup < deadline) yield return null;
            Directory.CreateDirectory(_directory);
            if (PetHost.I == null || !PetHost.I.Ready) { Debug.LogError("XQ_APPROVED_REVIEW host unavailable"); yield break; }
            yield return Change(XqView.Close);
            yield return Capture("01-close.png");
            var root = XqClosePanel.PickDoc()?.rootVisualElement;
            var close = root != null ? root.Q("xq-close-host") : null;
            if (close == null || close.resolvedStyle.backgroundColor.a > .01f) Debug.LogError("XQ_APPROVED_REVIEW close background is not transparent");
            else Debug.Log("XQ_APPROVED_REVIEW transparent host passed");
            yield return Change(XqView.Bench);
            yield return Capture("02-workbench.png");
            XqBenchPanel.ReviewPage(2); yield return new WaitForSecondsRealtime(3f);
            yield return Capture("03-memory-growth.png");
            XqBenchPanel.ReviewPage(4); yield return new WaitForSecondsRealtime(2f);
            yield return Capture("04-music.png");
            XqBenchPanel.ReviewPage(5); yield return new WaitForSecondsRealtime(2f);
            yield return Capture("05-settings.png");
            XqBenchPanel.ReviewPage(1); yield return new WaitForSecondsRealtime(2f);
            yield return Capture("06-life.png");
            yield return Change(XqView.Pet);
            yield return Capture("07-pet-ui.png");
            Debug.Log("XQ_APPROVED_REVIEW done"); Destroy(gameObject);
        }
        IEnumerator Change(XqView view)
        {
            float deadline = Time.realtimeSinceStartup + 20f;
            while (XqMode.Busy && Time.realtimeSinceStartup < deadline) yield return null;
            if (!XqMode.Set(view)) { Debug.LogError("XQ_APPROVED_REVIEW transition rejected: " + view); yield break; }
            yield return new WaitForSecondsRealtime(4f);
            while (XqMode.Busy && Time.realtimeSinceStartup < deadline) yield return null;
        }
        IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            // Render the actual runtime UI panel to a texture: layered-window desktop captures may be empty.
            var doc = XqClosePanel.PickDoc();
            var settings = doc != null ? doc.panelSettings : null;
            if (settings == null) { Debug.LogError("XQ_APPROVED_REVIEW panel missing"); yield break; }
            var old = settings.targetTexture;
            var rt = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
            rt.Create(); settings.targetTexture = rt;
            yield return null; yield return new WaitForEndOfFrame();
            var previous = RenderTexture.active; RenderTexture.active = rt;
            var texture = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0); texture.Apply();
            File.WriteAllBytes(Path.Combine(_directory, name), texture.EncodeToPNG());
            RenderTexture.active = previous; settings.targetTexture = old;
            Destroy(texture); rt.Release(); Destroy(rt);
            yield return new WaitForSecondsRealtime(1f);
            Debug.Log("XQ_APPROVED_REVIEW captured " + name);
        }
    }
}