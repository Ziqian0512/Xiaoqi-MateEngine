using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Xiaoqi.Mode;

namespace Xiaoqi.UI2
{
    public partial class XqBenchPanel
    {
        // Approved 1440 x 810 artwork. Only decoration is baked; all live text and actions are native UI.
        internal static bool SweetEnabled => !CampusPreview && Array.IndexOf(Environment.GetCommandLineArgs(), "-xq-bench-legacy") < 0;
        float S => _k * 620f / 810f;
        static DateTime SweetNow => DateTime.UtcNow.AddHours(8);
        static DateTime SweetToday => SweetNow.AddHours(-3).Date;
        VisualElement _sweetLayer;
        Image _sweetSurface;
        JObject _sweetLayout;
        JToken _sweetPageLayout;
        readonly Dictionary<string, Label> _sweetLabels = new Dictionary<string, Label>();
        readonly Dictionary<string, VisualElement> _sweetTargets = new Dictionary<string, VisualElement>();
        readonly Dictionary<string, Image> _sweetSprites = new Dictionary<string, Image>();
        DateTime _sweetDay = DateTime.MinValue;
        DateTime _sweetWeek = DateTime.MinValue;
        int _sweetEventPage, _sweetMemoryPage, _sweetDayRequest;
        JToken _sweetHistoryLife, _sweetHistoryDiary;
        bool _sweetDayLoading, _sweetDayFailed;
        string _sweetQuote = "", _sweetDiary = "";
        float _sweetBlinkUntil;
        string _sweetLastReply;
        readonly Dictionary<string, Texture2D> _sweetTextures=new Dictionary<string, Texture2D>();
        VisualElement _sweetModalBody;
        Label _sweetStatus;

        Texture2D SweetTexture(string asset)
        {
            if(!_sweetTextures.TryGetValue(asset,out var texture)){texture=Resources.Load<Texture2D>("SweetNotebook/"+asset);_sweetTextures[asset]=texture;}
            return texture;
        }

        void SweetRect(VisualElement el, float x, float y, float w, float h) { Abs(el, x*S, y*S, w*S, h*S); }
        static float F(JToken t, string k, float fallback=0) => t?[k] != null ? (float)t[k] : fallback;
        JToken SweetSlot(string key, string kind="labels") => (_sweetPageLayout?[kind] as JArray)?.FirstOrDefault(t=>(string)t["key"]==key && F(t,"w")<1440);
        void BuildSweetShell()
        {
            _sweetLayout=JObject.Parse(Resources.Load<TextAsset>("SweetNotebook/layout").text);
            _panel=new VisualElement {name="xq-bench-panel",pickingMode=PickingMode.Ignore}; SweetRect(_panel,0,0,1440,810); _stage.Add(_panel);
            _sweetSurface=new Image {name="xq-sweet-artwork",scaleMode=ScaleMode.StretchToFill,pickingMode=PickingMode.Ignore}; SweetRect(_sweetSurface,0,0,1440,810); _panel.Add(_sweetSurface);
            _sweetLayer=new VisualElement {name="xq-sweet-native",pickingMode=PickingMode.Ignore}; SweetRect(_sweetLayer,0,0,1440,810); _panel.Add(_sweetLayer);
            _rail=new VisualElement {pickingMode=PickingMode.Ignore}; SweetRect(_rail,18,20,76,770); _panel.Add(_rail);
            _header=new VisualElement {pickingMode=PickingMode.Ignore}; SweetRect(_header,130,28,1060,55); _panel.Add(_header);
            _leftHost=new VisualElement(); _bentoHost=new VisualElement(); _charBox=new VisualElement();
            _offBar=new VisualElement(); _offRetry=new VisualElement(); _modeBtn=new VisualElement();
            _petModeBtn=new VisualElement(); _closeModeBtn=new VisualElement(); _refreshBtn=new VisualElement();
            _toast=L("",12,Ink2,false); SweetRect(_toast,500,756,570,30); _toast.style.unityTextAlign=TextAnchor.MiddleCenter;
            _toast.style.backgroundColor=Rgb(255,253,249,.98f); Round(_toast,12*S); _toast.style.opacity=0; _panel.Add(_toast);
            _sweetStatus=L("",10,Ink3,false); SweetRect(_sweetStatus,1040,780,360,15); _sweetStatus.style.fontSize=10*S;
            _sweetStatus.style.unityTextAlign=TextAnchor.MiddleRight; _panel.Add(_sweetStatus);
        }

