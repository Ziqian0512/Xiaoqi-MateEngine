using System;
using System.Collections;
using System.IO;
using UniGLTF;
using UnityEngine;
using VRM;

namespace Xiaoqi.Spike
{
    /// <summary>房间预览：家具 FBX + 我们自己补的 5×5m 房间壳，固定 3/4 相机。</summary>
    public class RoomPreview : MonoBehaviour
    {
        private async void Start()
        {
            // no old process-level singleton; room is a scene in this same exe.
            // 防止 PetMode 的运行时引导混进房间预览
            foreach (var n in new[] { "XiaoqiSpike", "PetCamera", "PetLight", "SpikeFloor" })
            {
                var go = GameObject.Find(n);
                if (go != null) Destroy(go);
            }

            var prefab = Resources.Load<GameObject>("Room/TexturedPng/xiaoqi_room_png");
            if (prefab == null)
            {
                Debug.LogError("ROOM props prefab missing");
                return;
            }

            var room = Instantiate(prefab);
            room.name = "XiaoqiRoomProps";

            var renderers = room.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) { Debug.LogError("ROOM no renderers"); return; }

            // 用原始地板 mesh_34 定房间中心，避免被公寓外壳拉大
            Bounds floor = new Bounds(Vector3.zero, new Vector3(5.2f, 0.1f, 5.2f));
            bool foundFloor = false;
            foreach (var r in renderers)
            {
                if (r.name.ToLower().Contains("mesh_34"))
                {
                    floor = r.bounds;
                    foundFloor = true;
                    break;
                }
            }
            if (!foundFloor)
            {
                var all = new Bounds(renderers[0].bounds.center, Vector3.zero);
                foreach (var r in renderers) all.Encapsulate(r.bounds);
                floor = all;
            }

            var center = new Vector3(floor.center.x, floor.min.y, floor.center.z);
            BuildShell(center);

            var cam = room.GetComponentInChildren<Camera>(true);
            bool hasFbxCam = cam != null;
            GameObject camGo;
            if (hasFbxCam)
            {
                camGo = cam.gameObject;
                camGo.SetActive(true);
                cam.enabled = true;
                Debug.Log("ROOM_CAM from FBX");
            }
            else
            {
                camGo = new GameObject("RoomCamera");
                cam = camGo.AddComponent<Camera>();
                Debug.Log("ROOM_CAM created");
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.06f, 0.08f, 1f);
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.02f;
            cam.farClipPlane = 50f;
            cam.aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            // 房间视角：抬高到 2.25m 的 3/4 俯视，保证床/桌/人物都能入画。
            cam.fieldOfView = 55f;
            camGo.transform.position = center + new Vector3(2.15f, 2.25f, -2.15f);
            camGo.transform.LookAt(center + new Vector3(-0.45f, 0.65f, 0.10f));

            var lightGo = new GameObject("RoomLight");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.0f;
            light.color = new Color(1f, 0.95f, 0.88f);
            lightGo.transform.rotation = Quaternion.Euler(38f, -30f, 0f);

