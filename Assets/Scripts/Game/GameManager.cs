using Menus;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game
{
    // TODO implement more phases (story / cutscenes pre-boss fights if any
    public enum GamePhase
    {
        /// <summary>Le joueur choisit ou poser la zone jouable sur la map.</summary>
        ZonePlacement,
        Placement,
        Combat,
        Pause
    }

    // TODO implement save progress when we have multiple levels
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public PlacementManager placementManager;
        public CombatManager combatManager;
        public GamePhase currentPhase;
        private GamePhase _previousPhase; // to store what phase to go back to
        [SerializeField] LevelEndMenuManager levelEndMenuManager;
        [SerializeField] TextMeshProUGUI levelTitle;

        [Header("Zone jouable")]
        [Tooltip("Laisser vide pour demarrer directement en phase Placement (ancien comportement).")]
        [SerializeField] LevelZonePlacer zonePlacer;

        [Tooltip("Affiche pendant le choix de l'emplacement (ex: 'Choisis ou installer ton village').")]
        [SerializeField] GameObject zonePlacementHint;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;

            placementManager = GetComponent<PlacementManager>();
            combatManager = GetComponent<CombatManager>();
        }

        // TODO by default start at placement, change later when include story etc
        void Start()
        {
            if (levelTitle != null && LevelSelectManager.SelectedLevel != null)
                levelTitle.text = LevelSelectManager.SelectedLevel.levelName;

            if (zonePlacer != null)
                EnterZonePlacement();
            else
                EnterPlacement();
        }

        /// <summary>
        /// Phase 0 : le joueur promene la zone jouable sur la map et valide un
        /// emplacement. Le placement de defenses n'ouvre qu'apres validation.
        /// </summary>
        public void EnterZonePlacement()
        {
            currentPhase = GamePhase.ZonePlacement;
            placementManager.enabled = false;
            combatManager.enabled = false;

            if (zonePlacementHint != null) zonePlacementHint.SetActive(true);

            zonePlacer.ZoneConfirmed -= OnZoneConfirmed;
            zonePlacer.ZoneConfirmed += OnZoneConfirmed;
            zonePlacer.Begin();
        }

        void OnZoneConfirmed(LevelZone _)
        {
            zonePlacer.ZoneConfirmed -= OnZoneConfirmed;
            if (zonePlacementHint != null) zonePlacementHint.SetActive(false);
            EnterPlacement();
        }

        public void EnterPlacement()
        {
            currentPhase = GamePhase.Placement;
            placementManager.enabled = true;
            combatManager.enabled = false;
        }

        public void EnterCombat()
        {
            currentPhase = GamePhase.Combat;
            placementManager.enabled = false;
            combatManager.enabled = true;
        }

        public void PauseGame()
        {
            if (currentPhase == GamePhase.Pause) return;

            _previousPhase = currentPhase;
            currentPhase = GamePhase.Pause;

            placementManager.enabled = false;
            combatManager.enabled = false;
            if (zonePlacer != null) zonePlacer.enabled = false;

            Time.timeScale = 0f;
        }

        public void ResumeGame()
        {
            currentPhase = _previousPhase;

            placementManager.enabled = (currentPhase == GamePhase.Placement);
            combatManager.enabled = (currentPhase == GamePhase.Combat);
            if (zonePlacer != null)
                zonePlacer.enabled = (currentPhase == GamePhase.ZonePlacement) && zonePlacer.IsActive;

            Time.timeScale = 1f;
        }

        public void OnGameOver(bool won)
        {
            levelEndMenuManager.Show(won);
            Time.timeScale = 0f;
            if (won && LevelSelectManager.SelectedLevel)
                Player.Instance.MarkLevelCompleted(LevelSelectManager.SelectedLevel.levelID);
        }
    }
}