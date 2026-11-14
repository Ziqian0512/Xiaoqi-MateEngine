using System.Linq;
using UnityEditor;
using UnityEngine;

public static class XiaoqiRoomProbe
{
    [MenuItem("Xiaoqi/Probe Room Anchors")]
    public static void Probe()
    {
        var path = "Assets/Xiaoqi/_Private/Resources/Room/TexturedPng/xiaoqi_room_png.fbx";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogError("ROOM_PROBE missing " + path);
            return;
        }
        var renderers = prefab.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            var bb = r.bounds;
            if (bb.size.x > 0.7f && bb.size.z > 0.7f && bb.size.y > 0.15f)
                Debug.Log("ROOM_BIG " + r.name + " center=" + bb.center + " size=" + bb.size);
        }
        foreach (var r in renderers)
        {
            var n = r.name.ToLower();
            if (n.Contains("bed") || n.Contains("desk") || n.Contains("chair") ||
                n.Contains("table") || n.Contains("sofa") || n.Contains("window") ||
                n.Contains("door") || n.Contains("wardrobe"))
            {
                var b = r.bounds;
                Debug.Log("ROOM_PROBE " + r.name + " center=" + b.center + " size=" + b.size);
            }
        }
        var all = new Bounds(renderers[0].bounds.center, Vector3.zero);
        foreach (var r in renderers) all.Encapsulate(r.bounds);
        Debug.Log("ROOM_PROBE ALL center=" + all.center + " size=" + all.size);
        Debug.Log("ROOM_PROBE count=" + renderers.Length);
    }
}
