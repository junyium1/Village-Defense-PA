using UnityEngine;

public class VillageStats : MonoBehaviour
{
    public static int VillageLives = 20; // Vie globale (static pour y accéder partout)
    public int startLives = 20;

    void Start()
    {
        VillageLives = startLives;
    }

    // Fonction appelée quand un ennemi touche le village
    public void TakeDamage(int amount)
    {
        VillageLives -= amount;
        Debug.Log("Village attaqué ! Vies restantes : " + VillageLives);

        if (VillageLives <= 0)
        {
            EndGame();
        }
    }

    void EndGame()
    {
        Debug.LogError("GAME OVER - Le village est détruit !");
        // Time.timeScale = 0; pour mettre pause
    }
}