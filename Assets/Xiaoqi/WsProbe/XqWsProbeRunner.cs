using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using Xiaoqi.Host;

namespace Xiaoqi.WsProbe
{
    /// <summary>
    /// Isolated probe (branch xiaoqi/p3-ws-probe only) testing Unity 6.2+ World Space PanelSettings inside Mate Engine:
    ///   (a) Visibility & RGBA alpha under ME transparent desktop window
    ///   (b) 3D avatar depth occlusion (panel in front of avatar vs behind avatar)
    ///   (c) Mouse raycast / click hit accuracy (3 click points, BEFORE/AFTER logs via EventSystem/WorldDocumentRaycaster)
    /// Only activates when CLI contains -xq-ws-probe.
    /// </summary>
    public class XqWsProbeRunner : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-xq-ws-probe") < 0) return;
            var go = new GameObject("XqWsProbeRunner");
            DontDestroyOnLoad(go);
            go.AddComponent<XqWsProbeRunner>();
        }

        const float Ppu = 100f;
        UIDocument _doc;
        PanelSettings _ps;
        VisualElement _board;
        VisualElement _btnA;
        VisualElement _btnB;
        int _cntA, _cntB, _cntBoard;
        Texture2D _pixTex;

        static object GetProp(object obj, string name)
        {
            if (obj == null) return "null";
            var p = obj.GetType().GetProperty(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return p != null ? p.GetValue(obj, null) : "N/A";
        }

        IEnumerator Start()
        {
            Debug.Log("XQ_WSPROBE start");
            float t0 = Time.realtimeSinceStartup;
            while ((PetHost.I == null || !PetHost.I.Ready) && Time.realtimeSinceStartup - t0 < 10f)
                yield return null;
            yield return new WaitForSecondsRealtime(1.0f);

            var cam = Camera.main;
            var winCtrl = FindAnyObjectByType<Kirurobo.UniWindowController>();
            Debug.Log("XQ_WSPROBE env screen=" + Screen.width + "x" + Screen.height
                + " cam=" + (cam != null ? cam.name + " ortho=" + cam.orthographic + " clear=" + cam.clearFlags + " bg=" + cam.backgroundColor : "null")
                + " win=" + (winCtrl != null ? winCtrl.transparentType + "/" + winCtrl.hitTestType : "null")
                + " es=" + (EventSystem.current != null ? EventSystem.current.name : "null")
                + " queriesHitTriggers=" + Physics.queriesHitTriggers);

            var cfgGo = new GameObject("XqWsPanelInputConfig");
            DontDestroyOnLoad(cfgGo);
            var pic = cfgGo.AddComponent<PanelInputConfiguration>();
            pic.processWorldSpaceInput = true;
            pic.defaultEventCameraIsMainCamera = true;
            pic.autoCreatePanelComponents = true;

            if (EventSystem.current != null && EventSystem.current.GetComponent<WorldDocumentRaycaster>() == null)
            {
                var wdr = EventSystem.current.gameObject.AddComponent<WorldDocumentRaycaster>();
                Debug.Log("XQ_WSPROBE added WorldDocumentRaycaster to EventSystem cam=" + (wdr.eventCamera != null ? wdr.eventCamera.name : "null"));
            }

            _ps = Resources.Load<PanelSettings>("XqWsPanelSettings");
            if (_ps == null)
            {
                Debug.LogError("XQ_WSPROBE failed to load Resources/XqWsPanelSettings");
                yield break;
            }
            Debug.Log("XQ_WSPROBE ps name=" + _ps.name + " renderMode=" + GetProp(_ps, "renderMode") + " ppu=" + GetProp(_ps, "pixelsPerUnit")
                + " colliderMode=" + GetProp(_ps, "colliderUpdateMode") + " trigger=" + GetProp(_ps, "colliderIsTrigger"));

            var go = new GameObject("XqWsBoard");
            DontDestroyOnLoad(go);
            _doc = go.AddComponent<UIDocument>();
            _doc.panelSettings = _ps;
            _doc.worldSpaceSizeMode = UIDocument.WorldSpaceSizeMode.Fixed;
            _doc.worldSpaceSize = new Vector2(800f, 600f);

            yield return null;
            BuildUi();
            yield return null;

            if (cam == null) { Debug.LogError("XQ_WSPROBE no Camera.main"); yield break; }
            var bodyRect = PetHost.I != null ? PetHost.I.ScreenRect() : new Rect(Screen.width * 0.4f, Screen.height * 0.1f, 200f, 400f);
            Vector3 avatarCenter = cam.transform.position + cam.transform.forward * 2.5f;
            float avatarDepth = 2.5f;
            var anim = FindAvatarAnimator();
            if (anim != null)
            {
                var b = ComputeBounds(anim.gameObject);
                avatarCenter = b.center;
                avatarDepth = Vector3.Dot(avatarCenter - cam.transform.position, cam.transform.forward);
                Debug.Log("XQ_WSPROBE avatar bounds center=" + avatarCenter + " size=" + b.size + " depth=" + avatarDepth.ToString("0.000") + " screenRect=" + bodyRect);
            }

            // Pick a guaranteed opaque avatar pixel near torso/chest BEFORE board is placed near her
            go.transform.position = cam.transform.position + cam.transform.forward * (avatarDepth + 50f);
            go.transform.rotation = cam.transform.rotation;
            yield return new WaitForEndOfFrame();

            Vector2 avatarTestPx = new Vector2(bodyRect.center.x, bodyRect.center.y);
            Color baseAvatar = SamplePixel(avatarTestPx);
            if (baseAvatar.a < 0.8f)
            {
                for (float fx = 0.35f; fx <= 0.65f && baseAvatar.a < 0.8f; fx += 0.05f)
                for (float fy = 0.35f; fy <= 0.75f && baseAvatar.a < 0.8f; fy += 0.05f)
                {
                    var cand = new Vector2(bodyRect.xMin + bodyRect.width * fx, bodyRect.yMin + bodyRect.height * fy);
                    var c = SamplePixel(cand);
                    if (c.a >= 0.8f) { avatarTestPx = cand; baseAvatar = c; }
                }
            }
            Vector2 emptyPx = new Vector2(40f, Screen.height * 0.5f);
            Color baseEmpty = SamplePixel(emptyPx);
            Debug.Log("XQ_WSPROBE baseline avatarPx=" + FmtPt(avatarTestPx) + " rgba=" + FmtRgba(baseAvatar)
                + " | emptyPx=" + FmtPt(emptyPx) + " rgba=" + FmtRgba(baseEmpty));

            float frustumH = cam.orthographic ? cam.orthographicSize * 2f : 2f * avatarDepth * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float desiredBoardWorldH = frustumH * 0.50f;
            float baseWorldH = 600f / Ppu;
            float scale = desiredBoardWorldH / baseWorldH;
            go.transform.localScale = Vector3.one * scale;
            Ray centerRay = cam.ScreenPointToRay(avatarTestPx);

            var comps = go.GetComponents<Component>();
            var compNames = new List<string>();
            foreach (var c in comps) compNames.Add(c.GetType().Name);
            Debug.Log("XQ_WSPROBE board comps=[" + string.Join(",", compNames) + "] isWorldSpace=" + GetProp(_doc, "isWorldSpace")
                + " scale=" + scale.ToString("0.0000")
                + " boardWB=" + _board.worldBound + " btnAWB=" + _btnA.worldBound + " btnBWB=" + _btnB.worldBound);

            // ---- (A) + (B1) Panel IN FRONT of entire avatar (avatarDepth - 0.40m) ----
            PlaceAtDepth(go.transform, cam, centerRay, avatarDepth - 0.40f);
            yield return new WaitForSecondsRealtime(0.15f);
            yield return new WaitForEndOfFrame();

            Vector2 ptA = PanelPtToScreen(cam, new Vector2(160f, 130f)); // Pink BtnA (unoccluded top-left)
            Color cFrontA = SamplePixel(ptA);
            Vector2 px1 = cam.WorldToScreenPoint(go.transform.position);
            Color cFrontOverlap = SamplePixel(px1);              // Dead-center of board & avatar!
            Color cFrontOut = SamplePixel(emptyPx);
            var col = go.GetComponent<Collider>();
            Debug.Log("XQ_WSPROBE [A+B1 FRONT z=" + (avatarDepth - 0.40f).ToString("0.00") + "] collider=" + (col != null ? col.GetType().Name + " bounds=" + col.bounds : "NONE")
                + " | btnA=" + FmtPt(ptA) + " rgba=" + FmtRgba(cFrontA)
                + " | centerOverlap=" + FmtPt(px1) + " rgba=" + FmtRgba(cFrontOverlap)
                + " | outside=" + FmtPt(emptyPx) + " rgba=" + FmtRgba(cFrontOut));

            // ---- (B2) Panel BEHIND entire avatar (avatarDepth + 0.40m) ----
            PlaceAtDepth(go.transform, cam, centerRay, avatarDepth + 0.40f);
            yield return new WaitForSecondsRealtime(0.15f);
            yield return new WaitForEndOfFrame();

            Vector2 ptA2 = PanelPtToScreen(cam, new Vector2(160f, 130f)); // Orange/pink corner beside her
            Color cBackA = SamplePixel(ptA2);
            Color cBackOverlap = SamplePixel(px1);                       // Same center pixel: should show AVATAR!
            Color cBackOut = SamplePixel(emptyPx);
            Debug.Log("XQ_WSPROBE [B2 BEHIND z=" + (avatarDepth + 0.40f).ToString("0.00") + "] | btnA=" + FmtPt(ptA2) + " rgba=" + FmtRgba(cBackA)
                + " | centerOverlap=" + FmtPt(px1) + " rgba=" + FmtRgba(cBackOverlap)
                + " | outside=" + FmtPt(emptyPx) + " rgba=" + FmtRgba(cBackOut));

            // ---- (C) Mouse Hit Testing: 3 Click Points via EventSystem + WorldDocumentRaycaster ----
            PlaceAtDepth(go.transform, cam, centerRay, avatarDepth - 0.40f);
            yield return new WaitForSecondsRealtime(0.15f);
            yield return new WaitForEndOfFrame();

            Vector2 click1 = PanelPtToScreen(cam, new Vector2(160f, 130f)); // Inside BtnA
            Vector2 click2 = PanelPtToScreen(cam, new Vector2(580f, 400f)); // Inside BtnB
            Vector2 click3 = emptyPx;                                       // Outside panel completely

            yield return ProbeClickPoint(cam, 1, "BtnA", click1);
            yield return ProbeClickPoint(cam, 2, "BtnB", click2);
            yield return ProbeClickPoint(cam, 3, "Outside", click3);

            Debug.Log("XQ_WSPROBE DONE summary cntA=" + _cntA + " cntB=" + _cntB + " cntBoard=" + _cntBoard);
        }

        void BuildUi()
        {
            var root = _doc.rootVisualElement;
            root.Clear();
            root.style.width = 800; root.style.height = 600;
            root.style.backgroundColor = new Color(0, 0, 0, 0);

            _board = new VisualElement { name = "ws-board", pickingMode = PickingMode.Position };
            _board.style.position = Position.Absolute;
            _board.style.left = 20; _board.style.top = 20; _board.style.width = 760; _board.style.height = 560;
            _board.style.backgroundColor = new Color(0.96f, 0.42f, 0.08f, 1.0f); // High-contrast ORANGE so avatar vs panel never collides
            _board.style.borderTopLeftRadius = 24; _board.style.borderTopRightRadius = 24;
            _board.style.borderBottomLeftRadius = 24; _board.style.borderBottomRightRadius = 24;
            _board.RegisterCallback<ClickEvent>(e => { _cntBoard++; Debug.Log("XQ_WSPROBE cb Click target=ws-board cntBoard=" + _cntBoard); });
            _board.RegisterCallback<PointerDownEvent>(e => Debug.Log("XQ_WSPROBE cb PointerDown target=ws-board local=" + e.localPosition));

            _btnA = new VisualElement { name = "ws-btnA", pickingMode = PickingMode.Position };
            _btnA.style.position = Position.Absolute;
            _btnA.style.left = 40; _btnA.style.top = 40; _btnA.style.width = 240; _btnA.style.height = 180;
            _btnA.style.backgroundColor = new Color(1.00f, 0.20f, 0.55f, 1.0f); // bright pink
            _btnA.RegisterCallback<ClickEvent>(e => { _cntA++; Debug.Log("XQ_WSPROBE cb Click target=ws-btnA cntA=" + _cntA); });
            _btnA.RegisterCallback<PointerDownEvent>(e => Debug.Log("XQ_WSPROBE cb PointerDown target=ws-btnA local=" + e.localPosition));
            _btnA.Add(new Label("WS BTN A") { style = { fontSize = 24, color = Color.white } });

            _btnB = new VisualElement { name = "ws-btnB", pickingMode = PickingMode.Position };
            _btnB.style.position = Position.Absolute;
            _btnB.style.left = 440; _btnB.style.top = 300; _btnB.style.width = 280; _btnB.style.height = 200;
            _btnB.style.backgroundColor = new Color(0.10f, 0.80f, 0.70f, 1.0f); // bright teal
            _btnB.RegisterCallback<ClickEvent>(e => { _cntB++; Debug.Log("XQ_WSPROBE cb Click target=ws-btnB cntB=" + _cntB); });
            _btnB.RegisterCallback<PointerDownEvent>(e => Debug.Log("XQ_WSPROBE cb PointerDown target=ws-btnB local=" + e.localPosition));
            _btnB.Add(new Label("WS BTN B") { style = { fontSize = 24, color = Color.white } });

            _board.Add(_btnA);
            _board.Add(_btnB);
            root.Add(_board);
        }

        IEnumerator ProbeClickPoint(Camera cam, int idx, string tag, Vector2 screenPt)
        {
            Debug.Log("XQ_WSPROBE click#" + idx + " BEFORE tag=" + tag + " screenPt=" + FmtPt(screenPt)
                + " cntA=" + _cntA + " cntB=" + _cntB + " cntBoard=" + _cntBoard);

            Ray ray = cam.ScreenPointToRay(screenPt);
            bool physHit = Physics.Raycast(ray, out RaycastHit hit, 100f);
            string physDesc = physHit ? (hit.collider.gameObject.name + " dist=" + hit.distance.ToString("0.000")) : "NONE";

            var esDescs = new List<string>();
            if (EventSystem.current != null)
            {
                var ped = new PointerEventData(EventSystem.current)
                {
                    position = screenPt,
                    button = PointerEventData.InputButton.Left,
                    pointerId = -1,
                    eligibleForClick = true,
                    clickCount = 1,
                };
                var results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(ped, results);
                foreach (var r in results)
                {
                    esDescs.Add(r.gameObject.name + "@" + (r.module != null ? r.module.GetType().Name : "null") + ":d=" + r.distance.ToString("0.00") + ":wp=" + r.worldPosition);
                }

                // Drive the exact EventSystem raycast result target through ExecuteEvents (same path StandaloneInputModule takes)
                RaycastResult wsHit = default(RaycastResult);
                bool hasWs = false;
                foreach (var r in results)
                {
                    if (r.module is WorldDocumentRaycaster || (r.gameObject != null && r.gameObject.name == _ps.name))
                    {
                        wsHit = r; hasWs = true; break;
                    }
                }
                if (hasWs)
                {
                    ped.pointerCurrentRaycast = wsHit;
                    ped.pointerPressRaycast = wsHit;
                    ped.pressPosition = screenPt;
                    ExecuteEvents.ExecuteHierarchy(wsHit.gameObject, ped, ExecuteEvents.pointerEnterHandler);
                    ExecuteEvents.ExecuteHierarchy(wsHit.gameObject, ped, ExecuteEvents.pointerDownHandler);
                    ExecuteEvents.ExecuteHierarchy(wsHit.gameObject, ped, ExecuteEvents.pointerUpHandler);
                    ExecuteEvents.ExecuteHierarchy(wsHit.gameObject, ped, ExecuteEvents.pointerClickHandler);
                }
            }

            // Also test runtimePanel.Pick with panelPt mapped from plane intersection
            string pickDesc = "MISS_PLANE";
            Plane p = new Plane(-_doc.transform.forward, _doc.transform.position);
            if (p.Raycast(ray, out float enter))
            {
                Vector3 localHit = _doc.transform.InverseTransformPoint(ray.GetPoint(enter));
                float px = localHit.x * Ppu + 400f;
                float py = 300f - localHit.y * Ppu;
                var panelPt = new Vector2(px, py);
                var vt = _doc.runtimePanel != null ? _doc.runtimePanel.visualTree : null;
                VisualElement picked = _doc.runtimePanel != null ? _doc.runtimePanel.Pick(panelPt) : null;
                pickDesc = "pt=(" + px.ToString("0.0") + "," + py.ToString("0.0") + ") picked=" + (picked != null ? picked.name : "null")
                    + " vtRect=" + (vt != null ? vt.worldBound.ToString() : "null");
            }

            yield return null;
            Debug.Log("XQ_WSPROBE click#" + idx + " AFTER tag=" + tag
                + " phys=[" + physDesc + "]"
                + " es=[" + string.Join("; ", esDescs) + "]"
                + " " + pickDesc
                + " cntA=" + _cntA + " cntB=" + _cntB + " cntBoard=" + _cntBoard);
        }

        static void PlaceAtDepth(Transform t, Camera cam, Ray centerRay, float depthFromCam)
        {
            // Works for BOTH orthographic (ray shifts origin, constant forward) and perspective cameras!
            Vector3 fwd = cam.transform.forward;
            Vector3 dir = centerRay.direction.normalized;
            float curDepth = Vector3.Dot(centerRay.origin - cam.transform.position, fwd);
            float cos = Mathf.Max(0.2f, Vector3.Dot(dir, fwd));
            Vector3 pos = centerRay.origin + dir * ((depthFromCam - curDepth) / cos);
            t.position = pos;
            t.rotation = cam.transform.rotation;
        }

        Vector2 PanelPtToScreen(Camera cam, Vector2 panelPt)
        {
            float lx = (panelPt.x - 400f) / Ppu;
            float ly = (300f - panelPt.y) / Ppu;
            Vector3 w = _doc.transform.TransformPoint(new Vector3(lx, ly, 0f));
            Vector3 s = cam.WorldToScreenPoint(w);
            return new Vector2(s.x, s.y);
        }

        Color SamplePixel(Vector2 screenPt)
        {
            if (_pixTex == null) _pixTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            int x = Mathf.Clamp(Mathf.RoundToInt(screenPt.x), 0, Screen.width - 1);
            int y = Mathf.Clamp(Mathf.RoundToInt(screenPt.y), 0, Screen.height - 1);
            _pixTex.ReadPixels(new Rect(x, y, 1, 1), 0, 0);
            _pixTex.Apply();
            return _pixTex.GetPixel(0, 0);
        }

        static Animator FindAvatarAnimator()
        {
            foreach (var a in FindObjectsByType<Animator>(FindObjectsSortMode.None))
            {
                if (a.isHuman && a.gameObject.activeInHierarchy) return a;
            }
            return null;
        }

        static Bounds ComputeBounds(GameObject root)
        {
            Bounds b = default(Bounds); bool has = false;
            foreach (var r in root.GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled) continue;
                if (!has) { b = r.bounds; has = true; } else b.Encapsulate(r.bounds);
            }
            return b;
        }

        static string FmtPt(Vector2 v) { return "(" + v.x.ToString("0") + "," + v.y.ToString("0") + ")"; }
        static string FmtRgba(Color c) { return "(" + c.r.ToString("0.00") + "," + c.g.ToString("0.00") + "," + c.b.ToString("0.00") + ",a=" + c.a.ToString("0.00") + ")"; }
    }
}
