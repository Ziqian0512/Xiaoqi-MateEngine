using System;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Xiaoqi.Mode;

namespace Xiaoqi.UI2
{
    public partial class XqBenchPanel
    {
        internal static readonly bool CampusPreview = Array.IndexOf(Environment.GetCommandLineArgs(), "-xq-campus-preview") >= 0;
        static DateTime CampusNow => DateTime.UtcNow.AddHours(8);
        static readonly Color CampusInk = new Color(.24f,.20f,.30f,1f);
        static readonly Color CampusSoft = new Color(.48f,.43f,.53f,1f);
        Label _campusTrack, _campusArtist, _campusPlay, _campusTime, _campusClock;
        VisualElement _campusPrevButton, _campusPlayButton, _campusNextButton;
        Image _campusCover;

        VisualElement CampusPaper(VisualElement parent, float x, float y, float width, float height, bool warm = false)
        {
            var paper = Box(width * _k, height * _k, 14f * _k, warm ? new Color(1f,.98f,.93f,.96f) : new Color(.995f,.99f,1f,.94f));
            Abs(paper,x*_k,y*_k,width*_k,height*_k);
            Border(paper, .75f * _k, new Color(1f,1f,1f,.9f));
            paper.style.paddingLeft = paper.style.paddingRight = 17f*_k;
            paper.style.paddingTop = paper.style.paddingBottom = 15f*_k;
            parent.Add(paper); return paper;
        }

        Label CampusCopy(string value, float size, bool bold = false, int limit = 100)
        {
            var label = WrapTxt(value,size,CampusInk,bold,limit);
            label.style.marginTop = 0; label.style.marginBottom = 0;
            return label;
        }

        void BuildCampusRail()
        {
            _rail = Box(64f*_k,PanH*_k,22f*_k,new Color(.985f,.98f,1f,.97f));
            Abs(_rail,0,0,64f*_k,PanH*_k);
            _rail.style.alignItems = Align.Center; _rail.style.paddingTop = 23f*_k; _rail.style.paddingBottom = 18f*_k;
            var logo = L("祈",22f,CampusInk,true); logo.style.marginBottom = 34f*_k; _rail.Add(logo);
            string[] words = { "此刻", "手账", "记忆", "工具" };
            string[] icons = { "spark", "cal", "heart", "gear" };
            for (int i=0;i<words.Length;i++)
            {
                if(i==3) { var spacer = new VisualElement(); spacer.style.flexGrow=1; _rail.Add(spacer); }
                var button = Box(46f*_k,53f*_k,12f*_k,Color.clear);
                button.style.alignItems=Align.Center; button.style.justifyContent=Justify.Center; button.style.marginBottom=14f*_k;
                var glyph = new XqGlyph(icons[i],CampusSoft); glyph.style.width=glyph.style.height=17f*_k;
                button.Add(glyph); var text=L(words[i],9f,CampusSoft,false); text.style.marginTop=6f*_k; button.Add(text);
                _nav.Add(button); _navHover.Add(0f); _navGlyph.Add(glyph); _rail.Add(button);
            }
            _stage.Add(_rail);
        }

