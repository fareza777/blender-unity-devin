using UnityEngine;

namespace Slice
{
    public class XpGem : MonoBehaviour
    {
        public static void Spawn(Vector3 pos)
        {
            if (GameRefs.I == null || GameRefs.I.GemPrefab == null) return;
            var go = Instantiate(GameRefs.I.GemPrefab, pos, Quaternion.identity);
            go.AddComponent<XpGem>();
        }

        float _t;
        Transform _player;
        bool _picked;

        void Start() { _player = FindFirstObjectByType<PlayerController>()?.transform; }

        void Update()
        {
            _t += Time.deltaTime;
            transform.Rotate(0, 120f * Time.deltaTime, 0);
            transform.position += Vector3.up * Mathf.Sin(_t * 3f) * 0.3f * Time.deltaTime;

            if (_player == null || _picked) return;
            float d = Vector3.Distance(transform.position, _player.position);
            if (d < _player.GetComponent<PlayerController>().PickupRadius * 3f)
                transform.position = Vector3.MoveTowards(transform.position, _player.position + Vector3.up * 0.5f, (10f - d) * Time.deltaTime * 2f);
            if (d < 1.1f) Pick();
        }

        public void Pick()
        {
            if (_picked) return;
            _picked = true;
            LevelSystem.AddXp(1);
            Fx.Burst(transform.position, new Color(0.3f, 1f, 0.6f), 8);
            AudioManager.Play("gem");
            Destroy(gameObject);
        }
    }
}
