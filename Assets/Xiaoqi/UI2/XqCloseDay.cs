using System;

namespace Xiaoqi.UI2
{
    /// <summary>
    /// `/api/body/day` 的最小映射，字段与 XqTodayCard 里那份一致（同一接口，不另立口径）。
    /// 只取近景要用的：现在几点、今天的段，用来给「下一件事」兜底。
    /// </summary>
    [Serializable]
    public class XqCloseDay
    {
        public int v;
        public string date;
        public string now;
        public int now_min;
        public Seg[] segs;
        public StageData.StageBody body;
        public string say;

        [Serializable]
        public class Seg
        {
            public int a, b;
            public string start, end, name, act, where;
        }

        /// <summary>按开始时间找一个段（给 stage.next.at 对齐事项名用）。</summary>
        public Seg At(string start)
        {
            if (segs == null || string.IsNullOrEmpty(start)) return null;
            foreach (var s in segs)
                if (s != null && s.start == start) return s;
            return null;
        }

        /// <summary>下一段：now_min 之后最早开始的一段；没有就 null。</summary>
        public Seg Upcoming()
        {
            if (segs == null || now_min <= 0) return null;
            Seg best = null;
            foreach (var s in segs)
            {
                if (s == null) continue;
                if (s.a <= now_min) continue;
                if (best == null || s.a < best.a) best = s;
            }
            return best;
        }

        public static string When(Seg s)
        {
            if (s == null) return "";
            if (!string.IsNullOrEmpty(s.start) && !string.IsNullOrEmpty(s.end)) return s.start + "–" + s.end;
            if (!string.IsNullOrEmpty(s.start)) return s.start;
            return s.end ?? "";
        }

        public static string What(Seg s)
        {
            if (s == null) return "";
            if (!string.IsNullOrEmpty(s.name)) return s.name;
            if (!string.IsNullOrEmpty(s.act)) return s.act;
            return Place(s.where);
        }

        /// <summary>where 枚举只有 dorm / out 两个在客户端出现过，其余原样显示，不臆造中文。</summary>
        public static string Place(string where)
        {
            if (string.IsNullOrEmpty(where)) return "";
            if (where == "dorm") return "宿舍";
            if (where == "out") return "外出";
            return where;
        }
    }
}
