using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Menus
{
    /// <summary>Déplacements caméra rebindables depuis Options → Touches.</summary>
    public enum KeybindAction
    {
        Forward = 0,    // devant
        Backward = 1,   // derrière
        Left = 2,       // gauche
        Right = 3,      // droite
        Up = 4,         // haut (élévation)
        Down = 5        // bas (élévation)
    }

    /// <summary>
    /// Touches de déplacement de la caméra (persistées PlayerPrefs), sur le même
    /// modèle que <see cref="SettingsStore"/> : statique, notifié par
    /// <see cref="Changed"/>, l'UI (<see cref="KeybindsScreen"/>) ne fait que lire/écrire.
    ///
    /// Devant/Derrière/Gauche/Droite pilotent le composite « 2D Vector » de l'action
    /// Movement de CameraInput : on pose des binding overrides sur les assets
    /// enregistrés via <see cref="Register"/>, donc l'ancienne touche cesse
    /// réellement de fonctionner après un rebind.
    /// Haut/Bas n'existent pas dans CameraInput (l'élévation a été ajoutée après) :
    /// <see cref="CameraSystem"/> les lit directement via <see cref="IsHeld"/>.
    /// </summary>
    public static class KeybindStore
    {
        public const int ActionCount = 6;

        static readonly Key[] DefaultKeys = { Key.W, Key.S, Key.A, Key.D, Key.Space, Key.LeftShift };
        static readonly string[] ActionLabels = { "Devant", "Derrière", "Gauche", "Droite", "Haut", "Bas" };

        // Parts du composite « 2D Vector » de Movement, dans l'ordre des 4 premières
        // KeybindAction (Forward/Backward/Left/Right) — l'index sert des deux côtés.
        static readonly string[] CompositeParts = { "up", "down", "left", "right" };

        const string PrefKeyPrefix = "settings.key.";
        const string MapName = "Camera";
        const string MoveActionName = "Movement";

        /// <summary>Notifié après tout rebind (l'écran Touches se rafraîchit).</summary>
        public static event Action Changed;

        static readonly Key[] _keys = new Key[ActionCount];
        static readonly List<InputActionAsset> _assets = new List<InputActionAsset>();
        static bool _loaded;

        // ----------------------- valeurs (persistées) -----------------------

        /// <summary>Libellé français de l'action (« Devant », « Haut », …).</summary>
        public static string LabelOf(KeybindAction action)
        {
            return ActionLabels[(int)action];
        }

        public static Key Get(KeybindAction action)
        {
            Load();
            return _keys[(int)action];
        }

        /// <summary>
        /// Assigne une touche. Une même touche ne sert qu'à une seule action : si elle
        /// est déjà prise, les deux actions ÉCHANGENT leur touche (aucune action ne se
        /// retrouve sans binding).
        /// </summary>
        public static void Set(KeybindAction action, Key key)
        {
            Load();
            if (key == Key.None) return;

            int index = (int)action;
            if (_keys[index] == key) return;

            for (int i = 0; i < ActionCount; i++)
            {
                if (i == index || _keys[i] != key) continue;
                _keys[i] = _keys[index];
                PlayerPrefs.SetInt(PrefKeyPrefix + i, (int)_keys[i]);
            }

            _keys[index] = key;
            PlayerPrefs.SetInt(PrefKeyPrefix + index, (int)key);
            PlayerPrefs.Save();

            ApplyToAll();
            Notify();
        }

        public static void ResetToDefaults()
        {
            Load();
            for (int i = 0; i < ActionCount; i++)
            {
                _keys[i] = DefaultKeys[i];
                PlayerPrefs.SetInt(PrefKeyPrefix + i, (int)_keys[i]);
            }
            PlayerPrefs.Save();

            ApplyToAll();
            Notify();
        }

        public static bool IsDefault(KeybindAction action)
        {
            return Get(action) == DefaultKeys[(int)action];
        }

        /// <summary>Nom lisible de la touche assignée, tel qu'affiché sur le clavier du joueur.</summary>
        public static string DisplayName(KeybindAction action)
        {
            Key key = Get(action);
            KeyControl control = FindControl(key);
            if (control != null && !string.IsNullOrEmpty(control.displayName))
                return control.displayName;
            return key.ToString();
        }

        // ----------------------- lecture runtime -----------------------

        /// <summary>Touche maintenue ? (utilisé pour Haut/Bas, absents de CameraInput).</summary>
        public static bool IsHeld(KeybindAction action)
        {
            KeyControl control = FindControl(Get(action));
            return control != null && control.isPressed;
        }

        // ----------------------- application aux InputActionAsset -----------------------

        /// <summary>
        /// Enregistre un asset d'actions (une instance par <c>new CameraInput()</c>) :
        /// les overrides y sont posés immédiatement, puis à chaque rebind.
        /// </summary>
        public static void Register(InputActionAsset asset)
        {
            if (asset == null || _assets.Contains(asset)) return;
            _assets.Add(asset);
            ApplyTo(asset);
        }

        public static void Unregister(InputActionAsset asset)
        {
            _assets.Remove(asset);
        }

        public static void ApplyTo(InputActionAsset asset)
        {
            if (asset == null) return;
            Load();

            InputActionMap map = asset.FindActionMap(MapName, false);
            if (map == null) return;
            InputAction move = map.FindAction(MoveActionName, false);
            if (move == null) return;

            for (int b = 0; b < move.bindings.Count; b++)
            {
                InputBinding binding = move.bindings[b];
                if (!binding.isPartOfComposite) continue;

                int part = IndexOfPart(binding.name);
                if (part < 0) continue;

                string path = PathOf(_keys[part]);
                if (path != null) move.ApplyBindingOverride(b, path);
            }
        }

        // ----------------------- interne -----------------------

        static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            for (int i = 0; i < ActionCount; i++)
                _keys[i] = (Key)PlayerPrefs.GetInt(PrefKeyPrefix + i, (int)DefaultKeys[i]);
        }

        static void ApplyToAll()
        {
            for (int i = _assets.Count - 1; i >= 0; i--)
            {
                if (_assets[i] == null) _assets.RemoveAt(i);
                else ApplyTo(_assets[i]);
            }
        }

        static int IndexOfPart(string name)
        {
            for (int i = 0; i < CompositeParts.Length; i++)
                if (string.Equals(name, CompositeParts[i], StringComparison.OrdinalIgnoreCase))
                    return i;
            return -1;
        }

        /// <summary>Chemin de binding (« &lt;Keyboard&gt;/w ») de la touche, null sans clavier.</summary>
        static string PathOf(Key key)
        {
            KeyControl control = FindControl(key);
            return control != null ? "<Keyboard>/" + control.name : null;
        }

        /// <summary>Contrôle clavier d'une touche ; null si aucun clavier ou touche invalide.</summary>
        static KeyControl FindControl(Key key)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || key == Key.None) return null;
            // L'indexeur lève sur les valeurs hors plage (Key.Count, IME…) : on ne
            // veut jamais casser la boucle d'application pour une préf corrompue.
            try { return keyboard[key]; }
            catch (ArgumentException) { return null; }
        }

        static void Notify()
        {
            Action handler = Changed;
            if (handler != null) handler();
        }
    }
}
