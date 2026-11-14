using UnityEngine;

namespace Xiaoqi.Spike
{
    public class RoomStageDriver : MonoBehaviour
    {
        private RoomActionController _action;
        private XiaoqiPhone _phone;

        private bool _applied;

        private void Start()
        {
            XiaoqiLink.OnStage += Apply;
        }

        private void Update()
        {
            if (_action == null) _action = FindFirstObjectByType<RoomActionController>();
            if (_phone == null) _phone = FindFirstObjectByType<XiaoqiPhone>();
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
            if (_phone != null && s.phone != null) _phone.SetTyping(s.phone.typing);
            Debug.Log("XIAOQI_STAGE " + s.where + " spot=" + s.spot + " pose=" + s.pose
                      + " typing=" + (s.phone != null && s.phone.typing));
        }
    }
}
