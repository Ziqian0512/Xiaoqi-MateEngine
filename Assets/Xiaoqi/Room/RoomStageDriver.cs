using UnityEngine;

namespace Xiaoqi.Spike
{
    public class RoomStageDriver : MonoBehaviour
    {
        private RoomActionController _action;

        private void Start()
        {
            _action = FindFirstObjectByType<RoomActionController>();
            XiaoqiLink.OnStage += Apply;
            if (XiaoqiLink.Current != null) Apply(XiaoqiLink.Current);
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
            Debug.Log("XIAOQI_STAGE " + s.where + " spot=" + s.spot + " pose=" + s.pose);
        }
    }
}
