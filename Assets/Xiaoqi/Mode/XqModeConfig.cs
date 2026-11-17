using System;
using System.IO;
using UnityEngine;

namespace Xiaoqi.Mode
{
    /// <summary>
    /// P2/P3 的调参。单独放一个文件 xq_mode.json（和 xiaoqi_config.json 同目录），
    /// 不动 XiaoqiConfig，免得和设置页那条线冲突。文件不存在就用下面的默认值，不自动创建。
    /// 启动参数 -xq-mode close|bench 用来无人值守测试；-xq-bench 打开工作台入口。
    /// </summary>
    [Serializable]
    public class XqModeConfig
    {
        // ---- 入口 ----
        public bool hotkeys = false;          // Ctrl+Alt+1/2/3，默认关（设置页「快捷键切换形态」接这里）
        public bool bench_enabled = false;    // P2 通过前，工作台入口是灰的

        // ---- 近景镜头（P2-2，0 = 沿用 ME 默认）----
        public float close_zoom = 0f;         // AvatarBigScreenHandler.TargetZoom
        public float close_yoffset = -0.015f; // XQ: P2-2 R-1 实机标定初值（稳定后按双眼骨骼 WorldToViewportPoint 细调至 vx=0.50 vy=0.57）

        // ---- 工作台窗口（P3-1/2，比例按 1920x1080 标准图：左 7.3% 上 29.6% 宽 85.4% 高 68.5%）----
        public float bench_left_pct = 0.073f;
        public float bench_top_pct = 0.296f;
        public float bench_w_pct = 0.854f;
        public float bench_h_pct = 0.685f;
        public int bench_min_w = 900;
        public int bench_min_h = 560;
        public float bench_sit_pct = 0.49f;   // 她坐在窗口宽度的 49%
        public int bench_sit_right_gap = 220; // 离窗口右边缘至少多少物理像素（右上角三个系统按钮）
        public float bench_pet_size = 0f;     // >0 时进入工作台临时改 Size，退出恢复；0 = 不动（等实机标定）
        public string bench_theme = "pink";
        // ---- G74 W1 原生工作台（默认走 Unity 原生；-xq-bench-web 或 bench_web=true 才开旧 Edge 网页版）----
        public bool bench_web = false;
        public float bench_pet_frac = 0.34f;  // 她在工作台里的身高占屏高比例（夹在 0.28~0.42，和桌宠差不多大）
        // ---- G74 W2：她当「背景立绘」固定在右侧（置底不动），板在左边半透明；陀螺仪视差 ----
        public int bench_stage_mode = 0;         // 0 = 胸像/半身特写（默认）  1 = 全身缩进板高
        public float bench_bust_frac = 1.5f;     // 胸像：她身高 = 屏高的多少倍（0.9~2.6），越大越近
        public float bench_bust_top = 0.02f;     // 胸像：头顶低于板上沿多少（以板高为 1）
        public float bench_stage_frac = 0.92f;   // 全身：身高 = 板高的多少（0.5~1.0）
        public float bench_parallax_far = 0.012f; // 她（远景）随光标平移幅度（视口比例，0~0.05）
        public float bench_stage_dx = 0f;        // 她相对板中心左右偏多少（以板高为 1）
        public float bench_stage_top = 0f;       // （已不用）
        public bool bench_parallax = true;       // 陀螺仪 / 光标视差：整块舞台刚体平移+倾转，她在后面真 3D 转身
        public float bench_parallax_deg = 9f;    // 她最多转多少度（0~18）
        public float bench_parallax_px = 12f;    // 舞台最多平移多少设计 px
        public float bench_parallax_rot = 0f;    // 舞台 2D 倾转（默认关：一张纸在晃）
        public float bench_eye_vx = 0.11f;    // 眼睛落点（视口 0~1，左下原点）
        public float bench_eye_vy = 0.80f;
        public string edge_path = "";         // 空 = 自动找 msedge.exe

        private static XqModeConfig _i;

        public static XqModeConfig I
        {
            get
            {
                if (_i == null) _i = Load();
                return _i;
            }
        }

        public static bool HasArg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            return Array.IndexOf(args, name) >= 0;
        }

        public static string ArgValue(string name)
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }

        private static XqModeConfig Load()
        {
            XqModeConfig cfg = null;
            try
            {
                var path = Path.Combine(Application.dataPath, "..", "xq_mode.json");
                if (File.Exists(path)) cfg = JsonUtility.FromJson<XqModeConfig>(File.ReadAllText(path));
            }
            catch (Exception e)
            {
                Debug.LogWarning("XQ_MODE config: " + e.Message);
            }
            if (cfg == null) cfg = new XqModeConfig();
            if (HasArg("-xq-bench")) cfg.bench_enabled = true;
            if (HasArg("-xq-hotkeys")) cfg.hotkeys = true;
            if (HasArg("-xq-bench-web")) cfg.bench_web = true;
            return cfg;
        }
    }
}
