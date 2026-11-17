using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using Xiaoqi.Host;
using Xiaoqi.Mode;

namespace Xiaoqi.Probe
{
    /// <summary>
    /// AGY 取证探针（只在 xiaoqi/agy-* 对比分支，不合入 int-all）。命令行 -xq-shot DIR 才启用。
    /// 隔离桌面截不到屏、注入不了鼠标，所以在进程内：
    ///   - 用 ScreenCapture 在帧末抓整帧（含 UI Toolkit 覆盖层，透明处 alpha=0）；
    ///   - 反射改 XqBenchPanel 的静态视差输入 _pxS/_pyS 模拟「光标在最左/最右」；
    ///   - 反射改 _sx/_sy + 调 ApplyStagePos/ApplyPet 模拟「拖板」；
    ///   - 直接调 XqMode.Set 走两条入口与 5 轮来回。
    /// 不改任何正式逻辑。
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public class XqShotProbe : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var dir = Arg("-xq-shot");
            if (string.IsNullOrEmpty(dir)) return;
            var go = new GameObject("XqShotProbe");
            DontDestroyOnLoad(go);
            var p = go.AddComponent<XqShotProbe>();
            p._dir = dir;
            p._scn = Arg("-xq-shot-scn") ?? "pet";
        }

        static string Arg(string k)
        {
            var a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++) if (a[i] == k) return a[i + 1];
            return null;
        }

        string _dir, _scn;
        bool _force; float _fx, _fy;
        static readonly BindingFlags SF = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
        static readonly BindingFlags IF = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        Type _bp;

        void Update()
        {
            if (!_force || _bp == null) return;
            SetS("_pxS", _fx); SetS("_pyS", _fy);
        }

        void SetS(string n, float v) { var f = _bp.GetField(n, SF); if (f != null) f.SetValue(null, v); }
        object GetS(string n) { var f = _bp.GetField(n, SF); return f != null ? f.GetValue(null) : null; }

        IEnumerator Start()
        {
            Directory.CreateDirectory(_dir);
            _bp = typeof(Xiaoqi.UI2.XqBenchPanel);
            while (PetHost.I == null || !PetHost.I.Ready) yield return null;
            yield return new WaitForSecondsRealtime(6f);
            Debug.Log("XQ_SHOT start scn=" + _scn + " screen=" + Screen.width + "x" + Screen.height);
            yield return Shot("00_pet");

            if (_scn == "close")
            {
                XqMode.Set(XqView.Close);
                yield return WaitIdle(4f);
                yield return Shot("01_close");
            }
            bool set = XqMode.Set(XqView.Bench);
            Debug.Log("XQ_SHOT set bench=" + set + " from=" + _scn);
            yield return WaitIdle(6f);
            _force = true; _fx = 0f; _fy = 0f;
            yield return new WaitForSecondsRealtime(2.0f);
            yield return Shot("02_bench_center");
            LogPet("bench");

            // 站姿 20 秒不变
            yield return Shot("03_still_t0"); LogPet("still0");
            yield return new WaitForSecondsRealtime(10f);
            yield return Shot("04_still_t10"); LogPet("still10");
            yield return new WaitForSecondsRealtime(10f);
            yield return Shot("05_still_t20"); LogPet("still20");

            // 视差：光标最左 / 最右
            _fx = -1f; _fy = 0f; yield return new WaitForSecondsRealtime(1.6f);
            yield return Shot("06_par_left"); LogPet("parL");
            _fx = 1f; _fy = 0f; yield return new WaitForSecondsRealtime(1.6f);
            yield return Shot("07_par_right"); LogPet("parR");
            _fx = 0f; _fy = 0f; yield return new WaitForSecondsRealtime(1.6f);

            // 拖板：模拟拖到右下 / 拖出屏
            var inst = _bp.GetField("_i", SF).GetValue(null);
            yield return Drag(inst, 0.55f, 0.18f, "08_drag_mid");
            yield return Drag(inst, 9f, 9f, "09_drag_edge");
            yield return Drag(inst, -1f, -1f, "10_drag_reset");

            // 5 轮来回
            for (int r = 1; r <= 5; r++)
            {
                XqMode.Set(XqView.Close); yield return WaitIdle(4f);
                XqMode.Set(XqView.Bench); yield return WaitIdle(5f);
                XqMode.Set(XqView.Pet); yield return WaitIdle(5f);
                LogPet("round" + r + "-pet");
                if (r < 5) { XqMode.Set(XqView.Bench); yield return WaitIdle(6f); }
            }
            _force = false;
            yield return new WaitForSecondsRealtime(2f);
            yield return Shot("11_pet_after");
            LogPet("final");
            Debug.Log("XQ_SHOT done");
            yield return new WaitForSecondsRealtime(0.5f);
            Application.Quit();
        }

        IEnumerator Drag(object inst, float sx, float sy, string name)
        {
            SetS("_sx", sx); SetS("_sy", sy);
            try
            {
                var ap = _bp.GetMethod("ApplyStagePos", IF); if (ap != null && inst != null) ap.Invoke(inst, null);
                var pp = _bp.GetMethod("ApplyPet", SF); if (pp != null) pp.Invoke(null, null);
            }
            catch (Exception e) { Debug.Log("XQ_SHOT drag err " + e.Message); }
            Debug.Log("XQ_SHOT drag req=" + sx + "," + sy + " now=" + GetS("_sx") + "," + GetS("_sy"));
            yield return new WaitForSecondsRealtime(2.2f);
            yield return Shot(name); LogPet(name);
        }

        IEnumerator WaitIdle(float min)
        {
            float t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < min || XqMode.Busy) yield return null;
        }

        void LogPet(string tag)
        {
            var h = UnityEngine.Object.FindFirstObjectByType<AvatarBigScreenHandler>();
            var a = h != null ? h.avatarAnimator : null;
            var r = PetHost.I != null ? PetHost.I.ScreenRect() : default(Rect);
            Debug.Log("XQ_SHOT pet tag=" + tag + " mode=" + XqMode.Current
                + (a != null ? " idle=" + a.GetFloat("IdleIndex").ToString("0.00") + " speed=" + a.speed.ToString("0.00")
                    + " rot=" + a.transform.rotation.eulerAngles.ToString("0.0")
                    + " state=" + a.GetCurrentAnimatorStateInfo(0).shortNameHash : " anim=null")
                + " rect=" + r.x.ToString("0") + "," + r.y.ToString("0") + " " + r.width.ToString("0") + "x" + r.height.ToString("0")
                + " pxS=" + GetS("_pxS"));
        }

        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            Texture2D tex = null;
            try
            {
                tex = ScreenCapture.CaptureScreenshotAsTexture();
                var path = Path.Combine(_dir, name + ".png");
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Debug.Log("XQ_SHOT saved " + path + " " + tex.width + "x" + tex.height);
            }
            catch (Exception e) { Debug.Log("XQ_SHOT shot err " + e.Message); }
            if (tex != null) Destroy(tex);
        }
    }
}
