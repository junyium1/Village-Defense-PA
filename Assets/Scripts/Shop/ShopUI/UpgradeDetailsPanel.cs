using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game;

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
        [SerializeField] private Button upgradeButton; // bouton "Améliorer"

        private void Awake()
        {
            // Si le bouton n'est pas assigné dans l'inspecteur, on tente de le
            // récupérer parmi les enfants du panneau (pratique s'il y en a beaucoup).
            if (upgradeButton == null)
                upgradeButton = GetComponentInChildren<Button>(true);

            if (upgradeButton != null)
                upgradeButton.onClick.AddListener(Upgrade);
        }

        private void OnEnable()
        {
            if (data != null)
                Display(data);
        }

        // Permet d'afficher un autre item à la volée (optionnel).
        public void Show(ShopItemData item)
        {
            data = item;
            gameObject.SetActive(true);
            Display(item);
        }

        public void Display(ShopItemData item)
        {
            data = item;
            int level = ShopManager.Instance != null
                ? ShopManager.Instance.GetUpgradeLevel(item)
                : item.currentUpgradeLevel;
            bool maxed = level >= item.upgrades.Length;

            iconImage.sprite = item.icon;
            nameText.text = item.displayName;
            descriptionText.text = item.description;
            // On affiche le niveau à partir de 1 (niveau interne 0 = "level 1").
            currentUpgradeLevelText.text = (level + 1).ToString();

            // Effet actuel : au niveau de base = upgradeDescription ; sinon = description du niveau atteint.
            string currentEffect = level > 0
                ? item.upgrades[level - 1].description
                : item.upgradeDescription;

            if (maxed)
            {
                upgradeDescriptionText.text = currentEffect;
                crystalCostText.text = "MAX";
                if (upgradeButton != null)
                    upgradeButton.interactable = false;
            }
            else
            {
                UpgradeLevel next = item.upgrades[level];
                // "effet actuel > effet suivant"
                upgradeDescriptionText.text = $"{currentEffect} > {next.description}";
                // Coût du PROCHAIN niveau, pas le crystalCost de base de l'objet.
                crystalCostText.text = next.crystalCost.ToString();
                if (upgradeButton != null)
                    upgradeButton.interactable =
                        Player.Instance != null && Player.Instance.CanAffordUpgrade(next.crystalCost);
            }
        }

        // Branché sur le clic du bouton "Améliorer".
        public void Upgrade()
        {
            if (data == null || ShopManager.Instance == null)
                return;

            if (ShopManager.Instance.TryUpgrade(data))
                Display(data); // rafraîchit niveau, coût et état du bouton
        }
    }
}
