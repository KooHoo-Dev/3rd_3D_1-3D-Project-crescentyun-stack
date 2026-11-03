using UnityEngine;

namespace Study3D.OutPost
{
    // 플레이어가 기지에 처음 도착하면 연결된 마커들을 표시 (예: 하이브 네비)
    // 이 오브젝트에 Collider(Is Trigger 체크)가 필요함
    [RequireComponent(typeof(Collider))]
    public class BaseArrivalTrigger : MonoBehaviour
    {
        [SerializeField] private BaseWaypoint[] showMarkers;   // 도착하면 Show() 할 마커 (Start Hidden 체크된 것)
        [Tooltip("도착하면 표시할 전초기지 체력바. 비우면 씬에서 자동으로 찾음")]
        [SerializeField] private OutpostBaseHealthUI baseHealthUI;

        private bool triggered;

        // 컴포넌트를 붙일 때 자동으로 트리거로 설정
        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
            gameObject.layer = 2;   // Ignore Raycast
        }

        // 트리거 박스가 시야 레이캐스트를 가리지 않도록 Ignore Raycast 레이어로 고정 (ZoneActivationTrigger와 동일)
        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            gameObject.layer = 2;   // Ignore Raycast
        }

        private void OnTriggerEnter(Collider other)
        {
            if (triggered) return;
            if (other.GetComponentInParent<PlayerController>() == null) return;   // 플레이어만

            Arrive();
        }

        private void Arrive()
        {
            if (triggered) return;
            triggered = true;

            foreach (BaseWaypoint marker in showMarkers)
            {
                if (marker != null) marker.Show();
            }

            // 전초기지 체력바 표시 (좌측 상단)
            if (baseHealthUI == null) baseHealthUI = FindAnyObjectByType<OutpostBaseHealthUI>();
            if (baseHealthUI != null) baseHealthUI.Show();
            Debug.Log($"{name}: 기지 도착 → 마커 {showMarkers.Length}개 표시");
        }

        // 플레이 중 인스펙터의 ⋮ 메뉴에서 실행
        [ContextMenu("테스트: 기지 도착")]
        private void TestArrive()
        {
            if (!Application.isPlaying) { Debug.LogWarning("플레이 중에만 사용 가능"); return; }
            Arrive();
        }

        // 씬에서 트리거 범위 확인 (하늘색)
        private void OnDrawGizmos()
        {
            Collider col = GetComponent<Collider>();
            if (col == null) return;

            Gizmos.color = triggered ? new Color(0.5f, 0.5f, 0.5f, 0.2f) : new Color(0f, 0.8f, 1f, 0.2f);
            Gizmos.DrawCube(col.bounds.center, col.bounds.size);
            Gizmos.color = new Color(0f, 0.8f, 1f);
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }
}
