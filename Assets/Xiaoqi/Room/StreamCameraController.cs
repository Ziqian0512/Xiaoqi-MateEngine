using UnityEngine;

namespace Xiaoqi.Spike
{
    /// <summary>直播固定镜头：只框坐姿上半身/头部。</summary>
    public class StreamCameraController : MonoBehaviour
    {
        private Camera _cam;
        private Transform _character;
        private Vector3 _roomPos;
        private Quaternion _roomRot;
        private float _roomFov;
        private bool _stream;

        public void Setup(Camera cam, Transform character)
        {
            _cam = cam;
            _character = character;
            _roomPos = cam.transform.position;
            _roomRot = cam.transform.rotation;
            _roomFov = cam.fieldOfView;
        }

        public void Toggle()
        {
            if (_stream) ShowRoom();
            else ShowStream();
        }

        public void ShowStream()
        {
            if (_cam == null || _character == null) return;
            _stream = true;
            var root = _character;
            var front = root.forward;
            var target = root.position + Vector3.up * 1.05f;
            _cam.transform.position = target + front * 1.35f + Vector3.up * 0.15f;
            _cam.transform.LookAt(target + root.up * 0.05f);
            _cam.fieldOfView = 34f;
        }

        public void ShowRoom()
        {
            if (_cam == null) return;
            _stream = false;
            _cam.transform.position = _roomPos;
            _cam.transform.rotation = _roomRot;
            _cam.fieldOfView = _roomFov;
        }
    }
}
