using UnityEngine;
using Game.Defenses;

namespace Shop
{
    [CreateAssetMenu(menuName = "Shop/Trap")]
    public class TrapShopData : ShopItemData
    {
        public Defenses.DefenseData defenseData;
    }
}