using System;
using System.Collections;
using Game.Units;
using UnityEngine;
using TMPro;

namespace Game
{
    public class CombatManager : MonoBehaviour
    {
        public static CombatManager Instance { get; private set; }
        LevelData _levelData;

        // Événements observés par les systèmes externes (ex: pont Discord) sans couplage :
        // WaveStarted(numéro de vague) au début du spawn ; WaveRewardReady(vague, kills)
        // quand une vague peut être créditée (au départ de la suivante, ou à la victoire).
        public event Action<int> WaveStarted;
        public event Action<int, int> WaveRewardReady;

        public bool IsFinalWave => _levelData != null && currentWave >= _levelData.totalWaves;

        int _killsThisWave;

        [Header("Wave Settings")] [SerializeField]
        GameObject enemyPrefab;

        [SerializeField] UnitData enemyData;
        [SerializeField] Transform spawnPoint;
        [SerializeField] TextMeshProUGUI waveTimerText;

        [Header("Runtime")] public int currentWave = 0;
        public int playerLives = 2;
        public bool waveInProgress = false;
        float _countdown;
        int _lastDisplayedTime = -1;
        public int GoldEarned { get; private set; }
        public int CrystalsEarned { get; private set; }

        // -------------- singleton --------------
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }


        void OnEnable()
        {
            _levelData = Menus.LevelSelectManager.SelectedLevel;
            if (!_levelData)
            {
                Debug.LogError("No level selected!");
                return;
            }

            playerLives = _levelData.maxLives;
            _countdown = _levelData.timeBetweenWaves;
        }

        void Update()
        {
            if (waveInProgress || currentWave >= _levelData.totalWaves) return;

            _countdown -= Time.deltaTime;

            if (waveTimerText != null)
            {
                int timeToDisplay = Mathf.Max(0, Mathf.FloorToInt(_countdown));
                if (timeToDisplay != _lastDisplayedTime)
                {
                    waveTimerText.text = timeToDisplay.ToString();
                    _lastDisplayedTime = timeToDisplay;
                }
            }

            if (_countdown <= 0f)
                StartCoroutine(SpawnWave());
        }

        IEnumerator SpawnWave()
        {
            waveInProgress = true;
            currentWave++;

            // La vague précédente est créditée au départ de la suivante : les kills comptés
            // entre les deux lui appartiennent (le serveur plafonne de toute façon les kills).
            if (currentWave > 1)
            {
                WaveRewardReady?.Invoke(currentWave - 1, _killsThisWave);
                _killsThisWave = 0;
            }

            WaveStarted?.Invoke(currentWave);

            for (int i = 0; i < currentWave; i++)
            {
                SpawnEnemy();
                yield return new WaitForSeconds(0.5f);
            }

            waveInProgress = false;
            if (currentWave >= _levelData.totalWaves)
            {
                // wait for remaining enemies to die then trigger victory
                yield return new WaitUntil(NoEnemyAlive);
                //TODO TEST!!!
                float integrityRatio = (float)playerLives / _levelData.maxLives;
                CrystalsEarned = Mathf.RoundToInt(integrityRatio * _levelData.maxCrystalsReward);                int crystalsEarned = Mathf.RoundToInt(integrityRatio * _levelData.maxCrystalsReward);
                GoldEarned = _levelData.goldReward;
                Player.Instance.EarnCrystals(crystalsEarned);
                Player.Instance.EarnGold(_levelData.goldReward);

                // Dernière vague : créditée ici (il n'y a pas de vague suivante pour la flusher).
                WaveRewardReady?.Invoke(currentWave, _killsThisWave);
                _killsThisWave = 0;

                GameManager.Instance.OnGameOver(true);
            }
            else
            {
                _countdown = _levelData.timeBetweenWaves;
            }
        }

        // Seule la faction Enemy compte pour la victoire : des unités ALLIÉES encore en vie
        // (ex: renforts Golem du pont Discord) ne doivent pas bloquer la fin de partie.
        static bool NoEnemyAlive()
        {
            foreach (Unit unit in FindObjectsByType<Unit>(FindObjectsSortMode.None))
                if (unit.data != null && unit.data.faction == Faction.Enemy)
                    return false;
            return true;
        }

        void SpawnEnemy()
        {
            GameObject go = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
            Unit unit = go.GetComponent<Unit>();
            Health hp = go.GetComponent<Health>();
            if (unit != null) unit.data = enemyData;
            if (hp != null) hp.OnDeath += _ => OnEnemyDied(unit);
        }

        void OnEnemyDied(Unit u)
        {
            _killsThisWave++;
            // debug purposes, can be useful later
            // print($"{u.data.unitName} eliminated.");
            print("smt died"); // TODO check if functional once rebased from main
        }

        public void OnEnemyReachedEnd()
        {
            playerLives--;
            print($"Enemy reached end! Lives remaining: {playerLives}");

            if (playerLives <= 0)
                GameManager.Instance.OnGameOver(false);
        }
    }
}