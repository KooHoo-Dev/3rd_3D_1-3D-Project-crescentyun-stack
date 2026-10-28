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
        public event Action OnDeath;

        private Animator animator;
        private static readonly int DeathHash = Animator.StringToHash("Death");
        
        
        private void Awake()
        {
            currentHealth = maxHealth;
            animator = GetComponentInChildren<Animator>();
        }

        public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal)
        {
            if (isDead) return;

            currentHealth -= damage;
            Debug.Log($"{name} 피격: {damage}, 남은 체력: {currentHealth}");

            if (currentHealth <= 0f)
                Die();
        }

        private void Die()
        {
            isDead = true;
            animator.SetTrigger(DeathHash);
            OnDeath?.Invoke();
            
        }

      
    }
}
