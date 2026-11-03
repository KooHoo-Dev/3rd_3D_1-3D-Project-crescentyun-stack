using System;
using System.Collections.Generic;
using UnityEngine;

namespace Study3D.OutPost
{
    // 재료 1종류 + 개수 (건설/수리 비용 한 줄)
    [Serializable]
    public struct MaterialCost
    {
        public MaterialType Type;   // Wood / Scrap
        [Min(1)] public int Count;
    }

    // 포탑 1종류의 수치 묶음. 포탑마다 에셋 파일 1개 (Turret_MachineGun, Turret_TwinGun)
    [CreateAssetMenu(menuName = "OutPost/Turret Data", fileName = "Turret_")]
    public class TurretData : ScriptableObject
    {
        [Header("기본 정보 (건설 메뉴에 표시)")]
        public string DisplayName;
        public Sprite Icon;
        [TextArea] public string Description;

        [Header("프리팹")]
        [Tooltip("건설하면 생성될 포탑 프리팹")]
        public GameObject TurretPrefab;

        [Header("공격")]
        [Tooltip("총알 1발 대미지")]
        [Min(0f)] public float Damage = 20f;

        [Tooltip("발사 간격(초). 작을수록 빠르게 쏨")]
        [Min(0.01f)] public float FireInterval = 0.5f;

        [Tooltip("재장전 전까지 쏠 수 있는 총알 수 (총이 2개면 한 번 쏠 때 2발 소모)")]
        [Min(1)] public int MagazineSize = 20;

        [Tooltip("재장전 시간(초)")]
        [Min(0f)] public float ReloadTime = 2.5f;

        [Tooltip("좀비를 찾는 범위(m)")]
        [Min(0f)] public float Range = 15f;

        [Tooltip("머리가 도는 속도(도/초)")]
        [Min(0f)] public float RotateSpeed = 180f;

        [Tooltip("머리 방향과 좀비 방향의 차이가 이 각도 안이면 발사")]
        [Range(0f, 45f)] public float AttackAngle = 5f;

        [Header("체력")]
        [Min(1f)] public float MaxHealth = 300f;

        [Header("건설 재료 (기관총: Wood 3, Scrap 2 / 쌍기관총: Wood 4, Scrap 6)")]
        public List<MaterialCost> BuildCost = new();

        // 수리 비용 = 건설 비용의 절반 (홀수면 올림: 3개 → 2개, 1개 → 1개)
        public int RepairCountOf(MaterialCost cost) => Mathf.CeilToInt(cost.Count / 2f);
    }
}
