using System.Collections;
using UnityEngine;

namespace Xiaoqi.Spike
{
    /// <summary>房间占位动作：走到锚点、坐下、躺下。正式动画接入前先验证行为链路。</summary>
    public class RoomActionController : MonoBehaviour
    {
        private Transform _root;
        private Animator _animator;
        private IdleMotion _idle;
        private BubbleUI _bubble;

        private Vector3 _home;
        private Vector3 _desk;
        private Vector3 _bed;

        private bool _moving;
        private string _pose = "";

        public void Setup(GameObject root, Vector3 home, Vector3 desk, Vector3 bed, BubbleUI bubble)
        {
            _root = root.transform;
            _animator = root.GetComponent<Animator>() ?? root.GetComponentInChildren<Animator>();
            _idle = GetComponent<IdleMotion>();
            _bubble = bubble;
            _home = home;
            _desk = desk;
            _bed = bed;
        }

        public void GoDesk()
        {
            if (!_moving) StartCoroutine(MoveTo(_desk, true, "去书桌"));
        }

        public void GoBed()
        {
            if (!_moving) StartCoroutine(MoveTo(_bed, false, "去床上"));
        }

        public void GoHome()
        {
            if (!_moving) StartCoroutine(MoveTo(_home, false, "回到房间中间"));
        }

        public void SetPose(string pose)
        {
            _pose = pose ?? "";
            if (!_moving) ApplyPose();
        }

        private void ApplyPose()
        {
            if (_animator == null) return;
            switch (_pose)
            {
                case "sleep":
                    _animator.Play("Sleep");
                    break;
                case "lie_phone":
                    _animator.Play("LiePhone");
                    break;
                case "sit_write":
                    _animator.Play("SitWrite");
                    break;
                case "sit_eat":
                    _animator.Play("SitEat");
                    break;
                case "sit_idle":
                    _animator.Play("Sit");
                    break;
                case "stand_tidy":
                    _animator.Play("StandTidy");
                    break;
                case "stand_window":
                    _animator.Play("StandWindow");
                    break;
                case "walk_out":
                case "walk_in":
                    _animator.Play("Walk");
                    break;
            }
        }

        private IEnumerator MoveTo(Vector3 target, bool deskPose, string label)
        {
            _moving = true;
            if (_idle != null) _idle.enabled = false;
            if (_animator != null) _animator.Play("Walk");
            if (_bubble != null) _bubble.Show(label, 2f);

            var start = _root.position;
            target.y = start.y;
            float t = 0f;
            float duration = Mathf.Max(0.8f, Vector3.Distance(start, target) / 1.1f);
            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                _root.position = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t));
                var dir = target - start;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.001f)
                    _root.rotation = Quaternion.Slerp(_root.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);
                yield return null;
            }
            _root.position = target;

            if (deskPose)
            {
                _root.position = target + Vector3.up * 0.62f;
                _root.rotation = Quaternion.Euler(0f, 180f, 0f);
                if (_animator != null) _animator.Play("Sit");
                if (_bubble != null) _bubble.Show("坐下用电脑", 3f);
            }
            else
            {
                // 动画包里没有 Lie，先用坐姿占位；正式 Sleep 动画后再替换
                _root.position = target + Vector3.up * 0.35f;
                _root.rotation = Quaternion.Euler(0f, 180f, 0f);
                if (_animator != null) _animator.Play("Sit");
                if (_bubble != null) _bubble.Show("躺下休息", 3f);
            }

            ApplyPose();
            _moving = false;
        }
    }
}
