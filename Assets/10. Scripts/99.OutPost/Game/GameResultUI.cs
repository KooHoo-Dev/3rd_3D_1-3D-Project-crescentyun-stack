using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Study3D.OutPost
{
    // 승리/패배 결과 화면. 실행 시 코드로 만들어짐 (씬에 UI를 따로 만들 필요 없음)
    // GameManager와 같은 오브젝트에 붙임
    public class GameResultUI : MonoBehaviour
    {
        [SerializeField] private string victoryText = "하이브 파괴!\n<size=50%>승리</size>";
        [SerializeField] private string defeatText = "전초기지 파괴\n<size=50%>패배</size>";
        [Tooltip("결과 화면 버튼으로 이동할 씬 (Build Settings에 있어야 함)")]
        [SerializeField] private string mainMenuScene = "00. Main";
        [SerializeField] private TMP_FontAsset font;   // 한글 폰트 (비우면 한글이 깨질 수 있음)

        private GameObject root;
        private TMP_Text titleText;

        private void Start()
        {
            BuildUI();
            root.SetActive(false);

            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged += Show;
            else
                Debug.LogWarning("[GameResultUI] 씬에 GameManager가 없음");
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged -= Show;
        }

        private void Show(GameState state)
        {
            if (state == GameState.Playing) return;
            titleText.text = state == GameState.Victory ? victoryText : defeatText;
            root.SetActive(true);
        }

        // 승리/패배 모두 메인 메뉴로
        private void GoToMainMenu()
        {
            GameSession.ResetForNewGame();   // 인벤토리 등 씬이 바뀌어도 남는 것들 정리
            SceneManager.LoadScene(mainMenuScene);
        }

        #region UI 만들기

        private void BuildUI()
        {
            root = new GameObject("GameResult UI", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;   // 건설 메뉴(50)보다 위
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // 어두운 배경 (뒤쪽 클릭도 막음)
            var bg = CreateRect("Background", root.transform);
            Stretch(bg);
            bg.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);

            // 제목
            var title = CreateRect("Title", root.transform);
            title.anchoredPosition = new Vector2(0f, 100f);
            title.sizeDelta = new Vector2(1400f, 300f);
            titleText = AddText(title, "", 110);

            // 다시 시작 버튼
            var buttonRect = CreateRect("MainMenuButton", root.transform);
            buttonRect.anchoredPosition = new Vector2(0f, -160f);
            buttonRect.sizeDelta = new Vector2(340f, 90f);
            var buttonImage = buttonRect.gameObject.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.22f, 0.25f, 1f);
            var button = buttonRect.gameObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            button.onClick.AddListener(GoToMainMenu);

            var label = CreateRect("Label", buttonRect);
            Stretch(label);
            AddText(label, "MainMenu", 40).raycastTarget = false;

            // 버튼 클릭용 EventSystem (없을 때만, Input System용 모듈로)
            if (FindFirstObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private TMP_Text AddText(RectTransform rect, string text, float size)
        {
            var tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            return tmp;
        }

        private static RectTransform CreateRect(string objName, Transform parent)
        {
            var go = new GameObject(objName, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        #endregion
    }
}
