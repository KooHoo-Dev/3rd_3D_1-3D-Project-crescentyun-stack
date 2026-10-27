using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Study3D.OutPost
{
    // 사망하면 일정 시간 뒤 리스폰 화면을 띄우고, 버튼을 누르면 부활
    public class RespawnUI : MonoBehaviour
    {
        [SerializeField] private Button continueButton;
        [SerializeField] private float showDelay = 2f;   // 사망 모션을 보여줄 시간 (초)

        private Canvas canvas;
        private PlayerHealth playerHealth;

        [SerializeField] private RespawnDeathView deathView;   // 변수 추가

     
        
        
        private void Awake()
        {
            canvas = GetComponent<Canvas>();
            canvas.enabled = false;   // 처음엔 숨김
            continueButton.onClick.AddListener(OnClickContinue);
        }

        private void Start()
        {
            if (PlayerController.Instance != null)
                playerHealth = PlayerController.Instance.GetComponent<PlayerHealth>();

            if (playerHealth != null)
                playerHealth.OnDied += HandleDied;   // 사망 알림 구독
        }

        private void OnDestroy()
        {
            if (playerHealth != null)
                playerHealth.OnDied -= HandleDied;   // 구독 해제
        }

        private void HandleDied()
        {
            if (deathView != null) deathView.Show(playerHealth.transform);
            StartCoroutine(ShowAfterDelay());
        }
        
        private IEnumerator ShowAfterDelay()
        {
            yield return new WaitForSeconds(showDelay);
            canvas.enabled = true;
        }
        
        private void OnClickContinue()
        {
            canvas.enabled = false;
            if (deathView != null) deathView.Hide();
            playerHealth.Respawn();
        }
    }
}