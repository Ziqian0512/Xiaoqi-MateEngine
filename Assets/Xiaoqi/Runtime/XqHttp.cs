using System;
using System.Collections;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 统一 HTTP 请求 helper：对 Tailscale (100.64.0.0/10) 与局域网/回环地址绕过 Windows 系统代理，
/// 避免开着全局代理时 UnityWebRequest 卡死超时；公网域名仍走系统默认代理。
/// </summary>
public static class XqHttp
{
    public struct Resp
    {
        public bool ok;
        public bool connError;
        public long code;
        public string text;
        public string error;
    }

    static readonly HttpClient DirectClient = new HttpClient(new HttpClientHandler
    {
        UseProxy = false,
    });

    public static async Task<byte[]> GetDirectAudio(string url, string token)
    {
        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
        using (var msg = new HttpRequestMessage(HttpMethod.Get, url))
        {
            if (!string.IsNullOrEmpty(token)) msg.Headers.TryAddWithoutValidation("X-Token", token);
            using (var response = await DirectClient.SendAsync(msg, cts.Token).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            }
        }
    }
    public static bool IsDirectHost(string url)
    {
        Uri uri;
        if (!Uri.TryCreate(url, UriKind.Absolute, out uri)) return false;
        var host = uri.Host;
        if (string.IsNullOrEmpty(host)) return false;
        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)) return true;

        IPAddress ip;
        if (!IPAddress.TryParse(host, out ip)) return false;
        if (IPAddress.IsLoopback(ip)) return true;
        var b = ip.GetAddressBytes();
        if (b.Length != 4) return false;
        // 10.0.0.0/8, 172.16.0.0/12, 192.168.0.0/16, 100.64.0.0/10 (Tailscale CGNAT)
        if (b[0] == 10) return true;
        if (b[0] == 192 && b[1] == 168) return true;
        if (b[0] == 172 && b[1] >= 16 && b[1] <= 31) return true;
        if (b[0] == 100 && b[1] >= 64 && b[1] <= 127) return true;
        return false;
    }

    public static IEnumerator Get(string url, string token, int timeoutSec, Action<Resp> done)
    {
        if (IsDirectHost(url))
        {
            yield return SendDirect(HttpMethod.Get, url, token, null, timeoutSec, done);
            yield break;
        }
        using (var req = UnityWebRequest.Get(url))
        {
            if (!string.IsNullOrEmpty(token)) req.SetRequestHeader("X-Token", token);
            req.timeout = timeoutSec;
            yield return req.SendWebRequest();
            bool ok = req.result == UnityWebRequest.Result.Success && req.responseCode >= 200 && req.responseCode < 300;
            done(new Resp
            {
                ok = ok,
                connError = req.result == UnityWebRequest.Result.ConnectionError,
                code = req.responseCode,
                text = ok ? req.downloadHandler.text : null,
                error = req.error,
            });
        }
    }

    public static IEnumerator PostAudio(string url,string token,byte[] bytes,int timeoutSec,Action<Resp> done)
    {
        if(IsDirectHost(url))
        {
            var task=SendAudioAsync(url,token,bytes,timeoutSec);
            while(!task.IsCompleted)yield return null;
            if(task.IsFaulted||task.IsCanceled){done(new Resp {ok=false,connError=true,error="audio request failed"});yield break;}
            done(task.Result);yield break;
        }
        using(var req=new UnityWebRequest(url,"POST"))
        {
            req.uploadHandler=new UploadHandlerRaw(bytes);req.downloadHandler=new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type","audio/wav");if(!string.IsNullOrEmpty(token))req.SetRequestHeader("X-Token",token);
            req.timeout=timeoutSec;yield return req.SendWebRequest();
            bool ok=req.result==UnityWebRequest.Result.Success;
            done(new Resp {ok=ok,code=req.responseCode,text=ok?req.downloadHandler.text:null,error=req.error});
        }
    }
    static async Task<Resp> SendAudioAsync(string url,string token,byte[] bytes,int timeoutSec)
    {
        using(var cts=new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSec)))
        using(var message=new HttpRequestMessage(HttpMethod.Post,url))
        {
            if(!string.IsNullOrEmpty(token))message.Headers.TryAddWithoutValidation("X-Token",token);
            message.Content=new ByteArrayContent(bytes);message.Content.Headers.ContentType=new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");
            try
            {
                using(var response=await DirectClient.SendAsync(message,cts.Token).ConfigureAwait(false))
                    return new Resp {ok=response.IsSuccessStatusCode,code=(int)response.StatusCode,text=response.IsSuccessStatusCode?await response.Content.ReadAsStringAsync().ConfigureAwait(false):null};
            }
            catch(Exception e){return new Resp {ok=false,connError=true,error=e.Message};}
        }
    }

    public static IEnumerator PostJson(string url, string token, string json, int timeoutSec, Action<Resp> done)
    {
        if (IsDirectHost(url))
        {
            yield return SendDirect(HttpMethod.Post, url, token, json, timeoutSec, done);
            yield break;
        }
        var body = Encoding.UTF8.GetBytes(json ?? "");
        using (var req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrEmpty(token)) req.SetRequestHeader("X-Token", token);
            req.timeout = timeoutSec;
            yield return req.SendWebRequest();
            bool ok = req.result == UnityWebRequest.Result.Success && req.responseCode >= 200 && req.responseCode < 300;
            done(new Resp
            {
                ok = ok,
                connError = req.result == UnityWebRequest.Result.ConnectionError,
                code = req.responseCode,
                text = ok ? req.downloadHandler.text : null,
                error = req.error,
            });
        }
    }

    static IEnumerator SendDirect(HttpMethod method, string url, string token, string json, int timeoutSec, Action<Resp> done)
    {
        var task = SendDirectAsync(method, url, token, json, timeoutSec);
        while (!task.IsCompleted) yield return null;
        if (task.IsFaulted)
        {
            var msg = task.Exception != null ? task.Exception.GetBaseException().Message : "request faulted";
            done(new Resp { ok = false, connError = true, code = 0, error = msg });
            yield break;
        }
        done(task.Result);
    }

    static async Task<Resp> SendDirectAsync(HttpMethod method, string url, string token, string json, int timeoutSec)
    {
        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(Mathf.Max(1, timeoutSec))))
        using (var msg = new HttpRequestMessage(method, url))
        {
            if (!string.IsNullOrEmpty(token))
                msg.Headers.TryAddWithoutValidation("X-Token", token);
            if (json != null)
                msg.Content = new StringContent(json, Encoding.UTF8, "application/json");
            try
            {
                using (var res = await DirectClient.SendAsync(msg, cts.Token).ConfigureAwait(false))
                {
                    int code = (int)res.StatusCode;
                    string text = res.IsSuccessStatusCode
                        ? await res.Content.ReadAsStringAsync().ConfigureAwait(false)
                        : null;
                    return new Resp
                    {
                        ok = res.IsSuccessStatusCode,
                        connError = false,
                        code = code,
                        text = text,
                        error = res.IsSuccessStatusCode ? null : ("HTTP " + code),
                    };
                }
            }
            catch (Exception e)
            {
                return new Resp
                {
                    ok = false,
                    connError = true,
                    code = 0,
                    error = e.Message,
                };
            }
        }
    }
}
