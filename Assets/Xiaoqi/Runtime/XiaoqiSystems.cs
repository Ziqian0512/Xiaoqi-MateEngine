using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class XiaoqiSystems : MonoBehaviour
{
    private void Start()
    {
        var cfg = XiaoqiConfig.LoadOrCreate();
        XiaoqiRuntime.Config = cfg;
        XiaoqiRuntime.Dnd = cfg.dnd;
        ResolveModelPath(cfg);

        XiaoqiDefaults.Apply(cfg);
        XiaoqiHide.Apply();
        XiaoqiTray.Install();
        LoadModel();

        SceneManager.sceneLoaded += OnSceneLoaded;
        Debug.Log("XIAOQI_SYSTEMS ready model=" + cfg.model_path);

        if (Array.IndexOf(Environment.GetCommandLineArgs(), "--room") >= 0)
            XiaoqiModeController.EnterRoom();
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
    }

    private static void ResolveModelPath(XiaoqiConfig cfg)
    {
        if (cfg == null || string.IsNullOrEmpty(cfg.model_path) || Path.IsPathRooted(cfg.model_path)) return;
        var exeRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        var candidate = Path.GetFullPath(Path.Combine(exeRoot, cfg.model_path));
        if (File.Exists(candidate)) cfg.model_path = candidate;
    }

    private void LoadModel()
    {
        var loader = FindFirstObjectByType<VRMLoader>(FindObjectsInactive.Include);
        var path = XiaoqiRuntime.Config?.model_path ?? "";
        Debug.Log("XIAOQI_LOAD loader=" + (loader != null) + " path=" + path + " exists=" + File.Exists(path));
        if (loader != null && File.Exists(path))
            loader.LoadVRM(path);
    }
}
