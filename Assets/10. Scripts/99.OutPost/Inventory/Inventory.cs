using Study3D.OutPost;
using UnityEngine;

namespace Study.OutPost
{
    // 인벤토리의 용도를 표시하는 enum
    public enum InventoryType
    {
        None,       // 설정되지 않은 상태 (루팅 인벤토리 포함)
        Player,     // 유저 소유의 인벤토리(퀵슬롯 포함)
        
    }

    public class Inventory : MonoBehaviour
    {

        [Header("Inventory Settings")]
        [field: SerializeField] public string Name { get; private set; }
        [field: SerializeField] public InventoryType Type = InventoryType.None;
        [field: SerializeField] public int SizeRow { get; private set; } = 8;
        [field: SerializeField] public int SizeColumn { get; private set; } = 5;

        public InventorySlot[,] SlotGrid { get; private set; }

        private void Awake()
        {
            InitSlotGrid();

      
            void InitSlotGrid()
            {
               
                SlotGrid = new InventorySlot[SizeRow, SizeColumn];

              
                InventorySlot[] slots = GetComponentsInChildren<InventorySlot>();

         
                for(int i = 0; i < slots.Length; ++i)
                {
              
                    int rowIndex = i / SizeColumn;
                    int columnIndex = i % SizeColumn;

              
                    SlotGrid[rowIndex, columnIndex] = slots[i];
                }
            }
        }

        private void Start()
        {
            InventorySystem.Instance.Register(this);
        }

       
        public bool IsIn(InventorySlot slot)
        {
            if (slot == null) return false;

            // 선형 탐색으로 구현합시다.
            for(int x = 0; x < SlotGrid.GetLength(0); ++x)
            {
                for (int y = 0; y < SlotGrid.GetLength(1); ++y)
                {
                    if (SlotGrid[x, y] == slot) return true;
                }
            }
            return false;
        }
    }

}

