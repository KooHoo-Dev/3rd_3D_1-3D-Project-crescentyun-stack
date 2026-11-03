using System;
using System.Collections.Generic;
using UnityEngine;

namespace Study3D.OutPost
{
    // 포탑 체력. 0이 되면 머리를 숨겨 잔해(하체)만 남기고, 수리하면 다시 살아남
    // 좀비는 All 목록에서 가까운 포탑을 찾음 (태그 대신 목록 사용 — LootContainer.All과 같은 방식)
    public class TurretHealth : MonoBehaviour, IDamageable
    {
        public static readonly List<TurretHealth> All = new();

        [Tooltip("파괴되면 숨길 부분 (머리). 남은 부분이 잔해")]
        [SerializeField] private GameObject[] hideOnDestroyed;
        [Tooltip("파괴될 때 생성할 이펙트 (선택)")]
        [SerializeField] private GameObject destroyVFX;

        private AutoTurret turret;
        private float currentHealth;
        private bool isDestroyed;

        public TurretData Data => turret != null ? turret.Data : null;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => Data != null ? Data.MaxHealth : 1f;
        public bool IsDestroyed => isDestroyed;

        public event Action<float, float> OnHealthChanged;   // (현재, 최대) 체력바용
        public event Action OnDestroyed;
        public event Action OnRepaired;

        private void Awake()
        {
            turret = GetComponent<AutoTurret>();
        }

        private void OnEnable() => All.Add(this);
        private void OnDisable() => All.Remove(this);

        // Data는 건설 직후 Init으로 들어오므로 Awake가 아니라 Start에서 체력을 채움
        private void Start()
        {
            if (Data == null)
                Debug.LogError($"[TurretHealth] {name}: AutoTurret의 Data가 비어 있어서 최대 체력이 1이 됨", this);

            currentHealth = MaxHealth;
            OnHealthChanged?.Invoke(currentHealth, MaxHealth);
        }

        public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal)
        {
            if (isDestroyed) return;

            currentHealth = Mathf.Max(0f, currentHealth - damage);
            OnHealthChanged?.Invoke(currentHealth, MaxHealth);

            if (currentHealth <= 0f) Break();
        }

        private void Break()
        {
            isDestroyed = true;
            SetPartsVisible(false);

            if (destroyVFX != null)
                Destroy(Instantiate(destroyVFX, transform.position + Vector3.up, Quaternion.identity), 3f);

            Debug.Log($"{name} 파괴됨");
            OnDestroyed?.Invoke();
        }

        // 재료 확인/소모는 TurretRepairer가 하고, 여기서는 상태만 되돌림
        public void Repair()
        {
            if (!isDestroyed) return;

            isDestroyed = false;
            currentHealth = MaxHealth;
            SetPartsVisible(true);

            Debug.Log($"{name} 수리 완료");
            OnRepaired?.Invoke();
            OnHealthChanged?.Invoke(currentHealth, MaxHealth);
        }

        private void SetPartsVisible(bool visible)
        {
            foreach (GameObject part in hideOnDestroyed)
                if (part != null) part.SetActive(visible);
        }

#if UNITY_EDITOR
        // AutoTurret의 "에셋 포탑 설정 가져오기"에서 호출
        public void EditorSetHideParts(params GameObject[] parts)
        {
            UnityEditor.Undo.RecordObject(this, "Set Hide Parts");
            hideOnDestroyed = parts;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
