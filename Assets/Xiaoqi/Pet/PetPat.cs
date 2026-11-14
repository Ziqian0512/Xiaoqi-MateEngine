using UnityEngine;
using Xiaoqi.Host;

namespace Xiaoqi.Spike
{
    /// <summary>双击头顶 = 摸头：表情害羞 + 气泡一句。单击今天卡逻辑不受影响。</summary>
    public class PetPat : MonoBehaviour
    {
        private float _lastTap = -1f;
        private float _downAt;
        private Vector2 _downPos;
        private bool _downOnHead;

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                _downAt = Time.unscaledTime;
                _downPos = Input.mousePosition;
                _downOnHead = HitHead(Input.mousePosition);
            }
            if (!Input.GetMouseButtonUp(0)) return;

            float dt = Time.unscaledTime - _downAt;
            float moved = Vector2.Distance(Input.mousePosition, _downPos);
            if (!_downOnHead || dt > 0.3f || moved > 6f) return;

            if (Time.unscaledTime - _lastTap < 0.45f)
            {
                _lastTap = -1f;
                Pat();
            }
            else _lastTap = Time.unscaledTime;
        }

        private static bool HitHead(Vector3 screen)
        {
            if (PetHost.I == null) return false;
            var r = PetHost.I.ScreenRect();
            if (r.height < 2f) return false;
            var head = new Rect(r.x, r.y + r.height * 0.72f, r.width, r.height * 0.28f);
            return head.Contains(new Vector2(screen.x, screen.y));
        }

        private static void Pat()
        {
            XiaoqiFace.DebugSpike("害羞");
            PetBubble.Enqueue("唔…别揉头啦", "");
            Debug.Log("XQ_PAT");
        }
    }
}
