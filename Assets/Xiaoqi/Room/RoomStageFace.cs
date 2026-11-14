using UnityEngine;

namespace Xiaoqi.Spike
{
    public class RoomStageFace : MonoBehaviour
    {
        private XiaoqiExpression _face;

        private void Start()
        {
            _face = FindFirstObjectByType<XiaoqiExpression>();
            XiaoqiLink.OnStage += Apply;
            if (XiaoqiLink.Current != null) Apply(XiaoqiLink.Current);
        }

        private void OnDestroy()
        {
            XiaoqiLink.OnStage -= Apply;
        }

        private void Apply(StageData s)
        {
            if (_face == null || s == null || s.face == null) return;
            _face.Apply(s.face.top);
        }
    }
}
