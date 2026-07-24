using System;
using UnityEngine;
using DiscordBridge.Session;

namespace Game
{
    // Definition d'un succes : identifiant stable (cle PlayerPrefs), texte affiche,
    // condition evaluee a la demande. Classe simple (pas ScriptableObject) : les succes
    // sont declares en dur, pas des assets editables.
    public sealed class AchievementDef
    {
        public readonly string Id;
        public readonly string Title;
        public readonly string Description;
        public readonly Func<bool> Condition;

        public AchievementDef(string id, string title, string description, Func<bool> condition)
        {
            Id = id;
            Title = title;
            Description = description;
            Condition = condition;
        }
    }

    // Store des succes : evaluer les conditions, persiste l'etat debloque en PlayerPrefs.
    // MONOTONE : un succes debloque ne peut plus etre re-verrouille (sauf WipeSave).
    public static class AchievementStore
    {
        const string KeyPrefix = "Ach.";

        public static readonly AchievementDef[] Defs = CreateDefs();

        static AchievementDef[] CreateDefs()
        {
            return new[]
            {
                // Lie ton compte Discord depuis le menu principal.
                new AchievementDef(
                    "discord_link",
                    "Connecté",
                    "Lie ton compte Discord depuis le menu principal.",
                    () => SessionStore.IsLinked
                ),

                // Termine le premier niveau (plus petit levelID du catalogue).
                new AchievementDef(
                    "level_1",
                    "Premier pas",
                    "Termine le premier niveau.",
                    () =>
                    {
                        if (Player.Instance == null) return false;
                        LevelData[] levels = LevelCatalog.GetAll();
                        if (levels.Length == 0) return false;
                        return Player.Instance.IsLevelCompleted(levels[0].levelID);
                    }
                ),

                // Termine le premier niveau flagge isBoss (tri par levelID).
                new AchievementDef(
                    "first_boss",
                    "Tombeur de boss",
                    "Termine le premier niveau boss.",
                    () =>
                    {
                        if (Player.Instance == null) return false;
                        LevelData[] levels = LevelCatalog.GetAll();
                        for (int i = 0; i < levels.Length; i++)
                        {
                            if (levels[i].isBoss)
                                return Player.Instance.IsLevelCompleted(levels[i].levelID);
                        }
                        // Aucun niveau boss dans la liste : condition impossible.
                        return false;
                    }
                ),

                // Tous les niveaux completes.
                new AchievementDef(
                    "all_levels",
                    "Sauveur du village",
                    "Termine tous les niveaux.",
                    () =>
                    {
                        if (Player.Instance == null) return false;
                        LevelData[] levels = LevelCatalog.GetAll();
                        if (levels.Length == 0) return false;
                        for (int i = 0; i < levels.Length; i++)
                        {
                            if (!Player.Instance.IsLevelCompleted(levels[i].levelID))
                                return false;
                        }
                        return true;
                    }
                )
            };
        }

        // Lit le flag persiste (0 = verrouille par defaut, 1 = debloque).
        public static bool IsUnlocked(string id)
        {
            return PlayerPrefs.GetInt(KeyPrefix + id, 0) == 1;
        }

        // Evalue toutes les conditions : deblocage monotone (jamais de re-verrouillage).
        // Les exceptions eventuelles des conditions sont avalees pour ne pas casser l'UI.
        public static void EvaluateAll()
        {
            bool changed = false;
            for (int i = 0; i < Defs.Length; i++)
            {
                AchievementDef def = Defs[i];
                if (IsUnlocked(def.Id)) continue; // deja debloque : on ne touche a rien

                bool ok;
                try { ok = def.Condition != null && def.Condition(); }
                catch { ok = false; }

                if (ok)
                {
                    PlayerPrefs.SetInt(KeyPrefix + def.Id, 1);
                    changed = true;
                }
            }
            if (changed) PlayerPrefs.Save();
        }

        // Supprime tous les flags (appele par WipeSave pour coherence avec la progression).
        public static void ResetAll()
        {
            for (int i = 0; i < Defs.Length; i++)
                PlayerPrefs.DeleteKey(KeyPrefix + Defs[i].Id);
            PlayerPrefs.Save();
        }
    }
}
