using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Shop
{
    public class UpgradeDetailsPanel : MonoBehaviour
    {
        [SerializeField] private ShopItemData data;

        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text upgradeDescriptionText;
        [SerializeField] private TMP_Text currentUpgradeLevelText;
        [SerializeField] private TMP_Text crystalCostText;

        private void OnEnable()
        {
            if (data != null)
                Display(data);
        }

        public void Display(ShopItemData item)
        {
            int level = item.currentUpgradeLevel;

            iconImage.sprite = item.icon;
            nameText.text = item.displayName;
            descriptionText.text = item.description;
            upgradeDescriptionText.text = item.upgradeDescription;
            currentUpgradeLevelText.text = item.currentUpgradeLevel.ToString();
            crystalCostText.text = item.crystalCost.ToString();
        }
    }
}