        void BuildCampusPanel()
        {
            _panel = new VisualElement { name="xq-bench-panel",pickingMode=PickingMode.Ignore };
            Abs(_panel,(RailW+Gap)*_k,0,PanW*_k,PanH*_k);
            _panel.style.backgroundImage=Background.FromTexture2D(Resources.Load<Texture2D>("XqCampusRoom"));
            _panel.style.backgroundSize=new BackgroundSize(BackgroundSizeType.Cover);
            Round(_panel,23f*_k); Border(_panel,1f,new Color(1f,1f,1f,.85f)); _stage.Add(_panel);
            _centerPortrait=new Image { image=Resources.Load<Texture2D>("XqCampusInori"),scaleMode=ScaleMode.ScaleToFit,pickingMode=PickingMode.Ignore };
            Abs(_centerPortrait,300f*_k,70f*_k,398f*_k,545f*_k); _panel.Add(_centerPortrait);
            _header=new VisualElement { pickingMode=PickingMode.Ignore }; Abs(_header,0,0,PanW*_k,73f*_k); _panel.Add(_header);
            var title=L("小祈的生活",14f,CampusInk,true); Abs(title,28f*_k,22f*_k,220f*_k,24f*_k); _header.Add(title);
            var subtitle=L("港大 · 大一的第一个秋天",10f,CampusSoft,false); Abs(subtitle,28f*_k,46f*_k,300f*_k,18f*_k); _header.Add(subtitle);
            var switcher=Box(220f*_k,33f*_k,17f*_k,new Color(1f,1f,1f,.92f));
            Abs(switcher,750f*_k,23f*_k,220f*_k,33f*_k); switcher.style.flexDirection=FlexDirection.Row; switcher.style.alignItems=Align.Center;
            _petModeBtn=ModePill("桌宠",false,CampusSoft,Color.clear); _closeModeBtn=ModePill("近景",false,CampusSoft,Color.clear);
            _benchModeTag=ModePill("工作台",true,Color.white,new Color(.49f,.43f,.61f));
            switcher.Add(_petModeBtn); switcher.Add(_closeModeBtn); switcher.Add(_benchModeTag); _header.Add(switcher);
            _refreshBtn=Box(30f*_k,30f*_k,15f*_k,new Color(1,1,1,.93f)); Abs(_refreshBtn,978f*_k,25f*_k,30f*_k,30f*_k);
            _refreshBtn.style.alignItems=Align.Center; _refreshBtn.style.justifyContent=Justify.Center; _refreshBtn.Add(L("↻",15f,CampusSoft,false)); _header.Add(_refreshBtn);
            _modeBtn=new VisualElement(); _modeBtn.style.display=DisplayStyle.None; _modeBtnLabel=L("",10f,CampusSoft,false);
            _topClock=L("",12f,CampusInk,false); _charTopText=L("",10f,CampusSoft,false); _charBotText=L("",10f,CampusSoft,false); _metaLabel=L("",10f,CampusSoft,false);
            _charBox=new VisualElement(); _nameplate=new VisualElement();
            _leftHost=new VisualElement { name="xq-bench-left",pickingMode=PickingMode.Ignore };
            _bentoHost=new VisualElement { name="xq-bento-host",pickingMode=PickingMode.Ignore };
            _panel.Add(_leftHost); _panel.Add(_bentoHost);
            _offBar=new VisualElement(); _offBar.style.display=DisplayStyle.None; _offRetry=new VisualElement();
            _toast=L("",10f,CampusSoft,false); _toast.style.display=DisplayStyle.None;
        }

        void CampusOtherPageLayout()
        {
            Abs(_centerPortrait,328f*_k,78f*_k,344f*_k,520f*_k);
        }

        internal static bool CampusVerifyPhoto()
        {
            if (_i == null || _i._page != 0 || _i._pageClicks.Count == 0) return false;
            _i._pageClicks[0].Value();
            bool opened = _i._panel.Q("xq-photo-modal") != null;
            bool closed = DismissPhotoOverlay();
            return opened && closed && _i._panel.Q("xq-photo-modal") == null;
        }

        void CampusLayout()
        {
            Abs(_centerPortrait,300f*_k,70f*_k,398f*_k,545f*_k);
            _centerPortrait.style.display=DisplayStyle.Flex;
            _leftHost.style.display=DisplayStyle.Flex;
            Abs(_leftHost,28f*_k,90f*_k,266f*_k,484f*_k);
            Abs(_bentoHost,720f*_k,90f*_k,274f*_k,484f*_k);
        }

