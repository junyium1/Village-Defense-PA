using System;
using System.Collections.Generic;
using DiscordBridge.DTOs;
using UnityEngine;

namespace DiscordBridge.Data
{
    public class InventoryEntry
    {
        public ItemDefinition Definition { get; }
        public int Count { get; }

        public InventoryEntry(ItemDefinition definition, int count)
        {
            Definition = definition;
            Count = count;
        }
    }

    // Instancié au runtime : vue locale de l'inventaire ACTIF (objets non consommés) du
    // joueur lié, groupée par item_id. Repeuplée par ProfileSyncController.
    [CreateAssetMenu(fileName = "InventoryData", menuName = "Discord Bridge/Runtime/Inventory Data")]
    public class InventoryData : ScriptableObject
    {
        readonly List<InventoryEntry> _entries = new();
        public IReadOnlyList<InventoryEntry> Entries => _entries;
        public bool IsLoaded { get; private set; }

        public event Action OnInventoryChanged;

        void OnEnable() => ResetRuntimeState();

        // cf. PlayerProfileData.ResetRuntimeState : nécessaire même avec OnEnable pour couvrir
        // le cas "Reload Domain" désactivé entre deux lancements en Éditeur.
        public void ResetRuntimeState()
        {
            _entries.Clear();
            IsLoaded = false;
        }

        // Le serveur renvoie une ligne par exemplaire actif (pas de quantité) ; on regroupe ici
        // par item_id pour exposer une liste directement exploitable par le gameplay/l'UI.
        public void Populate(IEnumerable<InventoryItemDTO> serverItems, ItemDatabase database)
        {
            _entries.Clear();

            var counts = new Dictionary<string, int>();
            foreach (InventoryItemDTO serverItem in serverItems)
            {
                counts.TryGetValue(serverItem.ItemId, out int current);
                counts[serverItem.ItemId] = current + 1;
            }

            foreach (var (itemId, count) in counts)
            {
                ItemDefinition definition = database.GetById(itemId);
                if (definition != null)
                    _entries.Add(new InventoryEntry(definition, count));
            }

            IsLoaded = true;
            OnInventoryChanged?.Invoke();
        }

        public int GetCount(string itemId) =>
            _entries.Find(e => e.Definition.Id == itemId)?.Count ?? 0;

        public bool HasItem(string itemId) => GetCount(itemId) > 0;
    }
}
