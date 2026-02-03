using UnityEngine;
using System.Collections;

public class WaveSpawner : MonoBehaviour
{
    public Transform enemyPrefab;
    public Transform spawnPoint; // Positionne cet objet au même endroit que "Point0"
    
    public int enemiesToSpawn = 5; // Nombre d'ennemis pour gagner
    public bool finishedSpawning = false; // Pour savoir si on a fini la vague

    void Start()
    {
        StartCoroutine(SpawnWave());
    }

    IEnumerator SpawnWave()
    {
        for (int i = 0; i < enemiesToSpawn; i++)
        {
            SpawnEnemy();
            yield return new WaitForSeconds(2f);
        }
        finishedSpawning = true; // La vague est finie
    }

    void SpawnEnemy()
    {
        GameManager.EnemiesAlive++; // On ajoute 1 ennemi au compteur
        Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
    }
}