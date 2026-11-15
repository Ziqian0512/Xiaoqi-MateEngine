using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Xiaoqi.UI2
{
    /// <summary>
    /// 打包前把 PanelSettings 着色器引用和 UIDocument 收好。
    /// 不调用 AssetDatabase.SaveAssets()：6000.2.6f2 里对带着 UIDocument 的预制体
    /// 再走一遍 SavePrefabAsset 会 ACCESS_VIOLATION。预制体只经 LoadPrefabContents 保存。
    /// batchmode / 打包过程里不从 InitializeOnLoad 再保存一次。
    /// </summary>
    public static class XqUi2Authoring
    {
        static bool s_Busy;

        [InitializeOnLoadMethod]
        static void AfterLoad()
        {
            if (Application.isBatchMode) return;
            EditorApplication.delayCall += EditorTick;
        }

        static void EditorTick()
        {
            if (Application.isBatchMode) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (BuildPipeline.isBuildingPlayer) return;
            if (s_Busy) return;
            Ensure();
            WirePrefabAsset();
        }

        public static void Ensure()
        {
            if (s_Busy) return;
            s_Busy = true;
            try
            {
                Directory.CreateDirectory("Assets/Xiaoqi/UI2");
                var panel = LoadOrCreatePanel();
                panel.scaleMode = PanelScaleMode.ConstantPixelSize;
                panel.scale = 1f;
                panel.clearColor = false;
                panel.colorClearValue = Color.clear;
                panel.targetTexture = null;
                panel.sortingOrder = 360;
                var panelSo = new SerializedObject(panel);
                var depth = panelSo.FindProperty("m_ClearDepthStencil");
                if (depth != null) depth.boolValue = false;
                panelSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(panel);
                WarnIfShaderMissing(panel);
                AssetDatabase.SaveAssetIfDirty(panel);
                EnsureDocument(panel);
            }
            finally
            {
                s_Busy = false;
            }
        }

        static PanelSettings LoadOrCreatePanel()
        {
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(XqUi2Paths.Panel);
            if (panel == null && File.Exists(XqUi2Paths.Panel))
                AssetDatabase.DeleteAsset(XqUi2Paths.Panel);
            if (panel != null) return panel;
            panel = ScriptableObject.CreateInstance<PanelSettings>();
            panel.name = "XqPanelSettings";
            AssetDatabase.CreateAsset(panel, XqUi2Paths.Panel);
            return panel;
        }

        static void WarnIfShaderMissing(PanelSettings panel)
        {
            var so = new SerializedObject(panel);
            var shader = so.FindProperty("m_DefaultShader");
            if (shader == null) shader = so.FindProperty("m_RuntimeShader");
            if (shader != null && shader.objectReferenceValue == null)
                Debug.LogWarning("XQ_UI2 PanelSettings shader is still empty; player hit-test pixels need it saved.");
        }

        static void EnsureDocument(PanelSettings panel)
        {
            var path = XqUi2Paths.Document;
            var loaded = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var onAsset = loaded != null ? loaded.GetComponent<UIDocument>() : null;
            if (onAsset != null && onAsset.panelSettings == panel && Mathf.Approximately(onAsset.sortingOrder, 360f))
            {
                EditorUtility.ClearDirty(loaded);
                return;
            }

            if (loaded != null && onAsset != null)
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var doc = root.GetComponent<UIDocument>();
                    if (doc == null) return;
                    doc.panelSettings = panel;
                    doc.sortingOrder = 360f;
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
                ClearDocumentDirty();
                return;
            }

            if (loaded != null)
                AssetDatabase.DeleteAsset(path);

            var go = new GameObject("XqUi2Document");
            try
            {
                var doc = go.AddComponent<UIDocument>();
                doc.panelSettings = panel;
                doc.sortingOrder = 360f;
                PrefabUtility.SaveAsPrefabAsset(go, path);
                EditorUtility.ClearDirty(go);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
            ClearDocumentDirty();
        }

        static void ClearDocumentDirty()
        {
            var saved = AssetDatabase.LoadAssetAtPath<Object>(XqUi2Paths.Document);
            if (saved != null) EditorUtility.ClearDirty(saved);
        }

        public static void Wire(XqUi2Sample sample)
        {
            if (sample == null) return;
            var so = new SerializedObject(sample);
            Assign(so, "panel", AssetDatabase.LoadAssetAtPath<PanelSettings>(XqUi2Paths.Panel));
            Assign(so, "uss", AssetDatabase.LoadAssetAtPath<StyleSheet>(XqUi2Paths.Uss));
            Assign(so, "font", AssetDatabase.LoadAssetAtPath<Font>(XqUi2Paths.Font));
            Assign(so, "documentPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(XqUi2Paths.Document));
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void WirePrefabAsset()
        {
            if (s_Busy || Application.isBatchMode || BuildPipeline.isBuildingPlayer) return;
            const string path = "Assets/Xiaoqi/Resources/XiaoqiSystems.prefab";
            if (AssetDatabase.LoadAssetAtPath<PanelSettings>(XqUi2Paths.Panel) == null) return;
            if (AssetDatabase.LoadAssetAtPath<StyleSheet>(XqUi2Paths.Uss) == null) return;
            if (AssetDatabase.LoadAssetAtPath<Font>(XqUi2Paths.Font) == null) return;
            var root = PrefabUtility.LoadPrefabContents(path);
            if (root == null) return;
            try
            {
                var sample = root.GetComponent<XqUi2Sample>();
                bool added = sample == null;
                if (added) sample = root.AddComponent<XqUi2Sample>();
                var so = new SerializedObject(sample);
                bool missing = added;
                missing |= Empty(so, "panel");
                missing |= Empty(so, "uss");
                missing |= Empty(so, "font");
                missing |= Empty(so, "documentPrefab");
                if (!missing) return;
                Assign(so, "panel", AssetDatabase.LoadAssetAtPath<PanelSettings>(XqUi2Paths.Panel));
                Assign(so, "uss", AssetDatabase.LoadAssetAtPath<StyleSheet>(XqUi2Paths.Uss));
                Assign(so, "font", AssetDatabase.LoadAssetAtPath<Font>(XqUi2Paths.Font));
                Assign(so, "documentPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(XqUi2Paths.Document));
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static bool Empty(SerializedObject so, string name)
        {
            var p = so.FindProperty(name);
            return p == null || p.objectReferenceValue == null;
        }

        static void Assign(SerializedObject so, string name, Object value)
        {
            var p = so.FindProperty(name);
            if (p != null) p.objectReferenceValue = value;
        }
    }
}
