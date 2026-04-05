using UnityEngine;

namespace Shop
{
    [System.Serializable]
    public class UpgradeLevel
    {
        public string description;
        public int crystalCost;
        public float damageMultiplier = 1f;
        public float hpMultiplier = 1f;

        public float speedMultiplier = 1f;

        // TODO make unitData for racoon + mimic chest
        public int capacity; // specific to portal
        public int maxNbTauntedEnemies; // specific to mimic chest & racoon
    }

    public abstract class ShopItemData : ScriptableObject
    {
        public string displayName;
        public Sprite icon;

        [TextArea] // better display
        public string description;

        public int goldCost;
        public int crystalCost;
        public GameObject prefab;

        // 1 = bronze border / 2 = silver border / 3 - gold border
        public UpgradeLevel[] upgrades = new UpgradeLevel[3];
    }
}