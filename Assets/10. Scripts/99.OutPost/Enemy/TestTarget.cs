using UnityEngine;

namespace Study3D.OutPost
{
    public class TestTarget : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHp = 100f;
        private float hp;
        
        
        private void Awake() => hp = maxHp;

        public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal)
        {
            hp -= damage;
            Debug.Log($"{name} 피격! -{damage} (남은 HP: {hp})");
            if (hp <= 0f) Destroy(gameObject);
            
            
            
        }
    }
}