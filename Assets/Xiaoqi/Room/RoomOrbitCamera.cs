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

            if (Input.GetMouseButton(0) && !Input.GetKey(KeyCode.LeftShift))
            {
                _yaw += Input.GetAxis("Mouse X") * 4f;
                _pitch -= Input.GetAxis("Mouse Y") * 3f;
                _pitch = Mathf.Clamp(_pitch, 5f, 60f);
            }
            _distance = Mathf.Clamp(_distance - Input.mouseScrollDelta.y * 0.35f, 2.0f, 7.0f);

            var rot = Quaternion.Euler(_pitch, _yaw, 0f);
            _cam.transform.position = _target + rot * new Vector3(0f, 0f, -_distance);
            _cam.transform.LookAt(_target);
        }
    }
}
