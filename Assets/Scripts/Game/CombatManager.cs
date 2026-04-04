using System.Collections;
using Game.Units;
using UnityEngine;
using TMPro;

namespace Game
{
    public class CombatManager : MonoBehaviour
    {
        public static CombatManager Instance { get; private set; }

        [Header("Wave Settings")]
        [SerializeField] GameObject      enemyPrefab;
        [SerializeField] UnitData        enemyData;
        [SerializeField] Transform       spawnPoint;
        [SerializeField] float           timeBetweenWaves = 5f;
        [SerializeField] TextMeshProUGUI waveTimerText;

        [Header("Runtime")]
        public int   currentWave    = 0;
        public int   playerLives    = 20;
        public bool  waveInProgress = false;

        float _countdown;

        void Awake()
        {
            // simple singleton — only one CombatManager in the scene
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        void OnEnable()
        {
            _countdown = timeBetweenWaves;
        }

        void Update()
        {
            if (waveInProgress) return;

            _countdown -= Time.deltaTime;

            if (waveTimerText != null)
                waveTimerText.text = Mathf.Max(0, Mathf.Floor(_countdown)).ToString();

            if (_countdown <= 0f)
                StartCoroutine(SpawnWave());
        }

        IEnumerator SpawnWave()
        {
            waveInProgress = true;
            currentWave++;

            for (int i = 0; i < currentWave; i++)
            {
                SpawnEnemy();
                yield return new WaitForSeconds(0.5f);
            }

            // // we can use this if we want each wave to be separate - mayeb for a boss mechanic?
            // yield return new WaitUntil(() => 
            //     FindObjectsByType<Unit>(FindObjectsSortMode.None)
            //         .Length == 0
            // );

            waveInProgress = false;
            _countdown = timeBetweenWaves;
        }

        void SpawnEnemy()
        {
            GameObject go = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);

            Unit unit = go.GetComponent<Unit>();
            if (unit != null)
            {
                unit.data = enemyData;
                unit.OnDeath += OnEnemyDied;
            }
        }

        void OnEnemyDied(Unit u)
        {
            // hook in score, currency, etc. here
            Debug.Log($"{u.data.unitName} eliminated.");
        }

        public void OnEnemyReachedEnd()
        {
            playerLives--;
            print($"Enemy reached end! Lives remaining: {playerLives}");

            if (playerLives <= 0)
                GameManager.Instance.OnGameOver();
        }
    }
}