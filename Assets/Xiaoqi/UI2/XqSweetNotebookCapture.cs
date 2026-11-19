using System;
using System.Collections;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Xiaoqi.Host;
using Xiaoqi.Mode;

namespace Xiaoqi.UI2
{
    // Opt-in review of the same production UIDocument and action handlers. Never sends chat or media commands.
    internal sealed class XqSweetNotebookCapture : MonoBehaviour
    {
        string _directory;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var args=Environment.GetCommandLineArgs();int idx=Array.IndexOf(args,"-xq-sweet-capture");
            if(!XqBenchPanel.SweetEnabled||idx<0||idx+1>=args.Length)return;
            var go=new GameObject("XqSweetNotebookCapture");DontDestroyOnLoad(go);var capture=go.AddComponent<XqSweetNotebookCapture>();
            capture._directory=args[idx+1];capture.StartCoroutine(capture.Run());
        }
        IEnumerator Run()
        {
            Directory.CreateDirectory(_directory);float deadline=Time.realtimeSinceStartup+80;
            while((PetHost.I==null||!PetHost.I.Ready||Time.realtimeSinceStartup<12||XqMode.Busy)&&Time.realtimeSinceStartup<deadline)yield return null;
            if(PetHost.I==null||!PetHost.I.Ready){Debug.LogError("XQ_SWEET_REVIEW host unavailable");yield break;}
            if(XqMode.Current!=XqView.Bench&&!XqMode.Set(XqView.Bench)){Debug.LogError("XQ_SWEET_REVIEW mode rejected");yield break;}
            yield return new WaitForSecondsRealtime(7);
            deadline=Time.realtimeSinceStartup+22;while(XqBenchData.Ensure().Loading(0)&&Time.realtimeSinceStartup<deadline)yield return null;
            var report=new JObject { ["viewportWidth"]=Screen.width,["viewportHeight"]=Screen.height,["runtimeCapture"]=true };
            var pages=new JArray();report["pages"]=pages;
            string[] names={"now","journal","memory"};
            for(int p=0;p<3;p++)
            {
                XqBenchPanel.ReviewPage(p);yield return new WaitForSecondsRealtime(3);
                report["hitRegions"+p]=XqBenchPanel.SweetReviewHitRegions();
                if(!XqBenchPanel.SweetReviewHitRegions())Debug.LogError("XQ_SWEET_REVIEW hit regions failed page="+p);
                yield return Capture(names[p],false);
                var root=XqClosePanel.PickDoc().rootVisualElement;var stage=root.Q("xq-bench-stage");
                var nodes=new JArray();root.Q("xq-sweet-native").Query<Label>().ForEach(label=>{
                    var b=label.worldBound;nodes.Add(new JObject { ["name"]=label.name,["x"]=b.x,["y"]=b.y,["w"]=b.width,["h"]=b.height,["textLength"]=label.text.Length,["elided"]=label.isElided });
                });
                var bounds=stage.worldBound;pages.Add(new JObject { ["page"]=names[p],["stageW"]=bounds.width,["stageH"]=bounds.height,["nativeLabels"]=nodes });
                XqBenchPanel.SweetReviewDecor(false);yield return Capture(names[p]+"-decor",true);XqBenchPanel.SweetReviewDecor(true);
                bool action=XqBenchPanel.SweetReviewAction(p==0?"letter":p==1?"diary":"one");
                bool opened=XqBenchPanel.SweetReviewModal();bool closed=XqBenchPanel.DismissPhotoOverlay();
                if(!action||!opened||!closed)Debug.LogError("XQ_SWEET_REVIEW modal failed page="+p);
                else Debug.Log("XQ_SWEET_REVIEW modal passed page="+p);
                report["modal"+p]=action&&opened&&closed;
                if(p==1||p==2){report["pagination"+p]=XqBenchPanel.SweetReviewAction("next");yield return new WaitForSecondsRealtime(.5f);}
            }
            bool toJournal=XqBenchPanel.SweetReviewAction("nav-journal");yield return new WaitForSecondsRealtime(1);
            report["navigationToJournal"]=toJournal&&XqBenchPanel.SweetReviewAtPage(1);
            if(!XqBenchPanel.SweetReviewAtPage(1)){Debug.LogError("XQ_SWEET_REVIEW journal navigation failed");yield break;}
            XqBenchPanel.SweetReviewHistory(true);deadline=Time.realtimeSinceStartup+33;
            while(!XqBenchPanel.SweetReviewHistory()&&Time.realtimeSinceStartup<deadline)yield return null;
            report["historyRead"]=XqBenchPanel.SweetReviewHistory();
            yield return Capture("journal-history",false);XqBenchPanel.SweetReviewToday();yield return new WaitForSecondsRealtime(.5f);
            bool toMemory=XqBenchPanel.SweetReviewAction("nav-memory");yield return new WaitForSecondsRealtime(.5f);
            bool atMemory=XqBenchPanel.SweetReviewAtPage(2);
            bool toNow=XqBenchPanel.SweetReviewAction("nav-now");yield return new WaitForSecondsRealtime(2);
            report["navigationRoundtrip"]=toMemory&&atMemory&&toNow&&XqBenchPanel.SweetReviewAtPage(0);
            File.WriteAllText(Path.Combine(_directory,"runtime-report.json"),report.ToString());
            Debug.Log("XQ_SWEET_REVIEW complete "+_directory);Destroy(gameObject);
        }
        IEnumerator Capture(string name,bool decor)
        {
            var doc=XqClosePanel.PickDoc();var settings=doc.panelSettings;var stage=doc.rootVisualElement.Q("xq-bench-stage");
            var old=settings.targetTexture;var rt=new RenderTexture(Screen.width,Screen.height,24,RenderTextureFormat.ARGB32);rt.Create();settings.targetTexture=rt;
            yield return null;yield return new WaitForEndOfFrame();
            var previous=RenderTexture.active;RenderTexture.active=rt;
            var full=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);full.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);full.Apply();
            if(!decor)File.WriteAllBytes(Path.Combine(_directory,name+"-full.png"),full.EncodeToPNG());
            float upp=XqPanelXf.UnitsPerPx(stage.panel);var b=stage.worldBound;
            int x=Mathf.Clamp(Mathf.RoundToInt(b.xMin/upp),0,rt.width-1),y=Mathf.Clamp(Mathf.RoundToInt(rt.height-b.yMax/upp),0,rt.height-1);
            int w=Mathf.Min(Mathf.RoundToInt(b.width/upp),rt.width-x),h=Mathf.Min(Mathf.RoundToInt(b.height/upp),rt.height-y);
            var crop=new Texture2D(w,h,TextureFormat.RGBA32,false);crop.ReadPixels(new Rect(x,y,w,h),0,0);crop.Apply();
            File.WriteAllBytes(Path.Combine(_directory,name+".png"),crop.EncodeToPNG());
            RenderTexture.active=previous;settings.targetTexture=old;Destroy(full);Destroy(crop);rt.Release();Destroy(rt);
        }
    }
}
