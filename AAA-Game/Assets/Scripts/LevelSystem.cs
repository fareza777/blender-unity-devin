using UnityEngine;

namespace Slice
{
    public class LevelSystem : MonoBehaviour
    {
        public static LevelSystem I;
        public int Level = 1;
        public int Xp;
        public int XpToNext = 5;

        void Awake() { I = this; }

        public static void AddXp(int amount)
        {
            if (I == null) return;
            I.Xp += amount;
            while (I.Xp >= I.XpToNext)
            {
                I.Xp -= I.XpToNext;
                I.Level++;
                I.XpToNext = 4 + I.Level * 3;
                Game.I.SetState(GameState.LevelUp);
                AudioManager.Play("levelup");
                LevelUpUI.Show();
            }
        }
    }
}
