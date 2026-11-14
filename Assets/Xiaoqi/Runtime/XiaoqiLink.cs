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
        StartCoroutine(Poll());
    }

    private IEnumerator Poll()
    {
        while (true)
        {
            var cfg = XiaoqiRuntime.Config ?? XiaoqiConfig.LoadOrCreate();
            var url = (cfg.server ?? "").TrimEnd('/') + "/api/body/stage";
            if (!string.IsNullOrEmpty(url))
            {
                using (var req = UnityWebRequest.Get(url))
                {
                    if (!string.IsNullOrEmpty(cfg.token))
                        req.SetRequestHeader("X-Token", cfg.token);
                    req.timeout = 5;
                    yield return req.SendWebRequest();
                    if (req.result == UnityWebRequest.Result.Success)
                    {
                        try
                        {
                            var data = JsonUtility.FromJson<StageData>(req.downloadHandler.text);
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
            }
            yield return new WaitForSeconds(Mathf.Max(1, cfg.poll_sec));
        }
    }
}
