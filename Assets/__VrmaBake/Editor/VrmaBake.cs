// 把小祈桌宠工程（Unity）里的 Humanoid 动画整批烘成 .vrma，给网页版/别的项目当动作库。
//
// 两种模式：
//   -executeMethod VrmaBake.Bake     只烘固定几条（网页版在用的）
//   -executeMethod VrmaBake.BakeAll  把 Assets 下所有 .anim 全部烘出来（导出库）
//
// 说明：
//   - 复用 UniVRM 自带的 VrmAnimationExporter；
//   - 采样走 AnimationMode.SampleAnimationClip，人形肌肉曲线由 Unity 自己解算；
//   - 导出名做过去品牌化：去掉工程/厂商前缀，只留动作语义（ascii、小写、下划线）。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniGLTF;
using UniVRM10;
using UnityEditor;
using UnityEngine;

public static class VrmaBake
{
    const string ModelPath = "Assets/__VrmaBake/16.vrm";
    const string OutDir = @"E:\dsh-pet-projects\dspet-main\app\desk\web\anim";
    const string LibDir = @"E:\dsh-pet-projects\dspet-main\docs\me-extract\anim";

    static readonly (string clip, string name)[] Clips =
    {
        ("Assets/MATE ENGINE - Animations/PET_SITTING/BETA_PET_WINDOW_LAY.anim", "me_sit_window"),
        ("Assets/MATE ENGINE - Animations/PET_SITTING/ME_02/WINDOW_LAY_10.anim", "me_sit_window_10"),
        ("Assets/MATE ENGINE - Animations/PET_SITTING/ME_02/PET_SIT_01.anim", "me_sit_01"),
        ("Assets/MATE ENGINE - Animations/PET_SITTING/ME_02/PET_SIT_02.anim", "me_sit_02"),
        ("Assets/MATE ENGINE - Animations/PET_SITTING/ME_02/WINDOW_LAY_9.anim", "me_sit_window_9"),
        ("Assets/MATE ENGINE - Animations/PET_SITTING/ME_02/suwari3.anim", "me_sit_suwari"),
        ("Assets/MATE ENGINE - Animations/PET_IDLE/ME_02/PET_IDLE_16.anim", "me_idle"),
    };

    static Transform GetParentBone(Dictionary<HumanBodyBones, Transform> map, Vrm10HumanoidBones bone)
    {
        while (true)
        {
            if (bone == Vrm10HumanoidBones.Hips) break;
            var parentBone = Vrm10HumanoidBoneSpecification.GetDefine(bone).ParentBone.Value;
            var unityParent = Vrm10HumanoidBoneSpecification.ConvertToUnityBone(parentBone);
            if (map.TryGetValue(unityParent, out var found)) return found;
            bone = parentBone;
        }
        return null;
    }

