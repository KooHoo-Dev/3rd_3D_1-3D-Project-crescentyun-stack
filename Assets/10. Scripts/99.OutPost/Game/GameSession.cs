using UnityEngine;
using UnityEngine.SceneManagement;

namespace Study3D.OutPost
{
    // 새 게임을 위한 초기화. 씬에 붙일 필요 없음 (게임 시작 시 자동 등록)
    //
    // 씬을 다시 불러오면 대부분은 새로 만들어지지만, 아래는 씬이 바뀌어도 남아 있어서 직접 정리해야 함
    //  - InventorySystem : SingletonBase라서 DontDestroyOnLoad → 인벤토리, Hotbar(장착 총/바로가기)가 그대로 남음
    //                      다음 판에서는 새 씬의 InventorySystem이 "중복"으로 지워지고 옛 것이 계속 쓰임
    //  - SelectedCharacter : static → 지난 판에 고른 캐릭터가 남음
    //  - Time.timeScale   : 승리/패배 화면에서 0으로 멈춘 상태
    //
    // 메인 메뉴 씬(Build Settings 0번)이 열리면 자동으로 ResetForNewGame() 실행
    // → 결과 화면 버튼이든, 다른 경로든 메인 메뉴로 가기만 하면 초기화됨
    public static class GameSession
    {
        private const int MainMenuBuildIndex = 0;   // Build Settings: 0 = 00. Main

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Register()
        {
            // 에디터에서 Domain Reload를 꺼도 두 번 등록되지 않게
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single && scene.buildIndex == MainMenuBuildIndex)
                ResetForNewGame();
        }

        public static void ResetForNewGame()
        {
            Time.timeScale = 1f;

            // 인벤토리 / Hotbar 데이터와 UI를 통째로 제거 → 다음 게임 씬에서 새로 만들어짐
            if (InventorySystem.IsInitialized)
                Object.Destroy(InventorySystem.Instance.gameObject);

            SelectedCharacter.Clear();

            Debug.Log("[GameSession] 새 게임을 위해 초기화");
        }
    }
}
