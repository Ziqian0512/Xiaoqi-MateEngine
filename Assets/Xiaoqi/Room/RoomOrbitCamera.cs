using UnityEngine;

namespace Xiaoqi.Spike
{
    public class RoomOrbitCamera : MonoBehaviour
    {
        private Camera _cam;
        private Vector3 _target;
        private float _yaw;
        private float _pitch = 18f;
        private float _distance = 3.6f;

        public void Setup(Camera cam, Vector3 target)
        {
            _cam = cam;
            _target = target;
            var offset = cam.transform.position - target;
            _distance = offset.magnitude;
            var angles = Quaternion.LookRotation(target - cam.transform.position).eulerAngles;
            _yaw = angles.y;
            _pitch = 18f;
        }

        private void LateUpdate()
        {
            if (_cam == null) return;

            // 只允许按住鼠标中键绕房间中心旋转，避免左键拖拽/滚轮把相机拉出房间。
            if (Input.GetMouseButton(2))
            {
                _yaw += Input.GetAxis("Mouse X") * 3.2f;
                _pitch -= Input.GetAxis("Mouse Y") * 2.4f;
                _pitch = Mathf.Clamp(_pitch, 8f, 38f);
            }
            _distance = 3.6f;

            var rot = Quaternion.Euler(_pitch, _yaw, 0f);
            var pos = _target + rot * new Vector3(0f, 0f, -_distance);
            pos.x = Mathf.Clamp(pos.x, -2.2f, 2.2f);
            pos.z = Mathf.Clamp(pos.z, -2.2f, 2.2f);
            pos.y = Mathf.Clamp(pos.y, 0.8f, 2.4f);
            _cam.transform.position = pos;
            _cam.transform.LookAt(_target);
        }
    }
}
