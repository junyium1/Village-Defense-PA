using Menus;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game
{
    // TODO implement more phases (story / cutscenes pre-boss fights if any
    public enum GamePhase
    {
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

            Time.timeScale = 0f;
        }

        public void ResumeGame()
        {
            currentPhase = _previousPhase;

            placementManager.enabled = (currentPhase == GamePhase.Placement);
            combatManager.enabled = (currentPhase == GamePhase.Combat);

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