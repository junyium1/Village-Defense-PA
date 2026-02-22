using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static int EnemiesAlive = 0;
    public WaveSpawner spawner; // Glisser GameManager

    void Update()
    {
        if (EnemiesAlive <= 0 && spawner.finishedSpawning)
        {
            Debug.Log("VICTOIRE ! Tous les ennemis sont éliminés.");
            this.enabled = false; 
        }
        
        if (VillageStats.VillageLives <= 0)
        {
            Debug.Log("t nul");
            this.enabled = false;
        }
    }
}