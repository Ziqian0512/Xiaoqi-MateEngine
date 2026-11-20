using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Xiaoqi.UI2
{
    // A read-only cache of the console's configured voice probability.
    // Unknown/stale policy means text only; there is no second local lottery.
    public static class XqConsoleVoicePolicy
    {
        public sealed class Rule { public bool enabled; public float probability; }
        static Rule _rule;
        static float _at=-999;
        static string _server, _token;
        public static Rule Parse(string json)
        {
            try
            {
                var voice=JObject.Parse(json).SelectToken("budget.voice");
                if (voice?["enabled"]?.Type!=JTokenType.Boolean) return null;
                var number=voice["probability"];
                if (number==null || (number.Type!=JTokenType.Float && number.Type!=JTokenType.Integer)) return null;
                float p=(float)number;
                if (float.IsNaN(p)||float.IsInfinity(p)||p<0||p>1) return null;
                return new Rule {enabled=(bool)voice["enabled"],probability=p};
            }
            catch { return null; }
        }
        internal static void Accept(string json, XiaoqiConfig cfg)
        { _rule=Parse(json); _at=Time.unscaledTime; _server=cfg.server; _token=cfg.token; }
        public static bool Fresh => _rule!=null && XiaoqiRuntime.Config!=null &&
            _server==XiaoqiRuntime.Config.server && _token==XiaoqiRuntime.Config.token && Time.unscaledTime-_at<=120;
        public static string Summary => !Fresh?"控制台规则尚未同步 · 暂只显示文字":
            !_rule.enabled?"控制台自动发声已关闭":"按控制台概率发声 · "+Mathf.RoundToInt(_rule.probability*100)+"%";
        public static bool Decide(Rule rule, bool fresh, float draw) => fresh && rule!=null && rule.enabled &&
            (rule.probability>=1 || draw<rule.probability);
        public static bool ShouldSpeak(float draw) => Decide(_rule,Fresh,draw);
    }
}
