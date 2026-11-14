using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class XiaoqiBuild
{
    public static void Win64()
    {
        XiaoqiIdentity.Apply();
        EditorSceneManager.OpenScene("Assets/MATE ENGINE - Scenes/Mate Engine Main.unity", OpenSceneMode.Single);

        var prefabPath = "Assets/Xiaoqi/Resources/XiaoqiSystems.prefab";
        Directory.CreateDirectory("Assets/Xiaoqi/Resources");
        var temp = new GameObject("XiaoqiSystems");
        temp.AddComponent<Xiaoqi.Host.MeHost>();
        temp.AddComponent<XiaoqiSystems>();
        temp.AddComponent<XiaoqiSettings>();
        temp.AddComponent<XiaoqiLink>();
        temp.AddComponent<XiaoqiFeed>();
        temp.AddComponent<XiaoqiStartCard>();
        temp.AddComponent<XiaoqiAudit>();
        temp.AddComponent<Xiaoqi.Spike.XiaoqiFace>();
        temp.AddComponent<Xiaoqi.Spike.HeadAnchor>();
        temp.AddComponent<Xiaoqi.Spike.PetPresence>();
        temp.AddComponent<Xiaoqi.Spike.PetBubble>();
        temp.AddComponent<Xiaoqi.Spike.TodayCard>();
        temp.AddComponent<Xiaoqi.Spike.EmotionLineTuner>();
        temp.AddComponent<Xiaoqi.Spike.XiaoqiShotRunner>();
        temp.AddComponent<Xiaoqi.UI.XqMenu>();
        PrefabUtility.SaveAsPrefabAsset(temp, prefabPath);
        Object.DestroyImmediate(temp);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        AddressableAssetSettings.CleanPlayerContent();
        AddressableAssetSettings.BuildPlayerContent(out var addressablesResult);
        if (!string.IsNullOrEmpty(addressablesResult.Error))
        {
            UnityEngine.Debug.LogError(addressablesResult.Error);
            EditorApplication.Exit(1);
        }

        var scenes = new System.Collections.Generic.List<string>
        {
            "Assets/MATE ENGINE - Scenes/Mate Engine Main.unity"
        };

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes.ToArray(),
            target = BuildTarget.StandaloneWindows64,
            locationPathName = "Build/Xiaoqi/Xiaoqi.exe",
            options = BuildOptions.None
        });

        if (report.summary.result != BuildResult.Succeeded)
            EditorApplication.Exit(1);

        var model = "XiaoqiData/16.vrm";
        if (File.Exists(model))
        {
            var targetDir = Path.Combine(Path.GetDirectoryName("Build/Xiaoqi/Xiaoqi.exe"), "XiaoqiData");
            Directory.CreateDirectory(targetDir);
            File.Copy(model, Path.Combine(targetDir, "16.vrm"), true);
            var streaming = Path.Combine(Path.GetDirectoryName("Build/Xiaoqi/Xiaoqi.exe"), "Xiaoqi_Data", "StreamingAssets");
            Directory.CreateDirectory(streaming);
            File.Copy(model, Path.Combine(streaming, "16.vrm"), true);
        }
    }
}


