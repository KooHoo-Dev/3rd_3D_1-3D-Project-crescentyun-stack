using UnityEngine;

namespace Study3D.OutPost
{ 
    public enum InventoryType
        {
            None,
            Player
        }

    public class Inventory : MonoBehaviour
    {
        [Header("Inventory Settings")]
        [field: SerializeField] public string Name { get; private set; }

        [field: SerializeField] public InventoryType Type = InventoryType.None;
        

    }
        
           
    
}