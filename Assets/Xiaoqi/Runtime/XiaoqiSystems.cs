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

        var exeRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (!string.IsNullOrEmpty(cfg.model_path) && !Path.IsPathRooted(cfg.model_path))
        {
            var candidate = Path.GetFullPath(Path.Combine(exeRoot, cfg.model_path));
            if (File.Exists(candidate)) cfg.model_path = candidate;
        }

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
        XiaoqiDefaults.Apply(XiaoqiRuntime.Config);
        XiaoqiHide.Apply();
        XiaoqiTray.Install();
        LoadModel();
    }

    private void LoadModel()
    {
        var loader = FindFirstObjectByType<VRMLoader>();
        if (loader != null && File.Exists(XiaoqiRuntime.Config.model_path))
            loader.LoadVRM(XiaoqiRuntime.Config.model_path);
    }
}
