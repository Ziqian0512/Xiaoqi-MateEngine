using UnityEngine;

namespace Xiaoqi.Spike
{
    public class RoomStageDriver : MonoBehaviour
    {
        private RoomActionController _action;

        private bool _applied;

        private void Start()
        {
            XiaoqiLink.OnStage += Apply;
        }

        private void Update()
        {
            if (_action == null) _action = FindFirstObjectByType<RoomActionController>();
            if (!_applied && _action != null && XiaoqiLink.Current != null)
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
            if (_action == null || s == null) return;
            switch (s.spot)
            {
                case "bed":
                    _action.GoBed();
                    break;
                case "door":
                case "window":
                case "wardrobe":
                    _action.GoHome();
                    break;
                case "desk":
                default:
                    _action.GoDesk();
                    break;
            }
            _action.SetPose(s.pose);
            Debug.Log("XIAOQI_STAGE " + s.where + " spot=" + s.spot + " pose=" + s.pose);
        }
    }
}
