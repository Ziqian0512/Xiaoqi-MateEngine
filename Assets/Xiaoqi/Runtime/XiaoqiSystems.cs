using System.IO;
using UnityEngine;

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

        var loader = FindFirstObjectByType<VRMLoader>();
        if (loader != null && File.Exists(cfg.model_path))
            loader.LoadVRM(cfg.model_path);

        Debug.Log("XIAOQI_SYSTEMS ready model=" + cfg.model_path);
    }
}
