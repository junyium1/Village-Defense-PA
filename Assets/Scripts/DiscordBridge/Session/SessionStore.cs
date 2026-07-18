using System;
using UnityEngine;

namespace DiscordBridge.Session
{
    // Seul point d'accès au discord_id lié, persisté en PlayerPrefs (même mécanisme que
    // Player.cs pour la progression des niveaux). Toute autre classe du bridge doit passer
    // par ici plutôt que d'appeler PlayerPrefs directement.
    public static class SessionStore
    {
        const string DiscordIdKey = "DiscordBridge.DiscordId";

        public static bool IsLinked => !string.IsNullOrEmpty(DiscordId);

        public static string DiscordId
        {
            get => PlayerPrefs.GetString(DiscordIdKey, string.Empty);
            private set
            {
                PlayerPrefs.SetString(DiscordIdKey, value);
                PlayerPrefs.Save();
            }
        }

        // discordId doit être une string (pas un long/int) : un snowflake Discord dépasse la
        // précision sûre d'un nombre flottant et PlayerPrefs n'a pas de type entier 64 bits.
        public static void SetLinkedAccount(string discordId)
        {
            if (string.IsNullOrEmpty(discordId))
                throw new ArgumentException("discordId ne peut pas être vide.", nameof(discordId));

            DiscordId = discordId;
        }

        public static void ClearLinkedAccount()
        {
            PlayerPrefs.DeleteKey(DiscordIdKey);
            PlayerPrefs.Save();
        }
    }
}
