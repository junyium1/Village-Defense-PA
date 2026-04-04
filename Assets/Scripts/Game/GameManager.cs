using UnityEngine;

namespace Game
{
    // TODO implement more phases (story / cutscenes pre-boss fights if any
    public enum GamePhase { Placement, Combat, Pause }
    
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public PlacementManager placementManager;
        public CombatManager combatManager;
        public GamePhase currentPhase;
        private GamePhase _previousPhase; // to store what phase to go back to
        
        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;

            placementManager = GetComponent<PlacementManager>();
            combatManager    = GetComponent<CombatManager>();
        }
        
        // TODO by default start at placement, change later when include story etc
        void Start() => EnterPlacement();

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
        
        // TODO implement game over screen later
        public void OnGameOver()
        {
            print(">>>>>> Game Over!");
            // load game over screen, etc.
        }
    }
}