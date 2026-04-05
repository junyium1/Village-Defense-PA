using System.Collections.Generic;
using Game;
using UnityEngine;

namespace Shop
{
    public class ShopManager : MonoBehaviour
    {
        public GameObject shopMenuPanel;
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
            shopMenuPanel.SetActive(false);
        }

        private void Update()
        {
            if (gameManager.currentPhase != GamePhase.Placement)
            {
                CloseShopMenu();
                return;
            }

            if (Input.GetKeyDown(KeyCode.P))
            {
                if (_isActive)
                    CloseShopMenu();
                else
                    OpenShopMenu();
            }
        }

        private void OpenShopMenu()
        {
            _isActive = true;
            shopMenuPanel.SetActive(true);
        }

        public void CloseShopMenu()
        {
            _isActive = false;
            shopMenuPanel.SetActive(false);
        }

        // -------------- capitalism --------------
        public bool TryBuy(ShopItemData item)
        {
            if (!Player.Instance.CanAffordItem(item.goldCost))
            {
                Debug.Log($"Cannot afford {item.displayName}");
                return false;
            }

            Player.Instance.SpendGold(item.goldCost);
            Debug.Log($"Bought {item.displayName}");
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