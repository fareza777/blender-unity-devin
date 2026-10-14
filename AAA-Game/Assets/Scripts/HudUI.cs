using UnityEngine;
using UnityEngine.UI;

namespace Slice
{
    public class HudUI : MonoBehaviour
    {
        static HudUI _i;
        Image _hpFill, _xpFill;
        Text _timer, _kills, _level, _announce;
        float _announceT;

        void Start()
        {
            _i = this;
            var c = UiKit.Canvas("HUD");
            _hpFill = null;
            UiKit.Bar(c.transform, new Vector2(0, 1), new Vector2(30, -30), new Vector2(420, 30), new Color(0.1f, 0.1f, 0.14f, 0.9f), new Color(0.15f, 0.9f, 0.5f), out _hpFill);
            UiKit.Bar(c.transform, new Vector2(0.5f, 0), new Vector2(0, 22), new Vector2(700, 20), new Color(0.1f, 0.1f, 0.14f, 0.9f), new Color(0.3f, 0.7f, 1f), out _xpFill);
            _timer = UiKit.Txt(c.transform, "0:00", 34, Color.white, new Vector2(1, 1), new Vector2(-160, -40));
            _kills = UiKit.Txt(c.transform, "KILLS 0", 24, new Color(1f, 0.5f, 0.5f), new Vector2(1, 1), new Vector2(-160, -80));
            _level = UiKit.Txt(c.transform, "LV 1", 26, new Color(0.4f, 0.8f, 1f), new Vector2(0, 1), new Vector2(80, -75));
            _announce = UiKit.Txt(c.transform, "", 60, new Color(1f, 0.85f, 0.3f), new Vector2(0.5f, 0.75f), Vector2.zero);
        }

        void Update()
        {
            var p = FindFirstObjectByType<PlayerController>();
            var ls = LevelSystem.I;
            if (p != null && _hpFill != null) _hpFill.fillAmount = p.Hp.Current / p.Hp.Max;
            if (ls != null && _xpFill != null) _xpFill.fillAmount = (float)ls.Xp / ls.XpToNext;
            if (ls != null) _level.text = "LV " + ls.Level;
            if (Game.I != null)
            {
                int m = (int)(Game.I.RunTime / 60), s = (int)(Game.I.RunTime % 60);
                _timer.text = m + ":" + s.ToString("00");
                _kills.text = "KILLS " + Game.I.Kills;
            }
            if (_announceT > 0)
            {
                _announceT -= Time.unscaledDeltaTime;
                var c = _announce.color; c.a = Mathf.Clamp01(_announceT); _announce.color = c;
            }
        }

        public static void Announce(string s)
        {
            if (_i == null) return;
            _i._announce.text = s;
            _i._announceT = 2.2f;
            var c = _i._announce.color; c.a = 1f; _i._announce.color = c;
        }
    }
}
