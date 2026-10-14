using UnityEngine;

namespace Slice
{
    public class CamFollow : MonoBehaviour
    {
        public static CamFollow I;
        public Transform Target;
        public Vector3 Offset = new Vector3(0, 14f, -7.5f);
        public float Smooth = 7f;
        float _trauma;

        void Awake() { I = this; }

        public static void Shake(float amount)
        {
            if (I != null) I._trauma = Mathf.Min(1f, I._trauma + amount);
        }

        void LateUpdate()
        {
            if (Target == null) return;
            var goal = Target.position + Offset;
            transform.position = Vector3.Lerp(transform.position, goal, Time.unscaledDeltaTime * Smooth);
            _trauma = Mathf.Max(0f, _trauma - Time.unscaledDeltaTime * 1.6f);
            float s = _trauma * _trauma * 0.5f;
            transform.position += new Vector3(Random.Range(-s, s), Random.Range(-s, s) * 0.4f, Random.Range(-s, s));
            transform.rotation = Quaternion.Euler(58f, 0, 0);
        }
    }
}
