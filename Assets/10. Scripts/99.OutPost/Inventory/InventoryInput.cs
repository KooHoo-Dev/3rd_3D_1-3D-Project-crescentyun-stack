using UnityEngine;

namespace Study3D.OutPost
{
    public class InventoryInput : MonoBehaviour
    {
        [field: SerializeField] public InventorySlot CurrentSlot { get; private set; }
    }
}