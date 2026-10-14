using UnityEngine;

namespace Slice
{
    public class Weapon : MonoBehaviour
    {
        public float Damage = 12f;
        public float FireRate = 5f;      // shots per second
        public int ProjectileCount = 1;
        public float SpreadDeg = 10f;
        public float ProjectileSpeed = 26f;
        public GameObject ProjectilePrefab;
        public Transform Muzzle;

        float _cd;

        void Update() { _cd -= Time.deltaTime; }

        public bool TryFire(Vector3 dir)
        {
            if (_cd > 0f || ProjectilePrefab == null) return false;
            _cd = 1f / Mathf.Max(0.1f, FireRate);

            for (int i = 0; i < ProjectileCount; i++)
            {
                float t = ProjectileCount == 1 ? 0f : (i / (float)(ProjectileCount - 1)) - 0.5f;
                var rot = Quaternion.AngleAxis(t * SpreadDeg * (ProjectileCount - 1), Vector3.up);
                var go = Instantiate(ProjectilePrefab, Muzzle.position, Quaternion.identity);
                var p = go.GetComponent<Projectile>();
                p.Speed = ProjectileSpeed;
                p.Fire(rot * dir, Damage, false);
            }
            Fx.MuzzleFlash(Muzzle.position);
            AudioManager.Play("shoot");
            return true;
        }
    }
}
