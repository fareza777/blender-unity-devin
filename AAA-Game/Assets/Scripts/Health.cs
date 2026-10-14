using System;
using System.Collections.Generic;
using UnityEngine;

namespace Slice
{
    public class Health : MonoBehaviour
    {
        public float Max = 100f;
        public float Current;
        public bool Invulnerable;

        public Action<float> OnDamaged;
        public Action OnDied;
        public bool IsDead => Current <= 0f;

        Renderer[] _renders;
        Dictionary<Material, Color> _orig = new Dictionary<Material, Color>();
        float _flash;

        void Awake()
        {
            Current = Max;
            _renders = GetComponentsInChildren<Renderer>();
        }

        void Update()
        {
            if (_flash > 0f)
            {
                _flash -= Time.deltaTime;
                if (_flash <= 0f) RestoreFlash();
            }
        }

        public void Damage(float amount)
        {
            if (IsDead || Invulnerable || amount <= 0f) return;
            Current -= amount;
            Flash();
            OnDamaged?.Invoke(amount);
            if (IsDead) OnDied?.Invoke();
        }

        public void Heal(float amount)
        {
            if (IsDead) return;
            Current = Mathf.Min(Max, Current + amount);
        }

        void Flash()
        {
            _flash = 0.08f;
            if (_renders == null) return;
            foreach (var r in _renders)
            {
                if (r == null) continue;
                foreach (var m in r.materials)
                {
                    if (!m.HasProperty("_EmissionColor")) continue;
                    if (!_orig.ContainsKey(m)) _orig[m] = m.GetColor("_EmissionColor");
                    m.SetColor("_EmissionColor", Color.white * 3f);
                }
            }
        }

        void RestoreFlash()
        {
            foreach (var kv in _orig)
                if (kv.Key != null) kv.Key.SetColor("_EmissionColor", kv.Value);
        }

        void OnDestroy() { RestoreFlash(); }
    }
}
