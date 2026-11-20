using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Xiaoqi.Mode;
namespace Xiaoqi.UI2
{
    // Approved desktop layout. One owner for voice state, reply and quiet badge.
    public static class XqVoiceOverlay
    {
        static VisualElement _root,_dock,_tools,_status,_reply,_failure,_typing,_modes,_badge;
        static Label _stateText,_replyText,_errorText,_failureTitle;
        static ScrollView _replyScroll;static TextField _input;
        static readonly Dictionary<string,VisualElement> Hits=new Dictionary<string,VisualElement>();
        static readonly Dictionary<string,Action> Actions=new Dictionary<string,Action>();
        static float _nearAt=-100,_noticeAt=-100,_feedAt=-100,_recordAt;
        static string _notice="",_feed="",_pressed,_heldOrigin;static Vector2 _down;
        static bool _expanded,_typeOpen,_modeOpen,_replyOpen,_pointerRecording;static XqView _lastMode;
        static readonly Color Ink=new Color(.34f,.30f,.41f),Soft=new Color(.58f,.51f,.65f);
        static bool Visible(VisualElement e){for(var n=e;n!=null;n=n.parent)if(!n.visible||n.resolvedStyle.display==DisplayStyle.None)return false;return e!=null;}
        static void Display(VisualElement e,bool on){e.style.display=on?DisplayStyle.Flex:DisplayStyle.None;}
        static bool Contains(VisualElement e,Vector2 p)=>Visible(e)&&e.enabledInHierarchy&&e.worldBound.Contains(p);
        public static bool OverNow
        {
            get{if(!Visible(_root)||_root.panel==null)return false;var p=XqPanelXf.FromScreen(_root.panel,Input.mousePosition);foreach(var e in Hits.Values)if(Contains(e,p))return true;return Contains(_typing,p)||Contains(_reply,p)||Contains(_failure,p)||Contains(_status,p);}
        }
        public static bool Consumed{get;private set;}
        public static void ShowFeed(string text){if(string.IsNullOrWhiteSpace(text))return;_feed=text;_feedAt=Time.unscaledTime;_replyOpen=false;}
        public static void Notice(string text){_notice=text??"";_noticeAt=Time.unscaledTime;}
        public static void OpenTyping(){_typeOpen=true;_expanded=true;_nearAt=Time.unscaledTime;_input?.schedule.Execute(()=>_input.Focus());XqGlassMenu.CloseCurrent("typing");}
        public static void BeginVoice(){XqGlassMenu.CloseCurrent("voice");_recordAt=Time.unscaledTime;XqVoiceChatClient.Ensure().StartRecording();}
        public static void BeginMenuVoice(){if(XqVoiceChatClient.IsRecording||XqVoiceChatClient.IsRecognizing||XqVoiceChatClient.IsThinking)return;BeginVoice();_pointerRecording=XqVoiceChatClient.IsRecording;_heldOrigin="menu";}
        public static void CancelPointerRecording(){if(_pointerRecording)XqVoiceChatClient.Ensure().CancelRecording();_pointerRecording=false;_pressed=null;_heldOrigin=null;}
        static VisualElement Node(VisualElement parent,string name,bool card=false){var e=new VisualElement{name="pet-"+name,pickingMode=PickingMode.Ignore};e.style.position=Position.Absolute;parent.Add(e);if(card)Surface(e);return e;}
        internal static void Surface(VisualElement e){e.style.backgroundColor=new Color(.988f,.98f,1f,.98f);Radius(e,16);e.style.borderTopColor=e.style.borderRightColor=e.style.borderBottomColor=e.style.borderLeftColor=new Color(.86f,.83f,.90f);e.style.borderTopWidth=e.style.borderRightWidth=e.style.borderBottomWidth=e.style.borderLeftWidth=1;}
        static void Radius(VisualElement e,float r){e.style.borderTopLeftRadius=e.style.borderTopRightRadius=e.style.borderBottomLeftRadius=e.style.borderBottomRightRadius=r;}
        static Label Text(VisualElement parent,string name,string value,float size)
        {
            var l=new Label(value){name="pet-"+name,pickingMode=PickingMode.Ignore,enableRichText=false};l.style.color=Ink;l.style.fontSize=size;l.style.unityFontStyleAndWeight=FontStyle.Normal;
            l.style.marginLeft=l.style.marginTop=l.style.marginRight=l.style.marginBottom=0;l.style.paddingLeft=l.style.paddingTop=l.style.paddingRight=l.style.paddingBottom=0;parent.Add(l);return l;
        }
        static VisualElement Button(VisualElement parent,string id,string text,Action action)
        {
            var e=Node(parent,id);e.pickingMode=PickingMode.Position;Radius(e,11);var l=Text(e,id+"-text",text,14);l.style.unityTextAlign=TextAnchor.MiddleCenter;l.style.flexGrow=1;
            e.RegisterCallback<PointerEnterEvent>(_=>e.style.backgroundColor=new Color(.93f,.90f,.96f));e.RegisterCallback<PointerLeaveEvent>(_=>e.style.backgroundColor=Color.clear);Hits[id]=e;Actions[id]=action;return e;
        }
        static void Icon(VisualElement parent,string key){var icon=new XqGlyph(key,Soft){pickingMode=PickingMode.Ignore};parent.Add(icon);icon.style.position=Position.Absolute;icon.style.left=8;icon.style.top=7;icon.style.width=20;icon.style.height=20;}
        static void Build(VisualElement parent)
        {
            if(_root!=null&&_root.parent==parent)return;Hits.Clear();Actions.Clear();_root=Node(parent,"companion");_root.style.left=_root.style.top=0;_root.style.right=_root.style.bottom=0;
            if(Application.platform==RuntimePlatform.WindowsPlayer)_root.style.unityFont=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei UI","Microsoft YaHei"},14);
            _dock=Node(_root,"dock");var anchor=Button(_dock,"anchor","",()=>{_expanded=!_expanded;_nearAt=Time.unscaledTime;});Surface(anchor);anchor.tooltip="展开互动";Icon(anchor,"pet");
            _badge=Node(anchor,"quiet-badge");_badge.style.backgroundColor=Soft;Radius(_badge,9);Text(_badge,"moon","☾",12).style.color=Color.white;
            _tools=Node(_dock,"tools",true);Button(_tools,"type","✎",OpenTyping).tooltip="打字聊天";Button(_tools,"mic","",()=>{}).tooltip="按住说话 · Ctrl+N";Icon(Hits["mic"],"mic");Button(_tools,"shape","",()=>{_modeOpen=!_modeOpen;_nearAt=Time.unscaledTime;}).tooltip="选择形态";Icon(Hits["shape"],"expand");
            _status=Node(_root,"status",true);_stateText=Text(_status,"state","",13);
            _reply=Node(_root,"reply",true);Text(_reply,"reply-name","小祈",11).style.color=Soft;_replyScroll=new ScrollView();_replyScroll.style.marginTop=7;_replyScroll.horizontalScrollerVisibility=ScrollerVisibility.Hidden;_replyScroll.verticalScrollerVisibility=ScrollerVisibility.Auto;_reply.Add(_replyScroll);_replyText=Text(_replyScroll.contentContainer,"reply-text","",14);_replyText.style.whiteSpace=WhiteSpace.Normal;Button(_reply,"reply-more","展开这条回复 ↗",()=>_replyOpen=!_replyOpen);
            _failure=Node(_root,"failure",true);_failureTitle=Text(_failure,"failure-title","没听清这一次",13);_errorText=Text(_failure,"error","",11);_errorText.style.whiteSpace=WhiteSpace.Normal;_errorText.style.color=Soft;Button(_failure,"retry","再说一次",BeginVoice);Button(_failure,"error-type","改用打字",()=>{_notice="";OpenTyping();});
            _typing=Node(_root,"typing",true);Text(_typing,"typing-title","和小祈说句话",11).style.color=Soft;_input=new TextField{name="pet-input",isDelayed=false};_input.style.position=Position.Absolute;_input.style.fontSize=13;_input.textEdition.placeholder="慢慢说，我在。";_input.textEdition.hidePlaceholderOnFocus=true;_typing.Add(_input);var entry=_input.Q(className:"unity-text-field__input");if(entry!=null){entry.style.backgroundColor=new Color(.95f,.93f,.97f);entry.style.color=Ink;Radius(entry,9);entry.style.borderTopWidth=entry.style.borderRightWidth=entry.style.borderBottomWidth=entry.style.borderLeftWidth=0;} _input.RegisterCallback<KeyDownEvent>(e=>{if(e.keyCode==KeyCode.Return||e.keyCode==KeyCode.KeypadEnter){Send();e.StopPropagation();}});Button(_typing,"send","↑",Send);
            _modes=Node(_root,"modes",true);foreach(var view in new[]{XqView.Pet,XqView.Close,XqView.Bench}){var v=view;Button(_modes,"mode-"+v,v==XqView.Pet?"桌宠 ✓":v==XqView.Close?"近景":"工作台",()=>{_modeOpen=false;XqMode.Set(v);});}
        }
        static void Send(){if(_input==null||string.IsNullOrWhiteSpace(_input.value))return;if(XqVoiceChatClient.IsThinking||XqVoiceChatClient.IsRecognizing||XqVoiceChatClient.IsRecording){Notice("正在处理上一条内容，请稍等。");return;}var value=_input.value;_input.value="";_typeOpen=false;_replyOpen=false;XqVoiceChatClient.Ensure().SendText(value);}
        internal static Newtonsoft.Json.Linq.JObject ReviewBounds()
        {
            var report=new Newtonsoft.Json.Linq.JObject();foreach(var kv in Hits){var r=kv.Value.worldBound;report[kv.Key]=new Newtonsoft.Json.Linq.JArray(r.x,r.y,r.width,r.height);}
            report["globalHotkeyInstalled"]=XqGlobalVoiceKey.Available;report["oldHeadLabelSuppressed"]=true;return report;
        }
        static void Rect(VisualElement e,float x,float y,float w,float h,float u){e.style.left=x;e.style.top=y;e.style.width=w*u;e.style.height=h*u;}
        static void Pad(VisualElement e,float amount,float u){e.style.paddingLeft=e.style.paddingRight=e.style.paddingTop=e.style.paddingBottom=amount*u;}
        public static void Poll(VisualElement parent)
        {
            Consumed=false;XqGlobalVoiceKey.Ensure();Build(parent);var mode=XqMode.Current;
            if(_lastMode!=mode){if(_pointerRecording)XqVoiceChatClient.Ensure().CancelRecording();_pointerRecording=false;_pressed=null;_typeOpen=false;_modeOpen=false;_lastMode=mode;}
            bool on=mode==XqView.Pet&&!XqGlassMenu.IsOpen&&!XqSettingsPanel.OpenNow;Display(_root,on);if(!on)return;
            float u=Mathf.Max(.01f,XqPanelXf.UnitsPerPx(parent.panel));var body=XqBodyAnchor.Stable();if(body.height<8)return;
            var a=XqPanelXf.FromScreen(parent.panel,new Vector2(body.xMin,body.yMax));var b=XqPanelXf.FromScreen(parent.panel,new Vector2(body.xMax,body.yMin));var wa=XqMonitor.WorkArea(body.center);var lt=XqPanelXf.FromScreen(parent.panel,new Vector2(wa.xMin,wa.yMax));var rb=XqPanelXf.FromScreen(parent.panel,new Vector2(wa.xMax,wa.yMin));float minX=lt.x+12*u,maxX=rb.x-12*u,minY=lt.y+12*u,maxY=rb.y-12*u;
            bool flip=b.x+174*u>maxX;float dx=Mathf.Clamp(flip?a.x-177*u:b.x+14*u,minX,Mathf.Max(minX,maxX-163*u)),dy=Mathf.Clamp(a.y+(b.y-a.y)*.52f,minY,maxY-40*u);var pointer=XqPanelXf.FromScreen(parent.panel,Input.mousePosition);var near=new UnityEngine.Rect(a.x-48*u,a.y-30*u,b.x-a.x+96*u,b.y-a.y+60*u);
            if(near.Contains(pointer)||OverNow)_nearAt=Time.unscaledTime;if(Time.unscaledTime-_nearAt>3&&!_modeOpen&&!_typeOpen&&!_pointerRecording)_expanded=false;else if(near.Contains(pointer))_expanded=true;
            Rect(_dock,dx,dy,163,40,u);Rect(Hits["anchor"],flip?127*u:0,0,36,36,u);Rect(_badge,21*u,-5*u,18,18,u);Display(_badge,XiaoqiRuntime.Dnd||XiaoqiRuntime.Config.muted);Rect(_tools,flip?0:43*u,0,120,36,u);Display(_tools,_expanded);foreach(var id in new[]{"type","mic","shape"})Rect(Hits[id],(id=="type"?3:id=="mic"?42:81)*u,3*u,33,30,u);
            bool rec=XqVoiceChatClient.IsRecording,busy=XqVoiceChatClient.IsRecognizing||XqVoiceChatClient.IsThinking;bool notice=Time.unscaledTime-_noticeAt<6&&!string.IsNullOrEmpty(_notice);bool failed=!rec&&!busy&&!XqVoiceChatClient.IsStreaming&&!string.IsNullOrEmpty(XqVoiceChatClient.Status)&&Time.unscaledTime-XqVoiceChatClient.LastInteractionAt<9;
            string reply=XqVoiceChatClient.LatestReplyVisible;bool recent=Time.unscaledTime-XqVoiceChatClient.LastInteractionAt<9||XqVoiceChatClient.IsPlayingTts||_replyOpen;if(!recent||string.IsNullOrEmpty(reply)){reply=_feed;recent=Time.unscaledTime-_feedAt<7||_replyOpen;}
            float sx=Mathf.Clamp(dx+(flip?-42*u:0),minX,Mathf.Max(minX,maxX-218*u));Display(_status,rec||busy);Rect(_status,sx,Mathf.Max(minY,dy-54*u),184,42,u);Pad(_status,12,u);_stateText.text=rec?"● 在听你说 · "+Mathf.Max(0,Time.unscaledTime-_recordAt).ToString("00")+"s":XqVoiceChatClient.IsRecognizing?"正在识别…":"在想怎么说";_stateText.style.fontSize=13*u;
            Display(_reply,!rec&&!busy&&!failed&&!notice&&!_typeOpen&&recent&&!string.IsNullOrEmpty(reply)&&XiaoqiRuntime.Config.bubble&&!XiaoqiRuntime.Config.office_mode);float rh=_replyOpen?268:148;Rect(_reply,Mathf.Clamp(a.x-148*u,minX,Mathf.Max(minX,maxX-258*u)),Mathf.Clamp(a.y-(rh+12)*u,minY,Mathf.Max(minY,maxY-rh*u)),258,rh,u);Pad(_reply,16,u);_replyText.text=reply;_replyText.style.fontSize=14*u;_replyScroll.style.height=(_replyOpen?190:72)*u;Rect(Hits["reply-more"],16*u,(rh-30)*u,220,22,u);
            Display(_failure,(failed||notice)&&!rec&&!busy&&!_typeOpen);Rect(_failure,sx,Mathf.Clamp(dy+52*u,minY,Mathf.Max(minY,maxY-132*u)),218,132,u);Pad(_failure,14,u);_failureTitle.text=notice?"小提示":"操作未完成";_errorText.text=notice?_notice:XqVoiceChatClient.Status.Contains("识别")||XqVoiceChatClient.Status.Contains("文字")?"未识别到文字，本次未发送。":"请检查服务或设备后重试。";_failure.tooltip=notice?_notice:XqVoiceChatClient.Status;_errorText.style.fontSize=11*u;_errorText.style.marginTop=8*u;Rect(Hits["retry"],12*u,83*u,92,34,u);Rect(Hits["error-type"],110*u,83*u,94,34,u);
            Display(_typing,_typeOpen);Rect(_typing,Mathf.Clamp(dx,minX,Mathf.Max(minX,maxX-286*u)),Mathf.Clamp(dy+52*u,minY,Mathf.Max(minY,maxY-108*u)),286,108,u);Pad(_typing,14,u);Rect(_input,12*u,38*u,220,42,u);Rect(Hits["send"],241*u,42*u,32,34,u);
            Display(_modes,_modeOpen);Rect(_modes,Mathf.Clamp(dx,minX,Mathf.Max(minX,maxX-116*u)),Mathf.Clamp(dy+50*u,minY,Mathf.Max(minY,maxY-132*u)),116,132,u);int i=0;foreach(var v in new[]{XqView.Pet,XqView.Close,XqView.Bench})Rect(Hits["mode-"+v],6*u,(6+40*i++)*u,104,38,u);
            Hits["mode-"+XqView.Bench].SetEnabled(XqMode.BenchAvailable);
            string hit=null;foreach(var kv in Hits)if(Contains(kv.Value,pointer))hit=kv.Key;
            if(Input.GetKeyDown(KeyCode.Escape)){_typeOpen=_modeOpen=_replyOpen=false;if(_pointerRecording)XqVoiceChatClient.Ensure().CancelRecording();_pointerRecording=false;_pressed=null;}
            if(Input.GetMouseButtonDown(0)){_pressed=hit;_down=Input.mousePosition;if(hit!=null)Consumed=true;if((hit=="mic"||hit=="retry")&&!rec&&!busy){BeginVoice();_pointerRecording=XqVoiceChatClient.IsRecording;_heldOrigin=hit;if(!_pointerRecording)_pressed=null;}if(hit==null&&!Contains(_typing,pointer)&&!Contains(_modes,pointer)){_modeOpen=false;_typeOpen=false;}}
            if(Input.GetMouseButtonUp(0)){if(_pointerRecording){if(hit==_heldOrigin||_heldOrigin=="menu"||_heldOrigin=="retry")XqVoiceChatClient.Ensure().StopRecordingAndSend();else XqVoiceChatClient.Ensure().CancelRecording();_pointerRecording=false;_heldOrigin=null;Consumed=true;}else if(_pressed!=null&&_pressed==hit&&Vector2.Distance(_down,Input.mousePosition)<=8){Actions[hit]();Consumed=true;}_pressed=null;}
        }
    }
}
