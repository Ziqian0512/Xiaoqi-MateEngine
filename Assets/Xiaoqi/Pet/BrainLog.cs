using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Xiaoqi.Spike
{
    /// <summary>
    /// XQ: P1-4c brain.log。每行一个决定：时间、src、候选、选中、理由。
    /// 位置 %LOCALAPPDATA%/Xiaoqi/brain.log，不进库、不上传。超过 1 MB 改名为 brain.log.1 重开。
    /// 格式（空格分隔的 key=value，候选里用 | 分隔）：
    ///   2026-11-16 12:50:03 src=random evt=idle pick=7:PET_IDLE_4 fade=0.62 cand=3:PET_IDLE_3(1.2)|7:PET_IDLE_4(1.8) why=pool
    /// </summary>
    public static class BrainLog
    {
        private static string _path;
        private static readonly object _lock = new object();

        public static string PathNow
        {
            get
            {
                if (_path != null) return _path;
                try
                {
                    var dir = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Xiaoqi");
                    Directory.CreateDirectory(dir);
                    _path = System.IO.Path.Combine(dir, "brain.log");
                }
                catch (Exception e) { Debug.LogWarning("XQ_BRAIN path " + e.Message); _path = ""; }
                return _path;
            }
        }

        /// <param name="src">local | server | env | random</param>
        /// <param name="evt">idle | hover | drag_end | ...</param>
        /// <param name="rest">其余 key=value，调用方自己拼好，不要含换行</param>
        public static void Write(string src, string evt, string rest)
        {
            var line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " src=" + src + " evt=" + evt
                       + (string.IsNullOrEmpty(rest) ? "" : " " + rest);
            Debug.Log("XQ_BRAIN " + line);
            var p = PathNow;
            if (string.IsNullOrEmpty(p)) return;
            try
            {
                lock (_lock)
                {
                    var fi = new FileInfo(p);
                    if (fi.Exists && fi.Length > 1024 * 1024)
                    {
                        var old = p + ".1";
                        if (File.Exists(old)) File.Delete(old);
                        File.Move(p, old);
                    }
                    File.AppendAllText(p, line + "\n", new UTF8Encoding(false));
                }
            }
            catch (Exception e) { Debug.LogWarning("XQ_BRAIN write " + e.Message); }
        }
    }
}
