using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Xiaoqi.Mode;

namespace Xiaoqi.UI2
{
    // Desktop has one mode gate and one three-message conversation, no typing dock.
    public static class XqVoiceOverlay
    {
        static VisualElement _root,_gate,_modes,_state;
        static Label _stateText;
        static readonly VisualElement[] Cards=new VisualElement[3];
        static readonly Label[] Labels=new Label[3];
        static readonly ScrollView[] Scrolls=new ScrollView[3];
        static readonly XqPetConversation History=new XqPetConversation();
        static readonly XqPetFeedBuffer PendingFeed=new XqPetFeedBuffer();
        static readonly Dictionary<string,VisualElement> Hits=new Dictionary<string,VisualElement>();
        static readonly Dictionary<string,Action> Actions=new Dictionary<string,Action>();
        static bool _subscribed,_modeOpen;
        static string _pressed,_notice="",_lastFeed="";
        static float _noticeAt=-999,_feedAt=-999;
        static long _feedId;
        static Vector2 _down;
        static XqView _lastMode;
        public static bool Consumed {get;private set;}
        static bool Visible(VisualElement e){if(e==null)return false;for(var n=e;n!=null;n=n.parent)if(!n.visible||n.resolvedStyle.display==DisplayStyle.None)return false;return true;}
        static void Display(VisualElement e,bool show){e.style.display=show?DisplayStyle.Flex:DisplayStyle.None;}
        static bool Contains(VisualElement e,Vector2 p)=>Visible(e)&&e.enabledInHierarchy&&e.worldBound.Contains(p);
        public static bool OverNow
        {
            get
            {
                if(!Visible(_root)||_root.panel==null)return false;
                var p=XqPanelXf.FromScreen(_root.panel,Input.mousePosition);
                foreach(var e in Hits.Values)if(Contains(e,p))return true;
                foreach(var s in Scrolls)if(Contains(s,p)&&s.verticalScroller.highValue>0)return true;
                return false;
            }
        }
        public static void Notice(string text){_notice=text??"";_noticeAt=Time.unscaledTime;}
        public static void ShowFeed(string text)
        {
            if(XqMode.Current!=XqView.Pet||string.IsNullOrWhiteSpace(text)||XiaoqiRuntime.Config?.office_mode==true||XiaoqiRuntime.Dnd)return;
            float now=Time.unscaledTime;
            if(XqVoiceChatClient.IsRecording||XqVoiceChatClient.IsRecognizing||XqVoiceChatClient.IsThinking){PendingFeed.Wait(text,now);return;}
            if((text==_lastFeed&&now-_feedAt<30)||(text==XqVoiceChatClient.LatestReplyFull&&now-XqVoiceChatClient.LastInteractionAt<30))return;
            _lastFeed=text;_feedAt=now;History.Put(--_feedId,true,text,now);
        }
        public static void BeginVoice(){XqGlassMenu.CloseCurrent("voice");XqVoiceChatClient.Ensure().StartRecording();}
        // Compatibility for older integration entry points; Pet never opens a text dock.
        public static void OpenTyping(){Notice("按住 Ctrl+N 说话，松开发送；Esc 取消。");}
        public static void BeginMenuVoice(){Notice("按住 Ctrl+N 说话，松开发送。");}
        public static void CancelPointerRecording(){_pressed=null;}
        static VisualElement Node(VisualElement parent,string name){var e=new VisualElement{name="pet-"+name,pickingMode=PickingMode.Ignore};e.style.position=Position.Absolute;parent.Add(e);return e;}
        internal static void Radius(VisualElement e,float r){e.style.borderTopLeftRadius=e.style.borderTopRightRadius=e.style.borderBottomLeftRadius=e.style.borderBottomRightRadius=r;}
        internal static void Surface(VisualElement e){e.style.backgroundColor=new Color(.988f,.98f,1,.98f);Radius(e,16);Border(e,new Color(.86f,.83f,.90f),1);}
        // This runtime panel does not supply all built-in control theme rules.
        // Explicitly constrain the rectangular viewport; never clip the rounded card.
        internal static void ClipScroll(ScrollView view,float u)
        {
            view.style.minHeight=0;
            var viewport=view.contentViewport;var group=viewport.parent;
            if(group!=view){group.style.position=Position.Absolute;group.style.left=group.style.top=group.style.right=group.style.bottom=0;}
            viewport.style.position=Position.Absolute;viewport.style.left=viewport.style.top=viewport.style.bottom=0;viewport.style.right=9*u;
            Radius(viewport,0);viewport.style.overflow=Overflow.Hidden;
            view.contentContainer.style.flexShrink=0;view.contentContainer.style.width=Length.Percent(100);
            var bar=view.verticalScroller;bar.style.position=Position.Absolute;bar.style.right=bar.style.top=bar.style.bottom=0;bar.style.width=8*u;
        }
        static void Border(VisualElement e,Color c,float w){e.style.borderTopColor=e.style.borderRightColor=e.style.borderBottomColor=e.style.borderLeftColor=c;e.style.borderTopWidth=e.style.borderRightWidth=e.style.borderBottomWidth=e.style.borderLeftWidth=w;}
        static Label Text(VisualElement parent,string text,float size)
        {
            var l=new Label(text){pickingMode=PickingMode.Ignore,enableRichText=false};l.style.color=Color.white;l.style.fontSize=size;l.style.whiteSpace=WhiteSpace.Normal;l.style.textOverflow=TextOverflow.Clip;
            l.style.marginLeft=l.style.marginTop=l.style.marginRight=l.style.marginBottom=0;l.style.paddingLeft=l.style.paddingTop=l.style.paddingRight=l.style.paddingBottom=0;parent.Add(l);return l;
        }
        static VisualElement Button(VisualElement p,string id,string text,Action action)
        {
            var e=Node(p,id);e.pickingMode=PickingMode.Position;Surface(e);var l=Text(e,text,13);l.style.color=new Color(.40f,.34f,.49f);l.style.flexGrow=1;l.style.unityTextAlign=TextAnchor.MiddleCenter;Hits[id]=e;Actions[id]=action;return e;
        }
        static void Build(VisualElement parent)
        {
            if(!_subscribed){_subscribed=true;XqVoiceChatClient.OnMessage+=(id,assistant,text)=>{if(XqMode.Current==XqView.Pet)History.Put(id,assistant,text,Time.unscaledTime);};}
            if(_root!=null&&_root.parent==parent)return;
            _root?.RemoveFromHierarchy();Hits.Clear();Actions.Clear();
            _root=Node(parent,"companion");_root.style.left=_root.style.top=_root.style.right=_root.style.bottom=0;
            if(Application.platform==RuntimePlatform.WindowsPlayer)_root.style.unityFont=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei UI","Microsoft YaHei"},14);
            _gate=Button(_root,"shape","",()=>_modeOpen=!_modeOpen);_gate.tooltip="切换形态";
            var glyph=new XqGlyph("expand",new Color(.55f,.47f,.65f));glyph.style.position=Position.Absolute;glyph.style.left=glyph.style.top=9;glyph.style.width=glyph.style.height=18;_gate.Add(glyph);
            _modes=Node(_root,"modes");Surface(_modes);
            foreach(var view in new[]{XqView.Pet,XqView.Close,XqView.Bench}){var v=view;Button(_modes,"mode-"+v,v==XqView.Pet?"桌宠 · 此刻":v==XqView.Close?"近景":"工作台",()=>{_modeOpen=false;XqMode.Set(v);});}
            _state=Node(_root,"state");_state.style.backgroundColor=new Color(0,0,0,.43f);Radius(_state,14);_stateText=Text(_state,"",12);
            for(int i=0;i<3;i++)
            {
                var card=Cards[i]=Node(_root,"message-"+i);card.style.backgroundColor=new Color(0,0,0,.38f);Radius(card,18);
                var scroll=Scrolls[i]=new ScrollView(ScrollViewMode.Vertical){name="pet-message-scroll-"+i};scroll.style.flexGrow=1;scroll.horizontalScrollerVisibility=ScrollerVisibility.Hidden;scroll.verticalScrollerVisibility=ScrollerVisibility.Auto;card.Add(scroll);ClipScroll(scroll,1);
                Labels[i]=Text(scroll.contentContainer,"",14);
                Labels[i].style.unityTextOutlineColor=new Color(0,0,0,.45f);Labels[i].style.unityTextOutlineWidth=.3f;
            }
        }
        static void Rect(VisualElement e,float x,float y,float w,float h){e.style.left=x;e.style.top=y;e.style.width=w;e.style.height=h;}
        public static void Poll(VisualElement parent)
        {
            Consumed=false;XqGlobalVoiceKey.Ensure();XqCompanionUsage.Poll();Build(parent);
            var mode=XqMode.Current;if(mode!=_lastMode){History.Clear();PendingFeed.Clear();_modeOpen=false;_pressed=null;_lastMode=mode;}
            var pending=PendingFeed.Take(Time.unscaledTime,XqVoiceChatClient.IsRecording||XqVoiceChatClient.IsRecognizing||XqVoiceChatClient.IsThinking,XqVoiceChatClient.LatestReplyFull);if(pending!=null)ShowFeed(pending);
            bool busy=XqVoiceChatClient.IsRecording||XqVoiceChatClient.IsRecognizing||XqVoiceChatClient.IsThinking||XqVoiceChatClient.IsStreaming||XqVoiceChatClient.IsPreparingTts||XqVoiceChatClient.IsPlayingTts;
            History.Tick(Time.unscaledTime,busy);
            Display(_root,mode==XqView.Pet&&!XqGlassMenu.IsOpen&&!XqSettingsPanel.OpenNow);if(!Visible(_root))return;
            float u=XqPanelXf.UnitsPerPx(parent.panel);var body=XqBodyAnchor.Stable();if(body.height<8)return;
            var a=XqPanelXf.FromScreen(parent.panel,new Vector2(body.xMin,body.yMax));var b=XqPanelXf.FromScreen(parent.panel,new Vector2(body.xMax,body.yMin));
            var wa=XqMonitor.WorkArea(body.center);var lt=XqPanelXf.FromScreen(parent.panel,new Vector2(wa.xMin,wa.yMax));var rb=XqPanelXf.FromScreen(parent.panel,new Vector2(wa.xMax,wa.yMin));
            float minX=lt.x+8*u,maxX=rb.x-8*u,minY=lt.y+8*u,maxY=rb.y-8*u;
            float gx=Mathf.Clamp(b.x+8*u,minX,Mathf.Max(minX,maxX-36*u)),gy=Mathf.Clamp(b.y-38*u,minY,Mathf.Max(minY,maxY-36*u));
            Rect(_gate,gx,gy,36*u,36*u);_gate.Q<Label>().style.fontSize=22*u;
            Display(_modes,_modeOpen);Rect(_modes,Mathf.Clamp(gx-80*u,minX,Mathf.Max(minX,maxX-116*u)),Mathf.Max(minY,gy-126*u),116*u,120*u);
            int j=0;foreach(var v in new[]{XqView.Pet,XqView.Close,XqView.Bench}){var e=Hits["mode-"+v];Rect(e,5*u,(5+37*j++)*u,106*u,36*u);e.Q<Label>().style.fontSize=12*u;}
            Hits["mode-"+XqView.Bench].SetEnabled(XqMode.BenchAvailable);
            string state=XqVoiceChatClient.IsRecording?"● 正在听 · 松开 Ctrl+N 发送":XqVoiceChatClient.IsRecognizing?"正在识别…":XqVoiceChatClient.IsThinking?"在想怎么说…":XqVoiceChatClient.IsPreparingTts?"正在准备声音…":"";
            if(state==""&&Time.unscaledTime-_noticeAt<6)state=_notice;
            if(state==""&&Time.unscaledTime-XqVoiceChatClient.LastInteractionAt<6)state=XqVoiceChatClient.Status;
            _stateText.text=state;_stateText.style.fontSize=12*u;
            _state.style.paddingLeft=_state.style.paddingRight=11*u;_state.style.paddingTop=_state.style.paddingBottom=6*u;
            Display(_state,!string.IsNullOrEmpty(state));float stateH=state==""?0:Mathf.Max(30*u,_stateText.MeasureTextSize(state,218*u,VisualElement.MeasureMode.Exactly,0,VisualElement.MeasureMode.Undefined).y+12*u);
            float width=Mathf.Min(240*u,maxX-minX),cx=Mathf.Clamp((a.x+b.x-width)/2,minX,Mathf.Max(minX,maxX-width));
            int count=History.Messages.Count;float available=Mathf.Max(100*u,a.y-minY-12*u-stateH),total=stateH>0?stateH+6*u:0;
            var heights=new float[3];var widths=new float[3];
            for(int i=0;i<3;i++)
            {
                Display(Cards[i],i<count);if(i>=count)continue;
                var msg=History.Messages[i];float age=count-1-i;float scale=1-age*.055f;float w=width*scale;float font=(14-age*.7f)*u;
                ClipScroll(Scrolls[i],u);var label=Labels[i];label.text=msg.text;label.style.fontSize=font;label.style.width=w-35*u;
                float desired=label.MeasureTextSize(msg.text,w-35*u,VisualElement.MeasureMode.Exactly,0,VisualElement.MeasureMode.Undefined).y+16*u;
                // A definite text extent prevents the unthemed ScrollView from
                // shrinking a long label to its viewport and losing the scroll range.
                label.style.flexShrink=0;label.style.height=Mathf.Max(font,desired-16*u);
                Scrolls[i].contentContainer.style.height=Mathf.Max(font,desired-16*u);
                heights[i]=Mathf.Max(34*u,desired);widths[i]=w;total+=heights[i]+6*u;
                Cards[i].style.opacity=1-age*.12f;Border(Cards[i],new Color(1,1,1,.86f),msg.assistant?u:0);Radius(Cards[i],18*u);
                Cards[i].style.paddingLeft=Cards[i].style.paddingRight=12*u;Cards[i].style.paddingTop=Cards[i].style.paddingBottom=7*u;
            }
            // Exceptionally long conversations stay fully readable by scrolling;
            // ordinary one/two-line messages never have a fixed-height crop.
            if(total>available&&count>0)
            {
                for(int i=0;i<count;i++){float allowance=(available-(stateH>0?stateH+6*u:0)-6*u*count)/count;heights[i]=Mathf.Min(heights[i],Mathf.Max(34*u,allowance));}
                total=stateH>0?stateH+6*u:0;for(int i=0;i<count;i++)total+=heights[i]+6*u;
            }
            float y=Mathf.Max(minY,a.y-total-10*u);
            for(int i=0;i<count;i++){Rect(Cards[i],cx+(width-widths[i])/2,y,widths[i],heights[i]);y+=heights[i]+6*u;}
            Rect(_state,cx,y,width,stateH);
            var pointer=XqPanelXf.FromScreen(parent.panel,Input.mousePosition);string hit=null;foreach(var pair in Hits)if(Contains(pair.Value,pointer))hit=pair.Key;
            if(Input.GetKeyDown(KeyCode.Escape))_modeOpen=false;
            if(Input.GetMouseButtonDown(0)){_pressed=hit;_down=Input.mousePosition;if(hit!=null)Consumed=true;else if(!Contains(_modes,pointer))_modeOpen=false;}
            if(Input.GetMouseButtonUp(0)){if(_pressed!=null&&hit==_pressed&&Vector2.Distance(_down,Input.mousePosition)<=8){Actions[hit]();Consumed=true;}_pressed=null;}
        }
        internal static JObject ReviewBounds()
        {
            var report=new JObject();foreach(var pair in Hits){var r=pair.Value.worldBound;report[pair.Key]=new JArray(r.x,r.y,r.width,r.height);}
            var messages=new JArray();for(int i=0;i<History.Messages.Count;i++){var r=Cards[i].worldBound;messages.Add(new JObject{["assistant"]=History.Messages[i].assistant,["text"]=Labels[i].text,["bounds"]=new JArray(r.x,r.y,r.width,r.height),["border"]=Cards[i].resolvedStyle.borderTopWidth,["backgroundAlpha"]=Cards[i].resolvedStyle.backgroundColor.a,["elided"]=Labels[i].isElided,["scrollRange"]=Scrolls[i].verticalScroller.highValue,["scrollOffset"]=Scrolls[i].scrollOffset.y,["clippedViewport"]=Scrolls[i].contentViewport.style.overflow.value==Overflow.Hidden});}
            report["messages"]=messages;report["globalHotkeyInstalled"]=XqGlobalVoiceKey.Available;report["oldHeadLabelSuppressed"]=true;return report;
        }
        internal static void ReviewMessages(bool longText=false)
        {
            History.Clear();float now=Time.unscaledTime;
            History.Put(-100,true,"刚刚看你皱眉了，先歇一会儿嘛。",now);
            History.Put(-101,false,"这段做完就休息。",now);
            History.Put(-102,true,longText?new string('祈',700):"那我陪你，把最后一点慢慢写完。今天已经很努力了，给你一颗小星星。",now);
        }
        internal static void ReviewClear(){History.Clear();_modeOpen=false;}
        internal static void ReviewScrollBottom(){int i=History.Messages.Count-1;if(i>=0)Scrolls[i].verticalScroller.value=Scrolls[i].verticalScroller.highValue;}
        internal static void ReviewModes(){_modeOpen=true;}
    }
}
