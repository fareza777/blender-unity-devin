using UnityEngine;

namespace Slice
{
    // Demo autopilot: drives the player for recording (toggle .Enabled)
    public class AutoPilot : MonoBehaviour
    {
        public bool Enabled;
        PlayerController _p;
        Health _hp;
        float _dashT, _wanderT, _stuckT;
        Vector3 _wander, _lastPos;

        void Start() { _p = GetComponent<PlayerController>(); _hp = GetComponent<Health>(); _lastPos = transform.position; }

        public Vector3 Move()
        {
            _wanderT -= Time.deltaTime;
            var dp = transform.position - _lastPos; dp.y = 0;
            _lastPos = transform.position;
            if (dp.magnitude < 0.4f * Time.deltaTime) _stuckT += Time.deltaTime; else _stuckT = 0f;
            if (_stuckT > 1.0f) // wedged: burst toward open floor (nearest containing room center) with a side-slip
            {
                Vector3 c = transform.position; float cbd = float.MaxValue;
                var rm0 = RoomManager.I;
                if (rm0 != null)
                    foreach (var r in rm0.Rooms)
                        if (r.Bounds.ExpandedBy(1.5f).Contains(new Vector2(transform.position.x, transform.position.z)))
                        {
                            float d = (r.Center - transform.position).sqrMagnitude;
                            if (d < cbd) { cbd = d; c = r.Center; }
                        }
                var to = c - transform.position; to.y = 0;
                if (to.sqrMagnitude < 0.1f) to = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
                var d0 = to.normalized;
                return (d0 + Vector3.Cross(Vector3.up, d0) * (Mathf.Sin(Time.time * 5f) > 0 ? 0.9f : -0.9f)).normalized;
            }
            var gem = Nearest<XpGem>(14f);
            var enemy = Enemy.Nearest(transform.position, 14f);
            if (gem != null && !SameRoom(transform.position, gem.transform.position)) gem = null;
            if (enemy != null && !SameRoom(transform.position, enemy.transform.position)) enemy = null;

            if (enemy != null)
            {
                var to = enemy.transform.position - transform.position; to.y = 0;
                float d = to.magnitude;
                // kite: keep 5-9 range, strafe
                var orbit = Vector3.Cross(Vector3.up, to.normalized) * (Mathf.Sin(Time.time * 1.7f) > 0 ? 1 : -1);
                var v = orbit * 0.8f + (d < 4.5f ? -to.normalized : d > 9f ? to.normalized * 0.5f : Vector3.zero);
                if (_hp.Current < _hp.Max * 0.35f) v = -to.normalized; // run away low hp
                return v.normalized;
            }
            if (gem != null)
            {
                var g = gem.transform.position - transform.position; g.y = 0;
                return g.normalized;
            }
            // steer toward the next room's center (progresses the run)
            var nextGoal = NextRoomGoal();
            if (nextGoal.HasValue) return nextGoal.Value;
            if (_wanderT <= 0f) { _wanderT = 2f; _wander = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-0.3f, 1f)).normalized; }
            return _wander;
        }

        Vector3? NextRoomGoal()
        {
            var rm = RoomManager.I;
            if (rm == null) return null;
            var p = transform.position;
            Room target = null;
            foreach (var r in rm.Rooms)
                if (!r.IsStart && !r.Cleared) { target = r; break; }
            if (target == null || target.Bounds.Contains(new Vector2(p.x, p.z))) return null;
            // the room the player is inside (highest index wins on doorway overlaps)
            Room cur = null;
            foreach (var r in rm.Rooms)
                if (r.Bounds.ExpandedBy(0.6f).Contains(new Vector2(p.x, p.z)) && (cur == null || r.Index > cur.Index)) cur = r;
            // next hop along the chain toward the target
            Room hop = target;
            if (cur != null && cur.Index < target.Index)
                foreach (var r in rm.Rooms) if (r.Index == cur.Index + 1) { hop = r; break; }
            var door = EntryDoor(hop, cur != null ? cur.Center : p);
            // aim at the doorway until almost through it, then at the room center
            var wp = door;
            if (hop.Bounds.Contains(new Vector2(p.x, p.z)) || (p - door).magnitude < 1.8f) wp = hop.Center;
            var v = wp - p; v.y = 0;
            var dir = v.normalized;
            if (_stuckT > 0.7f) // slide along the wall to find the gap
                dir = (dir + Vector3.Cross(Vector3.up, dir) * (Mathf.Sin(Time.time * 3f) > 0 ? 1.3f : -1.3f)).normalized;
            return dir;
        }

        static bool SameRoom(Vector3 a, Vector3 b)
        {
            var rm = RoomManager.I;
            if (rm == null) return true;
            int ra = -2, rb = -2;
            foreach (var r in rm.Rooms)
            {
                if (r.Bounds.ExpandedBy(0.8f).Contains(new Vector2(a.x, a.z))) ra = r.Index;
                if (r.Bounds.ExpandedBy(0.8f).Contains(new Vector2(b.x, b.z))) rb = r.Index;
            }
            return ra == -2 || rb == -2 || ra == rb;
        }

        static Vector3 EntryDoor(Room r, Vector3 from)
        {
            var b = r.Bounds; var c = b.center;
            Vector3 best = default; float bd = float.MaxValue;
            for (int i = 0; i < 4; i++)
            {
                var e = i == 0 ? new Vector3(c.x, 0, b.yMax)
                      : i == 1 ? new Vector3(c.x, 0, b.yMin)
                      : i == 2 ? new Vector3(b.xMax, 0, c.y)
                      : new Vector3(b.xMin, 0, c.y);
                float d = (e - from).sqrMagnitude;
                if (d < bd) { bd = d; best = e; }
            }
            return best;
        }

        public bool Dash()
        {
            _dashT -= Time.deltaTime;
            var e = Enemy.Nearest(transform.position, 3.2f);
            bool danger = e != null || (_hp.Current < _hp.Max * 0.3f && Enemy.Nearest(transform.position, 8f) != null);
            if (danger && _dashT <= -0.6f) { _dashT = 0f; return true; }
            return false;
        }

        Vector3? FindBarrierAhead()
        {
            // walk toward the nearest active barrier to progress rooms
            Barrier b = null; float bd = 999f;
            foreach (var x in FindObjectsByType<Barrier>(FindObjectsSortMode.None))
            {
                if (!x.gameObject.activeInHierarchy) continue;
                float d = (x.transform.position - transform.position).sqrMagnitude;
                if (d < bd) { bd = d; b = x; }
            }
            if (b == null) return null;
            var v = b.transform.position - transform.position; v.y = 0;
            return v.normalized;
        }

        T Nearest<T>(float max) where T : Component
        {
            T best = null; float bd = max * max;
            foreach (var t in FindObjectsByType<T>(FindObjectsSortMode.None))
            {
                float d = (t.transform.position - transform.position).sqrMagnitude;
                if (d < bd) { bd = d; best = t; }
            }
            return best;
        }
    }

    public class Barrier : MonoBehaviour
    {
        float _t;
        void Update()
        {
            _t += Time.deltaTime;
            var r = GetComponent<Renderer>();
            if (r != null)
            {
                float p = 0.6f + Mathf.Sin(_t * 4f) * 0.25f;
                r.material.SetColor("_EmissionColor", new Color(1f, 0.2f, 0.5f) * p * 2f);
            }
        }
    }
}
