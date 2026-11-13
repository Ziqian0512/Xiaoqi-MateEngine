using System.Text.RegularExpressions;
using UnityEngine;

namespace Xiaoqi.Spike
{
    /// <summary>把 Python 状态里的 label/action 映射成房间行为。第一版用 label 做自主驱动。</summary>
    public class RoomAutonomy : MonoBehaviour
    {
        private RoomActionController _action;
        private BubbleUI _bubble;
        private string _last = "";
        private float _next;

        public void Setup(RoomActionController action, XiaoqiStateClient client, BubbleUI bubble)
        {
            _action = action;
            _bubble = bubble;
            client.OnState += OnState;
        }

        private void OnState(XiaoqiState s)
        {
            if (_action == null || s == null) return;
            if (Time.unscaledTime < _next) return;

            var label = s.label ?? "";
            var act = (s.action ?? "").ToLowerInvariant();
            string target = "";

            if (act.Contains("sleep") || Regex.IsMatch(label, "睡|困|躺|休息|蔫"))
                target = "bed";
            else if (act.Contains("study") || Regex.IsMatch(label, "自习|学习|上课|读|写|工作|上课"))
                target = "desk";
            else if (Regex.IsMatch(label, "吃|饭|做饭"))
                target = "home";

            if (target == "" || target == _last) return;
            _last = target;
            _next = Time.unscaledTime + 2.5f;

            if (_bubble != null) _bubble.Show("她：" + (label == "" ? target : label), 2f);
            if (target == "bed") _action.GoBed();
            else if (target == "desk") _action.GoDesk();
            else _action.GoHome();
        }
    }
}
