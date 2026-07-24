using Game;
using Menus;
using UnityEngine;
using UnityEngine.InputSystem;

// Mini console de triche : touche ² (au-dessus de Tab) pour ouvrir/fermer,
// taper une commande puis Entrée. Deux commandes seulement :
//   money  -> +10000 or et +10000 cristaux
//   unlock -> débloque tous les niveaux
// S'auto-instancie au lancement du jeu : aucun objet à poser dans les scènes.
public class CheatConsole : MonoBehaviour
{
    const int GoldCheat = 10000;
    const int CrystalsCheat = 10000;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        var go = new GameObject("CheatConsole");
        DontDestroyOnLoad(go);
        go.AddComponent<CheatConsole>();
    }

    bool _visible;
    string _input = "";
    string _feedback = "";
    bool _focusPending;

    void Update()
    {
        // backquoteKey = position physique de la touche ² sur un clavier AZERTY
        Keyboard kb = Keyboard.current;
        if (kb != null && kb.backquoteKey.wasPressedThisFrame)
        {
            _visible = !_visible;
            _input = "";
            _focusPending = _visible;
        }
    }

    void OnGUI()
    {
        if (!_visible) return;

        Event e = Event.current;
        if (e.type == EventType.KeyDown)
        {
            if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
            {
                Execute(_input.Trim().ToLowerInvariant());
                _input = "";
                e.Use();
            }
            else if (e.keyCode == KeyCode.Escape)
            {
                _visible = false;
                e.Use();
                return;
            }
        }

        GUI.Box(new Rect(5, 5, 350, 68), "Triche (² pour fermer)");

        GUI.SetNextControlName("CheatInput");
        _input = GUI.TextField(new Rect(12, 26, 336, 22), _input);
        // la touche ² insère son caractère dans le champ au moment du toggle : on le retire
        _input = _input.Replace("²", "").Replace("`", "");

        if (_focusPending)
        {
            GUI.FocusControl("CheatInput");
            _focusPending = false;
        }

        if (!string.IsNullOrEmpty(_feedback))
            GUI.Label(new Rect(12, 50, 336, 20), _feedback);
    }

    void Execute(string cmd)
    {
        if (cmd.Length == 0) return;

        if (Player.Instance == null)
        {
            _feedback = "Pas de Player dans la scène.";
            return;
        }

        switch (cmd)
        {
            case "money":
                Player.Instance.EarnGold(GoldCheat);
                Player.Instance.EarnCrystals(CrystalsCheat);
                _feedback = $"+{GoldCheat} or, +{CrystalsCheat} cristaux";
                break;

            case "unlock":
                Player.Instance.UnlockAllLevels();
                if (LevelSelectManager.Instance != null)
                    LevelSelectManager.Instance.RefreshButtons();
                _feedback = "Tous les niveaux débloqués + terminés (succès inclus)";
                break;

            default:
                _feedback = "Commandes : money | unlock";
                break;
        }
    }
}
