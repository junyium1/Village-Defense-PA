using System.Collections.Generic;
using DiscordBridge.Controllers;
using DiscordBridge.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiscordBridge.UI
{
    // Vue pure : ecoute InventoryData (SO) pour reconstruire la liste, et declenche une
    // re-synchro via ProfileSyncController. Ne connait jamais le reseau ni les DTOs.
    public class InventoryScreen : MonoBehaviour
    {
        [SerializeField] InventoryData inventoryData;
        [SerializeField] ProfileSyncController profileSync;

        [Header("Liste")]
        [SerializeField] Transform rowContainer;
        [SerializeField] InventoryRow rowTemplate;

        [Header("Commun")]
        [SerializeField] TextMeshProUGUI statusText;
        [SerializeField] Button refreshButton;
        [SerializeField] Button closeButton;

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
            Rebuild();
        }

        void OnDisable()
        {
            if (inventoryData != null) inventoryData.OnInventoryChanged -= Rebuild;
        }

        // Ouvre l'ecran depuis un bouton du menu et recupere un etat frais.
        public void Open()
        {
            gameObject.SetActive(true);
            OnRefreshClicked();
        }

        void Close() => gameObject.SetActive(false);

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
                row.Bind(entry);
                _spawnedRows.Add(row);
            }

            if (!inventoryData.IsLoaded)
                SetStatus("Non synchronise.");
            else if (entries.Count == 0)
                SetStatus("Inventaire vide.");
            else
                SetStatus(string.Empty);
        }

        void SetStatus(string message)
        {
            if (statusText != null) statusText.text = message;
        }
    }
}
