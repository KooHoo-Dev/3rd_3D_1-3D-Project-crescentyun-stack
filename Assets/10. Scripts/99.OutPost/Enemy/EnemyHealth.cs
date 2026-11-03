using System;
using UnityEngine;

namespace Study3D.OutPost
{
    public class EnemyHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHealth = 100f;

        private float currentHealth;
        private bool isDead;
        
        
        public bool IsDead => isDead;
        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public event Action<float, float> OnHealthChanged;   // (현재 체력, 최대 체력) — 체력바 갱신용

        // 이번 대미지를 준 쪽 (포탑 총알이면 포탑, 플레이어면 null). OnDamaged 안에서만 유효
        public Transform LastAttacker { get; private set; }
        public event Action OnDeath;
        public event Action OnDamaged;   // 맞았지만 아직 살아 있을 때

        private Animator animator;
        private static readonly int DeathHash = Animator.StringToHash("Death");
        
        
        private void Awake()
        {
            currentHealth = maxHealth;
            animator = GetComponentInChildren<Animator>();
        }

        // 대미지 직전에 Bullet이 호출 → 다음 TakeDamage 한 번에만 적용
        public void SetAttacker(Transform attacker)
        {
            LastAttacker = attacker;
        }

        public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal)
        {
            if (isDead) { LastAttacker = null; return; }

            currentHealth = Mathf.Max(0f, currentHealth - damage);
            Debug.Log($"{name} 피격: {damage}, 남은 체력: {currentHealth}");
            OnHealthChanged?.Invoke(currentHealth, maxHealth);   // 죽는 타격도 알림 (바가 0까지 줄어든 뒤 숨김)

            if (currentHealth <= 0f)
            {
                LastAttacker = null;
                Die();
                return;
            }

            OnDamaged?.Invoke();   // 살아 있을 때만 알림 (맞으면 추격 시작에 사용)
            LastAttacker = null;   // 근접 공격 등 SetAttacker 없이 들어온 대미지는 플레이어로 취급
        }

        private void Die()
        {
            isDead = true;
            animator.SetTrigger(DeathHash);
            OnDeath?.Invoke();
            
        }

      
    }
}
