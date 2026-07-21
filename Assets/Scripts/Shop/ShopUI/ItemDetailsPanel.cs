using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Shop
{
    public class ItemDetailsPanel : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text goldCostText;

        private ShopItemData _currentItem;

        public void Toggle(ShopItemData item)
        {
            // même item déjà affiché → on ferme
            if (gameObject.activeSelf && _currentItem == item)
            {
                Hide();
                return;
            }
            Show(item);
        }

        public void Show(ShopItemData item)
        {
            _currentItem = item;
            gameObject.SetActive(true);
            iconImage.sprite = item.icon;
            nameText.text = item.displayName;
            descriptionText.text = item.description;
            goldCostText.text = item.goldCost.ToString();
        }

        public void Hide()
        {
            _currentItem = null;
            gameObject.SetActive(false);
        }
    }
}