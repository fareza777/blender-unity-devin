using UnityEngine;

namespace Slice
{
    public static class Fx
    {
        public static readonly Color HitColor = new Color(1f, 0.85f, 0.2f);
        public static readonly Color HitPlayerColor = new Color(1f, 0.2f, 0.25f);

        static GameObject _psProto;
        static GameObject PsProto()
        {
            if (_psProto != null) return _psProto;
            var go = new GameObject("FxBurst");
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.4f; main.startLifetime = 0.35f; main.startSpeed = 6f;
            main.startSize = 0.14f; main.maxParticles = 60; main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.enabled = false;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.2f;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.material = new Material(Shader.Find("Particles/Standard Unlit"));
            r.material.SetFloat("_Mode", 1f); // additive
            r.material.SetColor("_Color", Color.white);
            go.SetActive(false);
            _psProto = go;
            return _psProto;
        }

        public static void Burst(Vector3 pos, Color color, int count)
        {
            var proto = PsProto();
            var go = Object.Instantiate(proto, pos, Quaternion.identity);
            go.SetActive(true);
            var ps = go.GetComponent<ParticleSystem>();
            var main = ps.main; main.startColor = color * 2f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            var m = new Material(r.sharedMaterial); m.SetColor("_Color", color * 3f); m.SetColor("_EmissionColor", color * 4f);
            r.material = m;
            ps.Emit(count);
            Object.Destroy(go, 1f);
        }

        public static void MuzzleFlash(Vector3 pos) => Burst(pos, new Color(1f, 0.9f, 0.3f), 5);

        public static void Explosion(Vector3 pos, float scale)
        {
            Burst(pos, new Color(1f, 0.4f, 0.1f), (int)(30 * scale));
            Burst(pos, Color.white, (int)(12 * scale));
            CamFollow.Shake(0.25f * scale);
        }
    }
}
