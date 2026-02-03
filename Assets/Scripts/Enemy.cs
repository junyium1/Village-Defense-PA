using UnityEngine;
using UnityEngine.UI;

public class Enemy : MonoBehaviour
{
    [Header("Stats")]
    public float speed = 10f;
    public float health = 100f;
    public int damageToVillage = 1; // Dégâts infligés au village

    [Header("Unity Setup")]
    public Image healthBar;

    private Transform target;
    private int wavepointIndex = 0; // Quel point on vise actuellement
    private float startHealth;

    void Start()
    {
        startHealth = health;
        // La cible devient le premier point du chemin
        target = Waypoints.points[0];
    }

    void Update()
    {
        // Mouvement vers le waypoint actuel
        Vector3 dir = target.position - transform.position;
        transform.Translate(dir.normalized * speed * Time.deltaTime, Space.World);

        // Si on est très proche du waypoint (0.2f de distance)
        if (Vector3.Distance(transform.position, target.position) <= 0.2f)
        {
            GetNextWaypoint();
        }
    }

    void GetNextWaypoint()
    {
        // Si on n'est pas au dernier point, on passe au suivant
        if (wavepointIndex < Waypoints.points.Length - 1)
        {
            wavepointIndex++;
            target = Waypoints.points[wavepointIndex];
        }
        else
        {
            // On est arrivé au bout (Village)
            ReachDestination();
        }
    }

    void ReachDestination()
    {
        // Trouver le script du village et lui faire des dégâts
        GameObject village = GameObject.Find("Village"); // "Village" dans l'inspecteur
        if (village != null)
        {
            village.GetComponent<VillageStats>().TakeDamage(damageToVillage);
        }
        
        // L'ennemi disparaît après avoir attaqué
        GameManager.EnemiesAlive--; // Important pour la victoire
        Destroy(gameObject);
    }

    public void TakeDamage(float amount)
    {
        health -= amount;
        if (healthBar != null) healthBar.fillAmount = health / startHealth;

        if (health <= 0) Die();
    }

    void Die()
    {
        GameManager.EnemiesAlive--; // Décrémente le compteur d'ennemis vivants
        Destroy(gameObject);
    }
}