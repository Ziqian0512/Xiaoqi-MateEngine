using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using Xiaoqi.Host;

public class XiaoqiSystems : MonoBehaviour
{
    private void Start()
    {
        var cfg = XiaoqiConfig.LoadOrCreate();
        XiaoqiRuntime.Config = cfg;
        XiaoqiRuntime.MigrateDnd();
        ResolveModelPath(cfg);

        XiaoqiDefaults.Apply(cfg);
        XiaoqiRunKey.Migrate();
        XiaoqiHide.Apply();
        XiaoqiTray.Install();
        Debug.Log("XIAOQI_SYSTEMS ready model=" + cfg.model_path + " host=" + (PetHost.I != null));

        SceneManager.sceneLoaded += OnSceneLoaded;

        if (Array.IndexOf(Environment.GetCommandLineArgs(), "--room") >= 0)
            XiaoqiLiveEntry.Open();

        StartCoroutine(DemoSteps());
        StartCoroutine(WatchWindow());
    }

    private IEnumerator WatchWindow()
    {
        yield return new WaitForSeconds(1.5f);
        Debug.Log("XQ_WATCH start");
        for (;;)
        {
            (PetHost.I as MeHost)?.EnsureOnScreen();
            yield return new WaitForSeconds(2f);
        }
    }

    private IEnumerator DemoSteps()
    {
        var a = Environment.GetCommandLineArgs();
        yield return new WaitForSeconds(2f);
        if (Array.IndexOf(a, "-demo-center") >= 0)
        {
            PetHost.I?.MoveToCenter();
            Debug.Log("XQ_DEMO center");
        }
        if (Array.IndexOf(a, "-demo-quiet") >= 0)
        {
            XiaoqiRuntime.SetQuietMinutes(60);
            Debug.Log("XQ_DEMO quiet");
        }
        if (Array.IndexOf(a, "-demo-nap") >= 0)
        {
            Xiaoqi.Spike.PetPresence.Nap(1);
            Debug.Log("XQ_DEMO nap");
        }
        if (Array.IndexOf(a, "-demo-hide") >= 0)
        {
            var av = PetHost.I != null ? PetHost.I.Avatar : null;
            if (av != null) av.gameObject.SetActive(false);
            Debug.Log("XQ_DEMO hide");
        }
        if (Array.IndexOf(a, "-demo-tuner") >= 0)
        {
            Xiaoqi.Spike.EmotionLineTuner.Toggle();
            Debug.Log("XQ_DEMO tuner");
        }
        if (Array.IndexOf(a, "-demo-apply") >= 0)
        {
            var h = PetHost.I;
            if (h != null)
            {
                h.Size = 1.2f;
                h.Topmost = false;
                XiaoqiRuntime.Config.bubble = false;
                XiaoqiConfig.Save(XiaoqiRuntime.Config);
                h.SaveSettings();
                Debug.Log("XQ_DEMO apply size=" + h.Size + " topmost=" + h.Topmost + " bubble=" + XiaoqiRuntime.Config.bubble);
            }
        }
    }

    private void Update()
    {
        if (XiaoqiRuntime.ExpireQuiet()) Debug.Log("XQ_DND end");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Mate Engine Main") return;
        StartCoroutine(ReloadAfterScene());
    }

    private IEnumerator ReloadAfterScene()
    {
        yield return null;
        yield return new WaitForSeconds(0.5f);
        XiaoqiRuntime.Config = XiaoqiConfig.LoadOrCreate();
        ResolveModelPath(XiaoqiRuntime.Config);
        XiaoqiHide.Apply();
        XiaoqiTray.Install();
        // 不在这里再 LoadVRM：VRMLoader.Start 会用已保存的 selectedModelPath 加载；
        // 再调一次会和它并发，后一次把前一次销毁。
    }

    private static void ResolveModelPath(XiaoqiConfig cfg)
    {
        if (cfg == null || string.IsNullOrEmpty(cfg.model_path) || Path.IsPathRooted(cfg.model_path)) return;
        var exeRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        var candidate = Path.GetFullPath(Path.Combine(exeRoot, cfg.model_path));
        if (File.Exists(candidate)) cfg.model_path = candidate;
    }
}
