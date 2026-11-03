using System;
using System.Collections.Generic;
using UnityEngine;

namespace Study3D.OutPost
{
    /// <summary>
    /// 캐릭터 한 명의 정보. 캐릭터마다 에셋 1개씩 만든다.
    /// (Project 창 우클릭 > Create > OutPost/Character Data)
    ///  - 선택창 표시용 : 이름, 초상화, 일러스트, 설명 이미지
    ///  - 게임 적용용   : 플레이어 프리팹, 스탯, 시작 아이템
    /// </summary>
    [CreateAssetMenu(menuName = "OutPost/Character Data", fileName = "Character_")]
    public class CharacterData : ScriptableObject
    {
        // 시작 아이템 한 줄 (아이템 + 개수)
        [Serializable]
        public class StartItem
        {
            public InventoryItem Item;
            [Min(1)] public int Count = 1;
        }

        [Header("기본 정보")]
        public string DisplayName;

        [Tooltip("체크하면 선택창에서 보기만 가능하고 게임을 시작할 수 없다 (미완성 캐릭터용)")]
        public bool IsStartLocked;

        [Header("선택창 이미지")]
        [Tooltip("하단 아이콘에 들어갈 초상화")]
        public Sprite Portrait;

        [Tooltip("배경 전체에 깔릴 캐릭터 일러스트")]
        public Sprite Illustration;

        [Tooltip("좌측 설명 패널에 들어갈 이미지 (설명 글이 포함된 이미지)")]
        public Sprite InfoImage;

        [Header("게임 적용")]
        [Tooltip("게임 씬에서 생성할 플레이어 프리팹 (PlayerController 가 붙어 있어야 함)")]
        public PlayerController PlayerPrefab;

        [Header("스탯")]
        [Min(1f)] public float MaxHp      = 100f;
        [Min(0f)] public float MoveSpeed  = 3f;
        [Min(0f)] public float RunSpeed   = 7f;
        [Min(1f)] public float MaxStamina = 100f;

        [Header("시작 아이템")]
        public List<StartItem> StartItems = new List<StartItem>();
    }
}
