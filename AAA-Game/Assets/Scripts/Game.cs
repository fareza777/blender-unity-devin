using UnityEngine;
using UnityEngine.SceneManagement;

namespace Slice
{
    public enum GameState { Playing, LevelUp, Dead, Victory }

    public class Game : MonoBehaviour
    {
        public static Game I;
        public GameState State = GameState.Playing;
        public float RunTime;
        public int Kills;
        public int RoomsCleared;

        void Awake() { I = this; }

        void Update()
        {
            if (State == GameState.Playing) RunTime += Time.deltaTime;
        }

        public void SetState(GameState s)
        {
            State = s;
            Time.timeScale = (s == GameState.LevelUp || s == GameState.Dead || s == GameState.Victory) ? 0f : 1f;
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
