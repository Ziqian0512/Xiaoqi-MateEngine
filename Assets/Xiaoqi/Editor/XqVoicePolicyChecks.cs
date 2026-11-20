using System;
using UnityEngine;
using Xiaoqi.UI2;
using Xiaoqi.Mode;

public static class XqVoicePolicyChecks
{
    static void Check(bool passed, string scenario)
    { if (!passed) throw new InvalidOperationException("Voice policy failed: " + scenario); }
    public static void VerifyAndBuild()
    {
        var cfg = new XiaoqiConfig();
        Check(XqVoiceChatClient.ShouldAutoSpeak(cfg, XqView.Close, false, 0.9999f), "close always speaks");
        Check(XqVoiceChatClient.ShouldAutoSpeak(cfg, XqView.Pet, false, 0.349f), "pet below threshold");
        Check(!XqVoiceChatClient.ShouldAutoSpeak(cfg, XqView.Pet, false, 0.35f), "pet threshold boundary");
        cfg.pet_voice_probability = 0f;
        Check(!XqVoiceChatClient.ShouldAutoSpeak(cfg, XqView.Pet, false, 0f), "zero probability");
        cfg.pet_voice_probability = 1f;
        Check(XqVoiceChatClient.ShouldAutoSpeak(cfg, XqView.Pet, false, 0.9999f), "full probability");
        Check(XqVoiceChatClient.ShouldAutoSpeak(cfg, XqView.Pet, false, 1f), "inclusive random maximum");
        Check(!XqVoiceChatClient.ShouldAutoSpeak(cfg, XqView.Bench, false, 0f), "bench silent");
        Check(!XqVoiceChatClient.ShouldAutoSpeak(cfg, XqView.Close, true, 0f), "quiet wins");
        cfg.muted = true;
        Check(!XqVoiceChatClient.ShouldAutoSpeak(cfg, XqView.Close, false, 0f), "mute wins");
        cfg.muted = false; cfg.bubble = false; cfg.office_mode = true;
        Check(!XqVoiceChatClient.ShouldAutoSpeak(cfg, XqView.Close, false, 0f), "office wins");
        cfg.bubble = true; cfg.office_mode = false; cfg.voice_tts_auto = false;
        Check(!XqVoiceChatClient.ShouldAutoSpeak(cfg, XqView.Close, false, 0f), "auto master switch");
        cfg.voice_tts_auto=true;cfg.bubble=false;
        Check(XqVoiceChatClient.ShouldAutoSpeak(cfg,XqView.Close,false,.9f),"hidden text does not mute close");
        cfg.office_mode=true;
        Check(!XqVoiceChatClient.ShouldAutoSpeak(cfg,XqView.Close,false,0f),"office remains silent independently");
        Debug.Log("XQ_VOICE_POLICY checks=13 passed");
        Check(XqGlobalVoiceKey.MatchesChord(0x4e,true,false,false,false),"Ctrl+N accepted");
        Check(!XqGlobalVoiceKey.MatchesChord(0x4e,false,false,false,false),"plain N untouched");
        Check(!XqGlobalVoiceKey.MatchesChord(0x4e,true,false,true,false),"Ctrl+Shift+N untouched");
        Check(!XqGlobalVoiceKey.MatchesChord(0x4e,true,true,false,false),"Ctrl+Alt+N untouched");
        Check(!XqGlobalVoiceKey.MatchesChord(0x56,true,false,false,false),"other shortcut untouched");
        Debug.Log("XQ_VOICE_KEY checks=5 passed");
        XiaoqiBuild.Win64();
    }
}
