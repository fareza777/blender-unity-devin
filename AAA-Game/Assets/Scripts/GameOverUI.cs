using UnityEngine;
using UnityEngine.UI;

namespace Slice
{
    public class GameOverUI : MonoBehaviour
    {
        static GameOverUI _i;
        void Start() { _i = this; }

        public static void Show(bool victory) { _i?.Build(victory); }

        void Build(bool victory)
        {
            var root = new GameObject("GameOverUI");
            var c = UiKit.Canvas("GameOverCanvas"); c.transform.SetParent(root.transform, false);
            var bg = UiKit.Panel(c.transform, new Vector2(0.5f, 0.5f), new Vector2(4000, 4000), new Color(0, 0, 0, 0.85f));
            var col = victory ? new Color(0.3f, 1f, 0.6f) : new Color(1f, 0.25f, 0.25f);
            UiKit.Txt(bg, victory ? "DUNGEON CONQUERED" : "YOU DIED", 90, col, new Vector2(0.5f, 0.5f), new Vector2(0, 160));
            int m = (int)(Game.I.RunTime / 60), s = (int)(Game.I.RunTime % 60);
            UiKit.Txt(bg, $"survived {m}:{s:00}   ·   {Game.I.Kills} kills   ·   {Game.I.RoomsCleared} rooms   ·   lv {LevelSystem.I.Level}",
                34, new Color(0.8f, 0.8f, 0.9f), new Vector2(0.5f, 0.5f), new Vector2(0, 60));
            var b = UiKit.Btn(bg, "RUN IT BACK", 40, new Vector2(0, -120), new Vector2(420, 90), new Color(0.15f, 0.15f, 0.25f), Color.white);
            b.onClick.AddListener(() => Game.I.Restart());
            AudioManager.Play(victory ? "victory" : "death");
        }
    }
}
