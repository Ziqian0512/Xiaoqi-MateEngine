using System;
using System.Collections;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Xiaoqi.Mode;

namespace Xiaoqi.UI2
{
    // Explicit synthetic integration test only. Ordinary startup never sends a test turn.
    internal sealed class XqCompanionIntegration : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var args=Environment.GetCommandLineArgs();int n=Array.IndexOf(args,"-xq-companion-integration");
            if(n<0||n+1>=args.Length)return;
            var go=new GameObject("XqCompanionIntegration");DontDestroyOnLoad(go);go.AddComponent<XqCompanionIntegration>().StartCoroutine(Run(args[n+1]));
        }
        static IEnumerator Run(string directory)
        {
            float deadline=Time.realtimeSinceStartup+90;
            while((XqMode.Current!=XqView.Close||XqMode.Busy||Time.realtimeSinceStartup<15)&&Time.realtimeSinceStartup<deadline)yield return null;
            var client=XqVoiceChatClient.Ensure();
            client.SendText("这是近景界面的连接测试，请只简短回复：听得到。",false);
            deadline=Time.realtimeSinceStartup+180;
            while(XqVoiceChatClient.IsThinking&&Time.realtimeSinceStartup<deadline)yield return null;
            bool reply=!string.IsNullOrEmpty(XqVoiceChatClient.LatestReplyFull),played=false;
            bool expectedSpeech=XqVoiceChatClient.ShouldAutoSpeak(XiaoqiRuntime.Config,XqMode.Current,XiaoqiRuntime.Dnd,0);
            deadline=Time.realtimeSinceStartup+200;
            while(reply&&expectedSpeech&&Time.realtimeSinceStartup<deadline)
            {
                if(XqVoiceChatClient.IsPlayingTts){played=true;break;}
                if(!string.IsNullOrEmpty(XqVoiceChatClient.Status))break;
                yield return null;
            }
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory,"integration.json"),new JObject { ["syntheticChatReplyReceived"]=reply,["speechExpectedUnderCurrentSettings"]=expectedSpeech,["ttsPlaybackStarted"]=played,["responseUsageProvided"]=XqVoiceChatClient.LatestUsage!=null,["status"]=XqVoiceChatClient.Status }.ToString());
            Debug.Log("XQ_COMPANION_INTEGRATION reply="+reply+" tts="+played);
        }
    }
}
