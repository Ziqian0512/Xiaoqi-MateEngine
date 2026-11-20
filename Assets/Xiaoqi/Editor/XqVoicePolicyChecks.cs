using System;
using UnityEngine;
using Xiaoqi.UI2;
using Xiaoqi.Mode;

public static class XqVoicePolicyChecks
{
    static int _checks;
    static void Check(bool passed,string scenario){_checks++;if(!passed)throw new InvalidOperationException("Companion check failed: "+scenario);}
    public static void VerifyAndBuild()
    {
        _checks=0;var cfg=new XiaoqiConfig();
        Check(XqVoiceChatClient.ShouldAutoSpeak(cfg,XqView.Close,false,.9999f),"close speaks");
        Check(!XqVoiceChatClient.ShouldAutoSpeak(cfg,XqView.Bench,false,0),"bench silent");
        Check(!XqVoiceChatClient.ShouldAutoSpeak(cfg,XqView.Close,true,0),"quiet wins");
        cfg.muted=true;Check(!XqVoiceChatClient.ShouldAutoSpeak(cfg,XqView.Close,false,0),"mute wins");
        cfg.muted=false;cfg.office_mode=true;Check(!XqVoiceChatClient.ShouldAutoSpeak(cfg,XqView.Close,false,0),"privacy wins");
        cfg.office_mode=false;cfg.voice_tts_auto=false;Check(XqVoiceChatClient.ShouldAutoSpeak(cfg,XqView.Close,false,0),"no hidden legacy master overrides close");
        var rule=XqConsoleVoicePolicy.Parse("{\"budget\":{\"voice\":{\"enabled\":true,\"probability\":0.12}}}");
        Check(rule!=null&&Mathf.Abs(rule.probability-.12f)<.0001f,"console contract");
        Check(XqConsoleVoicePolicy.Decide(rule,true,.119f),"configured probability below threshold");
        Check(!XqConsoleVoicePolicy.Decide(rule,true,.12f),"configured threshold exclusive");
        Check(!XqConsoleVoicePolicy.Decide(rule,false,0),"stale policy silent");
        rule.enabled=false;Check(!XqConsoleVoicePolicy.Decide(rule,true,0),"console disabled");
        rule.enabled=true;rule.probability=0;Check(!XqConsoleVoicePolicy.Decide(rule,true,0),"zero silent");
        rule.probability=1;Check(XqConsoleVoicePolicy.Decide(rule,true,1),"one always");
        foreach(var json in new[]{"{}","broken","{\"budget\":{\"voice\":{\"enabled\":true}}}","{\"budget\":{\"voice\":{\"enabled\":true,\"probability\":1.1}}}","{\"budget\":{\"voice\":{\"enabled\":true,\"probability\":-0.1}}}"})Check(XqConsoleVoicePolicy.Parse(json)==null,"invalid console policy");
        var h=new XqPetConversation();h.Put(1,false,"one",0);h.Put(2,true,"a",1);h.Put(2,true,"a complete sentence",2);
        Check(h.Messages.Count==2&&h.Messages[1].text=="a complete sentence","stream updates same message");
        h.Put(3,false,"three",3);h.Put(4,true,"four",4);Check(h.Messages.Count==3&&h.Messages[0].id==2,"both roles share three slots oldest first");
        h.Tick(11.99f,false);Check(h.Messages.Count==3,"visible before eight seconds");h.Tick(12,false);Check(h.Messages.Count==0,"expires at eight seconds");
        h.Put(5,true,new string('x',2000),20);h.Tick(100,true);Check(h.Messages[0].text.Length==2000,"busy keeps complete long message");h.Tick(107.9f,false);Check(h.Messages.Count==1,"eight seconds after activity");h.Tick(108,false);Check(h.Messages.Count==0,"no stale reply reappears");
        var feed=new XqPetFeedBuffer();feed.Wait("same reply",1);Check(feed.Take(2,true,"")==null,"feed waits for direct reply");Check(feed.Take(3,false,"same reply")==null,"early HTTP echo deduplicated");
        feed.Wait("a separate message",4);Check(feed.Take(5,false,"other reply")=="a separate message","unrelated feed retained");Check(feed.Take(6,false,"other reply")==null,"pending feed delivered once");
        feed.Wait("expired",7);Check(feed.Take(38,false,"")==null,"expired pending feed discarded");feed.Wait("old mode",40);feed.Clear();Check(feed.Take(41,false,"")==null,"mode change clears pending feed");
        Check(XqGlobalVoiceKey.MatchesChord(0x4e,true,false,false,false),"Ctrl+N accepted");
        Check(!XqGlobalVoiceKey.MatchesChord(0x4e,false,false,false,false),"plain N untouched");
        Check(!XqGlobalVoiceKey.MatchesChord(0x4e,true,false,true,false),"Ctrl+Shift+N untouched");
        Check(!XqGlobalVoiceKey.MatchesChord(0x4e,true,true,false,false),"Ctrl+Alt+N untouched");
        Check(!XqGlobalVoiceKey.MatchesChord(0x56,true,false,false,false),"other shortcut untouched");
        Debug.Log("XQ_COMPANION_BEHAVIOR checks="+_checks+" passed");
        XiaoqiBuild.Win64();
    }
}
