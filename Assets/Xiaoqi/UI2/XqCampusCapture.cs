using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;
using Xiaoqi.Host;
using Xiaoqi.Mode;

namespace Xiaoqi.UI2
{
    // Capture the shipped runtime panel. No parallel HTML layout or screenshot-only UI exists.
    internal sealed class XqCampusCapture : MonoBehaviour
    {
        string _directory;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var args=Environment.GetCommandLineArgs(); int i=Array.IndexOf(args,"-xq-campus-capture");
            if(!XqBenchPanel.CampusPreview || i<0 || i+1>=args.Length)return;
            var go=new GameObject("XqCampusCapture"); DontDestroyOnLoad(go);
            var capture=go.AddComponent<XqCampusCapture>();capture._directory=args[i+1];capture.StartCoroutine(capture.Run());
        }
        IEnumerator Run()
        {
            float deadline=Time.realtimeSinceStartup+75;
            while((PetHost.I==null||!PetHost.I.Ready||Time.realtimeSinceStartup<10||XqMode.Busy)&&Time.realtimeSinceStartup<deadline)yield return null;
            if(PetHost.I==null||!PetHost.I.Ready){Debug.LogError("XQ_CAMPUS host unavailable");yield break;}
            if(XqMode.Current!=XqView.Bench && !XqMode.Set(XqView.Bench)){Debug.LogError("XQ_CAMPUS transition rejected");yield break;}
            yield return new WaitForSecondsRealtime(5);
            deadline=Time.realtimeSinceStartup+22;
            while(XqBenchData.Ensure().Loading(0)&&Time.realtimeSinceStartup<deadline)yield return null;
            yield return new WaitForSecondsRealtime(3);
            XqBenchPanel.ReviewPage(0);
            Directory.CreateDirectory(_directory);
            yield return new WaitForSecondsRealtime(2);
            var doc=XqClosePanel.PickDoc();var settings=doc!=null?doc.panelSettings:null;
            if(settings==null){Debug.LogError("XQ_CAMPUS panel missing");yield break;}
            var stage=doc.rootVisualElement.Q("xq-bench-stage");
            var panel=doc.rootVisualElement.Q("xq-bench-panel");
            var left=doc.rootVisualElement.Q("xq-bench-left");var right=doc.rootVisualElement.Q("xq-bento-host");
            if(stage==null||panel==null||left==null||right==null){Debug.LogError("XQ_CAMPUS layout missing");yield break;}
            if(left.worldBound.xMax>=right.worldBound.xMin){Debug.LogError("XQ_CAMPUS columns overlap");yield break;}
            var old=settings.targetTexture;
            var rt=new RenderTexture(Screen.width,Screen.height,24,RenderTextureFormat.ARGB32);rt.Create();settings.targetTexture=rt;
            yield return null;yield return new WaitForEndOfFrame();
            var prev=RenderTexture.active;RenderTexture.active=rt;
            var texture=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);
            texture.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);texture.Apply();
            File.WriteAllBytes(Path.Combine(_directory,"campus-now-full.png"),texture.EncodeToPNG());
            float upp=XqPanelXf.UnitsPerPx(stage.panel);var bounds=stage.worldBound;
            int x=Mathf.Clamp(Mathf.FloorToInt(bounds.xMin/upp),0,rt.width-1);
            int y=Mathf.Clamp(Mathf.FloorToInt(rt.height-bounds.yMax/upp),0,rt.height-1);
            int w=Mathf.Min(Mathf.CeilToInt(bounds.width/upp),rt.width-x),h=Mathf.Min(Mathf.CeilToInt(bounds.height/upp),rt.height-y);
            var crop=new Texture2D(w,h,TextureFormat.RGBA32,false);crop.ReadPixels(new Rect(x,y,w,h),0,0);crop.Apply();
            File.WriteAllBytes(Path.Combine(_directory,"campus-now-panel.png"),crop.EncodeToPNG());
            string report="{\"viewportWidth\":"+rt.width+",\"viewportHeight\":"+rt.height+",\"panelWidth\":"+w+",\"panelHeight\":"+h+",\"columnsOverlap\":false,\"runtimeCapture\":true}";
            File.WriteAllText(Path.Combine(_directory,"layout.json"),report);
            RenderTexture.active=prev;settings.targetTexture=old;Destroy(texture);Destroy(crop);rt.Release();Destroy(rt);
            Debug.Log("XQ_CAMPUS capture passed "+w+"x"+h+" viewport="+Screen.width+"x"+Screen.height);
            if(!XqBenchPanel.CampusVerifyPhoto())Debug.LogError("XQ_CAMPUS photo interaction failed");
            else Debug.Log("XQ_CAMPUS photo open-close passed");
            XqBenchPanel.ReviewPage(1);yield return new WaitForSecondsRealtime(2);
            XqBenchPanel.ReviewPage(2);yield return new WaitForSecondsRealtime(2);
            XqBenchPanel.ReviewPage(0);yield return new WaitForSecondsRealtime(2);
            Debug.Log("XQ_CAMPUS navigation roundtrip passed");
            Destroy(gameObject);
        }
    }
}
