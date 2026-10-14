using System.Collections.Generic;
using UnityEngine;

// Drives Blender rig hierarchies (joint empties + mesh parts) with a procedural
// walk cycle. No Animator needed — finds joints by name under this transform.
public class ProceduralWalker : MonoBehaviour
{
    public float StrideRate = 2.6f;   // phase gain per metre travelled
    public float Intensity = 1f;      // boss uses ~0.7 for heavier stomps
    public bool Floater;

    readonly Dictionary<string, Transform> _j = new();
    readonly Dictionary<Transform, Vector3> _rest = new();
    float _phase; Vector3 _lastPos; float _smooth;

    void Awake()
    {
        foreach (var t in GetComponentsInChildren<Transform>(true))
        {
            _j[t.name] = t;
            _rest[t] = t.localPosition;
        }
        _lastPos = transform.position;
    }

    void LateUpdate()
    {
        float dt = Time.deltaTime; if (dt <= 0f) return;
        Vector3 v = (transform.position - _lastPos) / dt;
        _lastPos = transform.position;
        float speed = new Vector3(v.x, 0f, v.z).magnitude;
        _smooth = Mathf.MoveTowards(_smooth, Mathf.Clamp01(speed / 2.5f), dt * 8f);
        _phase += speed * StrideRate * dt;
        float p = _phase, w = _smooth * Intensity;

        if (Floater) { AnimFloater(p, w); return; }
        if (_j.ContainsKey("LegLF_Up")) { AnimQuad(p, w); return; }
        AnimBiped(p, w);
    }

    void Rot(string n, float x, float y = 0, float z = 0)
    {
        if (_j.TryGetValue(n, out var t)) t.localRotation = Quaternion.Euler(x, y, z);
    }
    void Bob(string n, float dy)
    {
        if (_j.TryGetValue(n, out var t)) t.localPosition = _rest[t] + new Vector3(0, dy, 0);
    }

    void AnimBiped(float p, float w)
    {
        float s = Mathf.Sin(p), c = Mathf.Cos(p);
        // legs swing opposite; knees bend while leg passes through
        Rot("LegL_Up", s * 38f * w);  Rot("LegR_Up", -s * 38f * w);
        Rot("LegL_Lo", Mathf.Max(0f, -c) * 42f * w); Rot("LegR_Lo", Mathf.Max(0f, c) * 42f * w);
        // arms counter-swing to legs, elbows slightly bent
        Rot("ArmL_Up", -s * 26f * w, 0, 6f); Rot("ArmR_Up", s * 26f * w, 0, -6f);
        Rot("ArmL_Lo", -14f - Mathf.Max(0f, s) * 18f * w);
        Rot("ArmR_Lo", -14f - Mathf.Max(0f, -s) * 18f * w);
        // torso twist + slight forward lean, head steady
        Rot("Torso", 5f + Mathf.Abs(s) * 4f * w, s * 7f * w, -s * 3f * w);
        Rot("Head", -4f * w, -s * 5f * w, s * 2f * w);
        // vertical bob: two bounces per stride
        Bob("Hips", Mathf.Abs(Mathf.Sin(p)) * 0.05f * w);
    }

    void AnimQuad(float p, float w)
    {
        float s = Mathf.Sin(p);
        // diagonal gait: LF+RB together, RF+LB opposite
        Rot("LegLF_Up", s * 30f * w); Rot("LegRB_Up", s * 30f * w);
        Rot("LegRF_Up", -s * 30f * w); Rot("LegLB_Up", -s * 30f * w);
        Rot("LegLF_Lo", Mathf.Max(0f, -Mathf.Cos(p)) * 30f * w);
        Rot("LegRB_Lo", Mathf.Max(0f, -Mathf.Cos(p)) * 30f * w);
        Rot("LegRF_Lo", Mathf.Max(0f, Mathf.Cos(p)) * 30f * w);
        Rot("LegLB_Lo", Mathf.Max(0f, Mathf.Cos(p)) * 30f * w);
        Rot("HeadJ", Mathf.Sin(p * 2f) * 6f * w, s * 8f * w, 0);
        Bob("Body", Mathf.Abs(Mathf.Sin(p)) * 0.04f * w);
    }

    void AnimFloater(float p, float w)
    {
        float t = Time.time;
        for (int i = 0; i < 5; i++)
            Rot($"Tent{i}", Mathf.Sin(t * 3f + i * 1.3f) * 18f, 0, Mathf.Cos(t * 2.4f + i) * 12f);
        Rot("Jaw", 8f + Mathf.Sin(t * 2f) * 10f);
        Bob("Body", Mathf.Sin(t * 1.8f) * 0.1f + Mathf.Abs(Mathf.Sin(p)) * 0.05f * w);
    }
}