    /// 导出名去品牌化：只留 ascii 语义，去掉工程/厂商前缀与 "beta" 之类
    static string SafeName(string raw)
    {
        var s = raw;
        string[] drop = { "MATE ENGINE", "MATE-ENGINE", "ME_02", "BETA_", "PET_", "-", " " };
        foreach (var d in drop) s = s.Replace(d, "_");
        var sb = new System.Text.StringBuilder();
        foreach (var c in s.ToLowerInvariant())
            sb.Append((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') ? c : '_');
        var outp = sb.ToString();
        while (outp.Contains("__")) outp = outp.Replace("__", "_");
        return outp.Trim('_');
    }

    static GameObject LoadAvatar()
    {
        AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceSynchronousImport);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (prefab == null) return null;
        var go = UnityEngine.Object.Instantiate(prefab);
        go.name = "BakeAvatar";
        // 只要骨骼：网格/材质/贴图全部拆掉，否则导出的 .vrma 会把整个模型（几十 MB）塞进去
        foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>(true)) UnityEngine.Object.DestroyImmediate(smr);
        foreach (var mr in go.GetComponentsInChildren<MeshRenderer>(true)) UnityEngine.Object.DestroyImmediate(mr);
        foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true)) UnityEngine.Object.DestroyImmediate(mf);
        return go;
    }

    public static void Bake()
    {
        try
        {
            Directory.CreateDirectory(OutDir);
            var go = LoadAvatar();
            if (go == null) { Debug.LogError("[VrmaBake] 模型没导入成 prefab"); EditorApplication.Exit(2); return; }
            var animator = go.GetComponent<Animator>() ?? go.GetComponentInChildren<Animator>();
            if (animator == null || !animator.isHuman) { Debug.LogError("[VrmaBake] 没有 humanoid Animator"); EditorApplication.Exit(3); return; }

            foreach (var (clipPath, name) in Clips)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                if (clip == null) { Debug.LogWarning("[VrmaBake] 找不到剪辑: " + clipPath); continue; }
                var bytes = BakeClip(go, animator, clip);
                File.WriteAllBytes(Path.Combine(OutDir, name + ".vrma"), bytes);
                Debug.Log($"[VrmaBake] {clip.name} ({clip.length:F2}s) -> {name}.vrma [{bytes.Length / 1024} KB]");
            }
            UnityEngine.Object.DestroyImmediate(go);
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogError("[VrmaBake] 失败: " + e); EditorApplication.Exit(1); }
    }

    /// 全量导出：Assets 下所有 Humanoid .anim
    public static void BakeAll()
    {
        try
        {
            Directory.CreateDirectory(LibDir);
            var go = LoadAvatar();
            if (go == null) { Debug.LogError("[VrmaBake] 模型没导入成 prefab"); EditorApplication.Exit(2); return; }
            var animator = go.GetComponent<Animator>() ?? go.GetComponentInChildren<Animator>();
            if (animator == null || !animator.isHuman) { Debug.LogError("[VrmaBake] 没有 humanoid Animator"); EditorApplication.Exit(3); return; }

            var paths = AssetDatabase.FindAssets("t:AnimationClip")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => p.StartsWith("Assets/") && p.EndsWith(".anim"))
                .OrderBy(p => p)
                .ToList();

            var used = new HashSet<string>();
            var ok = 0;
            var index = new List<string>();
            foreach (var p in paths)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(p);
                if (clip == null || clip.length <= 0.02f) continue;
                if (!clip.isHumanMotion) continue;
                var name = SafeName(clip.name);
                if (string.IsNullOrEmpty(name)) continue;
                var unique = name;
                var i = 2;
                while (!used.Add(unique)) unique = name + "_" + i++;
                try
                {
                    var bytes = BakeClip(go, animator, clip);
                    File.WriteAllBytes(Path.Combine(LibDir, unique + ".vrma"), bytes);
                    index.Add($"{unique}\t{clip.length:F2}\t{bytes.Length}\t{clip.name}");
                    ok++;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[VrmaBake] {clip.name} 失败: {e.Message}");
                }
            }
            File.WriteAllLines(Path.Combine(LibDir, "index.tsv"), index);
            Debug.Log($"[VrmaBake] BakeAll 完成：{ok}/{paths.Count} 条");
            UnityEngine.Object.DestroyImmediate(go);
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogError("[VrmaBake] BakeAll 失败: " + e); EditorApplication.Exit(1); }
    }

    static byte[] BakeClip(GameObject go, Animator animator, AnimationClip clip)
    {
        var data = new ExportingGltfData();
        using var exporter = new VrmAnimationExporter(data, new GltfExportSettings());
        exporter.Prepare(go);

        exporter.Export((VrmAnimationExporter vrma) =>
        {
            var map = new Dictionary<HumanBodyBones, Transform>();
            foreach (HumanBodyBones bone in Enum.GetValues(typeof(HumanBodyBones)))
            {
                if (bone == HumanBodyBones.LastBone) continue;
                var t = animator.GetBoneTransform(bone);
                if (t == null) continue;
                map.Add(bone, t);
            }

            vrma.SetPositionBoneAndParent(map[HumanBodyBones.Hips], go.transform);
            var parentOf = new Dictionary<HumanBodyBones, HumanBodyBones>();
            foreach (var kv in map)
            {
                var vrmBone = Vrm10HumanoidBoneSpecification.ConvertFromUnityBone(kv.Key);
                var parent = GetParentBone(map, vrmBone) ?? go.transform;
                vrma.AddRotationBoneAndParent(kv.Key, kv.Value, parent);
                foreach (var kv2 in map)
                {
                    if (kv2.Value == parent) { parentOf[kv.Key] = kv2.Key; break; }
                }
            }

            const int fps = 30;
            var frames = Mathf.Max(1, Mathf.RoundToInt(clip.length * fps));
            var root = go.transform;
            var worlds = new Dictionary<HumanBodyBones, Quaternion>();
            AnimationMode.StartAnimationMode();
            try
            {
                for (var i = 0; i <= frames; i++)
                {
                    var t = Mathf.Min(clip.length, i / (float)fps);
                    AnimationMode.SampleAnimationClip(go, clip, t);

                    // 归一化骨空间：以父骨当前朝向为参考、角色根坐标系里表达，T 姿势 = 单位旋转
                    foreach (var kv in map)
                        worlds[kv.Key] = Quaternion.Inverse(root.rotation) * kv.Value.rotation;
                    foreach (var kv in map)
                    {
                        var wP = parentOf.TryGetValue(kv.Key, out var pb) ? worlds[pb] : Quaternion.identity;
                        kv.Value.localRotation = Quaternion.Inverse(wP) * worlds[kv.Key];
                    }

                    vrma.AddFrame(TimeSpan.FromSeconds(t));
                }
            }
            finally
            {
                AnimationMode.StopAnimationMode();
            }
        });

        return data.ToGlbBytes();
    }
}
