using System;
using UnityEngine;

namespace Study3D.OutPost
{
    // 슬롯 한 칸에 들어가는 "아이템 묶음"
    // InventoryItem(에셋)은 같은 아이템끼리 공유되므로,
    // 개수·탄창처럼 칸마다 달라지는 값은 여기에 저장한다.
    [Serializable]
    public class ItemStack
    {
        public InventoryItem Data;   // 어떤 아이템인지
        public int Count;            // 겹친 개수
        public int Ammo = -1;        // 무기 전용: 탄창에 남은 탄 (-1 = 아직 장착 전 → 장착 시 가득 채움)

        public ItemStack(InventoryItem data, int count = 1)
        {
            Data = data;
            Count = Mathf.Clamp(count, 1, data != null ? data.MaxStackSize : 1);
        }

        public int MaxStack => Data != null ? Data.MaxStackSize : 0;
        public int SpaceLeft => MaxStack - Count;
        public bool IsFull => Count >= MaxStack;

        // 같은 아이템이고 겹칠 수 있는 종류인지
        public bool CanStackWith(ItemStack other)
        {
            return other != null && other.Data == Data && Data != null && Data.IsStackable;
        }

        /// <summary> other에서 넣을 수 있는 만큼 가져온다. 옮긴 개수를 돌려준다. </summary>
        public int MergeFrom(ItemStack other)
        {
            if (!CanStackWith(other)) return 0;

            int amount = Mathf.Min(SpaceLeft, other.Count);
            Count += amount;
            other.Count -= amount;
            return amount;
        }
    }
}
