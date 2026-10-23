using UnityEngine;

namespace Study3D.OutPost
{
    // 데미지를 받을 수 있는 모든 대상이 구현하는 약속
    public interface IDamageable
    {
        // hitPoint, hitNormal: 맞은 위치와 면의 방향 (피 튀김 이펙트 등에 사용)
        void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal);
    }
}