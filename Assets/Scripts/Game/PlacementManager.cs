using Shop;
using UnityEngine;

namespace Game
{
    public class PlacementManager : MonoBehaviour
    {
        [SerializeField] private PlacementSystem placementSystem;

        private ShopItemData pendingItem;

        public bool IsPlacing => placementSystem != null && placementSystem.IsPlacing;

        public void RequestPlacement(ShopItemData item)
        {
            if (placementSystem == null)
            {
                Debug.LogError("PlacementSystem non assigné dans PlacementManager");
                return;
            }

            if (item.prefab == null)
            {
                Debug.LogError($"Pas de prefab pour {item.displayName}");
                return;
            }

            // Rien n'a encore été payé : changer d'objet en cours de placement
            // remplace simplement la sélection précédente.
            Cleanup();
            pendingItem = item;

            placementSystem.OnPlacementComplete += HandlePlacementComplete;
            placementSystem.OnPlacementCancelled += HandlePlacementCancelled;

            placementSystem.StartPlacement(item.prefab, item.placementSize);
        }

        // GameManager désactive ce composant en quittant la phase Placement : un placement
        // encore en cours est simplement abandonné (rien n'a été débité).
        private void OnDisable()
        {
            if (pendingItem == null) return;
            if (placementSystem != null)
                placementSystem.StopPlacement();
            Cleanup();
            pendingItem = null;
        }

        // L'objet vient d'être posé : c'est ICI que l'achat est encaissé.
        // Le mode placement reste actif pour enchaîner les poses ; il ne s'arrête
        // que si l'or ne suffit plus pour la suivante (ou via Échap / changement d'item).
        private void HandlePlacementComplete(GameObject placed)
        {
            // Applique les multiplicateurs d'upgrade (dégâts / PV / vitesse) sur l'instance
            // fraîchement posée, selon le niveau d'upgrade stocké côté Player.
            UpgradeStatApplier.Apply(placed, pendingItem);

            Player.Instance.SpendGold(pendingItem.goldCost);
            Debug.Log($"{pendingItem.displayName} placé : -{pendingItem.goldCost} or");

            if (!Player.Instance.CanAffordItem(pendingItem.goldCost))
            {
                Debug.Log($"Plus assez d'or pour un autre {pendingItem.displayName}, fin du placement.");
                placementSystem.StopPlacement();
                Cleanup();
                pendingItem = null;
            }
        }

        private void HandlePlacementCancelled()
        {
            Debug.Log($"Placement de {pendingItem.displayName} annulé (rien débité)");
            Cleanup();
            pendingItem = null;
        }

        private void Cleanup()
        {
            placementSystem.OnPlacementComplete -= HandlePlacementComplete;
            placementSystem.OnPlacementCancelled -= HandlePlacementCancelled;
        }
    }
}