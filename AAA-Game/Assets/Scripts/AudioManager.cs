using System.Collections.Generic;
using UnityEngine;

namespace Slice
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager I;
        readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        AudioSource _src;

        void Awake()
        {
            I = this;
            _src = gameObject.AddComponent<AudioSource>();
            _src.playOnAwake = false; _src.spatialBlend = 0f;
            foreach (var c in Resources.LoadAll<AudioClip>("Sfx"))
                _clips[c.name.ToLowerInvariant()] = c;
        }

        public static void Play(string name, float vol = 1f)
        {
            if (I == null) return;
            if (I._clips.TryGetValue(name.ToLowerInvariant(), out var c) && c != null)
                I._src.PlayOneShot(c, vol);
        }
    }
}
