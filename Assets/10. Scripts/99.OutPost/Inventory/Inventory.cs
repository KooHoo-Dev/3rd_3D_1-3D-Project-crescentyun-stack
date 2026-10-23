using System;
using UnityEngine;




namespace Study3D.OutPost
{
    // 아이템 데이터 (ScriptableObject) + 희귀도·부위·능력치 정의.
    
    public enum EquipSlot { Slot1, Slot2, Melee }

    public struct ItemStat
    {
        public float Damage;
        public float Weight;
      
    }
    public class Inventory
    {
        
    }
}