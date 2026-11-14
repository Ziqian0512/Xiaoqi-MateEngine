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
        XiaoqiRuntime.Dnd = cfg.dnd;
        ResolveModelPath(cfg);

        XiaoqiDefaults.Apply(cfg);
        XiaoqiRunKey.Migrate();
        XiaoqiHide.Apply();
        XiaoqiTray.Install();
        Debug.Log("XIAOQI_SYSTEMS ready model=" + cfg.model_path + " host=" + (PetHost.I != null));

        SceneManager.sceneLoaded += OnSceneLoaded;

        if (Array.IndexOf(Environment.GetCommandLineArgs(), "--room") >= 0)
            XiaoqiLiveEntry.Open();
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
