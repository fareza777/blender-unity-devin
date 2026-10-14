using System.Collections.Generic;
using UnityEngine;

namespace Slice
{
    public enum EnemyKind { Chaser, Spitter, Boss }

    public class Enemy : MonoBehaviour
    {
        public static readonly List<Enemy> All = new List<Enemy>();
        public static Enemy Nearest(Vector3 pos, float maxDist)
        {
            Enemy best = null; float bd = maxDist * maxDist;
            foreach (var e in All)
            {
                if (e == null || e.Hp.IsDead) continue;
                float d = (e.transform.position - pos).sqrMagnitude;
                if (d < bd) { bd = d; best = e; }
            }
            return best;
        }

        public EnemyKind Kind = EnemyKind.Chaser;
        public float MoveSpeed = 4.5f;
        public float ContactDamage = 12f;
        public float KeepDistance = 0f;   // shooter backs off
        public float ShootRange = 10f;
        public float FireInterval = 1.6f;
        public float ProjectileDamage = 10f;
        public int XpValue = 1;
        public GameObject ProjectilePrefab;

        Health _hp;
        PlayerController _player;
        CharacterController _cc;
        float _fireCd, _contactCd, _chargeT, _chargeCd;
        Vector3 _chargeDir;

        public Health Hp => _hp;

        void Awake()
        {
            _hp = GetComponent<Health>();
            _cc = GetComponent<CharacterController>();
            All.Add(this);
        }

        void Start()
        {
            _player = FindFirstObjectByType<PlayerController>();
            _hp.OnDied += Die;
            _hp.OnDamaged += _ => Fx.Burst(transform.position + Vector3.up, Color.white, 4);
        }

        void OnDestroy() { All.Remove(this); }

        void Update()
        {
            if (_player == null || _hp.IsDead) return;
            if (Game.I != null && Game.I.State != GameState.Playing) return;

            Vector3 to = _player.transform.position - transform.position;
            to.y = 0;
            float dist = to.magnitude;
            Vector3 dir = dist > 0.001f ? to / dist : Vector3.forward;

            Vector3 vel = Vector3.zero;
            _fireCd -= Time.deltaTime;
            _chargeCd -= Time.deltaTime;

            switch (Kind)
            {
                case EnemyKind.Chaser:
                    vel = dir * MoveSpeed;
                    break;
                case EnemyKind.Spitter:
                    if (dist > KeepDistance) vel = dir * MoveSpeed;
                    else if (dist < KeepDistance - 2f) vel = -dir * MoveSpeed * 0.8f;
                    else vel = Vector3.zero;
                    if (dist < ShootRange && _fireCd <= 0f)
                    {
                        _fireCd = FireInterval;
                        Shoot(dir);
                    }
                    break;
                case EnemyKind.Boss:
                    if (_chargeT > 0f)
                    {
                        _chargeT -= Time.deltaTime;
                        vel = _chargeDir * 14f;
                        if (Random.value < 0.3f) Fx.Burst(transform.position, Color.red, 6);
                    }
                    else
                    {
                        vel = dir * (MoveSpeed * (dist > 6f ? 1.4f : 0.8f));
                        if (_chargeCd <= 0f && dist > 7f)
                        {
                            _chargeCd = 4f; _chargeT = 0.7f; _chargeDir = dir;
                            AudioManager.Play("boss");
                        }
                        else if (_fireCd <= 0f && dist < ShootRange)
                        {
                            _fireCd = FireInterval;
                            RadialShoot(10);
                        }
                    }
                    break;
            }

            if (_cc != null) _cc.Move(vel * Time.deltaTime);
            else transform.position += vel * Time.deltaTime;

            if (dir.sqrMagnitude > 0f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 8f);

            // contact damage
            _contactCd -= Time.deltaTime;
            if (dist < 1.3f && _contactCd <= 0f)
            {
                _contactCd = 0.5f;
                _player.Hp.Damage(ContactDamage);
                Fx.Burst(_player.transform.position + Vector3.up, Color.red, 12);
                AudioManager.Play("hurt");
            }
        }

        void Shoot(Vector3 dir)
        {
            if (ProjectilePrefab == null) return;
            var go = Instantiate(ProjectilePrefab, transform.position + Vector3.up * 0.9f + dir * 0.6f, Quaternion.identity);
            var p = go.GetComponent<Projectile>();
            p.Speed = 11f; p.MaxDist = 22f; p.Radius = 0.4f;
            p.Fire(dir, ProjectileDamage, true);
            AudioManager.Play("eshoot");
        }

        void RadialShoot(int n)
        {
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2f / n;
                var d = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
                var go = Instantiate(ProjectilePrefab, transform.position + Vector3.up * 0.9f + d * 0.8f, Quaternion.identity);
                var p = go.GetComponent<Projectile>();
                p.Speed = 8f; p.MaxDist = 18f; p.Radius = 0.4f;
                p.Fire(d, ProjectileDamage, true);
            }
            AudioManager.Play("eshoot");
        }

        void Die()
        {
            Game.I.Kills++;
            AudioManager.Play("kill");
            Fx.Explosion(transform.position + Vector3.up * 0.8f, Kind == EnemyKind.Boss ? 3f : 1f);
            for (int i = 0; i < XpValue; i++) XpGem.Spawn(transform.position + Random.insideUnitSphere * 0.6f + Vector3.up * 0.5f);
            if (Kind == EnemyKind.Boss) RoomManager.NotifyBossDead();
            Destroy(gameObject);
        }
    }
}
