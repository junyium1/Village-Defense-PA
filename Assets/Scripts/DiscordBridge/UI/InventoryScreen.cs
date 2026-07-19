using System.Collections.Generic;
using DiscordBridge.Controllers;
using DiscordBridge.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiscordBridge.UI
{
    // Vue pure : ecoute InventoryData (SO) pour reconstruire la liste, declenche une
    // re-synchro via ProfileSyncController et la consommation via ConsumeItemController.
    // Ne connait jamais le reseau ni les DTOs.
    public class InventoryScreen : MonoBehaviour
    {
        [SerializeField] InventoryData inventoryData;
        [SerializeField] ProfileSyncController profileSync;
        [SerializeField] ConsumeItemController consumeController;

        [Header("Liste")]
        [SerializeField] Transform rowContainer;
        [SerializeField] InventoryRow rowTemplate;

        [Header("Commun")]
        [SerializeField] TextMeshProUGUI statusText;
        [SerializeField] Button refreshButton;
        [SerializeField] Button closeButton;

        [Header("Animation (optionnel)")]
        [SerializeField] UIPanelAnimator panelAnimator;

        readonly List<InventoryRow> _spawnedRows = new();

        void Awake()
        {
            // Le template sert de moule : jamais affiche tel quel.
            if (rowTemplate != null) rowTemplate.gameObject.SetActive(false);
            if (refreshButton != null) refreshButton.onClick.AddListener(OnRefreshClicked);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }

        void OnDestroy()
        {
            if (refreshButton != null) refreshButton.onClick.RemoveListener(OnRefreshClicked);
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
        }

        void OnEnable()
        {
            if (inventoryData != null) inventoryData.OnInventoryChanged += Rebuild;
            if (consumeController != null)
            {
                consumeController.OnConsumeSucceeded += HandleConsumeSucceeded;
                consumeController.OnConsumeFailed += HandleConsumeFailed;
            }
            Rebuild();
        }

        void OnDisable()
        {
            if (inventoryData != null) inventoryData.OnInventoryChanged -= Rebuild;
            if (consumeController != null)
            {
                consumeController.OnConsumeSucceeded -= HandleConsumeSucceeded;
                consumeController.OnConsumeFailed -= HandleConsumeFailed;
            }
        }

        // Ouvre l'ecran depuis un bouton du menu et recupere un etat frais.
        public void Open()
        {
            gameObject.SetActive(true);
            if (panelAnimator != null) panelAnimator.PlayOpen();
            OnRefreshClicked();
        }

        void Close()
        {
            if (panelAnimator != null)
                panelAnimator.PlayClose(() => gameObject.SetActive(false));
            else
                gameObject.SetActive(false);
        }

        void OnRefreshClicked()
        {
            if (profileSync == null)
            {
                SetStatus("Synchronisation indisponible.");
                return;
            }

            SetStatus("Synchronisation...");
            _ = profileSync.SyncAsync();
        }

        void OnUseRequested(InventoryEntry entry)
        {
            if (consumeController == null)
            {
                SetStatus("Utilisation indisponible.");
                return;
            }

            SetStatus($"Utilisation de {entry.Definition.DisplayName}...");
            SetRowsInteractable(false);
            _ = consumeController.ConsumeAsync(entry.Definition.Id);
        }

        void HandleConsumeSucceeded(string itemId)
        {
            // L'inventaire a deja ete re-synchronise par le controleur -> Rebuild via event.
            SetStatus("Objet utilisé !");
            SetRowsInteractable(true);
        }

        void HandleConsumeFailed(string itemId, string errorMessage)
        {
            SetStatus($"Erreur : {errorMessage}");
            SetRowsInteractable(true);
        }

        // Reconstruit la liste depuis l'etat courant de l'inventaire (appele a l'ouverture et
        // a chaque OnInventoryChanged emis par le SO apres une synchro reseau).
        void Rebuild()
        {
            foreach (InventoryRow row in _spawnedRows)
                if (row != null) Destroy(row.gameObject);
            _spawnedRows.Clear();

            if (inventoryData == null || rowTemplate == null || rowContainer == null) return;

            IReadOnlyList<InventoryEntry> entries = inventoryData.Entries;
            foreach (InventoryEntry entry in entries)
            {
                InventoryRow row = Instantiate(rowTemplate, rowContainer);
                row.gameObject.SetActive(true);
                row.Bind(entry, consumeController != null ? OnUseRequested : null);
                _spawnedRows.Add(row);
            }

            if (!inventoryData.IsLoaded)
                SetStatus("Non synchronisé.");
            else if (entries.Count == 0)
                SetStatus("Inventaire vide.");
            else
                SetStatus(string.Empty);
        }

        void SetRowsInteractable(bool interactable)
        {
            foreach (InventoryRow row in _spawnedRows)
                if (row != null) row.SetInteractable(interactable);
        }

        void SetStatus(string message)
        {
            if (statusText != null) statusText.text = message;
        }
    }
}
