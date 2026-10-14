using UnityEngine;

namespace Slice
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public float MoveSpeed = 8f;
        public float DashSpeed = 26f;
        public float DashTime = 0.16f;
        public float DashCooldown = 1.1f;
        public float PickupRadius = 2.2f;
        public Weapon Weapon;
        public bool AutoAim;

        CharacterController _cc;
        Health _hp;
        float _dashT, _dashCd;
        Vector3 _dashDir;
        Camera _cam;
        Vector3 _vel;
        Transform _skin;
        float _bobT;

        public Health Hp => _hp ? _hp : (_hp = GetComponent<Health>());

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _hp = GetComponent<Health>();
            _hp.OnDied += () => { if (Game.I != null) Game.I.SetState(GameState.Dead); };
            // enemies deal contact damage but never body-block (roguelike convention)
            Physics.IgnoreLayerCollision(8, 9, true); // Enemy vs Player
        }

        void Start() { _cam = Camera.main; }

        AutoPilot _pilot;

        void Update()
        {
            if (Game.I != null && Game.I.State != GameState.Playing) return;
            _pilot = _pilot ? _pilot : GetComponent<AutoPilot>();
            bool bot = _pilot != null && _pilot.Enabled;

            Vector3 move = bot ? _pilot.Move() : ReadMove();
            Vector3 aim = ReadAim();
            bool dash = bot ? _pilot.Dash() : (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.LeftShift));

            _dashCd -= Time.deltaTime;
            if (dash && _dashCd <= 0f && move.sqrMagnitude > 0.01f)
            {
                _dashT = DashTime;
                _dashCd = DashCooldown;
                _dashDir = move.normalized;
                Hp.Invulnerable = true;
                Fx.Burst(transform.position, Color.cyan, 16);
                CamFollow.Shake(0.22f);
                AudioManager.Play("dash");
            }

            if (_dashT > 0f)
            {
                _dashT -= Time.deltaTime;
                _vel = _dashDir * DashSpeed;
                if (_dashT <= 0f) Hp.Invulnerable = false;
            }
            else _vel = Vector3.MoveTowards(_vel, move * MoveSpeed, 55f * Time.deltaTime);

            _cc.Move(_vel * Time.deltaTime);

            // out-of-bounds recovery: at low framerates a dash can tunnel through thin walls
            var rm = RoomManager.I;
            if (rm != null)
            {
                var xz = new Vector2(transform.position.x, transform.position.z);
                bool inside = false;
                foreach (var r in rm.Rooms) if (r.Bounds.ExpandedBy(1.2f).Contains(xz)) { inside = true; break; }
                if (!inside)
                {
                    float bd = float.MaxValue; Vector3 c = transform.position;
                    foreach (var r in rm.Rooms)
                    {
                        float d = (r.Center - transform.position).sqrMagnitude;
                        if (d < bd) { bd = d; c = r.Center; }
                    }
                    var back = c - transform.position; back.y = 0;
                    transform.position += back.normalized * (MoveSpeed * Time.deltaTime * 2f);
                }
            }

            if (aim.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(aim), Time.deltaTime * 14f);

            // auto-fire
            if (Weapon != null)
            {
                Weapon.Muzzle.position = transform.position + transform.forward * 0.9f + Vector3.up * 0.9f;
                if (Weapon.TryFire(transform.forward)) CamFollow.Shake(0.05f);
            }

            // ---- procedural hero animation on the Blender skin
            if (_skin == null) _skin = transform.Find("Skin");
            if (_skin != null)
            {
                float spd = Mathf.Clamp01(_vel.magnitude / MoveSpeed);
                _bobT += Time.deltaTime * (1.6f + spd * 9f);
                float bob = Mathf.Abs(Mathf.Sin(_bobT)) * 0.09f * spd;
                float breathe = Mathf.Sin(Time.time * 2.1f) * 0.025f;
                _skin.localPosition = new Vector3(0, bob + breathe, 0);
                var lean = Quaternion.Euler(_vel.magnitude * 1.5f, 0, 0);
                _skin.localRotation = Quaternion.Slerp(_skin.localRotation, lean, Time.deltaTime * 9f);
                float sq = _dashT > 0f ? 0.72f : 1f;
                var target = new Vector3(0.9f / Mathf.Sqrt(sq), 0.9f * sq, 0.9f / Mathf.Sqrt(sq));
                _skin.localScale = Vector3.Lerp(_skin.localScale, target, Time.deltaTime * 11f);
            }
        }

        Vector3 ReadMove()
        {
            var v = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
            return v.sqrMagnitude > 1f ? v.normalized : v;
        }

        Vector3 ReadAim()
        {
            if (AutoAim)
            {
                var e = Enemy.Nearest(transform.position, 14f);
                if (e != null)
                {
                    var d = e.transform.position - transform.position; d.y = 0;
                    return d.normalized;
                }
            }
            if (_cam != null)
            {
                var ray = _cam.ScreenPointToRay(Input.mousePosition);
                var plane = new Plane(Vector3.up, new Vector3(0, transform.position.y, 0));
                if (plane.Raycast(ray, out float t))
                {
                    var d = ray.GetPoint(t) - transform.position; d.y = 0;
                    if (d.sqrMagnitude > 0.04f) return d.normalized;
                }
            }
            return transform.forward;
        }

        void OnTriggerStay(Collider other)
        {
            var gem = other.GetComponent<XpGem>();
            if (gem != null) gem.Pick();
        }
    }
}
