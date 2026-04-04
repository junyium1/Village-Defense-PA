using Game;
using UnityEngine;

namespace Shop
{
    public class ShopManager : MonoBehaviour
    {
        public GameObject shopMenuPanel;
        private bool _isActive;
        public GameManager gameManager;
        
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

    }
}