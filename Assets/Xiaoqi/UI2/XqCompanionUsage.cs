using System;
using System.Collections;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Xiaoqi.UI2
{
    internal sealed class XqCompanionUsage : MonoBehaviour
    {
        static XqCompanionUsage _i;
        public static long? TodayTokens { get; private set; }
        public static DateTime UpdatedAt { get; private set; }
        float _next;
        bool _busy;
        public static void Poll()
        {
            if(_i==null){var go=new GameObject("XqCompanionUsage");DontDestroyOnLoad(go);_i=go.AddComponent<XqCompanionUsage>();}
            if(UpdatedAt!=default(DateTime)&&UpdatedAt.AddHours(8).Date!=DateTime.UtcNow.AddHours(8).Date)TodayTokens=null;
            if(_i._busy||Time.unscaledTime<_i._next)return;
            _i._next=Time.unscaledTime+30;_i.StartCoroutine(_i.Fetch());
        }
        IEnumerator Fetch()
        {
            _busy=true;
            try
            {
                var cfg=XiaoqiRuntime.Config;if(cfg==null||string.IsNullOrEmpty(cfg.server))yield break;
                XqHttp.Resp response=default(XqHttp.Resp);
                yield return XqHttp.Get(cfg.server.TrimEnd('/')+"/api/budget",cfg.token,8,r=>response=r);
                long? total=null;
                if(response.ok){try{total=(long?)JObject.Parse(response.text).SelectToken("usage.tokens");}catch{}}
                TodayTokens=total.HasValue&&total.Value>=0?total:null;
                UpdatedAt=DateTime.UtcNow;
                Debug.Log("XQ_COMPANION_USAGE fetched="+TodayTokens.HasValue);
            }
            finally{_busy=false;}
        }
    }
}
