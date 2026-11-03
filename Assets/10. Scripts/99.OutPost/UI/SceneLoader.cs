using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace Study3D.OutPost
{
    public class SceneLoader : MonoBehaviour
    {
        [SerializeField] private GameObject loadingCanvas;   // 로딩 패널
        [SerializeField] private string nextSceneName;       // 이동할 씬 이름
        [SerializeField] private float minLoadingTime = 2f;  // 최소 표시 시간

        private bool isLoading = false;

        // 버튼 OnClick에 연결
        public void LoadNextScene()
        {
            Debug.Log("LoadNextScene 호출됨");
            if (isLoading) return;          // 버튼 연타 방지
            isLoading = true;
            StartCoroutine(LoadRoutine());
        }

        private IEnumerator LoadRoutine()
        {
            loadingCanvas.SetActive(true);
            yield return null;              // 로딩 화면이 먼저 그려지도록 1프레임 대기

            AsyncOperation op = SceneManager.LoadSceneAsync(nextSceneName);
            op.allowSceneActivation = false; // 로딩이 끝나도 바로 넘어가지 않게

            float timer = 0f;

            // 로딩 완료(0.9) + 최소 시간 경과 둘 다 만족할 때까지 대기
            while (op.progress < 0.9f || timer < minLoadingTime)
            {
                timer += Time.unscaledDeltaTime;
                yield return null;
            }

            op.allowSceneActivation = true;  // 씬 전환
        }
    }
}
