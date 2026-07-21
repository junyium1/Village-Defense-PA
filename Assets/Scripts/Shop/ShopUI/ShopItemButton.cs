using UnityEngine;
using UnityEngine.UI;

namespace Shop
{
    [RequireComponent(typeof(Button))]
    public class ShopItemButton : MonoBehaviour
    {
        [SerializeField] private ShopItemData itemData;
        [SerializeField] private ItemDetailsPanel detailsPanel;

        private void Start()
        {
            GetComponent<Button>().onClick.AddListener(() => detailsPanel.Toggle(itemData));
        }
    }
}