        void BuildCampusNow(bool loading)
        {
            CampusLayout();
            var state=_bd.Get("state"); var life=_bd.Get("life");
            _campusClock=L(CampusNow.ToString("HH:mm"),43f,CampusInk,false);
            Abs(_campusClock,0,0,260f*_k,59f*_k); _leftHost.Add(_campusClock);
            var date=L(CampusNow.ToString("yyyy.MM.dd  ddd").ToUpperInvariant()+" · 香港",10.5f,CampusSoft,false);
            Abs(date,2f*_k,63f*_k,264f*_k,20f*_k); _leftHost.Add(date);
            string place=Str(life,"place.where"); if(place.Length==0)place=Str(life,"place.place"); if(place.Length==0)place=Str(state,"me.place");
            string temp=Str(life,"weather.temp"); string weather=temp.Length>0?temp+"°C "+Str(life,"weather.desc"):"天气暂未同步";
            var location=CampusCopy((place.Length>0?place:"地点暂未同步")+" · "+weather,11f,false,42);
            Abs(location,2f*_k,88f*_k,264f*_k,36f*_k); _leftHost.Add(location);

            var now=CampusPaper(_leftHost,0,134,266,174,true);
            now.Add(L("她的此刻",10.5f,CampusSoft,false));
            string doing=Str(life,"place.doing"); if(doing.Length==0)doing=Str(state,"activity");
            var segments=Tk(life,"segments") as JArray; int current=-1; int minute=CampusNow.Hour*60+CampusNow.Minute; if(CampusNow.Hour<3)minute+=1440;
            if(segments!=null) for(int i=0;i<segments.Count;i++)
                if(Num(segments[i],"a",0)<=minute && minute<Num(segments[i],"b",0)) {current=i;break;}
            if(doing.Length==0 && current>=0)doing=Str(segments[current],"name");
            var activity=CampusCopy(doing.Length>0?doing:"今天的安排，还在同步中。",17f,true,42);
            activity.style.marginTop=11f*_k; now.Add(activity);
            string mood=Str(state,"mood"); if(mood.Length==0)mood=Str(state,"affect.quadrant");
            var moodLabel=CampusCopy(mood.Length>0?"心情 · "+mood:"心情暂未同步",11f,false,28); moodLabel.style.color=CampusSoft; moodLabel.style.marginTop=12f*_k; now.Add(moodLabel);
            var stamp=_bd.StampOf("life");
            var sync=L(loading?"正在同步生活状态…":stamp>DateTime.MinValue?"最近同步 "+stamp.ToUniversalTime().AddHours(8).ToString("HH:mm"):"生活状态暂未同步",9f,CampusSoft,false);
            sync.style.position=Position.Absolute; sync.style.bottom=12f*_k; sync.style.left=17f*_k; now.Add(sync);

            var moments=new VisualElement { pickingMode=PickingMode.Ignore }; Abs(moments,3f*_k,333f*_k,260f*_k,130f*_k); _leftHost.Add(moments);
            moments.Add(L("今天的安排",11f,CampusInk,true));
            if(segments==null||segments.Count==0) {var empty=CampusCopy("日程同步后，这里会留下她一天的片刻。",11.5f,false,50);empty.style.marginTop=14f*_k;moments.Add(empty);}
            else
            {
                int start=Mathf.Max(0,current-1); if(current<0)start=0;
                for(int i=start;i<segments.Count&&i<start+2;i++)
                {
                    var row=CampusCopy(Str(segments[i],"start")+"  "+Str(segments[i],"name"),12f,false,32);
                    row.style.marginTop=15f*_k; moments.Add(row);
                }
            }

            var quote=CampusPaper(_bentoHost,0,0,274,132);
            string quoteTitle="她留下的话";
            string say=XqVoiceChatClient.LatestReplyFull;
            if(string.IsNullOrEmpty(say))say=Str(state,"last_reply");
            if(string.IsNullOrEmpty(say) && XqCloseData.Inst!=null)say=XqCloseData.Inst.Say;
            if(string.IsNullOrEmpty(say))
            {
                var diary=Tk(_bd.Get("diary"),"diary.items") as JArray;
                if(diary!=null)for(int i=diary.Count-1;i>=0;i--)
                {
                    string text=Str(diary[i],"text");
                    if(text.Length>0){say=text;quoteTitle="手账里的一句";break;}
                }
            }
            quote.Add(L(quoteTitle,10.5f,CampusSoft,false));
            var quoteText=CampusCopy(string.IsNullOrEmpty(say)?"她还没留下新消息。":say,13f,false,62);
            quoteText.style.marginTop=11f*_k;quote.Add(quoteText);

            var photos=new VisualElement { pickingMode=PickingMode.Ignore }; Abs(photos,0,151f*_k,274f*_k,184f*_k); _bentoHost.Add(photos);
            CampusPhoto(photos,"XqPhotoStudy","课桌旁",0,0,168,172,-2f);
            CampusPhoto(photos,"XqPhotoDorm","宿舍午后",165,40,108,131,3f);
            var photoNote=L("生活插画 · 点击放大",9f,CampusSoft,false); Abs(photoNote,3f*_k,338f*_k,270f*_k,17f*_k); _bentoHost.Add(photoNote);

            var music=CampusPaper(_bentoHost,0,365,274,112);
            music.style.paddingLeft=music.style.paddingRight=12f*_k;
            var sleeve=Box(44f*_k,44f*_k,9f*_k,new Color(.9f,.87f,.93f,.9f)); Abs(sleeve,12f*_k,13f*_k,44f*_k,44f*_k);
            sleeve.style.alignItems=Align.Center;sleeve.style.justifyContent=Justify.Center;sleeve.Add(L("♪",20f,CampusSoft,false));music.Add(sleeve);
            _campusCover=new Image { image=XqMusicBridge.Cover,scaleMode=ScaleMode.ScaleToFit,pickingMode=PickingMode.Ignore };
            _campusCover.style.position=Position.Absolute; _campusCover.style.width=_campusCover.style.height=Length.Percent(100);sleeve.Add(_campusCover);
            _campusTrack=L("",12f,CampusInk,true); Abs(_campusTrack,67f*_k,12f*_k,192f*_k,23f*_k);_campusTrack.style.whiteSpace=WhiteSpace.NoWrap;_campusTrack.style.overflow=Overflow.Hidden;_campusTrack.style.textOverflow=TextOverflow.Ellipsis;music.Add(_campusTrack);
            _campusArtist=L("",10f,CampusSoft,false);Abs(_campusArtist,67f*_k,37f*_k,192f*_k,18f*_k);music.Add(_campusArtist);
            _campusPrevButton=CampusMusicButton(music,"|‹",87,65,()=>{if(XqMusicBridge.CanPrev)XqMusicBridge.Prev();});
            _campusPlayButton=CampusMusicButton(music,"▶",128,65,()=>{if(XqMusicBridge.CanToggle)XqMusicBridge.TogglePlay();}); _campusPlay=_campusPlayButton.Q<Label>();
            _campusNextButton=CampusMusicButton(music,"›|",169,65,()=>{if(XqMusicBridge.CanNext)XqMusicBridge.Next();});
            _campusTime=L("",8.5f,CampusSoft,false);Abs(_campusTime,12f*_k,79f*_k,74f*_k,18f*_k);music.Add(_campusTime);
            RefreshCampusNow();
        }

