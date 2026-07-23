using UnityEngine;

namespace Menus
{
    /// <summary>
    /// Planche du menu 3D déclenchant une action de navigation
    /// (afficher un panneau, revenir au titre, wipe, quitter).
    /// </summary>
    public class SignPlankAction : SignPlankBase
    {
        public enum Action { ShowMain, ShowLevelSelect, ShowOptions, Wipe, Quit, NextPage, PrevPage, ShowKeybinds }

        [SerializeField] Action action = Action.ShowMain;

        public override void OnClicked()
        {
            // L'écran Touches est une superposition 2D : il ne passe pas par la
            // navigation entre pancartes (il s'affiche par-dessus celle des options).
            if (action == Action.ShowKeybinds)
            {
                KeybindsScreen.Open();
                return;
            }

            var menu = Menu3DController.Instance;
            if (menu == null) return;

            switch (action)
            {
                case Action.ShowMain: menu.ShowMain(); break;
                case Action.ShowLevelSelect: menu.ShowLevelSelect(); break;
                case Action.ShowOptions: menu.ShowOptions(); break;
                case Action.Wipe: menu.WipeSave(); break;
                case Action.Quit: menu.QuitGame(); break;
                case Action.NextPage: menu.NextLevelPage(); break;
                case Action.PrevPage: menu.PrevLevelPage(); break;
            }
        }
    }
}
