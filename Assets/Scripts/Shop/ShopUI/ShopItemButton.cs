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
            // Un clic = infos affichées ET mode placement lancé dans la foulée ;
            // l'or n'est débité qu'au moment où l'objet est réellement posé.
            GetComponent<Button>().onClick.AddListener(() =>
            {
                detailsPanel.Toggle(itemData);
                ShopManager.Instance.TryStartPlacingItem(itemData);
            });
        }
    }
}