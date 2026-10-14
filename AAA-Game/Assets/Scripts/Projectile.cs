using UnityEngine;

namespace Slice
{
    public class Projectile : MonoBehaviour
    {
        public float Speed = 22f;
        public float Damage = 10f;
        public float MaxDist = 30f;
        public bool Hostile;
        public float Radius = 0.35f;

        Vector3 _dir;
        float _traveled;
        TrailRenderer _trail;

        public void Fire(Vector3 dir, float damage, bool hostile)
        {
            _dir = dir.normalized;
            Damage = damage;
            Hostile = hostile;
            transform.rotation = Quaternion.LookRotation(_dir);
        }

        void Update()
        {
            float step = Speed * Time.deltaTime;
            var from = transform.position;
            transform.position += _dir * step;
            _traveled += step;
            if (_traveled > MaxDist) { Destroy(gameObject); return; }

            // swept sphere-cast along this frame's movement so fast shots
            // can't tunnel through enemies at low frame rates
            var hits = Physics.SphereCastAll(from, Radius, _dir, step + Radius * 0.5f);
            foreach (var rh in hits)
            {
                var h = rh.collider;
                if (h == null) continue;
                var hp = h.GetComponentInParent<Health>();
                if (hp == null || hp.IsDead)
                {
                    if (!h.isTrigger) { Destroy(gameObject); return; } // wall/barrier blocks
                    continue;
                }
                if (Hostile && h.GetComponentInParent<PlayerController>() == null) continue;
                if (!Hostile && h.GetComponentInParent<Enemy>() == null) continue;
                hp.Damage(Damage);
                Fx.Burst(transform.position, Hostile ? Fx.HitPlayerColor : Fx.HitColor, 10);
                Destroy(gameObject);
                return;
            }
        }
    }
}
