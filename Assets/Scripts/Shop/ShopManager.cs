using System.Collections.Generic;
using Game;
using Game.Defenses;
using UnityEngine;

namespace Shop
{
    public class ShopManager : MonoBehaviour
    {
        public GameObject shopMenuPanel;
        public GameObject shopUnitsAndDefenses;
        public GameObject shopUnits;
        public GameObject shopDefenses;
        public GameObject itemDetails;
        private bool _isActive;
        public GameManager gameManager;
        public static ShopManager Instance { get; private set; }
        readonly Dictionary<ShopItemData, int> _upgradeLevels = new();

        // -------------- singleton --------------
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        // -------------- panel show / hide --------------
        private void Start()
        {
            shopMenuPanel.SetActive(true);
            shopUnitsAndDefenses.SetActive(true);
            shopUnits.SetActive(false);
            shopDefenses.SetActive(false);
            itemDetails.SetActive(false);
            
        }
        

        // -------------- capitalism --------------
        // L'or n'est PAS débité ici : le paiement a lieu au moment où l'objet est
        // effectivement posé sur la grille (PlacementManager.HandlePlacementComplete).
        // Annuler le placement (Échap) ne coûte donc rien.
        public bool TryStartPlacingItem(ShopItemData item)
        {
            if (gameManager != null && gameManager.currentPhase != GamePhase.Placement)
            {
                Debug.Log($"{item.displayName} : placement uniquement en phase de placement.");
                return false;
            }

            if (item.prefab == null)
            {
                Debug.LogError($"{item.displayName} : aucun prefab assigné sur l'asset boutique.");
                return false;
            }

            if (!Player.Instance.CanAffordItem(item.goldCost))
            {
                Debug.Log($"Cannot afford {item.displayName}");
                return false;
            }

            gameManager.placementManager.RequestPlacement(item);
            return true;
        }

        public int GetUpgradeLevel(ShopItemData item)
            => _upgradeLevels.TryGetValue(item, out int level) ? level : 0;

        public bool TryUpgrade(ShopItemData item)
        {
            int currentLevel = GetUpgradeLevel(item);
            if (currentLevel >= item.upgrades.Length)
            {
                print($"{item.displayName} is already MAXXXED OUT!");
                return false;
            }

            UpgradeLevel next = item.upgrades[currentLevel];
            if (!Player.Instance.CanAffordUpgrade(next.crystalCost))
            {
                print($"Too poor to afford: {item.displayName}...");
                return false;
            }

            Player.Instance.SpendCrystals(next.crystalCost);
            _upgradeLevels[item] = currentLevel + 1;
            print($"{item.displayName} upgraded to level {currentLevel + 1}!");
            return true;
        }

        // -------------- stat getters --------------
        public float GetDamageMultiplier(ShopItemData item)
            => GetMultiplier(item, u => u.damageMultiplier);

        public float GetHpMultiplier(ShopItemData item)
            => GetMultiplier(item, u => u.hpMultiplier);

        public float GetSpeedMultiplier(ShopItemData item)
            => GetMultiplier(item, u => u.speedMultiplier);

        float GetMultiplier(ShopItemData item, System.Func<UpgradeLevel, float> selector)
        {
            int level = GetUpgradeLevel(item);
            return level > 0 ? selector(item.upgrades[level - 1]) : 1f;
        }
    }
}