using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Study3D.OutPost
{
    // 파괴된 포탑(잔해) 근처에서 N키 → 재료(건설의 절반)를 내고 수리
    // 플레이어는 캐릭터 선택 후 생성되므로 씬의 TurretSystem 오브젝트에 붙이고 PlayerController.Instance를 사용
    public class TurretRepairer : MonoBehaviour
    {
        [SerializeField] private float range = 2.5f;   // 잔해와의 수리 가능 거리 (m)

        private void Update()
        {
            if (!SimpleInput.GetKeyDown(Key.N)) return;
            if (TurretBuilder.IsBusy) return;
            if (InventorySystem.IsInitialized && InventorySystem.Instance.IsOpen) return;

            PlayerController player = PlayerController.Instance;
            if (player == null) return;

            TurretHealth turret = FindNearestWreck(player.transform.position);
            if (turret == null) return;

            if (!TurretCost.CanAfford(turret.Data, true))
            {
                Debug.Log($"수리 재료 부족: {TurretCost.Describe(turret.Data, true)}");
                return;
            }

            TurretCost.Pay(turret.Data, true);
            turret.Repair();
        }

        private TurretHealth FindNearestWreck(Vector3 from)
        {
            TurretHealth best = null;
            float bestSqr = range * range;

            foreach (TurretHealth t in TurretHealth.All)
            {
                if (!t.IsDestroyed) continue;
                Vector3 d = t.transform.position - from;
                d.y = 0f;
                if (d.sqrMagnitude > bestSqr) continue;
                bestSqr = d.sqrMagnitude;
                best = t;
            }
            return best;
        }
    }
}
