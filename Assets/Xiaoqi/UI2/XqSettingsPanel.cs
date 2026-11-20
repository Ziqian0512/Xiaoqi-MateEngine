using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Xiaoqi.Host;

namespace Xiaoqi.UI2
{
    // One opaque, shared settings surface. No nested legacy glass windows.
    public static class XqSettingsPanel
    {
        public static string[] PageNames => new[]{"相处习惯","语音与消息","桌面显示","更多与维护"};
        public const float CardW=576;
        static VisualElement _host,_card,_side,_header,_footer;
        static ScrollView _body;
        static Label _title,_subtitle,_diagnostics,_voiceRule;
        static readonly Dictionary<VisualElement,Action> Actions=new Dictionary<VisualElement,Action>();
        sealed class Drag { public VisualElement element; public Action<float> apply; public Action save; }
        static readonly List<Drag> Drags=new List<Drag>();
        static VisualElement _pressed;
        static Drag _drag;
        static Vector2 _down;
        static int _page,_openFrame,_swallow=-100;
        static string _sub="",_connection="尚未测试",_connectionServer,_connectionToken;
        static bool _advanced,_testing,_hooked;
        static float _u=1,_stageAt=-999,_nextStatus;
        static readonly Color Ink=new Color(.27f,.23f,.34f),Muted=new Color(.57f,.52f,.62f),Accent=new Color(.52f,.44f,.69f),Line=new Color(.90f,.87f,.93f);
        static XiaoqiConfig Cfg => XiaoqiRuntime.Config;
        public static bool OpenNow {get;private set;}
        public static bool CascadeShown => false;
        public static bool OverNow => OpenNow&&_card?.panel!=null&&_card.worldBound.Contains(XqPanelXf.FromScreen(_card.panel,Input.mousePosition));
        public static bool Swallow
        {
            get { if(_swallow<0)return false;if(Input.GetMouseButton(0)||Time.frameCount<=_swallow+3)return true;_swallow=-100;return false; }
        }
        public static void CascadeTick(Rect a,Rect b,bool c,float d,float e,float f){}
        public static void OpenCascade(int page){OpenSection(Mathf.Clamp(page,0,3));}
        // Existing integration callers still use the old six-category indexes.
        public static void OpenPage(int legacy){OpenSection(legacy==1?0:legacy==3?1:legacy==0||legacy==2?2:3);if(legacy==5)ShowSub("about");}
        public static void Open(){OpenSection(_page);}
        public static void CloseIfOpen(){OpenNow=false;if(_host!=null)_host.style.display=DisplayStyle.None;_pressed=null;_drag=null;}
        static void OpenSection(int page)
        {
            if(Cfg==null)return;
            if(_host==null&&!Build())return;
            XqGlassMenu.CloseCurrent("settings");XiaoqiAbout.Hide();
            _page=page;_sub="";_advanced=false;OpenNow=true;_openFrame=Time.frameCount;_host.style.display=DisplayStyle.Flex;_host.BringToFront();Render();Place();
        }
        static bool Build()
        {
            var doc=XqClosePanel.PickDoc();if(doc==null)return false;
            _host=new VisualElement{name="xq-care-settings",pickingMode=PickingMode.Ignore};_host.style.position=Position.Absolute;_host.style.left=_host.style.top=_host.style.right=_host.style.bottom=0;doc.rootVisualElement.Add(_host);
            _u=XqPanelXf.UnitsPerPx(doc.rootVisualElement.panel);
            if(Application.platform==RuntimePlatform.WindowsPlayer)_host.style.unityFont=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei UI","Microsoft YaHei"},14);
            _card=Node(_host,"care-panel");_card.pickingMode=PickingMode.Position;_card.style.backgroundColor=new Color(.988f,.980f,.996f,1);XqVoiceOverlay.Radius(_card,22*_u);Border(_card,Line,1);
            _side=Node(_card,"care-sidebar");_side.style.backgroundColor=new Color(.94f,.92f,.96f);XqVoiceOverlay.Radius(_side,20*_u);
            _header=Node(_card,"care-header");_footer=Node(_card,"care-footer");
            _body=new ScrollView(ScrollViewMode.Vertical){name="care-content"};_body.style.position=Position.Absolute;_body.horizontalScrollerVisibility=ScrollerVisibility.Hidden;_body.verticalScrollerVisibility=ScrollerVisibility.Auto;_card.Add(_body);
            XqVoiceOverlay.ClipScroll(_body,_u);
            _body.contentContainer.style.paddingRight=6*_u;
            if(!_hooked){_hooked=true;XiaoqiLink.OnStage+=_=>_stageAt=Time.unscaledTime;}
            _host.schedule.Execute(()=>{if(OpenNow){Place();UpdateStatus();}}).Every(100);
            return true;
        }
        static VisualElement Node(VisualElement parent,string name){var e=new VisualElement{name=name,pickingMode=PickingMode.Ignore};e.style.position=Position.Absolute;parent.Add(e);return e;}
        static void Border(VisualElement e,Color color,float width){e.style.borderTopColor=e.style.borderRightColor=e.style.borderBottomColor=e.style.borderLeftColor=color;e.style.borderTopWidth=e.style.borderRightWidth=e.style.borderBottomWidth=e.style.borderLeftWidth=width*_u;}
        static void Rect(VisualElement e,float x,float y,float w,float h){e.style.left=x*_u;e.style.top=y*_u;e.style.width=w*_u;e.style.height=h*_u;}
        static Label Text(VisualElement p,string text,float size=13,Color? color=null)
        {
            var l=new Label(text){enableRichText=false,pickingMode=PickingMode.Ignore};l.style.fontSize=size*_u;l.style.color=color??Ink;l.style.whiteSpace=WhiteSpace.Normal;l.style.marginLeft=l.style.marginTop=l.style.marginRight=l.style.marginBottom=0;l.style.paddingLeft=l.style.paddingTop=l.style.paddingRight=l.style.paddingBottom=0;p.Add(l);return l;
        }
        static VisualElement Button(VisualElement p,string text,Action action,bool filled=false)
        {
            var e=new VisualElement{pickingMode=PickingMode.Position};e.style.minHeight=32*_u;e.style.paddingLeft=e.style.paddingRight=12*_u;e.style.justifyContent=Justify.Center;e.style.backgroundColor=filled?Accent:new Color(.95f,.93f,.97f);XqVoiceOverlay.Radius(e,10*_u);
            var label=Text(e,text,12,filled?Color.white:Ink);label.style.unityTextAlign=TextAnchor.MiddleCenter;p.Add(e);Actions[e]=action;return e;
        }
        static void Save(){XiaoqiConfig.Save(Cfg);}
        static void HostSave(){PetHost.I?.SaveSettings();}
        static VisualElement Row(string title,string hint=null)
        {
            var row=new VisualElement{name="care-row",pickingMode=PickingMode.Ignore};row.style.minHeight=(hint==null?48:62)*_u;row.style.paddingTop=row.style.paddingBottom=9*_u;row.style.borderBottomWidth=_u;row.style.borderBottomColor=Line;row.style.flexShrink=0;_body.Add(row);
            var text=Text(row,title);text.style.width=226*_u;if(hint!=null){var h=Text(row,hint,10,Muted);h.style.marginTop=5*_u;h.style.width=226*_u;}return row;
        }
        static void Toggle(string title,string hint,bool value,Action<bool> set)
        {
            var row=Row(title,hint);var toggle=Button(row,value?"开":"关",()=>{set(!value);Save();Render();},value);toggle.style.position=Position.Absolute;toggle.style.right=0;toggle.style.top=13*_u;toggle.style.width=48*_u;
        }
        static void Choice(string title,string[] options,int index,Action<int> set)
        {
            var row=Row(title);var button=Button(row,options[Mathf.Clamp(index,0,options.Length-1)]+"  ›",()=>{set((index+1)%options.Length);Save();Render();});button.style.position=Position.Absolute;button.style.right=0;button.style.top=8*_u;button.style.minWidth=94*_u;button.tooltip="点击切换选项";
        }
        static void SliderRow(string title,float min,float max,float value,Action<float> set,Action commit,Func<float,string> format)
        {
            var row=Row(title);row.style.minHeight=76*_u;var label=Text(row,format(value),11,Muted);label.style.position=Position.Absolute;label.style.right=0;label.style.top=9*_u;
            var hit=new VisualElement{pickingMode=PickingMode.Position};hit.style.position=Position.Absolute;Rect(hit,0,36,344,24);row.Add(hit);
            var line=Node(hit,"slider-track");Rect(line,0,10,344,4);line.style.backgroundColor=Line;XqVoiceOverlay.Radius(line,2*_u);
            var fill=Node(line,"slider-fill");fill.style.left=fill.style.top=fill.style.bottom=0;fill.style.width=Length.Percent(100*Mathf.InverseLerp(min,max,value));fill.style.backgroundColor=Accent;
            var knob=Node(hit,"slider-thumb");knob.style.backgroundColor=Accent;XqVoiceOverlay.Radius(knob,7*_u);Rect(knob,338*Mathf.InverseLerp(min,max,value),5,14,14);
            Drags.Add(new Drag{element=hit,apply=t=>{float v=Mathf.Lerp(min,max,t);set(v);fill.style.width=Length.Percent(100*t);knob.style.left=338*t*_u;label.text=format(v);},save=()=>{commit?.Invoke();Save();}});
        }
        static void Note(string text)
        {
            var l=Text(_body,text,11,Muted);l.style.marginTop=12*_u;l.style.marginBottom=12*_u;l.style.flexShrink=0;
        }
        static void Link(string title,string hint,Action action)
        {
            var row=Row(title,hint);var b=Button(row,"打开 ↗",action);b.style.position=Position.Absolute;b.style.right=0;b.style.top=12*_u;
        }
        static void ShowSub(string sub){_sub=sub;_advanced=false;Render();}
        static void Render()
        {
            Actions.Clear();Drags.Clear();_pressed=null;_drag=null;_side.Clear();_header.Clear();_footer.Clear();_body.Clear();_diagnostics=null;_voiceRule=null;
            var brand=Text(_side,"祈  /  身边偏好",12,Accent);brand.style.position=Position.Absolute;Rect(brand,18,25,125,25);
            for(int i=0;i<4;i++){int page=i;var tab=Button(_side,PageNames[i],()=>{_page=page;_sub="";_advanced=false;Render();},i==_page);tab.style.position=Position.Absolute;Rect(tab,12,88+i*49,121,39);}
            var little=Text(_side,"把习惯，\n调成舒服的样子。",11,Muted);little.style.position=Position.Absolute;little.style.left=18*_u;little.style.bottom=25*_u;
            string title=_sub=="privacy"?"安静与隐私":_sub=="about"?"关于与许可":PageNames[_page];
            _title=Text(_header,title,20);_title.style.unityFontStyleAndWeight=FontStyle.Bold;_title.style.position=Position.Absolute;Rect(_title,0,19,310,29);
            _subtitle=Text(_header,_sub!=""?"本机偏好 / "+PageNames[_page]:new[]{"日常的小动作，按你的习惯来。","你说的话，我会认真听。","让小祈，在桌面上待得刚刚好。","只保留本机需要的照顾与维护。"}[_page],11,Muted);_subtitle.style.position=Position.Absolute;Rect(_subtitle,0,53,350,22);
            var close=Button(_header,"×",()=>{CloseIfOpen();_swallow=Time.frameCount;});close.style.position=Position.Absolute;Rect(close,342,15,30,30);
            var about=Button(_footer,_sub!=""?"‹ 返回":"关于与许可",()=>{if(_sub!=""){_sub="";Render();}else ShowSub("about");});about.style.position=Position.Absolute;Rect(about,0,3,110,29);
            var saved=Text(_footer,"更改即时生效 · 自动保存",10,Muted);saved.style.position=Position.Absolute;Rect(saved,166,10,205,20);
            if(_sub=="privacy")Privacy();else if(_sub=="about")About();else if(_page==0)Together();else if(_page==1)Voice();else if(_page==2)Desktop();else More();
            _body.scrollOffset=Vector2.zero;UpdateStatus(true);
        }
        static void Together()
        {
            var c=Cfg;
            Toggle("看向鼠标","眼神轻轻跟着你的指针",c.look_at_mouse,v=>{c.look_at_mouse=v;XqSettingsApply.SetLookAtMouse(v);HostSave();});
            Toggle("双击摸摸头","手动摸头始终可独立回应",c.pat_double,v=>{c.pat_double=v;XqSettingsApply.SetPatDouble(v);});
            Toggle("自动坐在窗口上","窗口合适时，在窗边陪你",c.window_sit,v=>{c.window_sit=v;XqSettingsApply.SetWindowSit(v);});
            Toggle("随音乐跳舞","跟随系统检测到的音乐活动",c.dance_on_music,v=>{c.dance_on_music=v;XqSettingsApply.SetDancing(v);HostSave();});
            Advanced();if(!_advanced)return;
            SliderRow("看向力度",0,1,c.look_strength,v=>{c.look_strength=v;XqSettingsApply.SetLookStrength(v);},HostSave,v=>Mathf.RoundToInt(v*100)+"%");
            int[] mins={0,1,3,5,10};Choice("闲置休息",new[]{"关闭","1 分钟","3 分钟","5 分钟","10 分钟"},Mathf.Max(0,Array.IndexOf(mins,c.idle_sleep_min)),i=>{c.idle_sleep_min=mins[i];XqSettingsApply.SetIdleSleep(mins[i]);});
            Note("闲置休息是桌面动作，不会修改小祈的云端作息。");
            Toggle("随心情变化表情","不影响手动摸头与互动",c.emotion_line,v=>c.emotion_line=v);
        }
        static void Voice()
        {
            Note("Ctrl+N  ·  全局按住说话，松开发送\nEsc 取消录音。桌宠模式只通过这组快捷键输入。");
            Toggle("静音小祈","只影响小祈，不改变音乐播放器音量",Cfg.muted,XqSettingsApply.SetMuted);
            SliderRow("小祈的音量",0,1,Cfg.volume,XqSettingsApply.SetVolume,()=>XqSettingsApply.CommitVolume(Cfg.volume),v=>Mathf.RoundToInt(v*100)+"%");
            Note("桌宠的回复始终显示文字，是否同时发声读取控制台设置的概率。近景主动对话保留语音陪伴。");
            _voiceRule=Text(_body,XqConsoleVoicePolicy.Summary,11,Accent);_voiceRule.style.marginTop=8*_u;
            Note("对话只留最近三条，上方较旧；长句自动换行。\n连续交谈时保持显示，结束后 8 秒隐去。\n静音、勿扰和办公隐私优先于自动发声。");
        }
        static void Desktop()
        {
            var h=PetHost.I;var c=Cfg;
            SliderRow("桌宠大小",.5f,2f,h?.Size??1,v=>{if(h!=null)h.Size=v;},HostSave,v=>v.ToString("0.00")+" ×");
            Toggle("保持置顶","让小祈留在普通窗口前面",h?.Topmost??c.topmost,v=>{c.topmost=v;if(h!=null)h.Topmost=v;HostSave();});
            Toggle("全屏应用时隐藏","游戏或演示全屏时让开画面",c.hide_on_fullscreen,v=>c.hide_on_fullscreen=v);
            Choice("小祈外出时",new[]{"仍然显示","暂时隐藏"},c.pet_when_out=="hide"?1:0,i=>c.pet_when_out=i==1?"hide":"show");
            Advanced();if(!_advanced)return;
            Toggle("随系统启动","仅影响这台电脑",h?.StartWithWindows??false,v=>{if(h!=null)h.StartWithWindows=v;HostSave();});
            Choice("画质",new[]{"低","中","高"},Mathf.Clamp(h?.Quality??1,0,2),i=>{if(h!=null)h.Quality=i;HostSave();});
            int[] fps={30,60,90,144};Choice("帧率",new[]{"30 FPS","60 FPS","90 FPS","144 FPS"},Mathf.Max(0,Array.IndexOf(fps,h?.Fps??60)),i=>{if(h!=null)h.Fps=fps[i];HostSave();});
            bool chibi=false;if(h!=null)foreach(var a in h.Actions)if(a.Id=="chibi")chibi=true;
            if(chibi)Toggle("Q 版头身比例","使用当前模型支持的比例切换",h.IsChibi,v=>{if(h.IsChibi!=v)h.RunAction("chibi");});
        }
        static void Advanced()
        {
            var b=Button(_body,_advanced?"收起高级选项":"展开高级选项",()=>{_advanced=!_advanced;Render();});b.style.marginTop=16*_u;b.style.flexShrink=0;
        }
        static void More()
        {
            _diagnostics=Text(_body,"正在读取本机状态…",11,Muted);_diagnostics.style.marginBottom=9*_u;
            Link("安静与隐私","临时勿扰与办公隐私分别管理",()=>ShowSub("privacy"));
            Link("互动日志","打开本机互动日志所在目录",()=>OpenPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Xiaoqi")));
            Link("重新加载当前形象","仅用于异常恢复，不是换装",()=>
            {
                var path=Cfg.model_path;
                if(!Path.IsPathRooted(path))path=Path.GetFullPath(Path.Combine(Application.dataPath,"..",path));
                if(PetHost.I!=null&&PetHost.I.Ready&&File.Exists(path)){PetHost.I.LoadModel(path);XqVoiceOverlay.Notice("正在重新加载当前形象。");}
                else XqVoiceOverlay.Notice("当前形象文件不可读取，请检查本机路径。");
            });
            Link("打开已有控制台","人设、模型、预算与云端规则在这里管理",()=>{Uri url;if(Uri.TryCreate(Cfg.server,UriKind.Absolute,out url)&&(url.Scheme=="http"||url.Scheme=="https"))Application.OpenURL(Cfg.server.TrimEnd('/')+"/p/set");});
            Advanced();if(!_advanced)return;
            Note("连接仅在本机保存。测试只读取状态，不会发送对话或启动其他项目。");
            Field("控制台地址",Cfg.server,false,v=>{Cfg.server=v.Trim().TrimEnd('/');_connection="地址已改变，请重新测试";Save();});
            Field("访问令牌",Cfg.token,true,v=>{Cfg.token=v.Trim();_connection="令牌已改变，请重新测试";Save();});
            var test=Button(_body,_testing?"正在测试连接…":"测试状态连接",()=>{if(!_testing){_testing=true;XqVoiceChatClient.Ensure().StartCoroutine(TestConnection());Render();}});test.style.marginTop=10*_u;
            Note(_connection);
        }
        static void Field(string title,string value,bool secret,Action<string> set)
        {
            Text(_body,title,11,Muted).style.marginTop=10*_u;var f=new TextField{value=value??"",isPasswordField=secret,isDelayed=true};f.style.marginTop=6*_u;f.style.minHeight=30*_u;f.style.fontSize=12*_u;_body.Add(f);f.RegisterValueChangedCallback(e=>set(e.newValue));
            var input=f.Q(className:"unity-text-field__input");if(input!=null){input.style.backgroundColor=Color.white;input.style.color=Ink;Border(input,Line,1);XqVoiceOverlay.Radius(input,7*_u);}
        }
        static IEnumerator TestConnection()
        {
            string server=Cfg.server,token=Cfg.token;XqHttp.Resp result=default(XqHttp.Resp);
            yield return XqHttp.Get(server.TrimEnd('/')+"/api/body/stage",token,8,r=>result=r);
            bool valid=false;try{valid=result.ok&&JObject.Parse(result.text)["body"]!=null;}catch{}
            _connection=valid?"状态接口可读取；对话、识别和合成需分别验证。":"状态连接未通过，请检查地址、令牌与服务。";
            _connectionServer=server;_connectionToken=token;_testing=false;if(OpenNow&&_page==3&&_sub=="")Render();
        }
        static void Privacy()
        {
            Toggle("静音小祈","保留文字；不改变音乐播放器",Cfg.muted,XqSettingsApply.SetMuted);
            Note("勿扰暂停主动消息和自动语音；你主动发起的对话仍显示文字。");
            foreach(int m in new[]{30,60}){int minutes=m;var b=Button(_body,"安静 "+minutes+" 分钟",()=>{XiaoqiRuntime.SetQuietMinutes(minutes);Render();});b.style.marginBottom=8*_u;}
            Button(_body,"一直安静，直到我手动恢复",()=>{XiaoqiRuntime.SetQuietForever();Render();});
            Note(XiaoqiRuntime.Dnd?XiaoqiRuntime.QuietLabel():"目前没有开启勿扰");
            if(XiaoqiRuntime.Dnd)Button(_body,"恢复主动陪伴",()=>{XiaoqiRuntime.ClearQuiet();Render();},true);
            Toggle("办公隐私","隐藏主动私密消息，暂停自动语音；手动对话仍显示文字",Cfg.office_mode,v=>Cfg.office_mode=v);
        }
        static void About()
        {
            Text(_body,"小祈 Xiaoqi  ·  "+Application.version,15,Accent);Note("桌面部分基于 Mate Engine（作者 Shinymoon）修改，按 Mate Engine 许可证 v2.1 分发：不得用于商业用途；修改后的源码按同一许可证公开。");
            Link("Mate Engine 许可全文","查看随应用分发的原始许可证",()=>OpenPath(Path.Combine(Application.streamingAssetsPath,"Xiaoqi","MATE-ENGINE-LICENSE.txt")));
            Link("公开源码","Ziqian0512 / Xiaoqi-MateEngine",()=>Application.OpenURL("https://github.com/Ziqian0512/Xiaoqi-MateEngine/tree/xiaoqi/p2-agy4"));
            Note("当前形象：16.vrm\n模型授权与素材来源见随包 docs/ASSETS.md。\n本页与偏好共用同一张面板，不再打开旧版窗口。");
            var llama=Path.Combine(Application.streamingAssetsPath,"LLAMA 3.2 COMMUNITY LICENSE.txt");if(File.Exists(llama))Link("LLAMA 3.2 许可","查看随包许可",()=>OpenPath(llama));
        }
        static void OpenPath(string path){if(File.Exists(path)||Directory.Exists(path))Application.OpenURL(new Uri(path).AbsoluteUri);else XqVoiceOverlay.Notice("这个本机文件或目录尚未生成。");}
        static string Recent(float at)=>at<0?"尚未验证":Time.unscaledTime-at<120?"最近成功":"此前成功，待重验";
        static void UpdateStatus(bool force=false)
        {
            if(!force&&Time.unscaledTime<_nextStatus)return;_nextStatus=Time.unscaledTime+.5f;
            if(_voiceRule!=null)_voiceRule.text=XqConsoleVoicePolicy.Summary;
            if(_diagnostics!=null)_diagnostics.text="状态同步："+Recent(_stageAt)+"    对话："+Recent(XqVoiceChatClient.LastChatSuccessAt)+"\n语音识别："+Recent(XqVoiceChatClient.LastSttSuccessAt)+"    声音播放："+Recent(XqVoiceChatClient.LastTtsSuccessAt)+"\nCtrl+N："+(XqGlobalVoiceKey.Available?"已启用":"尚未启用")+"  ·  各通道独立验证";
        }
        static void Place()
        {
            if(_card?.panel==null)return;
            var p=_card.panel;float u=XqPanelXf.UnitsPerPx(p);
            if(Mathf.Abs(_u-u)>.001f){_u=u;Render();}
            var body=XqBodyAnchor.Stable();var wa=XqMonitor.WorkArea(body.center);var lt=XqPanelXf.FromScreen(p,new Vector2(wa.xMin,wa.yMax));var rb=XqPanelXf.FromScreen(p,new Vector2(wa.xMax,wa.yMin));
            float w=Mathf.Min(576,(rb.x-lt.x)/_u-24),h=Mathf.Min(566,(rb.y-lt.y)/_u-24);_card.style.left=(lt.x+rb.x-w*_u)/2;_card.style.top=(lt.y+rb.y-h*_u)/2;_card.style.width=w*_u;_card.style.height=h*_u;
            Rect(_side,0,0,145,h);Rect(_header,165,0,w-185,84);Rect(_body,165,94,w-185,h-144);Rect(_footer,165,h-40,w-185,38);
            XqVoiceOverlay.ClipScroll(_body,_u);
        }
        public static bool HandlePointer()
        {
            if(!OpenNow||_card?.panel==null)return false;
            var p=XqPanelXf.FromScreen(_card.panel,Input.mousePosition);bool over=_card.worldBound.Contains(p);
            if(Input.GetKeyDown(KeyCode.Escape)){CloseIfOpen();_swallow=Time.frameCount;return true;}
            if(Time.frameCount<=_openFrame+1)return over;
            if(Input.GetMouseButtonDown(0))
            {
                if(!over){CloseIfOpen();_swallow=Time.frameCount;return true;}
                _down=Input.mousePosition;_pressed=HitAction(p);
                foreach(var d in Drags)if(Usable(d.element)&&d.element.worldBound.Contains(p)&&_body.worldBound.Contains(p)){_drag=d;_drag.apply(Mathf.InverseLerp(d.element.worldBound.xMin,d.element.worldBound.xMax,p.x));}
            }
            if(_drag!=null&&Input.GetMouseButton(0))_drag.apply(Mathf.InverseLerp(_drag.element.worldBound.xMin,_drag.element.worldBound.xMax,p.x));
            if(Input.GetMouseButtonUp(0))
            {
                if(_drag!=null){_drag.save();_drag=null;}
                else if(_pressed!=null&&HitAction(p)==_pressed&&Vector2.Distance(_down,Input.mousePosition)<8){var action=Actions[_pressed];_pressed=null;action();}
                _pressed=null;
            }
            return over;
        }
        static bool Usable(VisualElement e)
        {
            if(!e.enabledInHierarchy)return false;
            for(var n=e;n!=null;n=n.parent)if(n.resolvedStyle.display==DisplayStyle.None)return false;
            // Hidden scroll content must not activate through the footer/header.
            return !_body.Contains(e)||_body.worldBound.Overlaps(e.worldBound);
        }
        static VisualElement HitAction(Vector2 p)
        {
            VisualElement hit=null;
            foreach(var e in Actions.Keys)if(Usable(e)&&e.worldBound.Contains(p)&&(!_body.Contains(e)||_body.worldBound.Contains(p)))hit=e;
            return hit;
        }
        internal static bool ReviewActivate(string label)
        {
            foreach(var e in Actions.Keys)if(e.Q<Label>()?.text==label&&HitAction(e.worldBound.center)==e){var action=Actions[e];action();return true;}
            return false;
        }
        internal static JObject Review()
        {
            var r=_card.worldBound;var buttons=new JArray();foreach(var e in Actions.Keys)buttons.Add(new JObject{["text"]=e.Q<Label>()?.text,["usable"]=Usable(e)});
            var v=_body.contentViewport.worldBound;return new JObject{["page"]=_page,["subpage"]=_sub,["bounds"]=new JArray(r.x,r.y,r.width,r.height),["viewport"]=new JArray(v.x,v.y,v.width,v.height),["scrollRange"]=_body.verticalScroller.highValue,["scrollOffset"]=_body.scrollOffset.y,["clippedViewport"]=_body.contentViewport.style.overflow.value==Overflow.Hidden,["backgroundAlpha"]=_card.resolvedStyle.backgroundColor.a,["buttons"]=buttons};
        }
        internal static void ReviewOpen(int page,string sub="",bool advanced=false){OpenSection(page);if(sub!="")ShowSub(sub);if(advanced){_advanced=true;Render();}}
        internal static void ReviewScrollBottom(){_body.verticalScroller.value=_body.verticalScroller.highValue;}
    }
}
