using System.Text;

namespace Study3D.OutPost
{
    // 건설/수리 재료를 플레이어 인벤토리에서 확인하고 소모함
    // 수리 = 건설 재료의 절반 (TurretData.RepairCountOf, 올림)
    public static class TurretCost
    {
        public static int AmountOf(TurretData data, MaterialCost cost, bool repair)
            => repair ? data.RepairCountOf(cost) : cost.Count;

        public static bool CanAfford(TurretData data, bool repair)
        {
            if (data == null) return false;

            foreach (MaterialCost cost in data.BuildCost)
            {
                if (cost.Type == MaterialType.None) continue;
                if (CountOf(cost.Type) < AmountOf(data, cost, repair)) return false;
            }
            return true;
        }

        // CanAfford로 확인한 뒤에 호출
        public static void Pay(TurretData data, bool repair)
        {
            if (!InventorySystem.IsInitialized) return;

            foreach (MaterialCost cost in data.BuildCost)
            {
                if (cost.Type == MaterialType.None) continue;
                InventorySystem.Instance.ConsumeMaterial(cost.Type, AmountOf(data, cost, repair));
            }
        }

        // "Wood 2/3, Scrap 5/2" 처럼 (가진 개수/필요 개수)
        public static string Describe(TurretData data, bool repair)
        {
            if (data == null) return "";

            var sb = new StringBuilder();
            foreach (MaterialCost cost in data.BuildCost)
            {
                if (cost.Type == MaterialType.None) continue;
                if (sb.Length > 0) sb.Append(", ");
                sb.Append($"{cost.Type} {CountOf(cost.Type)}/{AmountOf(data, cost, repair)}");
            }
            return sb.Length > 0 ? sb.ToString() : "No materials";
        }

        private static int CountOf(MaterialType type)
            => InventorySystem.IsInitialized ? InventorySystem.Instance.CountMaterial(type) : 0;
    }
}
