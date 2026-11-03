using UnityEngine;

namespace Study3D.OutPost
{
    // 하이브(둥지). 폭탄 설치 구역 판정 + 폭파되면 모델을 잔해로 교체
    // 하이브 오브젝트에 붙임 (씬에 1개)
    public class Hive : MonoBehaviour
    {
        public static Hive Instance { get; private set; }

        [Header("폭탄 설치 구역")]
        [Tooltip("하이브 중심에서 이 반경(m) 안에만 폭탄 설치 가능 (주황 원 Gizmo)")]
        [SerializeField] private float plantRadius = 8f;

        [Header("모델")]
        [Tooltip("평소 모습. 폭파되면 숨김")]
        [SerializeField] private GameObject aliveModel;
        [Tooltip("폭파 후 잔해 (선택)")]
        [SerializeField] private GameObject destroyedModel;

        public bool IsDestroyed { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Debug.LogWarning("[Hive] 하이브가 2개 이상 있음. 마지막 것만 사용됨", this);
            Instance = this;

            if (destroyedModel != null) destroyedModel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // 높이는 무시한 수평 거리로 판정
        public bool IsInPlantZone(Vector3 pos)
        {
            Vector3 d = pos - transform.position;
            d.y = 0f;
            return d.sqrMagnitude <= plantRadius * plantRadius;
        }

        // HiveBomb이 폭발할 때 호출
        public void DestroyHive()
        {
            if (IsDestroyed) return;
            IsDestroyed = true;

            if (aliveModel != null) aliveModel.SetActive(false);
            if (destroyedModel != null) destroyedModel.SetActive(true);
            Debug.Log("하이브 파괴됨!");
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.5f, 0f);
            const int segments = 48;
            Vector3 center = transform.position;
            Vector3 prev = center + new Vector3(plantRadius, 0f, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                Vector3 next = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * plantRadius;
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }
    }
}
