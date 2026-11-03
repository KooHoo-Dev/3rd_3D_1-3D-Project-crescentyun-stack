using UnityEngine;

namespace Study3D.OutPost
{
    // 플레이어 시야 영역 계산 (커서 방향 부채꼴 + 주변 원)
    public class PlayerVision : MonoBehaviour
    {
        [Header("시야 크기")]
        [SerializeField] private float viewAngle = 90f;     // 부채꼴 전체 각도
        [SerializeField] private float viewDistance = 15f;  // 부채꼴 거리 (m)
        [SerializeField] private float nearRadius = 2.5f;   // 주변 원 반경 (m)

        [Header("레이캐스트")]
        [SerializeField] private int rayCount = 180;        // 360°를 몇 개로 나눌지 (180 = 2°마다)
        [SerializeField] private float eyeHeight = 1f;
        [SerializeField] private LayerMask obstacleMask;    // 시야를 가리는 벽 레이어

        [Header("메시")]
        [SerializeField] private MeshFilter viewMeshFilter;   // 시야 메시를 그릴 오브젝트
        [SerializeField] private float meshHeight = 0.05f;    // 바닥과 겹쳐 깜빡이지 않도록 살짝 위

        private Mesh viewMesh;
        private Vector3[] vertices;   // [0] 중심 + [1~rayCount] 레이 끝점
        private int[] triangles;      // 삼각형 1개당 정점 번호 3개
        
        
        private PlayerController player;

        public static PlayerVision Instance { get; private set; }
        
        
        private void Awake()
        {
            Instance = this;
            
            player = GetComponent<PlayerController>();
            viewMesh = new Mesh { name = "View Mesh" };
            viewMesh.MarkDynamic();               // 매 프레임 바뀌는 메시라고 알려줌

            // 프리팹은 씬 오브젝트(VisionMesh)를 참조할 수 없으므로 비어 있을 수 있다
            // → 생성 직후 PlayerSpawner 가 SetViewMeshFilter 로 넣어준다
            if (viewMeshFilter != null) viewMeshFilter.mesh = viewMesh;

            vertices = new Vector3[rayCount + 1];
            triangles = new int[rayCount * 3];
            
            
            
        }

        /// <summary> 시야 메시를 그릴 오브젝트 연결 (프리팹으로 생성된 경우 PlayerSpawner 가 호출) </summary>
        public void SetViewMeshFilter(MeshFilter filter)
        {
            viewMeshFilter = filter;
            if (viewMeshFilter != null) viewMeshFilter.mesh = viewMesh;
        }

        // aimDirection은 Update에서 갱신되므로, 그 뒤인 LateUpdate에서 계산
        private void LateUpdate()
        {
            if (viewMeshFilter == null) return;   // 연결 전에는 그리지 않음 (CanSee 판정은 계속 동작)

            Vector3 eye = transform.position + Vector3.up * eyeHeight;
            float groundY = transform.position.y + meshHeight;
            Transform meshTf = viewMeshFilter.transform;
            float step = 360f / rayCount;

            // 중심점 (발밑)
            Vector3 center = transform.position;
            center.y = groundY;
            vertices[0] = meshTf.InverseTransformPoint(center);

            for (int i = 0; i < rayCount; i++)
            {
                Vector3 dir = Quaternion.Euler(0f, i * step, 0f) * Vector3.forward;
                Vector3 point = CastViewRay(eye, dir, GetRange(dir));
                point.y = groundY;                                       // 눈높이 → 바닥 높이로 내림
                vertices[i + 1] = meshTf.InverseTransformPoint(point);   // 월드 → 메시 오브젝트 기준 좌표

                // (중심, 이번 점, 다음 점). 마지막 점은 1번과 이어서 원을 닫음
                triangles[i * 3]     = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = (i + 1) % rayCount + 1;
            }

            viewMesh.Clear();
            viewMesh.vertices = vertices;
            viewMesh.triangles = triangles;
            viewMesh.RecalculateBounds();   // 화면 밖 판정(컬링)용 크기 갱신
        }

        // 이 방향이 부채꼴 안이면 viewDistance, 밖이면 nearRadius
        private float GetRange(Vector3 dir)
        {
            float angle = Vector3.Angle(player.AimDirection, dir);
            return angle <= viewAngle * 0.5f ? viewDistance : nearRadius;
        }

        // CanSeePlayer의 ③벽 판정과 같은 원리: 벽에 맞으면 그 지점까지만
        private Vector3 CastViewRay(Vector3 origin, Vector3 dir, float range)
        {
            if (Physics.Raycast(origin, dir, out RaycastHit hit, range, obstacleMask))
                return hit.point;
            return origin + dir * range;
        }
        
        
        // 이 위치가 시야(밝은 영역) 안이면 true. 시야 메시와 같은 계산을 사용
        public bool CanSee(Vector3 targetPos)
        {
            Vector3 eye = transform.position + Vector3.up * eyeHeight;
            Vector3 toTarget = targetPos - eye;
            toTarget.y = 0f;                          // 메시처럼 수평으로만 판정
            float distance = toTarget.magnitude;
            if (distance < 0.01f) return true;

            Vector3 dir = toTarget / distance;

            // 1) 거리 + 각도: 부채꼴 안이면 viewDistance, 밖이면 nearRadius까지만
            if (distance > GetRange(dir)) return false;

            // 2) 벽: 가는 길에 벽이 있으면 안 보임
            return !Physics.Raycast(eye, dir, distance, obstacleMask);
        }
        
    }
}