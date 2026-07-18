using System;
using UnityEngine;

namespace DiscordBridge.Data
{
    // Instancié au runtime (pas un asset de contenu) : reflète l'état serveur du profil
    // Discord lié. Repeuplé par ProfileSyncController après chaque synchronisation.
    [CreateAssetMenu(fileName = "PlayerProfileData", menuName = "Discord Bridge/Runtime/Player Profile Data")]
    public class PlayerProfileData : ScriptableObject
    {
        public int Mana { get; private set; }
        public string NotifPref { get; private set; }
        public bool IsLoaded { get; private set; }

        public event Action OnProfileUpdated;

        void OnEnable() => ResetRuntimeState();

        // Un ScriptableObject asset garde ses valeurs en mémoire entre deux lancements du jeu
        // dans l'Éditeur si le rechargement de domaine est désactivé (Enter Play Mode Options) :
        // OnEnable seul ne refire pas forcément dans ce cas, donc on expose aussi cette méthode
        // pour que l'appelant (ProfileSyncController) puisse forcer un état propre au démarrage.
        public void ResetRuntimeState()
        {
            Mana = 0;
            NotifPref = null;
            IsLoaded = false;
        }

        public void Populate(int mana, string notifPref)
        {
            Mana = mana;
            NotifPref = notifPref;
            IsLoaded = true;
            OnProfileUpdated?.Invoke();
        }
    }
}
