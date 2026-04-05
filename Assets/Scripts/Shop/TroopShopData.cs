using Game.Units;
using UnityEngine;

namespace Shop
{
    [CreateAssetMenu(menuName = "Shop/Troop")]
    public class TroopShopData : ShopItemData
    {
        public UnitData unitData;
    }
}