using UnityEngine;

namespace Menus
{
    /// <summary>
    /// Menu pause 3D (GameScene) : même univers que le menu principal — pancarte
    /// enchaînée qui arrive en PENDULE à l'ouverture (Echap), planches cliquables
    /// (Reprendre / Options / Menu principal). Ouverture du sous-panneau Options :
    /// la pancarte pause DISPARAÎT et la pancarte options arrive en pendule (retour
    /// = miroir). Pas de « bousculade » ici (réservée au menu principal) : elle
    /// laissait la pancarte pause mal placée + non cliquable au retour (co-location).
    /// Toutes les animations tournent en temps NON-scalé (timeScale = 0 en pause).
    /// La racine visuelle est repositionnée devant la caméra à chaque ouverture
    /// (caméra de jeu mobile) ; les pivots des swings sont recalculés à ce moment.
    /// C'est <see cref="PauseMenuManager"/> qui pilote l'ouverture/fermeture
    /// (Echap, timeScale via GameManager) — ce contrôleur ne lit pas l'input Echap.
    /// </summary>
    public class PauseMenu3DController : MonoBehaviour
    {
        public static PauseMenu3DController Instance { get; private set; }

        [Header("Structure (prefab PauseMenu3D)")]
        [Tooltip("Racine visuelle déplacée devant la caméra à chaque ouverture.")]
        [SerializeField] Transform visualRoot;
        [SerializeField] Transform pauseSign;
        [SerializeField] Transform optionsSign;

        [Header("Câblage")]
        [SerializeField] Menu3DInput input;
        [SerializeField] PauseMenuManager legacyPause;

        [Header("Placement devant la caméra")]
        [SerializeField] float distanceFromCamera = 3f;
        [Tooltip("Décalage vertical de la racine par rapport à la hauteur caméra\n" +
                 "(la pancarte pend depuis le haut : légèrement au-dessus du centre).")]
        [SerializeField] float heightOffset = 0.6f;

        // Transitions : entry = pendule solo de la pancarte pause (ouverture/fermeture),
        // panel = pendule solo de la pancarte options (la pancarte pause est masquée
        // pendant). Auto-ajoutés/câblés à l'Awake si absents.
        SignpostPushSwap _entrySwap;
        SignpostPushSwap _panelSwap;

        enum Panel { Pause, Options }
        Panel _current = Panel.Pause;
        bool _visible;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            var swaps = GetComponents<SignpostPushSwap>();
            _entrySwap = swaps.Length > 0 ? swaps[0] : gameObject.AddComponent<SignpostPushSwap>();
            _panelSwap = swaps.Length > 1 ? swaps[1] : gameObject.AddComponent<SignpostPushSwap>();
            _entrySwap.useUnscaledTime = true;  // pause : timeScale = 0
            _panelSwap.useUnscaledTime = true;
            _entrySwap.SetTargets(null, pauseSign);      // pendule solo pancarte pause (ouverture/fermeture)
            _panelSwap.SetTargets(null, optionsSign);    // pendule solo pancarte options (la pause disparaît)

            if (input != null) input.enabled = false;        // raycast actif en pause seulement
            if (visualRoot != null) visualRoot.gameObject.SetActive(false);
        }

        // ----------------------- ouverture / fermeture (appelées par PauseMenuManager) -----------------------

        /// <summary>Ouvre le menu pause : place la pancarte devant la caméra + pendule entrant.</summary>
        public void Show()
        {
            if (_visible || visualRoot == null) return;
            _visible = true;
            _current = Panel.Pause;

            // Racine + pancarte pause réactivées AVANT le placement : le centrage lit
            // les bounds des renderers (invalides si inactifs).
            visualRoot.gameObject.SetActive(true);
            if (optionsSign != null) optionsSign.gameObject.SetActive(false);
            if (pauseSign != null) pauseSign.gameObject.SetActive(true);

            PlaceInFrontOfCamera();
            _panelSwap.SnapHomeSignpost(); // la pancarte pause repart toujours debout

            if (input != null) input.enabled = true;
            _entrySwap.SwingIn(null);
        }

