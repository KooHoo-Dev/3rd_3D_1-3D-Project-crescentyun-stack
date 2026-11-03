using UnityEngine;

namespace Study3D.OutPost
{
    // 플레이어가 이 구역에 들어오면 연결된 스폰 지역들의 생성을 시작시킴
    // 예: 특정 선을 넘으면 전초기지 공격 좀비 생성 시작
    // 이 오브젝트에 Collider(Is Trigger 체크)가 필요함
    [RequireComponent(typeof(Collider))]
    public class ZoneActivationTrigger : MonoBehaviour
    {
        [SerializeField] private ZombieSpawnZone[] zones;   // 시작시킬 지역들 (Wait For Activation 체크된 지역)

        private bool triggered;

        // 컴포넌트를 붙일 때 자동으로 트리거로 설정
        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
            gameObject.layer = 2;   // Ignore Raycast
        }

        // 트리거 박스가 시야 레이캐스트(벽 판정)를 가리지 않도록 Ignore Raycast 레이어로 고정
        // (Physics.Raycast는 기본적으로 트리거 콜라이더도 맞기 때문에, 벽 레이어와 같은 레이어면 시야가 막힘)
        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            gameObject.layer = 2;   // Ignore Raycast
        }

        private void OnTriggerEnter(Collider other)
        {
            if (triggered) return;
            if (other.GetComponentInParent<PlayerController>() == null) return;   // 플레이어만

            ActivateZones("플레이어 진입");
        }

        private void ActivateZones(string reason)
        {
            if (triggered) return;
            triggered = true;

            foreach (ZombieSpawnZone zone in zones)
            {
                if (zone != null) zone.Activate();
            }
            Debug.Log($"{name}: {reason} → 스폰 지역 {zones.Length}개 시작");
        }


        // ───── 테스트용 ─────
        [Header("테스트")]
        [SerializeField] private bool enableTestKey = true;   // 빌드 전에 끄기

        private void Update()
        {
            if (!enableTestKey || triggered) return;

#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.f5Key.wasPressedThisFrame) ActivateZones("F5 테스트");
#else
            if (Input.GetKeyDown(KeyCode.F5)) ActivateZones("F5 테스트");
#endif
        }

        // 플레이 중 인스펙터의 ⋮ 메뉴에서 실행
        [ContextMenu("테스트: 지역 시작")]
        private void TestActivate()
        {
            if (!Application.isPlaying) { Debug.LogWarning("플레이 중에만 사용 가능"); return; }
            ActivateZones("인스펙터 테스트");
        }

        // 씬에서 트리거 범위 확인 (주황색)
        private void OnDrawGizmos()
        {
            Collider col = GetComponent<Collider>();
            if (col == null) return;

            Gizmos.color = triggered ? new Color(0.5f, 0.5f, 0.5f, 0.2f) : new Color(1f, 0.5f, 0f, 0.25f);
            Gizmos.DrawCube(col.bounds.center, col.bounds.size);
            Gizmos.color = new Color(1f, 0.5f, 0f);
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);

            // 연결된 지역까지 선
            if (zones == null) return;
            foreach (ZombieSpawnZone zone in zones)
            {
                if (zone != null) Gizmos.DrawLine(col.bounds.center, zone.transform.position);
            }
        }
    }
}