using UnityEngine;

namespace Study3D.OutPost
{
    [CreateAssetMenu(fileName = "InventoryItem", 
        menuName = "Study SAO/InventoryItem", order = 1)]
    public class InventoryItem : ScriptableObject
    {
        public int      Key;            
        public string   Name;           
        public string   Description;    
        public Sprite   Icon;           
        public int      Price;          
    }
}