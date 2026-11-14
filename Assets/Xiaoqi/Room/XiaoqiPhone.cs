using UnityEngine;

namespace Xiaoqi.Spike
{
    public class XiaoqiPhone : MonoBehaviour
    {
        private GameObject _phone;

        public void Setup(GameObject avatarRoot)
        {
            var animator = avatarRoot.GetComponentInChildren<Animator>();
            var hand = animator != null && animator.isHuman
                ? animator.GetBoneTransform(HumanBodyBones.RightHand)
                : null;
            _phone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _phone.name = "XiaoqiPhone";
            _phone.transform.SetParent(hand != null ? hand : avatarRoot.transform, false);
            _phone.transform.localScale = new Vector3(0.055f, 0.095f, 0.012f);
            _phone.transform.localPosition = new Vector3(0f, 0.02f, 0.01f);
            _phone.transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
            var renderer = _phone.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material = new Material(Shader.Find("Standard")) { color = new Color(0.04f, 0.04f, 0.05f) };
            _phone.SetActive(false);
        }

        public void SetTyping(bool typing)
        {
            if (_phone != null) _phone.SetActive(typing);
        }
    }
}
