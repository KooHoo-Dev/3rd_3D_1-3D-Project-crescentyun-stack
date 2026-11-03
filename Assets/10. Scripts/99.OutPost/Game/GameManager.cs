using System;
using UnityEngine;

namespace Study3D.OutPost
{
    public enum GameState { Playing, Victory, Defeat }

    // 게임 진행 상태 관리. 하이브가 폭파되면 HiveBomb이 Win()을 호출함 (패배는 지금은 사용 안 함)
    // 씬의 빈 오브젝트(GameManager)에 GameResultUI와 같이 붙임
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        // 승리/패배로 게임이 끝났으면 true → 플레이어 사격, 건설 메뉴를 막음
        public static bool IsGameOver => Instance != null && Instance.State != GameState.Playing;

        public GameState State { get; private set; } = GameState.Playing;

        public event Action<GameState> OnStateChanged;   // GameResultUI가 구독

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Time.timeScale = 1f;   // 다시 시작했을 때 멈춘 상태로 남지 않게
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Win()  => EndGame(GameState.Victory);
        public void Lose() => EndGame(GameState.Defeat);

        private void EndGame(GameState result)
        {
            if (State != GameState.Playing) return;   // 두 번 처리되지 않게

            State = result;
            Time.timeScale = 0f;
            Debug.Log($"게임 종료: {result}");
            OnStateChanged?.Invoke(State);
        }
    }
}
