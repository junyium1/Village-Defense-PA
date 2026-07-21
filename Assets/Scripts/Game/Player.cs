using UnityEngine;
using System.Collections.Generic;

namespace Game
{
    public class Player : MonoBehaviour
    {
        public static Player Instance { get; private set; }

        [Header("Currencies")] public int gold = 1000; // buy haha
        public int crystals = 500; // upgrade

        private int HighestUnlockedLevel { get; set; } = 0;

        readonly HashSet<int> _completedLevels = new();

        const string SaveKeyHighestUnlocked = "HighestUnlockedLevel";
        const string SaveKeyCompletedLevels = "CompletedLevels";

        public bool IsLevelUnlocked(int levelID) => levelID <= HighestUnlockedLevel;
        public bool IsLevelCompleted(int levelID) => _completedLevels.Contains(levelID);

        // TODO fix
        // owned items and their current upgrade level
        // public Dictionary<ShopItemData, int> ownedItems = new();

        // -------------- singleton --------------
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadProgress();
        }

        // -------------- level progression --------------
        public void MarkLevelCompleted(int levelID)
        {
            _completedLevels.Add(levelID);
            if (levelID >= HighestUnlockedLevel)
                HighestUnlockedLevel = Mathf.Min(levelID + 1, 7); // cap at last level id (demon king boss)
            SaveProgress();
        }

        void SaveProgress()
        {
            PlayerPrefs.SetInt(SaveKeyHighestUnlocked, HighestUnlockedLevel);
            PlayerPrefs.SetString(SaveKeyCompletedLevels,
                string.Join(",", _completedLevels));
            PlayerPrefs.Save();
        }

        void LoadProgress()
        {
            HighestUnlockedLevel = PlayerPrefs.GetInt(SaveKeyHighestUnlocked, 0);

            string raw = PlayerPrefs.GetString(SaveKeyCompletedLevels, "");
            _completedLevels.Clear();
            if (!string.IsNullOrEmpty(raw))
            {
                foreach (string part in raw.Split(','))
                    if (int.TryParse(part, out int id))
                        _completedLevels.Add(id);
            }
        }

        // Triche (console ²) : débloque tout jusqu'au boss final (même cap que MarkLevelCompleted).
        public void UnlockAllLevels()
        {
            HighestUnlockedLevel = 7;
            SaveProgress();
        }

        public void ResetProgress()
        {
            HighestUnlockedLevel = 0;
            _completedLevels.Clear();
            PlayerPrefs.DeleteKey(SaveKeyHighestUnlocked);
            PlayerPrefs.DeleteKey(SaveKeyCompletedLevels);
            PlayerPrefs.Save();
        }

        // -------------- capitalism --------------
        public bool CanAffordUpgrade(int crystalCost) => crystals >= crystalCost;
        public bool CanAffordItem(int goldCost) => gold >= goldCost;
        public void SpendGold(int amount) => gold -= amount;
        public void SpendCrystals(int amount) => crystals -= amount;
        public void EarnGold(int amount) => gold += amount;
        public void EarnCrystals(int amount) => crystals += amount;
    }
}