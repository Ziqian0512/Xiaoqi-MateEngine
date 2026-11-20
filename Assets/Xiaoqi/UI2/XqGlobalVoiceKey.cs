using System;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
using Xiaoqi.Mode;
namespace Xiaoqi.UI2
{
    // Only queue commands inside the Windows hook. Unity work runs on Update.
    public sealed class XqGlobalVoiceKey : MonoBehaviour
    {
        delegate IntPtr Hook(int code,IntPtr message,IntPtr data);
        [StructLayout(LayoutKind.Sequential)] struct Key {public uint vk,scan,flags,time;public UIntPtr extra;}
        [DllImport("user32.dll",SetLastError=true)] static extern IntPtr SetWindowsHookEx(int id,Hook callback,IntPtr module,uint thread);
        [DllImport("user32.dll")] static extern bool UnhookWindowsHookEx(IntPtr hook);
        [DllImport("user32.dll")] static extern IntPtr CallNextHookEx(IntPtr hook,int code,IntPtr message,IntPtr data);
        [DllImport("user32.dll")] static extern short GetAsyncKeyState(int key);
        [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] static extern IntPtr GetModuleHandle(string name);
        static XqGlobalVoiceKey _instance;
        Hook _callback;IntPtr _hook;bool _captured,_cancelled,_owns;volatile bool _enabled;int _commands;XqView _mode;
        public static bool Available=>_instance!=null&&_instance._hook!=IntPtr.Zero;
        public static void Ensure(){if(_instance!=null||Application.platform!=RuntimePlatform.WindowsPlayer)return;var go=new GameObject("XqGlobalVoiceKey");DontDestroyOnLoad(go);_instance=go.AddComponent<XqGlobalVoiceKey>();}
        void Awake(){_callback=Intercept;_hook=SetWindowsHookEx(13,_callback,GetModuleHandle(null),0);Debug.Log("XQ_VOICE_KEY Ctrl+N "+(_hook!=IntPtr.Zero?"ready":"unavailable error="+Marshal.GetLastWin32Error()));if(_hook==IntPtr.Zero)XqVoiceOverlay.Notice("Ctrl+N 未能启用，请在偏好设置中检查快捷键状态。");}
        void Queue(int c){int old;do{old=_commands;}while(Interlocked.CompareExchange(ref _commands,old|c,old)!=old);}
        public static bool MatchesChord(uint key,bool control,bool alt,bool shift,bool win)=>key==0x4e&&control&&!alt&&!shift&&!win;
        IntPtr Intercept(int code,IntPtr message,IntPtr data)
        {
            if(code<0)return CallNextHookEx(_hook,code,message,data);var k=Marshal.PtrToStructure<Key>(data);int m=message.ToInt32();bool down=m==0x100||m==0x104,up=m==0x101||m==0x105;
            if((k.flags&0x10)!=0)return CallNextHookEx(_hook,code,message,data);
            if(k.vk==0x4e){if(down&&!_captured&&_enabled&&MatchesChord(k.vk,(GetAsyncKeyState(0x11)&0x8000)!=0,(GetAsyncKeyState(0x12)&0x8000)!=0,(GetAsyncKeyState(0x10)&0x8000)!=0,((GetAsyncKeyState(0x5b)|GetAsyncKeyState(0x5c))&0x8000)!=0)){_captured=true;_cancelled=false;Queue(1);}if(_captured){if(up){if(!_cancelled)Queue(2);_captured=false;_cancelled=false;}return new IntPtr(1);}}
            if(_captured&&k.vk==0x1b){if(down){_cancelled=true;Queue(4);}return new IntPtr(1);}
            if(_captured&&up&&(k.vk==0x11||k.vk==0xa2||k.vk==0xa3)){if(!_cancelled)Queue(2);_cancelled=true;}
            return CallNextHookEx(_hook,code,message,data);
        }
        void Update()
        {
            _enabled=!XqMode.Busy&&(XqMode.Current==XqView.Pet||XqMode.Current==XqView.Close);int c=Interlocked.Exchange(ref _commands,0);
            if(_owns&&(!_enabled||XqMode.Current!=_mode)){XqVoiceChatClient.Ensure().CancelRecording();_owns=false;_cancelled=true;}
            if((c&1)!=0&&_enabled&&!XqVoiceChatClient.IsRecording&&!XqVoiceChatClient.IsRecognizing&&!XqVoiceChatClient.IsThinking){_mode=XqMode.Current;XqVoiceOverlay.BeginVoice();_owns=XqVoiceChatClient.IsRecording;}
            if((c&4)!=0&&_owns){XqVoiceChatClient.Ensure().CancelRecording();_owns=false;}else if((c&2)!=0&&_owns){XqVoiceChatClient.Ensure().StopRecordingAndSend();_owns=false;}
            if(_owns&&!XqVoiceChatClient.IsRecording)_owns=false;
        }
        void OnApplicationQuit(){Release();}void OnDestroy(){Release();if(_instance==this)_instance=null;}
        void Release(){_enabled=false;if(_hook!=IntPtr.Zero){UnhookWindowsHookEx(_hook);_hook=IntPtr.Zero;}if(_owns)XqVoiceChatClient.Ensure().CancelRecording();_owns=false;}
    }
}
