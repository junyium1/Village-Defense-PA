using UnityEngine;

namespace DiscordBridge.UI
{
    // Desactive Menu3DInput quand le panneau parent est actif (open),
    // le reactive a la fermeture. Empeche les clics sur les planches 3D
    // pendant qu'un panneau 2D (inventaire, liaison discord) est ouvert.
    public class PanelMenuBlocker : MonoBehaviour
    {
        Menus.Menu3DInput _menuInput;

        void OnEnable()
        {
            if (_menuInput == null)
                _menuInput = FindObjectOfType<Menus.Menu3DInput>();

            if (_menuInput != null)
                _menuInput.enabled = false;
        }

        void OnDisable()
        {
            if (_menuInput != null)
                _menuInput.enabled = true;
        }
    }
}
