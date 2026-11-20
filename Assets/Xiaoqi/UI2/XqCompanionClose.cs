using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Xiaoqi.Mode;

namespace Xiaoqi.UI2
{
    // Approved revision 7, 1920 x 1080. Artwork is the actual exported preview artwork;
    // all changing information and hit targets remain native UI Toolkit elements.
    public partial class XqClosePanel
    {
        struct CompanionSlot { public VisualElement el; public Rect rect; public float font, compactFont; }
        readonly List<CompanionSlot> _compSlots = new List<CompanionSlot>();
        readonly Dictionary<string, VisualElement> _compHits = new Dictionary<string, VisualElement>();
        readonly Dictionary<string, Action> _compActions = new Dictionary<string, Action>();
        Label _compSeconds, _compActivity, _compWhisper, _compListenWord, _compUsage, _compUsageTitle, _compNotice, _compPlaybackState;
        VisualElement _compStatus, _compDecor, _compModal, _compModeTrigger; bool _compModeOpen;
        Image _compCover, _compChibi;
        float _compScale, _compNextTick, _compNoticeUntil, _compPatUntil;
        string _compPressed;
        string _compLastAction;
        float _compLastActionAt=-100;
        float _compReviewWidth,_compReviewHeight;
        Font _compRegularFont;
        static readonly Color CompanionInk = Rgb(112,80,128);
        static readonly Color CompanionSoft = Rgb(154,123,171);

