using UnityEngine;
using UnityEngine.UI;

namespace Slice
{
    public class LevelUpUI : MonoBehaviour
    {
        static LevelUpUI _i;
        GameObject _root;
        float _autoPickT = -1f;
        Button _first;

        void Start() { _i = this; }

        void Update()
        {
            if (_autoPickT > 0f)
            {
                _autoPickT -= Time.unscaledDeltaTime;
                if (_autoPickT <= 0f && _first != null) _first.onClick.Invoke();
            }
        }

        public static void Show() { _i?.Build(); }

        void Build()
        {
            if (_root != null) Destroy(_root);
            _root = new GameObject("LevelUpUI");
            var c = UiKit.Canvas("LevelUpCanvas"); c.transform.SetParent(_root.transform, false);
            var bg = UiKit.Panel(c.transform, new Vector2(0.5f, 0.5f), new Vector2(4000, 4000), new Color(0, 0, 0, 0.78f));
            UiKit.Txt(bg, "LEVEL UP", 72, new Color(0.3f, 1f, 0.6f), new Vector2(0.5f, 0.5f), new Vector2(0, 240));
            UiKit.Txt(bg, "choose your power", 30, new Color(0.7f, 0.7f, 0.8f), new Vector2(0.5f, 0.5f), new Vector2(0, 185));

            var picks = UpgradeSystem.DrawThree();
            for (int i = 0; i < picks.Count; i++)
            {
                var u = picks[i];
                float x = (i - 1) * 430f;
                var b = UiKit.Btn(bg, "", 30, new Vector2(x, -20), new Vector2(390, 260), new Color(0.12f, 0.12f, 0.2f, 0.95f), Color.white);
                UiKit.Txt(b.transform, u.Name, 34, new Color(1f, 0.85f, 0.3f), new Vector2(0.5f, 0.5f), new Vector2(0, 60));
                UiKit.Txt(b.transform, u.Desc, 26, new Color(0.85f, 0.85f, 0.9f), new Vector2(0.5f, 0.5f), new Vector2(0, -30), TextAnchor.MiddleCenter, new Vector2(360, 120));
                b.onClick.AddListener(() =>
                {
                    u.Apply();
                    Game.I.SetState(GameState.Playing);
                    Fx.Burst(FindFirstObjectByType<PlayerController>().transform.position + Vector3.up, new Color(0.3f, 1f, 0.6f), 30);
                    Destroy(_root);
                });
                if (i == 0) _first = b;
            }

            var pilot = FindFirstObjectByType<AutoPilot>();
            if (pilot != null && pilot.Enabled) _autoPickT = 0.9f;
        }
    }
}
