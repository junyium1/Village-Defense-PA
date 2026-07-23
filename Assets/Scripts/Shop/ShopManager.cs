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

        // Le niveau d'upgrade est stocké (et persisté) sur le Player.
        public int GetUpgradeLevel(ShopItemData item)
            => Player.Instance != null ? Player.Instance.GetUpgradeLevel(item) : item.currentUpgradeLevel;

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
            Player.Instance.SetUpgradeLevel(item, currentLevel + 1);
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
            if (level <= 0 || item.upgrades == null || item.upgrades.Length == 0)
                return 1f;
            // Clamp défensif : un niveau persisté (PlayerPrefs) peut dépasser la taille du
            // tableau si celui-ci a été réduit après coup.
            int idx = Mathf.Min(level, item.upgrades.Length) - 1;
            return selector(item.upgrades[idx]);
        }
    }
}