            var vrmPath = Path.Combine(Application.streamingAssetsPath, "16.vrm");
            if (File.Exists(vrmPath))
            {
                try
                {
                    var vrm = await VrmUtility.LoadAsync(vrmPath, new RuntimeOnlyAwaitCaller());
                    vrm.ShowMeshes();
                    vrm.EnableUpdateWhenOffscreen();
                    var root = vrm.Root.transform;
                    root.position = center + new Vector3(-0.15f, 0f, -0.35f);
                    root.rotation = Quaternion.Euler(0f, 180f, 0f);

                    var rs = vrm.Root.GetComponentsInChildren<Renderer>();
                    if (rs.Length > 0)
                    {
                        var vb = new Bounds(rs[0].bounds.center, Vector3.zero);
                        foreach (var r in rs) vb.Encapsulate(r.bounds);
                        root.localScale = Vector3.one;
                        Debug.Log($"ROOM_CHARACTER_HEIGHT={vb.size.y:F3}m");
                    }
                    var bubble = gameObject.AddComponent<BubbleUI>();
                    var mouth = gameObject.AddComponent<XiaoqiMouth>();
                    mouth.Setup(vrm.Root, bubble);
                    var phone = gameObject.AddComponent<XiaoqiPhone>();
                    phone.Setup(vrm.Root);
                    FixMaterials(vrm.Root);
                    var animator = vrm.Root.GetComponent<Animator>();
                    if (animator == null) animator = vrm.Root.GetComponentInChildren<Animator>();
                    var controller = Resources.Load<RuntimeAnimatorController>("Animations/XiaoqiRoom");
                    if (animator != null && controller != null)
                    {
                        animator.runtimeAnimatorController = controller;
                        animator.applyRootMotion = false;
                        animator.Play("Idle");
                        Debug.Log("ROOM_ANIMATOR attached");
                    }

                    var idle = gameObject.AddComponent<IdleMotion>();
                    idle.Setup(vrm.Root);
                    if (animator != null && controller != null) idle.enabled = false;

                    var home = center + new Vector3(-0.15f, 0f, -0.35f);
                    var action = gameObject.AddComponent<RoomActionController>();
                    // 锚点来自 FBX bounds 探测：
                    // bed mesh_10.006 ≈ center + (-1.5, 0, -1.74)
                    // desk sm_401   ≈ center + (-0.02, 0, -2.39)，取桌前沿以免穿墙
                    action.Setup(vrm.Root, home,
                        center + new Vector3(-0.02f, 0f, -1.75f),
                        center + new Vector3(-1.20f, 0f, -1.60f),
                        bubble);

                    var streamCam = gameObject.AddComponent<StreamCameraController>();
                    streamCam.Setup(cam, vrm.Root.transform);

                    if (Array.IndexOf(Environment.GetCommandLineArgs(), "--room-demo") >= 0)
                        StartCoroutine(RoomDemo(action));
                    if (Array.IndexOf(Environment.GetCommandLineArgs(), "--stream-demo") >= 0)
                        StartCoroutine(StreamDemo(action, streamCam));

                    var expr = gameObject.AddComponent<XiaoqiExpression>();
                    expr.Setup(vrm.Root);
                    // U5：表情由 RoomStageFace 从 /api/body/stage 驱动。
                    // 旧 /api/avatar/state 的 StateClient/RoomAutonomy 不再接入。
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                }
            }

            Debug.Log($"ROOM_OK props={renderers.Length} floor={floor.size} center={center}");
        }

        private IEnumerator StreamDemo(RoomActionController action, StreamCameraController stream)
        {
            yield return new WaitForSeconds(2f);
            action.GoDesk();
            yield return new WaitForSeconds(2.5f);
            stream.ShowStream();
        }

        private IEnumerator RoomDemo(RoomActionController action)
        {
            yield return new WaitForSeconds(2f);
            action.GoDesk();
            yield return new WaitForSeconds(4f);
            action.GoBed();
        }

        private void FixMaterials(GameObject root)
        {
            var shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("UniGLTF/UniUnlit");
            if (shader == null) shader = Shader.Find("Unlit/Texture");
            if (shader == null) return;

            foreach (var r in root.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var old = mats[i];
                    if (old == null) continue;
                    var m = new Material(shader);
                    m.mainTexture = old.mainTexture;
                    m.color = old.color;
                    mats[i] = m;
                }
                r.sharedMaterials = mats;
            }
            Debug.Log("ROOM_MATERIALS " + shader.name);
        }

        private void BuildShell(Vector3 center)
        {
            var matFloor = new Material(Shader.Find("Standard")) { color = new Color(0.34f, 0.25f, 0.18f) };
            var matWall = new Material(Shader.Find("Standard")) { color = new Color(0.72f, 0.66f, 0.58f) };
            var matCeil = new Material(Shader.Find("Standard")) { color = new Color(0.55f, 0.55f, 0.6f) };

            MakeBox("Floor", center + new Vector3(0, -0.05f, 0), new Vector3(5.2f, 0.1f, 5.2f), matFloor);
            MakeBox("Wall_Back", center + new Vector3(0, 1.4f, 2.6f), new Vector3(5.2f, 2.8f, 0.1f), matWall);
            MakeBox("Wall_Left", center + new Vector3(-2.6f, 1.4f, 0), new Vector3(0.1f, 2.8f, 5.2f), matWall);
            MakeBox("Wall_Right", center + new Vector3(2.6f, 1.4f, 0), new Vector3(0.1f, 2.8f, 5.2f), matWall);
            MakeBox("Ceiling", center + new Vector3(0, 2.82f, 0), new Vector3(5.2f, 0.1f, 5.2f), matCeil);
            MakeBox("Wall_Front", center + new Vector3(0, 1.4f, -2.6f), new Vector3(5.2f, 2.8f, 0.1f), matWall);
        }

        private void MakeBox(string name, Vector3 pos, Vector3 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = pos;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().material = mat;
        }
    }
}

