using System;
using UnityEngine;

namespace Study3D.OutPost
{
    // 플레이어의 전초기지. 좀비(Assault)가 진격해서 공격하는 대상
    // 콜라이더 필요: 좀비가 가장 가까운 면까지 거리로 공격 판정함 (BoxCollider 권장, 볼록하지 않은 MeshCollider는 X)
    public class OutpostBase : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHealth = 1000f;

        private float currentHealth;
        private bool isDestroyed;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsDestroyed => isDestroyed;

        public event Action<float, float> OnHealthChanged;   // (현재, 최대) UI용
        public event Action OnDestroyed;

        private void Awake()
        {
            currentHealth = maxHealth;
        }

        public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal)
        {
            if (isDestroyed) return;

            currentHealth = Mathf.Max(0f, currentHealth - damage);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0f)
            {
                isDestroyed = true;
                Debug.Log("전초기지 파괴됨!");
                OnDestroyed?.Invoke();

                // 전초기지가 무너지면 패배
                if (GameManager.Instance != null) GameManager.Instance.Lose();
            }
        }
    }
}