        /// <summary>Ferme le menu pause : pendule sortant, puis la racine est désactivée.</summary>
        public void Hide()
        {
            if (!_visible || visualRoot == null) return;
            _visible = false;

            if (input != null) input.enabled = false;

            // Fermeture demandée depuis le panneau Options : on revient d'abord
            // instantanément sur la pancarte pause, debout, avant le pendule sortant.
            if (_current == Panel.Options)
            {
                if (optionsSign != null) optionsSign.gameObject.SetActive(false);
                if (pauseSign != null) pauseSign.gameObject.SetActive(true);
                _panelSwap.SnapHomeSignpost();
                _current = Panel.Pause;
            }

            _entrySwap.SwingOut(() => { if (!_visible) visualRoot.gameObject.SetActive(false); });
        }

        // ----------------------- navigation interne (planches) -----------------------

        /// <summary>Pause → Options : la pancarte pause disparaît, la pancarte options arrive en pendule.</summary>
        public void ShowOptions()
        {
            if (!_visible || _current != Panel.Pause) return;
            _current = Panel.Options;
            if (pauseSign != null) pauseSign.gameObject.SetActive(false); // la pancarte pause disparaît
            _panelSwap.SwingIn(null);
        }

        /// <summary>Options → Pause : la pancarte options repart en pendule, la pancarte pause revient (cliquable).</summary>
        public void ShowPauseMain()
        {
            if (!_visible || _current != Panel.Options) return;
            _current = Panel.Pause;
            _panelSwap.SwingOut(() =>
            {
                if (optionsSign != null) optionsSign.gameObject.SetActive(false);
                if (pauseSign != null) pauseSign.gameObject.SetActive(true);
            });
        }

        public void ResumeGame()
        {
            if (legacyPause != null) legacyPause.Resume();
        }

        public void QuitToMainMenu()
        {
            if (legacyPause != null) legacyPause.QuitToMainMenu();
        }

        // ----------------------- placement devant la caméra -----------------------

        void PlaceInFrontOfCamera()
        {
            Camera cam = Camera.main;
            if (cam == null || visualRoot == null) return;

            // Billboard : la pancarte fait face à la caméra (vue plongeante du jeu) et
            // reste alignée sur l'écran (up caméra). Flip 180° : la face lisible
            // (planches + texte) est sur le +z local du modèle → on la retourne vers
            // la caméra.
            visualRoot.rotation = Quaternion.LookRotation(cam.transform.forward, cam.transform.up)
                                  * Quaternion.Euler(0f, 180f, 0f);

            // Cible = centre de l'écran, à distanceFromCamera devant (+ nudge vertical).
            Vector3 target = cam.transform.position
                             + cam.transform.forward * distanceFromCamera
                             + cam.transform.up * heightOffset;

            // Centrage EXACT sur le board de la pancarte active : les pivots FBX sont
            // cuits très loin de la géométrie, on recale donc sur les bounds monde
            // (rotation déjà appliquée, pancarte déjà active).
            Transform sign = pauseSign != null ? pauseSign : optionsSign;
            visualRoot.position = target;
            visualRoot.position += target - GetBoardCenter(sign);

            // Pivots des swings (monde) recalculés APRÈS le déplacement.
            _entrySwap.RefreshPivots();
            _panelSwap.RefreshPivots();
        }

        // Centre monde du board (enfant « Board ») de la pancarte ; repli sur les
        // bounds combinés si le board est introuvable.
        static Vector3 GetBoardCenter(Transform sign)
        {
            if (sign == null) return Vector3.zero;
            Transform board = sign.Find("Board");
            if (board != null)
            {
                var r = board.GetComponent<Renderer>();
                if (r != null) return r.bounds.center;
            }
            var rends = sign.GetComponentsInChildren<Renderer>(true);
            if (rends.Length == 0) return sign.position;
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            return b.center;
        }
    }
}
