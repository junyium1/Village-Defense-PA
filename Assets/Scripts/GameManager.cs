using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static int EnemiesAlive = 0;
    public WaveSpawner spawner; // Glisser GameManager

    void Update()
    {
        // Condition de Victoire :
        // 1. Plus d'ennemis en vie
        // 2. Le spawner a fini de faire apparaître sa vague (on ajoutera cette variable juste après)
        
        if (EnemiesAlive <= 0 && spawner.finishedSpawning)
        {
            Debug.Log("VICTOIRE ! Tous les ennemis sont éliminés.");
            // Désactive le script pour ne pas spammer le message
            this.enabled = false; 
        }

        // Condition de Défaite (déjà gérée dans VillageStats, mais on peut vérifier ici aussi)
        if (VillageStats.VillageLives <= 0)
        {
            // Game Over Logic
        }
    }
}