using System.Collections.Generic;
using Jay;
using Study.OutPost;


namespace Study3D.OutPost
{
    public class InventorySystem : SingletonBase<InventorySystem>
    {
        private List<Inventory> Inventories {get; set;} = new List <Inventory>();


        public void Register(Inventory inventory)
        {
            Inventories.Add(inventory);
        }
        
        public void Remove (Inventory inventory)
        {
            Inventories.Remove(inventory);
        }

        public Inventory GetInventoryOrNull(string targetName)
        {
            for (int i = 0; i < Inventories.Count; i++)
            {
                if (Inventories[i].Name.Equals(targetName))
                    return Inventories[i];
            }
            return null;
        }

        public Inventory GetInventoryOrNull(InventorySlot slot)
        {
           
            for(int i = 0; i < Inventories.Count; ++i)
            {
                if (Inventories[i].IsIn(slot)) return Inventories[i];
            }

            return null;
        }
        
        
    }
}