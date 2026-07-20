using UnityEngine;

namespace Menus
{
    /// <summary>
    /// Planche d'action du menu pause 3D (GameScene) — équivalent de
    /// <see cref="SignPlankAction"/> pour le menu principal, mais route vers
    /// <see cref="PauseMenu3DController"/> : Reprendre, Options, Menu principal,
    /// Retour (options → pause).
    /// </summary>
    public class PausePlankAction : SignPlankBase
    {
        public enum Action { Resume, ShowOptions, ShowPauseMain, QuitToMainMenu }

        [SerializeField] Action action = Action.Resume;

        public override void OnClicked()
        {
            var menu = PauseMenu3DController.Instance;
            if (menu == null) return;

            switch (action)
            {
                case Action.Resume: menu.ResumeGame(); break;
                case Action.ShowOptions: menu.ShowOptions(); break;
                case Action.ShowPauseMain: menu.ShowPauseMain(); break;
                case Action.QuitToMainMenu: menu.QuitToMainMenu(); break;
            }
        }
    }
}
