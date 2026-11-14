using UnityEngine;

namespace Xiaoqi.Spike
{
    public class RoomStageFace : MonoBehaviour
    {
        private XiaoqiExpression _face;

        private bool _applied;

        private void Start()
        {
            XiaoqiLink.OnStage += Apply;
        }

        private void Update()
        {
            if (_face == null) _face = FindFirstObjectByType<XiaoqiExpression>();
            if (!_applied && _face != null && XiaoqiLink.Current != null)
            {
                _applied = true;
                Apply(XiaoqiLink.Current);
            }
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
