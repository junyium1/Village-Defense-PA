using UnityEngine;
public class Player : MonoBehaviour
{
    public static Player Instance { get; private set; }

    [Header("Currencies")] public int gold = 67; // buy haha
    public int crystals = 0; // upgrade

    // TODO fix
    // owned items and their current upgrade level
    // public Dictionary<ShopItemData, int> ownedItems = new();

    //TODO implement progression here (save which levels have been beat)

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
    }

    // -------------- capitalism --------------
    public bool CanAffordUpgrade(int crystalCost) => crystals >= crystalCost;
    public bool CanAffordItem(int goldCost) => gold >= goldCost;
    public void SpendGold(int amount) => gold -= amount;
    public void SpendCrystals(int amount) => crystals -= amount;
    public void EarnGold(int amount) => gold += amount;
    public void EarnCrystals(int amount) => crystals += amount;
}