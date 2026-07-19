using UnityEngine;

namespace DiscordBridge.Session
{
    // Réserve locale des effets "one-shot" (durée serveur = 0, ex: reinforce) déjà
    // consommés côté serveur mais pas encore déclenchés en partie. Persistée en
    // PlayerPrefs : survit au redémarrage, comme l'inventaire l'exemplaire est déjà
    // décompté serveur — perdre ce compteur ferait perdre l'achat au joueur.
    public static class PendingItemEffectsStore
    {
        const string KeyPrefix = "discord_pending_effect_";

        public static int GetCount(string itemId) =>
            PlayerPrefs.GetInt(KeyPrefix + itemId, 0);

        public static void Add(string itemId, int count = 1)
        {
            if (string.IsNullOrEmpty(itemId) || count <= 0) return;
            PlayerPrefs.SetInt(KeyPrefix + itemId, GetCount(itemId) + count);
            PlayerPrefs.Save();
        }

        // Décrémente et retourne true si un exemplaire était disponible.
        public static bool TryConsume(string itemId)
        {
            int count = GetCount(itemId);
            if (count <= 0) return false;
            PlayerPrefs.SetInt(KeyPrefix + itemId, count - 1);
            PlayerPrefs.Save();
            return true;
        }
    }
}
