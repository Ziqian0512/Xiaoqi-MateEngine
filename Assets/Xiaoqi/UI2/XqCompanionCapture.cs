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
    // Opt-in production review: reads real data; never sends chat, records the
    // microphone, uploads windows, changes mute settings or controls the music player.
    internal sealed class XqCompanionCapture : MonoBehaviour
    {
        string _dir;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-xq-companion-capture");if(index<0||index+1>=args.Length)return;
            var go=new GameObject("XqCompanionCapture");DontDestroyOnLoad(go);var run=go.AddComponent<XqCompanionCapture>();run._dir=args[index+1];run.StartCoroutine(run.Run());
        }
        IEnumerator Run()
        {
            Directory.CreateDirectory(_dir);float deadline=Time.realtimeSinceStartup+85;
            while((PetHost.I==null||!PetHost.I.Ready||XqMode.Busy||Time.realtimeSinceStartup<12)&&Time.realtimeSinceStartup<deadline)yield return null;
            if(PetHost.I==null||!PetHost.I.Ready){Debug.LogError("XQ_COMPANION_REVIEW host unavailable");yield break;}
            if(XqMode.Current!=XqView.Close)XqMode.Set(XqView.Close);
            yield return new WaitForSecondsRealtime(8);
            var report=XqClosePanel.CompanionReview();report["actualViewport"]=new JArray(Screen.width,Screen.height);
            yield return Capture("close");
            XqClosePanel.CompanionReviewSize(1366,768);yield return new WaitForSecondsRealtime(.6f);
            report["compactNativeLayout"]=XqClosePanel.CompanionReview();yield return Capture("compact",1366,768);
            XqClosePanel.CompanionReviewSize(0,0);yield return new WaitForSecondsRealtime(.6f);
            report["usageModal"]=XqClosePanel.CompanionReviewOpen("usage");yield return new WaitForSecondsRealtime(.5f);yield return Capture("usage");XqClosePanel.CompanionReviewDismiss();
            report["musicModal"]=XqClosePanel.CompanionReviewOpen("music-details");yield return new WaitForSecondsRealtime(.5f);XqClosePanel.CompanionReviewDismiss();
            report["replyModal"]=XqClosePanel.CompanionReviewOpen("reply");yield return new WaitForSecondsRealtime(.5f);XqClosePanel.CompanionReviewDismiss();
            foreach(var mode in new[]{XqView.Bench,XqView.Pet,XqView.Close})
            {
                deadline=Time.realtimeSinceStartup+20;while(XqMode.Busy&&Time.realtimeSinceStartup<deadline)yield return null;
                bool accepted=XqMode.Set(mode);yield return new WaitForSecondsRealtime(5);
                report["switch"+mode]=accepted&&XqMode.Current==mode&&!XqMode.Busy;
                Debug.Log("XQ_COMPANION_REVIEW switch="+mode+" passed="+report["switch"+mode]);
            }
            XqMode.Set(XqView.Pet);yield return new WaitForSecondsRealtime(5);
            report["petControls"]=XqVoiceOverlay.ReviewBounds();yield return Capture("pet-idle");
            XqVoiceOverlay.OpenTyping();yield return new WaitForSecondsRealtime(.5f);yield return Capture("pet-typing");
            foreach(var sub in new[]{"talk","doing","position","quiet","dance"})
            {XqGlassMenu.ReviewMenu(sub);yield return new WaitForSecondsRealtime(.6f);report["menu-"+sub]=XqGlassMenu.ReviewBounds();yield return Capture("menu-"+sub);}
            XqGlassMenu.CloseCurrent("review");XqMode.Set(XqView.Close);yield return new WaitForSecondsRealtime(4);
            File.WriteAllText(Path.Combine(_dir,"report.json"),report.ToString());
            Debug.Log("XQ_COMPANION_REVIEW complete");Destroy(gameObject);
        }
        IEnumerator Capture(string name,int width=0,int height=0)
        {
            var settings=XqClosePanel.PickDoc().panelSettings;var old=settings.targetTexture;
            var rt=new RenderTexture(width>0?width:Screen.width,height>0?height:Screen.height,24,RenderTextureFormat.ARGB32);rt.Create();
            var before=RenderTexture.active;RenderTexture.active=rt;GL.Clear(true,true,Color.clear);RenderTexture.active=before;
            bool oldClear=settings.clearColor;var oldColor=settings.colorClearValue;settings.clearColor=true;settings.colorClearValue=Color.clear;settings.targetTexture=rt;
            yield return null;yield return new WaitForEndOfFrame();
            var previous=RenderTexture.active;RenderTexture.active=rt;
            var texture=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);texture.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);texture.Apply();
            File.WriteAllBytes(Path.Combine(_dir,name+"-ui.png"),texture.EncodeToPNG());RenderTexture.active=previous;settings.targetTexture=old;settings.clearColor=oldClear;settings.colorClearValue=oldColor;Destroy(texture);
            // UI Toolkit can still have queued work referring to its last target.
            yield return null;yield return new WaitForEndOfFrame();yield return null;yield return new WaitForEndOfFrame();
            rt.Release();Destroy(rt);
            if(name=="close")
            {
                yield return new WaitForEndOfFrame();
                var frame=ScreenCapture.CaptureScreenshotAsTexture();
                if(frame!=null){File.WriteAllBytes(Path.Combine(_dir,"close-frame.png"),frame.EncodeToPNG());Destroy(frame);}
            }
        }
    }
    public partial class XqClosePanel
    {
        internal static JObject CompanionReview()
        {
            var i=_inst;var report=new JObject();if(i==null)return report;
            report["transparentHost"]=i._host.resolvedStyle.backgroundColor.a<.01f;
            var hits=new JObject();foreach(var target in i._compHits)
            {
                var e=target.Value;var center=e.worldBound.center;
                // Disabled transport buttons deliberately do not accept input.
                hits[target.Key]=!e.enabledInHierarchy||i.CompanionHit(center)==target.Key;
            }
            report["hitTargets"]=hits;
            var labels=new JArray();foreach(var slot in i._compSlots)
                if(slot.el is Label label)labels.Add(new JObject { ["name"]=label.name,["x"]=label.worldBound.x,["y"]=label.worldBound.y,["w"]=label.worldBound.width,["h"]=label.worldBound.height,["elided"]=label.isElided });
            report["labels"]=labels;
            var p=i._host.panel;var centerPoint=new Vector2(i._host.worldBound.center.x,i._host.worldBound.height*.4f);
            report["centerHasNoAction"]=i.CompanionHit(centerPoint)==null;
            report["cameraRectFull"]=Camera.main==null||Camera.main.rect==new Rect(0,0,1,1);
            report["todayUsageAvailable"]=XqCompanionUsage.TodayTokens.HasValue;
            report["signature"]=new JArray(i._plate.worldBound.x,i._plate.worldBound.y,i._plate.worldBound.width,i._plate.worldBound.height);
            report["wave"]=new JArray(i._spectrumRow.worldBound.x,i._spectrumRow.worldBound.y,i._spectrumRow.worldBound.width,i._spectrumRow.worldBound.height);
            return report;
        }
        internal static bool CompanionReviewOpen(string id)
        {
            if(_inst==null)return false;_inst.CompanionActivate(id);return _inst._compModal?.parent!=null;
        }
        internal static void CompanionReviewDismiss(){_inst?._compModal?.RemoveFromHierarchy();if(_inst!=null)_inst._compModal=null;}
        internal static void CompanionReviewSize(float width,float height){if(_inst==null)return;_inst._compReviewWidth=width;_inst._compReviewHeight=height;_inst.Place();}
    }
}
