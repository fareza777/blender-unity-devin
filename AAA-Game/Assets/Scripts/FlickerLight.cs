using UnityEngine;

// Warm torch flicker driven by Perlin noise.
public class FlickerLight : MonoBehaviour
{
    public float Base = 1f;
    public float Amount = 0.35f;
    float _seed;
    Light _l;
    void Awake() { _l = GetComponent<Light>(); _seed = Random.value * 100f; if (_l) Base = _l.intensity; }
    void Update()
    {
        if (!_l) return;
        float n = Mathf.PerlinNoise(_seed, Time.time * 6f);
        _l.intensity = Base * (1f - Amount + n * Amount * 2f);
    }
}
