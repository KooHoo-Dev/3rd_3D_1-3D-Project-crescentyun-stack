using System.Collections.Generic;
using UnityEngine;

namespace Study3D.OutPost
{
    // 근접무기 (V 칸)
    // 무기 모델 프리팹(예: SM_Wep_Bat_Wood_01)의 루트에 붙이고, InventoryItem.MeleePrefab에 연결한다.
    //
    // 판정 방식: 시간 + 범위
    //  1) TryAttack()으로 공격 시작 → hitTime 초 뒤에 한 번 판정
    //  2) 캐릭터 앞쪽 부채꼴(반경 range, 각도 hitAngle) 안의 IDamageable에게 데미지
    //  3) attackDuration 초가 지나면 다음 공격 가능
    // Gun의 재장전(reloadEndTime)과 같은 "시각 예약" 방식이다.
    public class MeleeWeapon : MonoBehaviour
    {
        [Header("무기정보")]
        [Tooltip("Animator WeaponType 값 (0 소총, 1 권총, 3 바주카 예약, 2 근접)")]
        [SerializeField] private int weaponType = 2;
        [SerializeField] private string weaponName = "Bat";
        [SerializeField] private Sprite icon;

        [Header("공격")]
        [SerializeField] private float damage = 35f;
        [Tooltip("공격 시작부터 다음 공격이 가능해질 때까지 (초). AttackB2HandMelee = 45프레임 ≈ 1.5초")]
        [SerializeField] private float attackDuration = 1.0f;
        [Tooltip("공격 시작 후 실제로 맞는 순간 (초). 방망이가 앞을 지나가는 프레임에 맞춘다")]
        [SerializeField] private float hitTime = 0.45f;
        [Tooltip("판정 반경 (m)")]
        [SerializeField] private float range = 1.8f;
        [Tooltip("판정 부채꼴 전체 각도 (도)")]
        [Range(10f, 360f)]
        [SerializeField] private float hitAngle = 120f;
        [Tooltip("판정 중심 높이 (캐릭터 발 기준, m)")]
        [SerializeField] private float hitHeight = 1f;
        [Tooltip("맞을 수 있는 레이어 (적 레이어만 켜면 가장 깔끔함)")]
        [SerializeField] private LayerMask hitMask = ~0;

        [Header("꺼내기 / 넣기 (Animator 상태 길이에 맞춤)")]
        [SerializeField] private float drawTime = 0.8f;
        [SerializeField] private float putBackTime = 0.8f;

        // 내부 변수
        private Transform owner;            // 휘두르는 캐릭터 (판정 기준 위치와 방향)
        private bool isAttacking;
        private bool hasHit;                // 이번 공격에서 판정을 이미 했는지
        private float attackStartTime;
        private float attackEndTime;

        private readonly Collider[] hitBuffer = new Collider[32];
        private readonly HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();   // 한 공격에 같은 적을 두 번 때리지 않도록

        public int WeaponType => weaponType;
        public string WeaponName => weaponName;
        public Sprite Icon => icon;
        public float DrawTime => drawTime;
        public float PutBackTime => putBackTime;
        public bool IsAttacking => isAttacking;

        public event System.Action<int> OnHit;   // 이번 공격에 맞은 대상 수 (타격감 연출용)

        // 생성 직후 PlayerController가 호출
        public void Init(Transform ownerTransform)
        {
            owner = ownerTransform;
        }

        private void OnDisable()
        {
            // 무기를 넣거나 죽어서 꺼지면 공격 중단
            isAttacking = false;
        }

        private void Update()
        {
            if (!isAttacking) return;

            if (!hasHit && Time.time >= attackStartTime + hitTime)
            {
                hasHit = true;
                DoHit();
            }

            if (Time.time >= attackEndTime)
                isAttacking = false;
        }

        // 공격을 시도합니다. 실제로 시작했으면 true (애니메이션 재생 판단용)
        public bool TryAttack()
        {
            if (isAttacking) return false;
            if (owner == null) return false;

            isAttacking = true;
            hasHit = false;
            attackStartTime = Time.time;
            attackEndTime = Time.time + attackDuration;
            return true;
        }

        // 공격을 중단합니다. (구르기, 달리기, 무기 교체)
        public void CancelAttack()
        {
            isAttacking = false;
        }

        private void DoHit()
        {
            Vector3 center = owner.position + Vector3.up * hitHeight;
            Vector3 forward = owner.forward;
            float halfAngle = hitAngle * 0.5f;

            int count = Physics.OverlapSphereNonAlloc(center, range, hitBuffer, hitMask, QueryTriggerInteraction.Ignore);
            hitTargets.Clear();

            for (int i = 0; i < count; i++)
            {
                Collider col = hitBuffer[i];
                if (col.transform.IsChildOf(owner)) continue;   // 자기 자신 제외

                IDamageable target = col.GetComponentInParent<IDamageable>();
                if (target == null || hitTargets.Contains(target)) continue;

                // 부채꼴 안에 있는지 (수평 방향만 비교)
                Vector3 hitPoint = col.ClosestPoint(center);
                Vector3 toTarget = hitPoint - center;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude > 0.0001f && Vector3.Angle(forward, toTarget) > halfAngle) continue;

                hitTargets.Add(target);
                Vector3 hitNormal = toTarget.sqrMagnitude > 0.0001f ? -toTarget.normalized : -forward;
                target.TakeDamage(damage, hitPoint, hitNormal);
            }

            if (hitTargets.Count > 0)
            {
                Debug.Log($"{weaponName} 타격: {hitTargets.Count}명");
                OnHit?.Invoke(hitTargets.Count);
            }
        }

        // Scene 뷰에서 판정 범위 확인용
        private void OnDrawGizmosSelected()
        {
            Transform t = owner != null ? owner : transform;
            Vector3 center = t.position + Vector3.up * hitHeight;
            float halfAngle = hitAngle * 0.5f;

            Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.8f);
            Vector3 left = Quaternion.Euler(0f, -halfAngle, 0f) * t.forward;
            Vector3 right = Quaternion.Euler(0f, halfAngle, 0f) * t.forward;
            Gizmos.DrawLine(center, center + left * range);
            Gizmos.DrawLine(center, center + right * range);

            const int segments = 16;
            Vector3 prev = center + left * range;
            for (int i = 1; i <= segments; i++)
            {
                float a = -halfAngle + hitAngle * i / segments;
                Vector3 next = center + Quaternion.Euler(0f, a, 0f) * t.forward * range;
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }
    }
}