        void CampusPhoto(VisualElement parent,string resource,string caption,float x,float y,float width,float height,float angle)
        {
            var frame=Box(width*_k,height*_k,3f*_k,new Color(1f,1f,1f,.99f));Abs(frame,x*_k,y*_k,width*_k,height*_k);
            frame.style.rotate=new UnityEngine.UIElements.Rotate(new Angle(angle,AngleUnit.Degree));
            frame.style.paddingLeft=frame.style.paddingRight=frame.style.paddingTop=5f*_k;frame.style.paddingBottom=24f*_k;
            var img=new Image { image=Resources.Load<Texture2D>(resource),scaleMode=ScaleMode.ScaleAndCrop,pickingMode=PickingMode.Ignore };
            img.style.width=img.style.height=Length.Percent(100);frame.Add(img);
            var label=L(caption,9f,CampusSoft,false);Abs(label,8f*_k,(height-20f)*_k,(width-16f)*_k,16f*_k);frame.Add(label);
            var tape=Box(35f*_k,10f*_k,1f,new Color(.84f,.79f,.68f,.65f));Abs(tape,(width*.5f-17f)*_k,-4f*_k,35f*_k,10f*_k);frame.Add(tape);
            Click(frame,()=>ShowPhotoPreview(caption,resource,"生活插画 · 静态展示素材",Color.white));parent.Add(frame);
        }

        VisualElement CampusMusicButton(VisualElement parent,string text,float x,float y,Action action)
        {
            var button=Box(30f*_k,29f*_k,14.5f*_k,new Color(.92f,.89f,.96f,.9f));Abs(button,x*_k,y*_k,30f*_k,29f*_k);
            button.style.alignItems=Align.Center;button.style.justifyContent=Justify.Center;button.Add(L(text,12f,CampusInk,false));Click(button,action);parent.Add(button);return button;
        }

        void RefreshCampusNow()
        {
            if(!CampusPreview||_page!=0||_campusTrack==null||_campusTrack.panel==null)return;
            _campusClock.text=CampusNow.ToString("HH:mm");var track=XqMusicBridge.Current;
            _campusTrack.text=XqMusicBridge.Available?track.title:"一起听音乐";
            _campusArtist.text=XqMusicBridge.Available?track.artist:"等待系统播放器";
            _campusPlay.text=XqMusicBridge.IsPlaying?"Ⅱ":"▶";_campusCover.image=XqMusicBridge.Cover;
            _campusPrevButton.style.opacity=XqMusicBridge.CanPrev?1f:.35f;_campusPlayButton.style.opacity=XqMusicBridge.CanToggle?1f:.35f;_campusNextButton.style.opacity=XqMusicBridge.CanNext?1f:.35f;
            _campusTime.text=track.durationSec>0?XqMusicBridge.Clock(XqMusicBridge.PositionSec):"--:--";
        }

        static void CampusConsole()
        {
            string server=XiaoqiRuntime.Config?.server?.TrimEnd('/');
            if(!string.IsNullOrEmpty(server))Application.OpenURL(server+"/p/set");
        }
    }
}
