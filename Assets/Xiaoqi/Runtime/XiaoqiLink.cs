using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class XiaoqiLink : MonoBehaviour
{
    public static StageData Current;
    public static event Action<StageData> OnStage;

    private void Start()
    {
        if (TryFakeStage()) return;
        StartCoroutine(Poll());
    }

    private bool TryFakeStage()
    {
        var args = Environment.GetCommandLineArgs();
        var i = Array.IndexOf(args, "-fake-stage");
        if (i < 0 || i + 1 >= args.Length) return false;
        return ApplyFake(args[i + 1]);
    }

    public static bool ApplyFake(string name)
    {
        var path = System.IO.Path.IsPathRooted(name)
            ? name
            : System.IO.Path.Combine(Application.streamingAssetsPath, "Xiaoqi", "fake_stage", name + ".json");
        if (!System.IO.File.Exists(path))
        {
            Debug.LogWarning("XiaoqiLink fake stage missing: " + path);
            return false;
        }
        try
        {
            var data = JsonUtility.FromJson<StageData>(System.IO.File.ReadAllText(path));
            if (data != null && data.face != null && data.face.rose_at == "@now")
            {
                data.face.rose_at = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                data.face.rose = true;
            }
            Current = data;
            OnStage?.Invoke(data);
            Debug.Log("XIAOQI_FAKE_STAGE " + path);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning("XiaoqiLink fake stage: " + e.Message);
            return false;
        }
    }

    private IEnumerator Poll()
    {
        while (true)
        {
            var cfg = XiaoqiRuntime.Config ?? XiaoqiConfig.LoadOrCreate();
            var url = (cfg.server ?? "").TrimEnd('/') + "/api/body/stage";
            if (!string.IsNullOrEmpty(url))
            {
                XqHttp.Resp resp = default(XqHttp.Resp);
                yield return XqHttp.Get(url, cfg.token, 5, r => resp = r);
                if (resp.ok)
                {
                    try
                    {
                        var data = JsonUtility.FromJson<StageData>(resp.text);
                        if (data != null)
                        {
                            Current = data;
                            OnStage?.Invoke(data);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning("XiaoqiLink parse: " + e.Message);
                    }
                }
            }
            yield return new WaitForSeconds(Mathf.Max(1, cfg.poll_sec));
        }
    }
}
