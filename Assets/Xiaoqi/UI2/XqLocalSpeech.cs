using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;

namespace Xiaoqi.UI2
{
    internal static class XqLocalSpeech
    {
        // One owned hidden process per explicit recording. No cloud audio upload,
        // no daemon, and no alternative project/application is launched.
        public static IEnumerator Recognize(byte[] wav,Action<string> done)
        {
            string result=null;
            if(Application.platform!=RuntimePlatform.WindowsPlayer&&Application.platform!=RuntimePlatform.WindowsEditor){done(null);yield break;}
            string folder=Path.Combine(Application.temporaryCachePath,"xiaoqi-speech");Directory.CreateDirectory(folder);
            string id=Guid.NewGuid().ToString("N"),audio=Path.Combine(folder,id+".wav"),script=Path.Combine(folder,id+".ps1");
            Process process=null;
            try
            {
                var resource=Resources.Load<TextAsset>("XqLocalSpeech");
                if(resource!=null)
                {
                    File.WriteAllText(script,resource.text,Encoding.ASCII);File.WriteAllBytes(audio,wav);
                    process=new Process {StartInfo=new ProcessStartInfo {
                        FileName=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),@"WindowsPowerShell\v1.0\powershell.exe"),
                        Arguments="-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \""+script+"\" -AudioPath \""+audio+"\"",
                        UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=Encoding.UTF8
                    }};
                    process.Start();
                }
            }
            catch(Exception e){UnityEngine.Debug.LogWarning("XQ_STT local start: "+e.GetType().Name);process?.Dispose();process=null;}
            try
            {
                if(process!=null)
                {
                    var output=process.StandardOutput.ReadToEndAsync();var errors=process.StandardError.ReadToEndAsync();
                    float deadline=Time.realtimeSinceStartup+25;
                    while(!process.HasExited&&Time.realtimeSinceStartup<deadline)yield return null;
                    if(!process.HasExited)process.Kill();
                    else
                    {
                        while(!output.IsCompleted)yield return null;
                        try{result=(string)Newtonsoft.Json.Linq.JObject.Parse(output.Result)["text"];}catch{}
                    }
                }
            }
            finally
            {
                if(process!=null){try{if(!process.HasExited)process.Kill();}catch{}process.Dispose();}
                if(File.Exists(audio))File.Delete(audio);if(File.Exists(script))File.Delete(script);
            }
            done(string.IsNullOrWhiteSpace(result)?null:result.Trim());
        }
    }
}
