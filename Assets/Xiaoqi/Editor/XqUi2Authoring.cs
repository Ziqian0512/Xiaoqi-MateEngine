using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Xiaoqi.UI2
{
    /// <summary>
    /// 把 PanelSettings / UIDocument 收成资源，并在打包前写进 XiaoqiSystems。
    /// 运行时还会再强制 Constant Pixel Size 和 clearColor = false。
    /// </summary>
    public static class XqUi2Authoring
    {
        [InitializeOnLoadMethod]
        static void AfterLoad()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                Ensure();
                WirePrefabAsset();
            };
        }

        public static void Ensure()
        {
            Directory.CreateDirectory("Assets/Xiaoqi/UI2");
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(XqUi2Paths.Panel);
            if (panel == null && File.Exists(XqUi2Paths.Panel))
                AssetDatabase.DeleteAsset(XqUi2Paths.Panel);
            if (panel == null)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                panel.name = "XqPanelSettings";
                AssetDatabase.CreateAsset(panel, XqUi2Paths.Panel);
            }

            panel.renderMode = PanelRenderMode.ScreenSpaceOverlay;
            panel.scaleMode = PanelScaleMode.ConstantPixelSize;
            panel.scale = 1f;
            panel.clearColor = false;
            panel.colorClearValue = Color.clear;
            panel.targetTexture = null;
            panel.sortingOrder = 360;
            EditorUtility.SetDirty(panel);

            var shaders = new SerializedObject(panel);
            var shader = shaders.FindProperty("m_DefaultShader");
            if (shader != null && shader.objectReferenceValue == null)
                Debug.LogWarning("XQ_UI2 PanelSettings shader is still empty; player hit-test pixels need it saved.");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(XqUi2Paths.Document);
            var existing = prefab != null ? prefab.GetComponent<UIDocument>() : null;
            if (existing == null)
            {
                var go = new GameObject("XqUi2Document");
                var doc = go.AddComponent<UIDocument>();
                doc.panelSettings = panel;
                doc.sortingOrder = 360f;
                PrefabUtility.SaveAsPrefabAsset(go, XqUi2Paths.Document);
                Object.DestroyImmediate(go);
            }
            else
            {
                existing.panelSettings = panel;
                existing.sortingOrder = 360f;
                EditorUtility.SetDirty(prefab);
            }

            AssetDatabase.SaveAssets();
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
            const string path = "Assets/Xiaoqi/Resources/XiaoqiSystems.prefab";
            if (AssetDatabase.LoadAssetAtPath<PanelSettings>(XqUi2Paths.Panel) == null) return;
            if (AssetDatabase.LoadAssetAtPath<StyleSheet>(XqUi2Paths.Uss) == null) return;
            if (AssetDatabase.LoadAssetAtPath<Font>(XqUi2Paths.Font) == null) return;
            var root = PrefabUtility.LoadPrefabContents(path);
            if (root == null) return;
            var sample = root.GetComponent<XqUi2Sample>();
            bool added = sample == null;
            if (added) sample = root.AddComponent<XqUi2Sample>();
            var so = new SerializedObject(sample);
            bool missing = added;
            missing |= Empty(so, "panel");
            missing |= Empty(so, "uss");
            missing |= Empty(so, "font");
            missing |= Empty(so, "documentPrefab");
            if (missing)
            {
                Assign(so, "panel", AssetDatabase.LoadAssetAtPath<PanelSettings>(XqUi2Paths.Panel));
                Assign(so, "uss", AssetDatabase.LoadAssetAtPath<StyleSheet>(XqUi2Paths.Uss));
                Assign(so, "font", AssetDatabase.LoadAssetAtPath<Font>(XqUi2Paths.Font));
                Assign(so, "documentPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(XqUi2Paths.Document));
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            PrefabUtility.UnloadPrefabContents(root);
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
