using UnityEngine;

namespace Shop
{
    //TODO add specific buildings
    [CreateAssetMenu(menuName = "Shop/Building")]
    public class BuildingShopData : ShopItemData
    {
        [Header("Global Boosts")]
        public float damageBoostPercent;
        public float fireRateBoostPercent;
        public float rangeBoostPercent;
    }
}