        VisualElement SweetGroup(JToken slot)
        {
            if(slot?["group"]==null || slot["group"].Type==JTokenType.Null)return _sweetLayer;
            var g=slot["group"]; string name="group-"+F(g,"x")+"-"+F(g,"y");
            var parent=_sweetLayer.Q(name); if(parent!=null)return parent;
            parent=new VisualElement {name=name,pickingMode=PickingMode.Ignore}; SweetRect(parent,F(g,"x"),F(g,"y"),F(g,"w"),F(g,"h"));
            parent.style.rotate=new UnityEngine.UIElements.Rotate(new Angle(F(slot,"angle"),AngleUnit.Degree)); _sweetLayer.Add(parent);return parent;
        }
        Label SweetLabel(string key, string value, bool wrap=true)
        {
            var slot=SweetSlot(key); if(slot==null)return null;
            var parent=SweetGroup(slot);var g=slot["group"];
            var label=SweetText(parent,key,value,F(slot,"x")-(g is JObject?F(g,"x"):0),F(slot,"y")-(g is JObject?F(g,"y"):0),F(slot,"w"),F(slot,"h")+3,F(slot,"cssFont",15),wrap);
            var rgb=System.Text.RegularExpressions.Regex.Matches((string)slot["color"]??"rgb(62,60,89)",@"\d+");
            if(rgb.Count>=3)label.style.color=Rgb(int.Parse(rgb[0].Value),int.Parse(rgb[1].Value),int.Parse(rgb[2].Value));
            if((bool?)slot["bold"]==true)label.style.unityFontStyleAndWeight=FontStyle.Bold;
            label.style.letterSpacing=F(slot,"spacing")*S;
            _sweetLabels[key]=label;return label;
        }
        Label SweetText(VisualElement parent,string key,string value,float x,float y,float w,float h,float size=15,bool wrap=true)
        {
            var label=new Label(value??"") {name="sweet-"+key,pickingMode=PickingMode.Ignore,enableRichText=false};
            SweetRect(label,x,y,w,h);label.style.fontSize=size*S;label.style.color=Rgb(62,60,89);
            label.style.marginLeft=label.style.marginTop=label.style.marginRight=label.style.marginBottom=0;
            label.style.paddingLeft=label.style.paddingTop=label.style.paddingRight=label.style.paddingBottom=0;
            label.style.whiteSpace=wrap?WhiteSpace.Normal:WhiteSpace.NoWrap;
            // Plain labels only. Rounded surfaces are never clipped in the transparent Unity window.
            label.style.overflow=Overflow.Hidden;label.style.textOverflow=TextOverflow.Ellipsis;
            parent.Add(label);return label;
        }
        VisualElement SweetHot(string key,Action action,string tip=null)
        {
            var r=SweetSlot(key,"targets");if(r==null)return null;
            var hot=new VisualElement {name="sweet-hit-"+key,pickingMode=PickingMode.Ignore,tooltip=tip??key};
            SweetRect(hot,F(r,"x"),F(r,"y"),F(r,"w"),F(r,"h"));Round(hot,10*S);_sweetLayer.Add(hot);_sweetTargets[key]=hot;
            Click(hot,action);return hot;
        }
        void SweetSprite(string key,string asset)
        {
            var r=SweetSlot(key,"sprites");if(r==null)return;var parent=SweetGroup(r);var g=r["group"];
            var image=new Image {image=SweetTexture(asset),scaleMode=ScaleMode.ScaleToFit,pickingMode=PickingMode.Ignore};
            SweetRect(image,F(r,"x")-(g is JObject?F(g,"x"):0),F(r,"y")-(g is JObject?F(g,"y"):0),F(r,"w"),F(r,"h"));
            parent.Add(image);_sweetSprites[key]=image;
        }
        void SweetWink(){_sweetBlinkUntil=Time.unscaledTime+1.1f;Toast("(｡•ᴗ-)✧");}
        void RenderSweetPage()
        {
            if(_sweetLayer==null)return;
            if(_bd==null)_bd=XqBenchData.Ensure();
            if(_page>2)_page=0;
            _sweetLayer.Clear();_pageClicks.Clear();_sweetLabels.Clear();_sweetTargets.Clear();_sweetSprites.Clear();
            string id=new[]{"now","journal","memory"}[_page];
            _sweetPageLayout=(_sweetLayout["pages"] as JArray).First(p=>(string)p["id"]==id);
            _sweetSurface.image=SweetTexture(id+"-surface");
            if(_page==0)SweetBuildNow();else if(_page==1)SweetBuildJournal();else SweetBuildMemory();
            SweetHot("nav-now",()=>SelectPage(0),"此刻");SweetHot("nav-journal",()=>SelectPage(1),"生活手账");SweetHot("nav-memory",()=>SelectPage(2),"记忆与成长");
            SweetHot("tools",CampusConsole,"打开老板工具台");SweetHot("pet",()=>XqMode.Set(XqView.Pet),"桌宠");SweetHot("close",()=>XqMode.Set(XqView.Close),"近景");
            SweetTick();
        }
        void SweetBuildNow()
        {
            var clock=SweetLabel("clock",SweetNow.ToString("HH:mm"),false);clock.style.width=110*S;clock.style.fontSize=37*S;
            var date=SweetLabel("date","",true); if(date!=null)date.style.width=300*S;
            var state=_bd.Get("state");var life=_bd.Get("life");
            string quote=XqVoiceChatClient.LatestReplyFull;
            if(string.IsNullOrEmpty(quote))quote=Str(state,"last_reply");
            if(string.IsNullOrEmpty(quote)&&XqCloseData.Inst!=null)quote=XqCloseData.Inst.Say;
            string title="她刚刚说";
            if(string.IsNullOrEmpty(quote)){quote=SweetDiaryBody(_bd.Get("diary"));title="手账里的一句";}
            _sweetQuote=quote??"";
            _sweetLastReply=XqVoiceChatClient.LatestReplyFull;
            SweetLabel("quoteTitle",title,false);
            SweetLabel("quote",_sweetQuote.Length>0?Cut(_sweetQuote,72):"她还没留下新消息。\n今天的小事，等你慢慢说。",true);
            string doing=Str(life,"place.doing");if(doing.Length==0)doing=Str(state,"activity");
            SweetLabel("mood",doing.Length>0?doing:"生活状态同步中…",false);
            SweetLabel("song","",false);SweetLabel("artist","",false);
            var play=SweetLabel("play","",false);play.style.backgroundColor=Rgb(134,127,171);Round(play,16*S);play.style.overflow=Overflow.Visible;play.style.unityTextAlign=TextAnchor.MiddleCenter;
            foreach(string key in new[]{"prevLabel","nextLabel"}){var symbol=SweetLabel(key,key=="prevLabel"?"‹":"›",false);if(symbol!=null)symbol.style.unityTextAlign=TextAnchor.MiddleCenter;}
            // The approved player reserves 125px for track/artist. Long names truncate with full detail on click.
            SweetSprite("chibi","chibi-wink");
            SweetHot("letter",()=>SweetModal("她留下的话",_sweetQuote.Length>0?_sweetQuote:"还没有新消息。"));
            SweetHot("player",()=>SweetModal("一起听",XqMusicBridge.Available?XqMusicBridge.Current.title+"\n"+XqMusicBridge.Current.artist:"打开音乐播放器并播放后，这里会显示系统媒体会话。"));
            SweetHot("prev",()=>{if(XqMusicBridge.CanPrev)XqMusicBridge.Prev();},"上一首");
            SweetHot("play",()=>{if(XqMusicBridge.CanToggle)XqMusicBridge.TogglePlay();},"播放 / 暂停");
            SweetHot("next",()=>{if(XqMusicBridge.CanNext)XqMusicBridge.Next();},"下一首");SweetHot("chibi",SweetWink,"戳戳小祈");
        }
        static string SweetDiaryBody(JToken diary)
        {
            var items=Tk(diary,"diary.items") as JArray;
            if(items==null)return "";
            return string.Join("\n\n",items.Where(t=>Str(t,"text").Length>0).Select(t=>Str(t,"text")));
        }
        void SweetChooseDay(DateTime date)
        {
            _sweetDay=date.Date;_sweetEventPage=0;_sweetHistoryLife=_sweetHistoryDiary=null;_sweetDayFailed=false;
            int request=++_sweetDayRequest;
            if(_sweetDay==SweetToday){_sweetDayLoading=false;RenderSweetPage();return;}
            _sweetDayLoading=true;RenderSweetPage();
            _bd.FetchDay(_sweetDay.ToString("yyyy-MM-dd"),(life,diary,failed)=>{
                if(request!=_sweetDayRequest)return;
                _sweetHistoryLife=life;_sweetHistoryDiary=diary;_sweetDayLoading=false;_sweetDayFailed=failed;
                if(_shown&&_page==1)RenderSweetPage();
            });
        }
        void SweetBuildJournal()
        {
            if(_sweetDay==DateTime.MinValue)_sweetDay=SweetToday;
            if(_sweetWeek==DateTime.MinValue)_sweetWeek=SweetToday.AddDays(-(((int)SweetToday.DayOfWeek+6)%7));
            var today=SweetToday;bool live=_sweetDay==today;
            var life=live?_bd.Get("life"):_sweetHistoryLife;var diary=live?_bd.Get("diary"):_sweetHistoryDiary;
            _sweetDiary=SweetDiaryBody(diary);
            string date=_sweetDay.ToString("MMM. dd / dddd",CultureInfo.InvariantCulture).ToUpperInvariant();
            SweetLabel("date",date,false);SweetLabel("metaDate",_sweetDay.ToString("MM / dd"),false);
            SweetLabel("metaWeek",_sweetDay.ToString("yyyy年 M月 · ddd",CultureInfo.GetCultureInfo("zh-CN")),false);
            SweetLabel("diaryTitle",live?"今天的小小记录":"那天的小小记录",false);
            string empty=_sweetDayLoading?"正在翻找这一天的记录…":_sweetDayFailed?"这天的记录暂时没连上。\n点书页重试。":"这一天，还没有留下手账。";
            SweetLabel("diary",_sweetDiary.Length>0?Cut(_sweetDiary,100):empty,true);
            var frameTitle=SweetLabel("frameTitle",_sweetDay.ToString("MM.dd")+" / 小祈的手账",false);frameTitle.style.width=340*S;
            // Decorative caption, independent of records; never fabricate a diary entry to fill this frame.
            var caption=SweetLabel("frameText","把日子慢慢写下，也把你放在心上。 ♡",false);if(caption!=null)caption.style.width=365*S;
            SweetSprite("chibi","chibi-ivory");SweetSprite("wink","chibi-wink");
            for(int i=0;i<7;i++)
            {
                var d=_sweetWeek.AddDays(i);var slot=SweetSlot("day"+i);float x=F(slot,"x"),y=F(slot,"y");bool on=d==_sweetDay;
                var box=new VisualElement {pickingMode=PickingMode.Ignore};SweetRect(box,x,y,56,62);Round(box,10*S);
                box.style.backgroundColor=on?Rgb(119,116,167):Rgb(255,255,255,.85f);Border(box,1*S,on?Rgb(119,116,167):Rgb(224,226,241));_sweetLayer.Add(box);
                var dow=SweetText(box,"dow",d.ToString("ddd",CultureInfo.InvariantCulture).ToUpperInvariant(),0,8,56,12,9,false);
                var num=SweetText(box,"day",d.Day.ToString(),0,23,56,28,19,false);
                dow.style.unityTextAlign=num.style.unityTextAlign=TextAnchor.MiddleCenter;dow.style.color=num.style.color=on?Color.white:Rgb(93,90,125);
                SweetHot("day"+i,()=>SweetChooseDay(d),d.ToString("yyyy-MM-dd"));
            }
            var weekPrev=SweetText(_sweetLayer,"previous-week","‹",632,224,30,30,24,false);
            var weekNext=SweetText(_sweetLayer,"next-week","›",674,224,30,30,24,false);
            Click(weekPrev,()=>{_sweetWeek=_sweetWeek.AddDays(-7);SweetChooseDay(_sweetWeek);});
            Click(weekNext,()=>{if(_sweetWeek.AddDays(7)<=today){_sweetWeek=_sweetWeek.AddDays(7);SweetChooseDay(_sweetWeek);}else Toast("还没到下一周呢。");});
            var segments=Tk(life,"segments") as JArray;
            var events=segments?.Where(IsMainBenchSeg).ToList()??new List<JToken>();
            int pages=Mathf.Max(1,(events.Count+3)/4);_sweetEventPage=Mathf.Clamp(_sweetEventPage,0,pages-1);
            for(int i=0;i<4;i++)
            {
                int idx=_sweetEventPage*4+i;if(idx>=events.Count)break;var e=events[idx];float y=377+i*57;
                var dot=new VisualElement {pickingMode=PickingMode.Ignore};SweetRect(dot,535,y+7,6,6);Round(dot,3*S);dot.style.backgroundColor=Rgb(179,170,219);_sweetLayer.Add(dot);
                SweetText(_sweetLayer,"event-time-"+i,Str(e,"start"),548,y,42,20,11,false).style.color=Rgb(142,136,166);
                string name=Str(e,"name");if(name.Length==0)name=Str(e,"act");
                SweetText(_sweetLayer,"event-title-"+i,name,594,y-1,244,24,15,false);
                SweetText(_sweetLayer,"event-place-"+i,Str(e,"place"),594,y+23,244,19,11,false).style.color=Rgb(142,136,166);
            }
            if(events.Count==0)SweetText(_sweetLayer,"events-empty",_sweetDayLoading?"日程同步中…":_sweetDayFailed?"日程暂未连接":"这天还没有日程记录。",548,387,270,72,14);
            var pageLabel=SweetLabel("page",(_sweetEventPage+1)+" / "+pages+" · 日程小记",false);pageLabel.style.width=200*S;
            SweetHot("next",()=>{_sweetEventPage=(_sweetEventPage+1)%pages;RenderSweetPage();},"翻四个片刻");
            SweetHot("diary",()=>{if(_sweetDayFailed)SweetChooseDay(_sweetDay);else SweetModal(_sweetDay.ToString("yyyy.MM.dd")+" · 小祈手账",_sweetDiary.Length>0?_sweetDiary:empty);});
            SweetHot("portrait",()=>SweetModal("一页手账",_sweetDiary.Length>0?_sweetDiary:empty));SweetHot("chibi",SweetWink);SweetHot("wink",SweetWink);
        }
        void SweetBuildMemory()
        {
            var files=(Tk(_bd.Get("memory"),"files") as JArray)?.OrderBy(t=>{
                string name=Str(t,"file");return name.StartsWith("user")?0:name.StartsWith("avoid")?1:name.StartsWith("daily")?2:3;
            }).ThenByDescending(t=>Str(t,"file").StartsWith("daily")?Str(t,"file"):"").ToList();int count=files?.Count??0;
            for(int i=0;i<2;i++)
            {
                string key=i==0?"one":"two";JToken file=count>0?files[(_sweetMemoryPage*2+i)%count]:null;
                string tag=file!=null?CleanMemTag(Str(file,"file")):"记忆小事";
                string body=file!=null?CleanMemBody(Str(file,"head"),Str(file,"body")):"有了真实的记忆记录，就认真收在这里。";
                var tab=SweetLabel(key+"Tag",tag,false);tab.style.backgroundColor=i==0?Rgb(221,232,247):Rgb(244,225,238);
                Border(tab,1*S,i==0?Rgb(200,213,235):Rgb(224,202,220));Round(tab,9*S);tab.style.overflow=Overflow.Visible;
                tab.style.unityTextAlign=TextAnchor.MiddleCenter;
                SweetLabel(key+"Title",tag+"，慢慢记得。",false);SweetLabel(key,Cut(body,68),true);
                SweetHot(key,()=>SweetModal(tag,IsOfficeMode()?"办公模式已隐藏记忆正文。":body));
                if(IsOfficeMode())_sweetLabels[key].text="办公模式已隐藏记忆正文。";
            }
            var growth=_bd.Get("growth");string weekly=Str(growth,"weekly"),self=Str(growth,"self_now");
            string text=weekly.Length>0?weekly:self;
            SweetLabel("threeTitle","从记得，到更懂一点。",false);
            SweetLabel("three",IsOfficeMode()?"办公模式已隐藏成长正文。":text.Length>0?Cut(text,72):"成长记录同步后，会在这里慢慢展开。",true);
            SweetSprite("chibi","chibi-heart");SweetHot("chibi",SweetWink);
            SweetHot("three",()=>SweetModal("一起成长",IsOfficeMode()?"办公模式已隐藏成长正文。":text.Length>0?text:"还没有成长记录。"));
            SweetHot("photo",()=>SweetModal("一起收藏的日子","把你随口说过的小事，认真放在心上。",Resources.Load<Texture2D>("SweetNotebook/memory-album")));
            SweetHot("next",()=>{_sweetMemoryPage=(_sweetMemoryPage+1)%Mathf.Max(1,(count+1)/2);RenderSweetPage();},"翻一张小事");
        }
        void SweetTick()
        {
            if(_sweetLayer==null)return;
            if(_page==0)
            {
                if(_sweetLastReply!=XqVoiceChatClient.LatestReplyFull){RenderSweetPage();return;}
                if(_sweetLabels.TryGetValue("clock",out var clock))clock.text=SweetNow.ToString("HH:mm");
                var life=_bd?.Get("life");var state=_bd?.Get("state");
                string place=Str(life,"place.where");if(place.Length==0)place=Str(life,"place.place");if(place.Length==0)place=Str(state,"me.place");
                string temp=Str(life,"weather.temp"),weather=temp.Length>0?temp+"°C "+Str(life,"weather.desc"):"天气待同步";
                if(_sweetLabels.TryGetValue("date",out var date))date.text=SweetNow.ToString("yyyy.MM.dd · ddd",CultureInfo.InvariantCulture).ToUpperInvariant()+"\n"+(place.Length>0?Cut(place,16):"地点待同步")+" / "+weather;
                if(_sweetLabels.TryGetValue("song",out var title))title.text=XqMusicBridge.Available?XqMusicBridge.Current.title:"一起听音乐";
                if(_sweetLabels.TryGetValue("artist",out var artist))artist.text=XqMusicBridge.Available?(string.IsNullOrEmpty(XqMusicBridge.Current.artist)?"系统播放器 · 一起听":XqMusicBridge.Current.artist):"等待播放器连接";
                if(_sweetLabels.TryGetValue("play",out var play)){play.text=XqMusicBridge.IsPlaying?"Ⅱ":"▶";play.style.opacity=XqMusicBridge.CanToggle?1:.35f;}
                foreach(string key in new[]{"prev","play","next"})
                {
                    if(_sweetLabels.TryGetValue(key+"Label",out var symbol))symbol.style.opacity=(key=="prev"?XqMusicBridge.CanPrev:XqMusicBridge.CanNext)?1f:.35f;
                }
            }
            foreach(var pair in _sweetTargets)
            {
                bool hover=OverEl(pair.Value);pair.Value.style.backgroundColor=hover?Rgb(135,128,175,.065f):Color.clear;
            }
            if(_sweetSprites.TryGetValue("chibi",out var chibi))
            {
                bool wink=_sweetBlinkUntil>Time.unscaledTime||(_sweetTargets.TryGetValue("chibi",out var target)&&OverEl(target));
                chibi.image=SweetTexture(wink?(_page==0?"chibi-heart":"chibi-wink"):_page==2?"chibi-heart":_page==0?"chibi-wink":"chibi-ivory");
            }
            if(_sweetStatus!=null)_sweetStatus.text=_bd!=null&&_bd.Loading(_page)?"正在同步…":_bd!=null&&_bd.Offline(_page)?"部分服务未连接 · 当前内容可能为缓存":"";
        }
        bool SweetPagePoll()
        {
            float scroll=Input.mouseScrollDelta.y;
            if(_page==1&&Mathf.Abs(scroll)>.01f&&_sweetTargets.TryGetValue("diary",out var page)&&OverEl(_panel)&&!OverEl(page))
            {
                var life=_sweetDay==SweetToday?_bd.Get("life"):_sweetHistoryLife;var events=Tk(life,"segments") as JArray;
                int pages=Mathf.Max(1,((events?.Count??0)+3)/4);_sweetEventPage=Mathf.Clamp(_sweetEventPage+(scroll<0?1:-1),0,pages-1);RenderSweetPage();
            }
            return false;
        }
        void SweetModal(string title,string body,Texture2D portrait=null)
        {
            DismissPhotoOverlay();
            var modal=new VisualElement {name="xq-photo-modal",pickingMode=PickingMode.Ignore};SweetRect(modal,0,0,1440,810);Round(modal,24*S);modal.style.backgroundColor=Rgb(65,62,86,.22f);_panel.Add(modal);
            _sweetModalBody=new VisualElement {pickingMode=PickingMode.Ignore};SweetRect(_sweetModalBody,390,145,690,540);Round(_sweetModalBody,22*S);Border(_sweetModalBody,1*S,Rgb(214,214,234));_sweetModalBody.style.backgroundColor=Rgb(255,253,250);modal.Add(_sweetModalBody);
            SweetText(_sweetModalBody,"modal-title",title,30,24,590,35,22,false);
            SweetText(_sweetModalBody,"modal-close","×",640,20,30,34,24,false);
            if(portrait!=null){var image=new Image {image=portrait,scaleMode=ScaleMode.ScaleToFit,pickingMode=PickingMode.Ignore};SweetRect(image,30,80,630,370);_sweetModalBody.Add(image);SweetText(_sweetModalBody,"modal-copy",body,30,464,630,40,15);}
            else
            {
                var scroll=new ScrollView(ScrollViewMode.Vertical);SweetRect(scroll,30,85,630,410);_sweetModalBody.Add(scroll);
                var copy=new Label(body??"") {enableRichText=false};copy.style.fontSize=17*S;copy.style.color=Rgb(62,60,89);copy.style.whiteSpace=WhiteSpace.Normal;copy.style.flexShrink=0;scroll.Add(copy);
            }
        }
        void SweetModalPoll(VisualElement modal)
        {
            bool close=Input.GetKeyDown(KeyCode.Escape);
            var closeLabel=_sweetModalBody?.Q("sweet-modal-close");
            bool valid=!OverEl(_sweetModalBody)||OverEl(closeLabel);
            if(Input.GetMouseButtonDown(0)&&valid){_approvedArmEl=modal;_approvedArmAt=Input.mousePosition;}
            if(Input.GetMouseButtonUp(0)&&valid&&_approvedArmEl==modal&&Vector2.Distance(_approvedArmAt,Input.mousePosition)<=8f)close=true;
            if(close){DismissPhotoOverlay();_approvedArmEl=null;}
        }
        internal static bool SweetReviewAction(string key)
        {
            if(_i==null||!_i._sweetTargets.TryGetValue(key,out var target))return false;
            var action=_i.SweetActionAt(target.worldBound.center,out var hit);if(action==null||hit!=target)return false;action();return true;
        }
        Action SweetActionAt(Vector2 point,out VisualElement target)
        {
            target=null;
            for(int i=_pageClicks.Count-1;i>=0;i--)
            {
                var el=_pageClicks[i].Key;
                if(el.panel!=null&&el.resolvedStyle.display!=DisplayStyle.None&&el.worldBound.Contains(point)){target=el;return _pageClicks[i].Value;}
            }
            return null;
        }
        internal static bool SweetReviewHitRegions()
        {
            if(_i==null)return false;
            return _i._sweetTargets.All(p=>_i.SweetActionAt(p.Value.worldBound.center,out var hit)!=null&&hit==p.Value);
        }
        internal static void SweetReviewDecor(bool visible){if(_i?._sweetLayer!=null)_i._sweetLayer.style.visibility=visible?Visibility.Visible:Visibility.Hidden;}
        internal static bool SweetReviewModal() => _i?._panel.Q("xq-photo-modal")!=null;
        internal static bool SweetReviewHistory(bool start=false)
        {
            if(_i==null)return false;
            if(start)_i.SweetChooseDay(SweetToday.AddDays(-1));
            return !_i._sweetDayLoading && _i._sweetHistoryLife!=null && !_i._sweetDayFailed;
        }
        internal static void SweetReviewToday(){if(_i!=null)_i.SweetChooseDay(SweetToday);}
        internal static bool SweetReviewAtPage(int page) => _i!=null&&_i._shown&&_i._page==page;
    }
}
