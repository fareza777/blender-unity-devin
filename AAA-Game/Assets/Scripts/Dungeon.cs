using System.Collections.Generic;
using UnityEngine;

namespace Slice
{
    [System.Serializable]
    public class Room
    {
        public Rect Bounds;          // world-space rect (x,z)
        public int Index;
        public bool IsStart, IsBoss;
        public bool Activated, Cleared;
        public List<Vector3> SpawnPoints = new List<Vector3>();
        public GameObject Barrier;
        public List<EnemyKind> Wave = new List<EnemyKind>();
        public Vector3 Center => new Vector3(Bounds.center.x, 0, Bounds.center.y);
    }

    public static class RectX
    {
        public static Rect ExpandedBy(this Rect r, float f) => new Rect(r.x - f, r.y - f, r.width + f * 2f, r.height + f * 2f);
    }
}
