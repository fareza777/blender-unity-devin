using UnityEngine;

namespace Slice
{
    public class GameRefs : MonoBehaviour
    {
        public static GameRefs I;
        public GameObject GemPrefab;
        void Awake() { I = this; }
    }
}
