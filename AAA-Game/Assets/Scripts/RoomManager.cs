using System.Collections.Generic;
using UnityEngine;

namespace Slice
{
    public class RoomManager : MonoBehaviour
    {
        public static RoomManager I;
        public List<Room> Rooms = new List<Room>();
        public GameObject ChaserPrefab, SpitterPrefab, BossPrefab;
        public GameObject BarrierPrefab;
        PlayerController _player;
        Room _current;

        void Awake() { I = this; }
        void Start() { _player = FindFirstObjectByType<PlayerController>(); }

        void Update()
        {
            if (_player == null || Game.I.State != GameState.Playing) return;
            var p = _player.transform.position;

            foreach (var r in Rooms)
            {
                if (r.Cleared || r.Activated) continue;
                if (r.Bounds.Contains(new Vector2(p.x, p.z)))
                    Activate(r);
            }

            if (_current != null && _current.Activated && !_current.Cleared)
            {
                bool alive = false;
                foreach (var e in Enemy.All)
                    if (e != null && !e.Hp.IsDead && _current.Bounds.ExpandedBy(2f).Contains(new Vector2(e.transform.position.x, e.transform.position.z)))
                        alive = true;
                if (!alive) Clear(_current);
            }
        }

        void Activate(Room r)
        {
            r.Activated = true; _current = r;
            if (r.Barrier != null) r.Barrier.SetActive(true);
            foreach (var kind in r.Wave)
            {
                var pos = r.SpawnPoints[Random.Range(0, r.SpawnPoints.Count)];
                var prefab = kind == EnemyKind.Boss ? BossPrefab : kind == EnemyKind.Spitter ? SpitterPrefab : ChaserPrefab;
                var go = Instantiate(prefab, pos, Quaternion.identity);
                var e = go.GetComponent<Enemy>();
                e.Kind = kind;
            }
            if (r.IsBoss) { HudUI.Announce("THE WARDEN AWAKENS"); AudioManager.Play("boss"); }
            else HudUI.Announce("ROOM " + r.Index);
            AudioManager.Play("room");
        }

        void Clear(Room r)
        {
            r.Cleared = true; _current = null;
            Game.I.RoomsCleared++;
            if (r.Barrier != null) r.Barrier.SetActive(false);
            HudUI.Announce("CLEARED");
            AudioManager.Play("clear");
        }

        public static void NotifyBossDead()
        {
            Game.I.SetState(GameState.Victory);
            GameOverUI.Show(true);
        }
    }
}
