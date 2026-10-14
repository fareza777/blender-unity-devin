using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using Slice;

public static class SliceBootstrap
{
    static Material Mat(string name, Color color, Color emission, float metallic = 0f, float smooth = 0.5f)
    {
        var m = new Material(Shader.Find("Standard"));
        m.name = name;
        m.color = color;
        m.SetFloat("_Metallic", metallic);
        m.SetFloat("_Glossiness", smooth);
        if (emission.maxColorComponent > 0f)
        {
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            m.SetColor("_EmissionColor", emission);
        }
        AssetDatabase.CreateAsset(m, "Assets/Materials/" + name + ".mat");
        return m;
    }

    static GameObject Box(Transform parent, Vector3 pos, Vector3 size, Material mat, bool collider = true, string name = "box")
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    // attach a Blender-made FBX model as the visible skin of a primitive host
    static void AttachModel(GameObject host, string fbxPath, float scale, float yOffset = 0f, Color glow = default, bool floater = false, float walkIntensity = 1f)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (model == null) return;
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(model);
        inst.name = "Skin";
        inst.transform.SetParent(host.transform, false);
        inst.transform.localPosition = Vector3.up * yOffset;
        inst.transform.localScale = Vector3.one * scale;
        foreach (var mr in inst.GetComponentsInChildren<Renderer>())
        {
            var mats = mr.materials; // instantiates per-instance copies
            foreach (var mm in mats)
            {
                mm.color = Color.Lerp(mm.color, Color.white, 0.3f); // lift dark albedo
                if (glow.maxColorComponent > 0f)
                {
                    mm.EnableKeyword("_EMISSION");
                    mm.SetColor("_EmissionColor", glow);
                }
            }
            mr.materials = mats;
        }
        var r = host.GetComponent<Renderer>();
        if (r != null) r.enabled = false;
        var walker = inst.AddComponent<ProceduralWalker>();
        walker.Floater = floater; walker.Intensity = walkIntensity;
    }

    [MenuItem("Slice/Build Vertical Slice")]
    public static void Build()
    {
        // ---- materials
        AssetDatabase.DeleteAsset("Assets/Materials"); AssetDatabase.CreateFolder("Assets", "Materials");
        var mFloor   = Mat("Floor",   new Color(0.58f, 0.52f, 0.46f), new Color(0.03f, 0.02f, 0.012f), 0.08f, 0.4f);
        var mFloorIn = Mat("FloorIn", new Color(0.30f, 0.24f, 0.18f), new Color(0.9f, 0.45f, 0.12f) * 0.6f);
        var mWall    = Mat("Wall",    new Color(0.55f, 0.52f, 0.46f), new Color(0.02f, 0.018f, 0.012f), 0.08f, 0.45f);
        var mTrim    = Mat("WallTrim",new Color(0.11f, 0.09f, 0.06f), new Color(1f, 0.55f, 0.15f) * 0.9f);
        var mBarrier = Mat("Barrier", new Color(0.2f, 0.02f, 0.08f),  new Color(1f, 0.2f, 0.5f) * 2f);
        var mPlayer  = Mat("Player",  new Color(0.08f, 0.3f, 0.36f),  new Color(0.1f, 0.95f, 1f) * 1.8f);
        var mGun     = Mat("Gun",     new Color(0.08f, 0.08f, 0.12f), new Color(1f, 0.8f, 0.2f) * 1.5f);
        var mChaser  = Mat("Chaser",  new Color(0.3f, 0.05f, 0.18f),  new Color(1f, 0.1f, 0.55f) * 2f);
        var mSpitter = Mat("Spitter", new Color(0.3f, 0.16f, 0.04f),  new Color(1f, 0.55f, 0.1f) * 2f);
        var mBoss    = Mat("Boss",    new Color(0.35f, 0.05f, 0.05f), new Color(1f, 0.1f, 0.1f) * 2.4f);
        var mBullet  = Mat("Bullet",  new Color(0.3f, 0.25f, 0.05f),  new Color(1f, 0.9f, 0.25f) * 4f);
        var mEBullet = Mat("EBullet", new Color(0.3f, 0.05f, 0.08f),  new Color(1f, 0.15f, 0.3f) * 4f);
        var mGem     = Mat("Gem",     new Color(0.05f, 0.25f, 0.12f), new Color(0.2f, 1f, 0.5f) * 4f);
        var mPillar  = Mat("Pillar",  new Color(0.5f, 0.48f, 0.44f), new Color(0.45f, 0.25f, 0.08f) * 0.5f, 0.12f, 0.5f);
        var mCrystal = Mat("Crystal", new Color(0.2f, 0.12f, 0.04f), new Color(1f, 0.6f, 0.12f) * 2.5f);
        var mEye     = Mat("Eye",     Color.white, Color.white * 4f);
        var mSconce  = Mat("Sconce",  new Color(0.3f, 0.15f, 0.05f),  new Color(1f, 0.55f, 0.15f) * 3f);
        var mDebris  = Mat("Debris",  new Color(0.22f, 0.20f, 0.17f), Color.black, 0.3f, 0.4f);
        var mBanner  = Mat("Banner",  new Color(0.30f, 0.04f, 0.05f), new Color(0.55f, 0.06f, 0.08f) * 0.5f);

        // procedural textures
        var tFloor = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/floor.png");
        var tWall  = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/wall.png");
        if (tFloor != null) { mFloor.SetTexture("_MainTex", tFloor); mFloor.SetTextureScale("_MainTex", new Vector2(10, 7)); }
        if (tWall != null)  { mWall.SetTexture("_MainTex", tWall);   mWall.SetTextureScale("_MainTex", new Vector2(3, 1.6f));
                              mPillar.SetTexture("_MainTex", tWall); mPillar.SetTextureScale("_MainTex", new Vector2(1.5f, 1.5f)); }

        // ---- scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.19f, 0.155f, 0.12f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.05f, 0.035f, 0.02f);
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.005f;

        var root = new GameObject("DUNGEON");

        // ---- room path
        var rooms = new List<(int idx, float cx, float cz, float w, float d, bool boss)>
        {
            (0, 0, 0, 20, 14, false),
            (1, 20, 0, 20, 14, false),
            (2, 20, 14, 20, 14, false),
            (3, 40, 14, 20, 14, false),
            (4, 60, 14, 20, 14, false),
            (5, 60, 0, 20, 14, false),
            (6, 80, 0, 20, 14, false),
            (7, 80, 14, 22, 16, true),
        };
        // door connections: (room, edge 'E','W','N','S')
        var doors = new Dictionary<int, HashSet<char>>
        {
            {0, new HashSet<char>{'E'}},
            {1, new HashSet<char>{'W','N'}},
            {2, new HashSet<char>{'S','E'}},
            {3, new HashSet<char>{'W','E'}},
            {4, new HashSet<char>{'W','S'}},
            {5, new HashSet<char>{'N','E'}},
            {6, new HashSet<char>{'W','N'}},
            {7, new HashSet<char>{'S'}},
        };
        var exitDoorOf = new Dictionary<int, char> // exit leads to NEXT room
        {
            {1,'N'},{2,'E'},{3,'E'},{4,'S'},{5,'E'},{6,'N'}
        };

        var rm = new GameObject("Rooms").transform; rm.SetParent(root.transform, false);
        var builtEdges = new HashSet<string>();
        var rng = new System.Random(1337);
        var roomList = new List<Room>();

        foreach (var (idx, cx, cz, w, d, boss) in rooms)
        {
            var rr = new GameObject("Room" + idx).transform; rr.SetParent(rm, false);
            var room = new Room { Index = idx, IsStart = idx == 0, IsBoss = boss,
                Bounds = new Rect(cx - w / 2f, cz - d / 2f, w, d) };
            roomList.Add(room);

            // floor
            var floor = Box(rr, new Vector3(cx, -0.5f, cz), new Vector3(w, 1f, d), mFloor, true, "floor");
            // emissive border trim on floor edge
            float bt = 0.12f;
            Box(rr, new Vector3(cx, 0.02f, cz - d / 2 + bt), new Vector3(w, 0.04f, bt * 2), mTrim, false, "trim");
            Box(rr, new Vector3(cx, 0.02f, cz + d / 2 - bt), new Vector3(w, 0.04f, bt * 2), mTrim, false, "trim");
            Box(rr, new Vector3(cx - w / 2 + bt, 0.02f, cz), new Vector3(bt * 2, 0.04f, d), mTrim, false, "trim");
            Box(rr, new Vector3(cx + w / 2 - bt, 0.02f, cz), new Vector3(bt * 2, 0.04f, d), mTrim, false, "trim");

            // walls: N(+z) S(-z) E(+x) W(-x), with door gap at edge midpoint when needed
            void Edge(char e, Vector3 a, Vector3 b, float doorPos)
            {
                float minx = Mathf.Min(a.x, b.x), maxx = Mathf.Max(a.x, b.x);
                float minz = Mathf.Min(a.z, b.z), maxz = Mathf.Max(a.z, b.z);
                string key = minx.ToString("0") + "_" + minz.ToString("0") + "_" + maxx.ToString("0") + "_" + maxz.ToString("0");
                if (builtEdges.Contains(key)) return;
                builtEdges.Add(key);
                bool hasDoor = doors.TryGetValue(idx, out var ds) && ds.Contains(e);
                if (!hasDoor) { WallRun(rr, a, b, mWall, mTrim); return; }
                // split at door midpoint along edge direction
                var dir = (b - a).normalized;
                var mid = a + (b - a) * doorPos;
                var d1 = mid - dir * 1.6f; var d2 = mid + dir * 1.6f;
                WallRun(rr, a, d1, mWall, mTrim);
                WallRun(rr, d2, b, mWall, mTrim);
                // lintel above door
                Box(rr, mid + Vector3.up * 2.6f, new Vector3(Mathf.Max(0.6f, Mathf.Abs(dir.x) * 3.2f), 1f, Mathf.Max(0.6f, Mathf.Abs(dir.z) * 3.2f)), mWall, false, "lintel");
            }
            Edge('N', new Vector3(cx - w / 2, 0, cz + d / 2), new Vector3(cx + w / 2, 0, cz + d / 2), 0.5f);
            Edge('S', new Vector3(cx - w / 2, 0, cz - d / 2), new Vector3(cx + w / 2, 0, cz - d / 2), 0.5f);
            Edge('E', new Vector3(cx + w / 2, 0, cz - d / 2), new Vector3(cx + w / 2, 0, cz + d / 2), 0.5f);
            Edge('W', new Vector3(cx - w / 2, 0, cz - d / 2), new Vector3(cx - w / 2, 0, cz + d / 2), 0.5f);

            // props: pillars
            int pillars = idx == 0 ? 0 : rng.Next(1, 3 + (boss ? 2 : 0));
            for (int i = 0; i < pillars; i++)
            {
                float px = cx + (float)(rng.NextDouble() * (w - 8) - (w - 8) / 2);
                float pz = cz + (float)(rng.NextDouble() * (d - 8) - (d - 8) / 2);
                Box(rr, new Vector3(px, 1.4f, pz), new Vector3(1.4f, 2.8f, 1.4f), mPillar, true, "pillar");
                Box(rr, new Vector3(px, 3f, pz), new Vector3(1.7f, 0.25f, 1.7f), mTrim, false, "pillarTop");
            }
            // crystals at corners
            if (idx > 0)
            {
                float ax = cx + (rng.Next(2) == 0 ? -1 : 1) * (w / 2 - 1.6f);
                float az = cz + (rng.Next(2) == 0 ? -1 : 1) * (d / 2 - 1.6f);
                var cr = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cr.name = "crystal"; cr.transform.SetParent(rr, false);
                cr.transform.position = new Vector3(ax, 0.7f, az);
                cr.transform.localScale = new Vector3(0.7f, 1.4f, 0.7f);
                cr.transform.rotation = Quaternion.Euler(0, (float)rng.NextDouble() * 90f, 20f);
                cr.GetComponent<Renderer>().sharedMaterial = mCrystal;
                Object.DestroyImmediate(cr.GetComponent<Collider>());
            }

            // floor inlay: dim emissive inset frame so the floor reads as space
            float ii = 2.2f, it = 0.1f;
            Box(rr, new Vector3(cx, 0.03f, cz - d / 2 + ii), new Vector3(w - ii * 2, 0.05f, it), mFloorIn, false, "inlay");
            Box(rr, new Vector3(cx, 0.03f, cz + d / 2 - ii), new Vector3(w - ii * 2, 0.05f, it), mFloorIn, false, "inlay");
            Box(rr, new Vector3(cx - w / 2 + ii, 0.03f, cz), new Vector3(it, 0.05f, d - ii * 2), mFloorIn, false, "inlay");
            Box(rr, new Vector3(cx + w / 2 - ii, 0.03f, cz), new Vector3(it, 0.05f, d - ii * 2), mFloorIn, false, "inlay");

            // wall sconces: glowing fixtures at each edge midpoint (double as door torches)
            var mSconceL = mSconce;
            for (int si = 0; si < 4; si++)
            {
                var ex = si == 0 ? 0f : si == 1 ? 0f : si == 2 ? -1f : 1f;
                var ez = si == 0 ? -1f : si == 1 ? 1f : 0f;
                var sp = new Vector3(cx + ex * (w / 2f - 0.5f), 2.1f, cz + ez * (d / 2f - 0.5f));
                var sc = Box(rr, sp, new Vector3(0.26f, 0.55f, 0.26f), mSconceL, false, "sconce");
                sc.transform.rotation = Quaternion.Euler(0, 45, 0);
                var sgo = new GameObject("sconcelight"); sgo.transform.SetParent(rr, false);
                var sl = sgo.AddComponent<Light>();
                sl.type = LightType.Point; sl.range = 5.5f; sl.intensity = 1.2f; sl.color = new Color(1f, 0.55f, 0.2f);
                sgo.AddComponent<FlickerLight>();
                sgo.transform.position = sp + new Vector3(-ex * 0.7f, 0.25f, -ez * 0.7f);
            }
            // banners on the long walls
            if (idx > 0)
            {
                var bn = Box(rr, new Vector3(cx - w / 4f, 2.5f, cz - d / 2 + 0.42f), new Vector3(1.6f, 1.4f, 0.06f), mBanner, false, "banner");
                var bn2 = Box(rr, new Vector3(cx + w / 4f, 2.5f, cz + d / 2 - 0.42f), new Vector3(1.6f, 1.4f, 0.06f), mBanner, false, "banner");
            }
            // floor debris shards
            int debris = rng.Next(3, 6);
            for (int i = 0; i < debris; i++)
            {
                float dx = cx + (float)(rng.NextDouble() * (w - 4) - (w - 4) / 2);
                float dz = cz + (float)(rng.NextDouble() * (d - 4) - (d - 4) / 2);
                var db = Box(rr, new Vector3(dx, 0.07f, dz), new Vector3(0.2f + (float)rng.NextDouble() * 0.5f, 0.12f, 0.2f + (float)rng.NextDouble() * 0.5f), mDebris, false, "debris");
                db.transform.rotation = Quaternion.Euler(0, (float)rng.NextDouble() * 90f, 0);
            }
            // Blender braziers flanking corners of combat/boss rooms
            if (idx > 0)
            {
                var braz = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Brazier.fbx");
                if (braz != null)
                {
                    var corners = new[] { new Vector2(cx - w / 2f + 1.5f, cz - d / 2f + 1.5f), new Vector2(cx + w / 2f - 1.5f, cz + d / 2f - 1.5f) };
                    foreach (var cp in corners)
                    {
                        var bgo = (GameObject)PrefabUtility.InstantiatePrefab(braz);
                        bgo.transform.SetParent(rr, false);
                        bgo.transform.position = new Vector3(cp.x, 0, cp.y);
                        var bgo2 = new GameObject("brazierlight"); bgo2.transform.SetParent(rr, false);
                        var bl = bgo2.AddComponent<Light>();
                        bl.type = LightType.Point; bl.range = 6f; bl.intensity = 1.3f; bl.color = new Color(1f, 0.5f, 0.15f);
                        bgo2.AddComponent<FlickerLight>();
                        bgo2.transform.position = new Vector3(cp.x, 2.2f, cp.y);
                    }
                }
            }

            // room light
            var lt = new GameObject("light" + idx).transform; lt.SetParent(rr, false);
            var l = lt.gameObject.AddComponent<Light>();
            l.type = LightType.Point; l.range = Mathf.Max(w, d) * 1.1f; l.intensity = 3.1f;
            l.color = boss ? new Color(1f, 0.3f, 0.22f) : new Color(1f, 0.62f, 0.3f);
            lt.position = new Vector3(cx, 4.2f, cz);
            // second dim fill light low over the floor
            var lt2 = new GameObject("fill" + idx).transform; lt2.SetParent(rr, false);
            var l2 = lt2.gameObject.AddComponent<Light>();
            l2.type = LightType.Point; l2.range = Mathf.Max(w, d) * 0.8f; l2.intensity = 0.7f;
            l2.color = new Color(0.95f, 0.55f, 0.25f);
            lt2.position = new Vector3(cx - w / 4f, 2.5f, cz);

            // spawn points
            for (int i = 0; i < 5; i++)
                room.SpawnPoints.Add(new Vector3(
                    cx + (float)(rng.NextDouble() * (w - 6) - (w - 6) / 2), 1f,
                    cz + (float)(rng.NextDouble() * (d - 6) - (d - 6) / 2)));
        }

        // waves
        roomList[1].Wave = new List<EnemyKind> { EnemyKind.Chaser, EnemyKind.Chaser, EnemyKind.Chaser };
        roomList[2].Wave = new List<EnemyKind> { EnemyKind.Chaser, EnemyKind.Chaser, EnemyKind.Chaser, EnemyKind.Spitter };
        roomList[3].Wave = new List<EnemyKind> { EnemyKind.Chaser, EnemyKind.Chaser, EnemyKind.Chaser, EnemyKind.Chaser, EnemyKind.Spitter, EnemyKind.Spitter };
        roomList[4].Wave = new List<EnemyKind> { EnemyKind.Chaser, EnemyKind.Chaser, EnemyKind.Chaser, EnemyKind.Chaser, EnemyKind.Spitter, EnemyKind.Spitter };
        roomList[5].Wave = new List<EnemyKind> { EnemyKind.Chaser, EnemyKind.Chaser, EnemyKind.Chaser, EnemyKind.Chaser, EnemyKind.Chaser, EnemyKind.Spitter, EnemyKind.Spitter, EnemyKind.Spitter };
        roomList[6].Wave = new List<EnemyKind> { EnemyKind.Chaser, EnemyKind.Chaser, EnemyKind.Chaser, EnemyKind.Chaser, EnemyKind.Chaser, EnemyKind.Chaser, EnemyKind.Spitter, EnemyKind.Spitter, EnemyKind.Spitter };
        roomList[7].Wave = new List<EnemyKind> { EnemyKind.Boss, EnemyKind.Chaser, EnemyKind.Chaser };

        // ---- prefabs
        var prefabDir = "Assets/Prefabs";
        AssetDatabase.DeleteAsset(prefabDir); AssetDatabase.CreateFolder("Assets", "Prefabs");

        GameObject MakeBullet(Material m, string name)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.localScale = new Vector3(0.28f, 0.28f, 0.6f);
            go.GetComponent<Renderer>().sharedMaterial = m;
            var col = go.GetComponent<Collider>(); col.isTrigger = true;
            var tr = go.AddComponent<TrailRenderer>();
            tr.time = 0.12f; tr.startWidth = 0.22f; tr.endWidth = 0.02f;
            tr.material = new Material(Shader.Find("Particles/Standard Unlit"));
            tr.material.SetColor("_Color", m.GetColor("_EmissionColor") * 0.5f);
            go.AddComponent<Projectile>();
            var path = prefabDir + "/" + name + ".prefab";
            var p = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return p;
        }
        var bulletPrefab = MakeBullet(mBullet, "Bullet");
        var ebulletPrefab = MakeBullet(mEBullet, "EBullet");

        // gem
        var gem = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gem.transform.localScale = new Vector3(0.4f, 0.55f, 0.4f);
        gem.transform.rotation = Quaternion.Euler(0, 45, 20);
        gem.GetComponent<Renderer>().sharedMaterial = mGem;
        Object.DestroyImmediate(gem.GetComponent<Collider>());
        var gemPrefab = PrefabUtility.SaveAsPrefabAsset(gem, prefabDir + "/Gem.prefab");
        Object.DestroyImmediate(gem);

        GameObject MakeEnemy(string name, Material m, Vector3 scale, float hp, float speed, float contactDmg, int xp, string fbx = null, float fbxScale = 1f, Color fbxGlow = default, bool floater = false, float walkIntensity = 1f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.transform.localScale = scale;
            go.layer = 8; // Enemy layer: does not body-block the Player layer
            go.GetComponent<Renderer>().sharedMaterial = m;
            // glowing eyes
            var e1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            e1.transform.SetParent(go.transform, false);
            e1.transform.localPosition = new Vector3(-0.22f, 0.3f, 0.4f);
            e1.transform.localScale = new Vector3(0.18f, 0.18f, 0.18f);
            e1.GetComponent<Renderer>().sharedMaterial = mEye;
            Object.DestroyImmediate(e1.GetComponent<Collider>());
            var e2 = Object.Instantiate(e1, go.transform);
            e2.transform.localPosition = new Vector3(0.22f, 0.3f, 0.4f);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            if (fbx != null)
            {
                AttachModel(go, fbx, fbxScale, 0f, fbxGlow, floater, walkIntensity);
                // hide primitive eye placeholders too when a real skin exists
                e1.GetComponent<Renderer>().enabled = false;
                e2.GetComponent<Renderer>().enabled = false;
            }
            var cc = go.AddComponent<CharacterController>();
            cc.height = 2f * scale.y; cc.radius = 0.5f * scale.x; cc.center = Vector3.zero;
            cc.stepOffset = 0.4f; cc.slopeLimit = 50f;
            var h = go.AddComponent<Health>(); h.Max = hp;
            var en = go.AddComponent<Enemy>();
            en.MoveSpeed = speed; en.ContactDamage = contactDmg; en.XpValue = xp;
            en.ProjectilePrefab = ebulletPrefab;
            var path = prefabDir + "/" + name + ".prefab";
            var p = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return p;
        }
        var chaserPrefab = MakeEnemy("Chaser", mChaser, new Vector3(0.8f, 0.9f, 0.8f), 40f, 5f, 12f, 2, "Assets/Models/Chaser.fbx", 1.0f, new Color(0.9f, 0.1f, 0.5f) * 0.8f);
        var spitterPrefab = MakeEnemy("Spitter", mSpitter, new Vector3(0.9f, 1.1f, 0.9f), 60f, 3.4f, 8f, 3, "Assets/Models/Spitter.fbx", 1.1f, new Color(0.4f, 0.9f, 0.7f) * 0.7f, floater: true);
        spitterPrefab.GetComponent<Enemy>().KeepDistance = 8f;
        PrefabUtility.SavePrefabAsset(spitterPrefab);
        var bossPrefab = MakeEnemy("Boss", mBoss, new Vector3(2.2f, 2.4f, 2.2f), 700f, 3.2f, 22f, 30, "Assets/Models/Warden.fbx", 1.15f, new Color(1f, 0.12f, 0.12f) * 0.9f, walkIntensity: 0.7f);
        var bossE = bossPrefab.GetComponent<Enemy>();
        bossE.ShootRange = 16f; bossE.FireInterval = 2.4f; bossE.ProjectileDamage = 14f;
        PrefabUtility.SavePrefabAsset(bossPrefab);

        // barrier prefab-ish: thin glowing wall
        var barrier = Box(null, Vector3.zero, new Vector3(3.4f, 3.2f, 0.3f), mBarrier, true, "Barrier");
        barrier.AddComponent<Barrier>();
        var barrierPrefab = PrefabUtility.SaveAsPrefabAsset(barrier, prefabDir + "/Barrier.prefab");
        Object.DestroyImmediate(barrier);

        // barriers at exit doors of combat rooms
        foreach (var kv in exitDoorOf)
        {
            var room = roomList[kv.Key];
            var c = room.Bounds;
            char e = kv.Value;
            Vector3 pos = e == 'E' ? new Vector3(c.xMax, 1.6f, c.center.y)
                        : e == 'W' ? new Vector3(c.xMin, 1.6f, c.center.y)
                        : e == 'N' ? new Vector3(c.center.x, 1.6f, c.yMax)
                        : new Vector3(c.center.x, 1.6f, c.yMin);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(barrierPrefab);
            if (e == 'E' || e == 'W') go.transform.rotation = Quaternion.Euler(0, 90, 0);
            go.transform.position = pos;
            go.name = "Barrier_" + kv.Key;
            go.SetActive(false);
            room.Barrier = go;
        }
        // boss room entry barrier (locks player in)
        {
            var room = roomList[7];
            var pos = new Vector3(room.Bounds.center.x, 1.6f, room.Bounds.yMin);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(barrierPrefab);
            go.transform.position = pos;
            go.SetActive(false);
            room.Barrier = go;
        }

        // ---- player
        var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.layer = 9; // Player layer
        player.transform.position = new Vector3(0, 1f, 0);
        player.GetComponent<Renderer>().sharedMaterial = mPlayer;
        Object.DestroyImmediate(player.GetComponent<Collider>());
        var pcc = player.AddComponent<CharacterController>();
        pcc.height = 2f; pcc.radius = 0.45f; pcc.center = Vector3.zero;
        var php = player.AddComponent<Health>(); php.Max = 100f;
        var gun = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gun.name = "Gun"; gun.transform.SetParent(player.transform, false);
        gun.transform.localPosition = new Vector3(0.35f, -0.05f, 0.55f);
        gun.transform.localScale = new Vector3(0.22f, 0.22f, 0.85f);
        gun.GetComponent<Renderer>().sharedMaterial = mGun;
        Object.DestroyImmediate(gun.GetComponent<Collider>());
        var pl = new GameObject("PlayerLight").transform; pl.SetParent(player.transform, false);
        var plight = pl.gameObject.AddComponent<Light>();
        plight.type = LightType.Point; plight.range = 12f; plight.intensity = 2f; plight.color = new Color(0.3f, 0.9f, 1f);
        pl.localPosition = Vector3.up * 1.6f;
        var wpn = player.AddComponent<Weapon>();
        wpn.ProjectilePrefab = bulletPrefab;
        wpn.Damage = 14f; wpn.FireRate = 4f;
        var muzzle = new GameObject("Muzzle").transform;
        muzzle.SetParent(player.transform, false);
        muzzle.localPosition = new Vector3(0.35f, -0.05f, 1.1f);
        wpn.Muzzle = muzzle;
        var pc = player.AddComponent<PlayerController>();
        pc.Weapon = wpn;
        var pilot = player.AddComponent<AutoPilot>(); pilot.Enabled = false;
        AttachModel(player, "Assets/Models/HeroKnight.fbx", 0.9f, 0f, new Color(1f, 0.72f, 0.32f) * 0.35f);

        // ---- camera + post
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = 50f; cam.nearClipPlane = 0.3f; cam.farClipPlane = 200f;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.035f, 0.028f, 0.022f);
        var cf = camGo.AddComponent<CamFollow>();
        cf.Target = player.transform;
        camGo.transform.position = player.transform.position + cf.Offset;
        camGo.transform.rotation = Quaternion.Euler(58f, 0, 0);
        var layer = camGo.AddComponent<PostProcessLayer>();
        layer.volumeLayer = 1 << 0; // Default layer
        var volGo = new GameObject("PPVolume");
        volGo.layer = 0;
        var vol = volGo.AddComponent<PostProcessVolume>();
        vol.isGlobal = true;
        var profile = ScriptableObject.CreateInstance<PostProcessProfile>();
        var bloom = profile.AddSettings<Bloom>();
        bloom.intensity.value = 3.2f; bloom.threshold.value = 1.05f; bloom.softKnee.value = 0.7f;
        var vig = profile.AddSettings<Vignette>();
        vig.intensity.value = 0.3f;
        var cg = profile.AddSettings<ColorGrading>();
        cg.postExposure.value = 1.0f; cg.saturation.value = 15f; cg.contrast.value = 6f;
        var ao = profile.AddSettings<AmbientOcclusion>();
        ao.intensity.value = 0.4f; ao.mode.value = AmbientOcclusionMode.ScalableAmbientObscurance;
        vol.profile = profile;
        AssetDatabase.CreateAsset(profile, "Assets/Materials/PPProfile.asset");

        // ---- managers
        var mgr = new GameObject("Managers");
        mgr.AddComponent<Game>();
        mgr.AddComponent<LevelSystem>();
        mgr.AddComponent<AudioManager>();
        var rmc = mgr.AddComponent<RoomManager>();
        rmc.Rooms = roomList;
        rmc.ChaserPrefab = chaserPrefab; rmc.SpitterPrefab = spitterPrefab; rmc.BossPrefab = bossPrefab;
        rmc.BarrierPrefab = barrierPrefab;
        var ui = new GameObject("UI");
        ui.AddComponent<HudUI>(); ui.AddComponent<LevelUpUI>(); ui.AddComponent<GameOverUI>();

        // XP gem prefab via GameRefs (serialized, survives domain reload)
        var refs = mgr.AddComponent<GameRefs>();
        refs.GemPrefab = gemPrefab;

        // directional fill
        var sun = new GameObject("Fill").AddComponent<Light>();
        sun.type = LightType.Directional; sun.intensity = 0.75f; sun.color = new Color(0.55f, 0.65f, 0.95f);
        sun.transform.rotation = Quaternion.Euler(55, -30, 0);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Slice.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Slice.unity", true) };
        AssetDatabase.SaveAssets();
        Debug.Log("[SliceBootstrap] build complete");
    }

    static void WallRun(Transform parent, Vector3 a, Vector3 b, Material wall, Material trim)
    {
        var len = Vector3.Distance(a, b);
        if (len < 0.05f) return;
        var mid = (a + b) / 2f + Vector3.up * 1.6f;
        var horiz = Mathf.Abs(a.x - b.x) > 0.01f;
        var size = horiz ? new Vector3(len, 3.2f, 0.6f) : new Vector3(0.6f, 3.2f, len);
        Box(parent, mid, size, wall, true, "wall");
        // top emissive trim
        var ts = horiz ? new Vector3(len, 0.14f, 0.7f) : new Vector3(0.7f, 0.14f, len);
        Box(parent, new Vector3(mid.x, 3.28f, mid.z), ts, trim, false, "walltrim");
    }
}