        VisualElement CompanionNode(VisualElement parent, string name, float x, float y, float w, float h)
        {
            var node = new VisualElement { name = "companion-" + name, pickingMode = PickingMode.Ignore };
            parent.Add(node); CompanionSlotFor(node,x,y,w,h); return node;
        }
        void CompanionSlotFor(VisualElement el,float x,float y,float w,float h,float font=0,float compactFont=0)
        {
            el.style.position=Position.Absolute; el.style.flexShrink=0;
            _compSlots.Add(new CompanionSlot { el=el,rect=new Rect(x,y,w,h),font=font,compactFont=compactFont });
        }
        Label CompanionText(VisualElement parent,string name,string text,float x,float y,float w,float h,float size,Color? color=null,float compact=0)
        {
            var label=new Label(text) { name="companion-"+name,pickingMode=PickingMode.Ignore,enableRichText=false };
            label.style.color=color??CompanionInk; label.style.marginLeft=label.style.marginTop=label.style.marginRight=label.style.marginBottom=0;
            label.style.paddingLeft=label.style.paddingTop=label.style.paddingRight=label.style.paddingBottom=0;
            label.style.unityFontStyleAndWeight=FontStyle.Normal;label.style.whiteSpace=WhiteSpace.NoWrap;
            label.style.textOverflow=TextOverflow.Ellipsis;label.style.overflow=Overflow.Hidden;
            parent.Add(label);CompanionSlotFor(label,x,y,w,h,size,compact);return label;
        }
        Image CompanionArt(VisualElement parent,string name,string asset,float x,float y,float w,float h)
        {
            var image=new Image {name="companion-"+name,image=Resources.Load<Texture2D>("CloseCompanion/"+asset),scaleMode=ScaleMode.StretchToFill,pickingMode=PickingMode.Ignore};
            parent.Add(image);CompanionSlotFor(image,x,y,w,h);return image;
        }
        void CompanionHot(string id,VisualElement el,Action action,string tip)
        {
            _compHits[id]=el;_compActions[id]=action;el.pickingMode=PickingMode.Position;el.tooltip=tip;
            el.RegisterCallback<ClickEvent>(e=>{e.StopPropagation();CompanionActivate(id);});
            el.RegisterCallback<PointerEnterEvent>(e=>{if(el.enabledInHierarchy)el.style.opacity=.86f;});
            el.RegisterCallback<PointerLeaveEvent>(e=>el.style.opacity=1f);
        }
        void CompanionActivate(string id)
        {
            if(_lastClickFrame==Time.frameCount||(_compLastAction==id&&Time.unscaledTime-_compLastActionAt<.18f)||!_compHits.TryGetValue(id,out var target)||!target.enabledInHierarchy)return;
            _compLastAction=id;_compLastActionAt=Time.unscaledTime;
            _lastClickFrame=Time.frameCount;
            if(_compActions.TryGetValue(id,out var action))action();
            Debug.Log("XQ_COMPANION action="+id);CompanionRefresh();
        }
        VisualElement CompanionSticker(string key,float x,float y,float w,float h,float degree,string word,float textY,float size,out Label label)
        {
            var node=CompanionNode(_host,key,x,y,w,h);node.style.rotate=new UnityEngine.UIElements.Rotate(new Angle(degree,AngleUnit.Degree));
            CompanionArt(node,key+"-art",key,-12,-12,w+24,h+24);
            label=CompanionText(node,key+"-word",word,0,textY,w,34,size,null,key=="quiet"?17:20);
            label.style.unityTextAlign=TextAnchor.MiddleCenter;return node;
        }
        void BuildCompanionClose()
        {
            XqMoonStyle.Clear(_host);
            // Match the approved Windows preview's regular YaHei typography locally.
            // The existing bundled font remains the fallback on other platforms.
            if(Application.platform==RuntimePlatform.WindowsPlayer||Application.platform==RuntimePlatform.WindowsEditor)
            {
                _compRegularFont=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei UI","Microsoft YaHei"},16);
                if(_compRegularFont!=null)_host.style.unityFont=_compRegularFont;
            }
            foreach(var child in _host.Children().ToArray())
                if(child!=_plate&&child!=_spectrumRow&&child!=_chatDock&&child!=_modeDock)child.RemoveFromHierarchy();
            _kickers.Clear();_marks.Clear();_texts.Clear();_bigs.Clear();_actionPills.Clear();_slots.Clear();
            _compDecor=CompanionArt(_host,"structure","structure",0,0,1920,1080);_compDecor.SendToBack();
            BuildCompanionWave();

            _cardNext=CompanionNode(_host,"clock",96,82,480,216);_bodyNext=new VisualElement();
            CompanionArt(_cardNext,"clock-bow","bow",-22,-33,82,58).style.rotate=new UnityEngine.UIElements.Rotate(new Angle(-13,AngleUnit.Degree));
            var caption=CompanionText(_cardNext,"clock-caption","A LITTLE MOMENT WITH YOU",67,0,360,15,11.5f,CompanionSoft);
            caption.style.letterSpacing=3;
            _clockBig=CompanionText(_cardNext,"clock-time","",0,25,210,85,72,Rgb(108,82,132));
            _compSeconds=CompanionText(_cardNext,"clock-seconds","",222,72,48,28,20,CompanionSoft);
            _locPill=CompanionText(_cardNext,"date","",4,117.6f,476,25,17,CompanionSoft,17);
            _weatherPill=CompanionText(_cardNext,"weather","",4,148.6f,476,30,17,CompanionSoft,17);
            CompanionText(_host,"left-kicker","THE LITTLE THINGS, WITH YOU",106,310,450,17,10,CompanionSoft).style.letterSpacing=3;
            CompanionText(_host,"left-title","把今天，讲给我听。",106,337,470,49,32);
            CompanionText(_host,"right-title","一起做点小事。",1422,379,450,45,26);
            CompanionArt(_host,"moon-bow","bow",1694,421,88,62).style.rotate=new UnityEngine.UIElements.Rotate(new Angle(5,AngleUnit.Degree));

            _actPat=CompanionSticker("pat",258,430,188,174,-7,"摸摸头",69,19,out _actPatLbl);
            _approvedListen=CompanionSticker("listen",1628,784,224,154,4,"听我说",48,19,out _compListenWord);
            _actScreen=CompanionSticker("screen",1398,455,204,204,-4,"看我屏幕",159,18,out _actScreenLbl);
            _approvedQuiet=CompanionSticker("quiet",1648,550,158,172,7,"安静陪伴",138,16,out _approvedQuietLabel);
            CompanionHot("pat",_actPat,()=>{
                Xiaoqi.Spike.XiaoqiFace.React(.65f,.32f);_compPatUntil=Time.unscaledTime+1.6f;
                CompanionNotice("(｡•ᴗ•｡)  摸摸收到啦。");
            },"摸摸头 · 小祈会用表情回应");
            CompanionHot("listen",_approvedListen,CompanionRecord,"点一下录音，再点一下发送识别；最多 20 秒");
            CompanionHot("quiet",_approvedQuiet,()=>{
                var cfg=XiaoqiRuntime.Config;if(cfg==null)return;
                bool silent=cfg.muted||XiaoqiRuntime.Dnd;
                if(silent){cfg.dnd=false;XiaoqiRuntime.ClearQuiet();XqSettingsApply.SetMuted(false);}
                else XqSettingsApply.SetMuted(true);
                XiaoqiConfig.Save(cfg);
                CompanionNotice(cfg.muted?"声音已关闭，仍然可以打字聊天。":cfg.voice_tts_auto&&!cfg.office_mode?"已解除静音和勿扰，我在听。":"已解除静音和勿扰；自动播报仍遵循语音设置。");
            },"关闭 / 恢复小祈的声音；恢复时同时解除勿扰，不改变音乐音量");
            CompanionHot("screen",_actScreen,()=>{
                CompanionNotice("正在分享当前窗口标题…");
                XqScreenCompanion.PushNow(XqCloseData.Ensure(),CompanionNotice);
            },"窗口陪伴 · 仅分享当前应用和窗口标题，不识别画面内容");

            _compStatus=CompanionNode(_host,"status",1386,74,470,284);
            CompanionArt(_compStatus,"cloud","status-surface",0,0,470,284);
            _compChibi=new Image {image=Resources.Load<Texture2D>("CloseCompanion/chibi-sit"),scaleMode=ScaleMode.ScaleToFit,pickingMode=PickingMode.Ignore};
            _compStatus.Add(_compChibi);CompanionSlotFor(_compChibi,-63,-71,214,214);
            var dot=CompanionNode(_compStatus,"status-dot",123,43,6,6);dot.style.backgroundColor=Rgb(175,184,148);SetRadius(dot,3);
            CompanionText(_compStatus,"status-title","她的此刻",138,31,270,29,19,CompanionSoft,20);
            _compActivity=CompanionText(_compStatus,"activity","生活状态同步中…",123,77,295,42,22,null,23);
            _compWhisper=CompanionText(_compStatus,"whisper","慢慢来，我在这里哦。",123,123,295,25,12,CompanionSoft,15);
            CompanionText(_compStatus,"cloud-heart","♡",416,150,23,25,16,Rgb(195,142,174));
            _tokenCapsule=CompanionNode(_compStatus,"usage",33,214,410,53);
            CompanionText(_tokenCapsule,"usage-star","☆",6,9,30,30,25,Rgb(180,152,199));
            _compUsageTitle=CompanionText(_tokenCapsule,"usage-title","TOKEN / 本轮消耗",43,7,210,24,12,CompanionSoft,16);
            _compUsage=CompanionText(_tokenCapsule,"usage-value","尚未获取",242,7,155,24,14,CompanionSoft,16);
            _compUsage.style.unityTextAlign=TextAnchor.UpperRight;
            var usageLine=CompanionNode(_tokenCapsule,"usage-rule",43,37,354,2);
            usageLine.generateVisualContent+=ctx=>{var p=ctx.painter2D;p.strokeColor=Rgb(213,188,225);p.lineWidth=Mathf.Max(.7f,_compScale*1.5f);for(float x=0;x<usageLine.contentRect.width;x+=13*_compScale){p.BeginPath();p.MoveTo(new Vector2(x,0));p.LineTo(new Vector2(Mathf.Min(x+9*_compScale,usageLine.contentRect.width),0));p.Stroke();}};
            CompanionHot("usage",_tokenCapsule,()=>{
                var u=XqVoiceChatClient.LatestUsage;
                string today=XqCompanionUsage.TodayTokens.HasValue?$"今日累计 {XqCompanionUsage.TodayTokens.Value:N0} tokens\n来源：云端用量账本 /api/budget\n更新：{XqCompanionUsage.UpdatedAt.ToLocalTime():HH:mm:ss}\n\n":"今日账本暂未获取。\n\n";
                CompanionModal("对话用量",today+(u==null?"本轮响应没有提供有效用量。":$"本轮输入 {u.promptTokens:N0} tokens\n本轮输出 {u.completionTokens:N0} tokens\n本轮合计 {u.totalTokens:N0} tokens"));
            },"查看云端今日账本与最近一轮响应用量");

            _plate.Clear();_plate.style.backgroundImage=new StyleBackground(Resources.Load<Texture2D>("CloseCompanion/signoff"));
            BuildCompanionPlayer();BuildCompanionChat();
            _cardSay=CompanionNode(_host,"reply",574.3f,917.1f,771.4f,34.3f);_bodySay=new VisualElement();
            _sayText=CompanionText(_cardSay,"reply-text","",9,4,753,28,15,Rgb(110,93,122),17);
            _sayText.style.unityTextAlign=TextAnchor.MiddleRight;
            _sayText.style.textShadow=new TextShadow {offset=new Vector2(0,1),blurRadius=1,color=new Color(1,1,1,.8f)};
            CompanionHot("reply",_cardSay,()=>{
                string full=XqVoiceChatClient.LatestReplyFull;
                if(string.IsNullOrEmpty(full))full=_data?.Say;
                CompanionModal("小祈说",string.IsNullOrEmpty(full)?"还没有新的回复。":full);
            },"查看完整回复");
            _compNotice=CompanionText(_host,"notice","",574.3f,1014,771.4f,30,14,CompanionSoft,17);
            _compNotice.style.unityTextAlign=TextAnchor.MiddleCenter;
            AnimAdd(_compStatus,1,Vector2.zero);AnimAdd(_actPat,2,Vector2.zero);AnimAdd(_approvedListen,2,Vector2.zero);
            AnimAdd(_actScreen,2,Vector2.zero);AnimAdd(_approvedQuiet,2,Vector2.zero);AnimAdd(_spectrumRow,5,Vector2.zero);
            _host.schedule.Execute(()=>{if(_shown)CompanionRefresh();}).Every(250);
            CompanionRefresh();
        }
        void BuildCompanionPlayer()
        {
            _cardMusic=CompanionNode(_host,"player",80,690,468,300);_bodyMusic=new VisualElement();
            CompanionArt(_cardMusic,"player-surface","player-surface",-24,-24,516,348);
            CompanionText(_cardMusic,"radio-tag","OUR LITTLE RADIO",27,20,210,20,11,CompanionSoft,14).style.letterSpacing=2;
            CompanionArt(_cardMusic,"tiny-bow","bow",179,18,39,28).style.rotate=new UnityEngine.UIElements.Rotate(new Angle(10));
            var seam=CompanionNode(_cardMusic,"player-seam",249,177,188,1);seam.style.backgroundColor=Rgb(219,201,225);
            _compPlaybackState=CompanionText(_cardMusic,"playback-state","",28,208,188,23,11,CompanionSoft,15);
            CompanionArt(_cardMusic,"record","record",76,32,162,162);
            // The fallback is explicitly illustrative; it is never presented as a retrieved album cover.
            _compCover=CompanionArt(_cardMusic,"cover","",25,51,124,124);_musicCover=_compCover;
            _compCover.scaleMode=ScaleMode.ScaleAndCrop;_compCover.style.rotate=new UnityEngine.UIElements.Rotate(new Angle(-4,AngleUnit.Degree));
            SetAllBorder(_compCover,2,Color.white);SetRadius(_compCover,14);
            var together=CompanionText(_cardMusic,"together","LISTENING TOGETHER / 一起听",254,42,194,16,9,CompanionSoft);together.style.letterSpacing=1;
            _musicTitle=CompanionText(_cardMusic,"song","",254,74,194,40,28);
            _musicArtist=CompanionText(_cardMusic,"artist","",255,119,191,48,13,CompanionSoft,16);
            _musicArtist.style.whiteSpace=WhiteSpace.Normal;
            CompanionText(_cardMusic,"player-heart","♡",429,15,24,25,18,Rgb(201,148,175));
            var track=CompanionNode(_cardMusic,"music-progress",28,254,412,3);track.style.backgroundColor=Rgb(223,208,232);
            _musicFill=new VisualElement {pickingMode=PickingMode.Ignore};_musicFill.style.height=Length.Percent(100);_musicFill.style.backgroundColor=Rgb(174,139,194);track.Add(_musicFill);
            _musicElapsed=CompanionText(_cardMusic,"elapsed","--:--",28,267,100,20,11,CompanionSoft,15);
            _musicDuration=CompanionText(_cardMusic,"duration","--:--",340,267,100,20,11,CompanionSoft,15);_musicDuration.style.unityTextAlign=TextAnchor.UpperRight;
            _musicPrev=CompanionNode(_cardMusic,"previous",242,192,42,42);
            _musicPlay=CompanionNode(_cardMusic,"play",296,188,50,50);_musicPlay.style.backgroundColor=Rgb(164,134,183);SetRadius(_musicPlay,20);
            _musicNext=CompanionNode(_cardMusic,"next",358,192,42,42);
            CompanionIcon(_musicPrev,"prev",13,13,16,16);CompanionIcon(_musicPlay,"play",17,17,16,16);CompanionIcon(_musicNext,"next",13,13,16,16);
            _musicPlayLbl=null;_musicLikeLbl=null;_musicLike=new VisualElement();
            CompanionHot("prev",_musicPrev,()=>XqMusicBridge.Prev(),"上一首 · 由系统媒体会话提供");
            CompanionHot("play",_musicPlay,()=>XqMusicBridge.TogglePlay(),"播放 / 暂停");
            CompanionHot("next",_musicNext,()=>XqMusicBridge.Next(),"下一首 · 由系统媒体会话提供");
            var info=CompanionNode(_cardMusic,"music-details",254,74,194,95);
            CompanionHot("music-details",info,()=>CompanionModal("一起听",XqMusicBridge.Available?XqMusicBridge.Current.title+"\n"+XqMusicBridge.Current.artist+"\n\n来源："+XqMusicBridge.Source:"还没有系统媒体会话。\n请在常用播放器中播放音乐。\n灰色按钮表示该控制当前不可用。"),"查看完整曲名与媒体来源");
        }
        void BuildCompanionChat()
        {
            XqMoonStyle.Clear(_chatDock);_chatDock.Q("xq-soft-shadow")?.RemoveFromHierarchy();
            _chatDock.style.backgroundColor=Rgb(246,236,249,.95f);SetAllBorder(_chatDock,1,Color.white);
            if(_chatDock.childCount>0)_chatDock[0].style.display=DisplayStyle.None;
            CompanionSlotFor(_chatDock,574.3f,954.3f,771.4f,51.4f);
            _chatInput.style.position=Position.Absolute;CompanionSlotFor(_chatInput,40,7,630,38,15,17);
            _chatInput.tooltip="和小祈说句话，按 Enter 发送";
            _chatInput.textEdition.placeholder="和小祈说句话……";
            _chatInput.textEdition.hidePlaceholderOnFocus=true;
            var input=_chatInput.Q(className:"unity-text-field__input");
            if(input!=null){input.style.backgroundColor=Color.clear;SetAllBorder(input,0,Color.clear);input.style.color=CompanionInk;}
            _chatMicBtn.Clear();_chatSendBtn.Clear();_chatMicLbl=null;_chatSendLbl=null;
            CompanionSlotFor(_chatMicBtn,684,10,34,34);CompanionSlotFor(_chatSendBtn,730,9,34,34);
            CompanionIcon(_chatMicBtn,"mic",9,8,16,18);CompanionIcon(_chatSendBtn,"send",9,8,16,18);
            _chatMicBtn.tooltip="开始录音 / 结束并发送";_chatSendBtn.tooltip="发送文字";
            CompanionIcon(_chatDock,"chat",17,17,16,16);
            XqMoonStyle.Clear(_modeDock);_modeDock.Q("xq-soft-shadow")?.RemoveFromHierarchy();_modeDock.style.backgroundColor=Rgb(246,236,249,.95f);SetAllBorder(_modeDock,1,Color.white);
            CompanionSlotFor(_modeDock,1372,718,224,216);
            _compModeTrigger=CompanionNode(_host,"mode-trigger",1372,953,54,54);_compModeTrigger.style.backgroundColor=Rgb(246,236,249,.98f);SetAllBorder(_compModeTrigger,1,Color.white);SetRadius(_compModeTrigger,19);CompanionIcon(_compModeTrigger,"expand",16,16,23,23);CompanionHot("modes",_compModeTrigger,()=>_compModeOpen=!_compModeOpen,"选择形态");
            int i=0;foreach(var button in _modeDock.Children())
            {
                if(button is Label)continue;
                CompanionSlotFor(button,12,30+i*55,200,49);
                button.style.paddingLeft=button.style.paddingRight=0;
                var label=button.Q<Label>(className:"xq-cl-btn-word");
                if(label!=null){label.text=new[]{"桌宠","近景","工作台"}[Mathf.Min(i,2)];label.style.color=i==1?Color.white:CompanionInk;label.style.unityFontStyleAndWeight=FontStyle.Normal;CompanionSlotFor(label,0,0,200,49,15,17);label.style.unityTextAlign=TextAnchor.MiddleCenter;}
                button.style.backgroundColor=i==1?Rgb(164,134,183):Color.clear;i++;
            }
        }
        void CompanionIcon(VisualElement parent,string icon,float x,float y,float w,float h)
        {
            var node=CompanionNode(parent,icon+"-icon",x,y,w,h);
            node.generateVisualContent+=ctx=>{
                float u=node.contentRect.width/16f;if(u<=0)return;var p=ctx.painter2D;
                p.strokeColor=icon=="play"||icon=="send"?Color.white:Rgb(169,132,191);p.fillColor=p.strokeColor;p.lineWidth=1.2f*u;
                Action<Vector2[]> line=points=>{p.BeginPath();p.MoveTo(points[0]*u);for(int j=1;j<points.Length;j++)p.LineTo(points[j]*u);p.Stroke();};
                if(icon=="prev"||icon=="next"){
                    bool next=icon=="next";float a=next?4:12,b=next?11:5,c=next?13:3;
                    line(new[]{new Vector2(a,3),new Vector2(b,8),new Vector2(a,13),new Vector2(a,3)});line(new[]{new Vector2(c,3),new Vector2(c,13)});
                }else if(icon=="play"){
                    if(XqMusicBridge.IsPlaying){for(int j=0;j<2;j++){float a=5+j*5;line(new[]{new Vector2(a,4),new Vector2(a,12)});}}
                    else{p.BeginPath();p.MoveTo(new Vector2(5,3)*u);p.LineTo(new Vector2(12,8)*u);p.LineTo(new Vector2(5,13)*u);p.ClosePath();p.Fill();}
                }else if(icon=="expand"){
                    line(new[]{new Vector2(6,2),new Vector2(2,2),new Vector2(2,6)});line(new[]{new Vector2(10,2),new Vector2(14,2),new Vector2(14,6)});line(new[]{new Vector2(2,10),new Vector2(2,14),new Vector2(6,14)});line(new[]{new Vector2(10,14),new Vector2(14,14),new Vector2(14,10)});
                }else if(icon=="send"){
                    line(new[]{new Vector2(4,7),new Vector2(8,3),new Vector2(12,7)});line(new[]{new Vector2(8,3),new Vector2(8,14)});
                }else if(icon=="mic"){
                    p.BeginPath();p.MoveTo(new Vector2(5,4)*u);p.BezierCurveTo(new Vector2(5,0)*u,new Vector2(11,0)*u,new Vector2(11,4)*u);p.LineTo(new Vector2(11,9)*u);p.BezierCurveTo(new Vector2(11,13)*u,new Vector2(5,13)*u,new Vector2(5,9)*u);p.ClosePath();p.Stroke();
                    line(new[]{new Vector2(2,8),new Vector2(2,11),new Vector2(5,14),new Vector2(11,14),new Vector2(14,11),new Vector2(14,8)});line(new[]{new Vector2(8,14),new Vector2(8,17)});
                }else{line(new[]{new Vector2(3,3),new Vector2(13,3),new Vector2(14,11),new Vector2(6,12),new Vector2(2,15),new Vector2(2,4),new Vector2(3,3)});}
            };
        }
        void PlaceCompanionClose(float width,float height)
        {
            if(_compSlots.Count==0)return;
            if(_compReviewWidth>0){width=_compReviewWidth;height=_compReviewHeight;}
            _compScale=Mathf.Min(width/1920f,height/1080f);float ox=(width-1920*_compScale)*.5f,oy=(height-1080*_compScale)*.5f;
            bool compact=Screen.width<1600;
            if(_compReviewWidth>0)compact=width<1600;
            foreach(var slot in _compSlots)
            {
                var r=slot.rect;SetApprovedRect(slot.el,r.x*_compScale+(slot.el.parent==_host?ox:0),r.y*_compScale+(slot.el.parent==_host?oy:0),r.width*_compScale,r.height*_compScale);
                slot.el.style.right=slot.el.style.bottom=StyleKeyword.Auto;
                if(slot.font>0)slot.el.style.fontSize=(compact&&slot.compactFont>0?slot.compactFont:slot.font)*_compScale;
            }
            foreach(var body in new[]{_bodyNext,_bodyMusic,_bodySay})XqMoonStyle.Clear(body);
            SetApprovedRect(_plate,ox+212*_compScale,oy+620*_compScale,316*_compScale,64*_compScale);_plate.style.rotate=new UnityEngine.UIElements.Rotate(new Angle(-1));
            SetApprovedRect(_spectrumRow,ox+574.3f*_compScale,oy+840*_compScale,771.4f*_compScale,77.14f*_compScale);_spectrumRow.style.bottom=StyleKeyword.Auto;_spectrumRow.style.right=StyleKeyword.Auto;
            SetRadius(_chatDock,26*_compScale);SetRadius(_modeDock,26*_compScale);
            foreach(var e in new[]{_chatDock,_modeDock,_chatMicBtn,_chatSendBtn})e.style.paddingLeft=e.style.paddingRight=e.style.paddingTop=e.style.paddingBottom=0;
            _chatMicBtn.style.marginRight=0;SetRadius(_chatMicBtn,20*_compScale);SetRadius(_chatSendBtn,20*_compScale);_chatSendBtn.style.backgroundColor=Rgb(164,134,183);
            foreach(var child in _modeDock.Children())SetRadius(child,18*_compScale);
            _compDecor.MarkDirtyRepaint();CompanionRefresh();
        }
        void CompanionRecord()
        {
            var voice=XqVoiceChatClient.Ensure();
            if(XqVoiceChatClient.IsRecording)voice.StopRecordingAndSend();else voice.StartRecording();
        }
        void CompanionNotice(string text)
        {
            if(_compNotice==null)return;_compNotice.text=text;_compNoticeUntil=Time.unscaledTime+6;
            _compNotice.tooltip=text;
        }
        void CompanionRefresh()
        {
            if(_compActivity==null)return;
            _modeDock.style.display=_compModeOpen?DisplayStyle.Flex:DisplayStyle.None;
            var now=DateTime.Now;_clockBig.text=now.ToString("HH:mm");_compSeconds.text=now.ToString("ss");
            var state=_data?.State;var life=XqBenchData.Ensure().Get("life");
            string doing=(string)life?.SelectToken("place.doing");if(string.IsNullOrEmpty(doing))doing=(string)state?.SelectToken("activity");
            _compActivity.text=string.IsNullOrEmpty(doing)?"生活状态暂未同步":doing;_compActivity.tooltip=_compActivity.text;
            var cfg=XiaoqiRuntime.Config;bool muted=cfg!=null&&(cfg.muted||XiaoqiRuntime.Dnd);
            _approvedQuietLabel.text=muted?"恢复声音":"安静陪伴";_approvedQuietLabel.style.color=muted?Rgb(136,89,160):Rgb(174,129,157);
            _compWhisper.text=XiaoqiRuntime.Dnd?"当前勿扰中，点月亮恢复声音。":cfg!=null&&cfg.muted?"安静陪着你，打字也可以哦。":"慢慢来，我在这里哦。";
            bool busy=XqVoiceChatClient.IsThinking||XqVoiceChatClient.IsRecognizing;
            _approvedListen.SetEnabled(!busy);_chatMicBtn.SetEnabled(!busy);
            _compListenWord.text=XqVoiceChatClient.IsRecording?"说好了，发送":XqVoiceChatClient.IsRecognizing?"识别中…":XqVoiceChatClient.IsThinking?"等我一下":"听我说";
            _compListenWord.style.color=XqVoiceChatClient.IsRecording?Rgb(178,113,148):CompanionInk;
            _actPatLbl.text=Time.unscaledTime<_compPatUntil?"蹭蹭 ♡":"摸摸头";
            _chatSendBtn.SetEnabled(!busy&&!XqVoiceChatClient.IsRecording);
            bool today=XqCompanionUsage.TodayTokens.HasValue;
            _compUsageTitle.text=today?"TOKEN / 今日消耗":"TOKEN / 本轮消耗";
            _compUsage.text=today?XqCompanionUsage.TodayTokens.Value.ToString("N0")+" tokens":XqVoiceChatClient.LatestUsage==null?"尚未获取":XqVoiceChatClient.LatestUsage.totalTokens.ToString("N0")+" tokens";
            if(Time.unscaledTime>_compNoticeUntil)_compNotice.text="";
            if(XqMusicBridge.Cover==null){_compCover.image=Resources.Load<Texture2D>("XqMusicCover");_compCover.tooltip="陪伴插画占位；播放器未提供封面";}else{_compCover.image=XqMusicBridge.Cover;_compCover.tooltip="播放器提供的封面";}
            _musicTitle.tooltip=XqMusicBridge.Current.title;_musicArtist.tooltip=XqMusicBridge.Current.artist;
            _compPlaybackState.text=!XqMusicBridge.Available?"○ 等一首歌":XqMusicBridge.IsPlaying?"● 正在一起听":"○ 先暂停一会儿";
            _musicPlay.Q("companion-play-icon")?.MarkDirtyRepaint();
            RefreshChatDock();
        }
        string CompanionHit(Vector2 point)
        {
            if(_host.panel==null)return null;var picked=_host.panel.Pick(point);
            foreach(var hit in _compHits)if(picked!=null&&hit.Value.enabledInHierarchy&&hit.Value.worldBound.Contains(point)&&(picked==hit.Value||hit.Value.Contains(picked)))return hit.Key;
            return null;
        }
        bool CompanionOverAny()
        {
            if(_host?.panel==null)return false;
            var point=XqPanelXf.FromScreen(_host.panel,Input.mousePosition);
            return CompanionHit(point)!=null||OverEl(_modeDock)||OverEl(_chatDock)||(_compModal!=null&&OverEl(_compModal));
        }
        public static bool DismissCompanionPopup()
        {
            if(_inst==null)return false;
            if(_inst._compModal!=null){_inst._compModal.RemoveFromHierarchy();_inst._compModal=null;return true;}
            if(_inst._compModeOpen){_inst._compModeOpen=false;_inst.CompanionRefresh();return true;}
            if(XqVoiceChatClient.IsRecording){XqVoiceChatClient.Ensure().CancelRecording();return true;}
            return false;
        }
        void CompanionPoll()
        {
            if(_host?.panel==null)return;
            if(Time.unscaledTime>=_compNextTick){_compNextTick=Time.unscaledTime+.2f;CompanionRefresh();XqScreenCompanion.ObserveForeground();XqCompanionUsage.Poll();}
            if(_musicElapsed!=null)_musicElapsed.text=XqMusicBridge.Available&&XqMusicBridge.Current.durationSec>0?XqMusicBridge.Clock(XqMusicBridge.PositionSec):"--:--";
            if(_musicFill!=null)_musicFill.style.width=Length.Percent(XqMusicBridge.Progress*100);
            for(int i=0;i<_spectrumBars.Count&&i<XqSysAudioBridge.BarCount;i++)_spectrumBars[i].style.height=Length.Percent(Mathf.Clamp(XqSysAudioBridge.Bars[i],.05f,1)*100);
            var point=XqPanelXf.FromScreen(_host.panel,Input.mousePosition);
            if(Input.GetMouseButtonDown(0)){_approvedDown=Input.mousePosition;_compPressed=CompanionHit(point);if(_compModeOpen){int idx=0;foreach(var button in _modeDock.Children()){if(button.worldBound.Contains(point)){_compPressed="view-"+idx;break;}idx++;}}}
            if(Input.GetMouseButtonUp(0)){if(_compPressed!=null&&_compPressed.StartsWith("view-")&&(Input.mousePosition-_approvedDown).sqrMagnitude<64){int idx=int.Parse(_compPressed.Substring(5));var views=new[]{XqView.Pet,XqView.Close,XqView.Bench};if(idx<3){_compModeOpen=false;XqMode.Set(views[idx]);}_compPressed=null;return;}if(_compPressed!=null&&(Input.mousePosition-_approvedDown).sqrMagnitude<64&&CompanionHit(point)==_compPressed)CompanionActivate(_compPressed);_compPressed=null;}
        }
        void CompanionModal(string title,string text)
        {
            _compModal?.RemoveFromHierarchy();
            _compModal=new VisualElement {name="companion-detail",pickingMode=PickingMode.Position};
            _compModal.style.position=Position.Absolute;_compModal.style.left=Length.Percent(27);_compModal.style.top=Length.Percent(25);_compModal.style.width=Length.Percent(46);_compModal.style.maxHeight=Length.Percent(48);
            _compModal.style.paddingLeft=_compModal.style.paddingRight=24*_compScale;_compModal.style.paddingTop=_compModal.style.paddingBottom=18*_compScale;
            _compModal.style.backgroundColor=Rgb(252,246,253,.99f);SetAllBorder(_compModal,1,Color.white);SetRadius(_compModal,20*_compScale);
            var header=new Label(title+"   ·   点击此处收起") {enableRichText=false};header.style.color=CompanionInk;header.style.fontSize=20*_compScale;header.style.marginBottom=12*_compScale;
            header.RegisterCallback<ClickEvent>(e=>{e.StopPropagation();_compModal.RemoveFromHierarchy();_compModal=null;});_compModal.Add(header);
            var scroll=new ScrollView();scroll.style.flexShrink=1;var body=new Label(text) {enableRichText=false};body.style.whiteSpace=WhiteSpace.Normal;body.style.color=CompanionInk;body.style.fontSize=18*_compScale;scroll.Add(body);_compModal.Add(scroll);_host.Add(_compModal);_compModal.BringToFront();
        }
    }
}
