using UnityEngine;
using UnityEngine.UI;

public class Enemy : MonoBehaviour
{
    [Header("Stats de base")]
    public float speed = 10f;
    public float turnSpeed = 10f;
    public float health = 100f;
    public int damageToVillage = 1;

    [Header("Assiègement du Village")]
    public float roamRadius = 20f; // La taille de la zone où ils se baladent
    private bool isRoaming = false; // Permet de savoir dans quel état est l'ennemi
    private Vector3 roamTargetPoint; // Le point aléatoire où il va
    private Transform villageTransform;

    [Header("Unity Setup")]
    public Image healthBar;

    private Transform pathTarget;
    private int wavepointIndex = 0;
    private float startHealth;

    void Start()
    {
        startHealth = health;
        pathTarget = Waypoints.points[0];
    }

    void Update()
    {
        if (!isRoaming)
        {
            MoveAlongPath();
        }
        else
        {
            RoamAroundVillage();
        }
    }

    
    void MoveAlongPath()
    {
        Vector3 dir = pathTarget.position - transform.position;

        if (dir != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Lerp(transform.rotation, lookRotation, Time.deltaTime * turnSpeed);
        }

        transform.position = Vector3.MoveTowards(transform.position, pathTarget.position, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, pathTarget.position) <= 0.05f)
        {
            GetNextWaypoint();
        }
    }

    void GetNextWaypoint()
    {
        if (wavepointIndex < Waypoints.points.Length - 1)
        {
            wavepointIndex++;
            pathTarget = Waypoints.points[wavepointIndex];
        }
        else
        {
            
            ReachDestination();
        }
    }

    void ReachDestination()
    {
        GameObject village = GameObject.Find("Village");
        if (village != null)
        {
            villageTransform = village.transform;
            village.GetComponent<VillageStats>().TakeDamage(damageToVillage);
        }
        
        // On change l'état au lieu de détruire l'ennemi
        isRoaming = true;
        PickNewRoamTarget();
    }

    
    void RoamAroundVillage()
    {
        if (villageTransform == null) return;

        Vector3 dir = roamTargetPoint - transform.position;

        if (dir != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Lerp(transform.rotation, lookRotation, Time.deltaTime * turnSpeed);
        }

        // On peut réduire un peu la vitesse quand ils se baladent si on veut (ex: speed * 0.5f)
        transform.position = Vector3.MoveTowards(transform.position, roamTargetPoint, (speed * 0.5f) * Time.deltaTime);

        // S'il est arrivé à son point aléatoire, on en choisit un nouveau
        if (Vector3.Distance(transform.position, roamTargetPoint) <= 0.1f)
        {
            PickNewRoamTarget();
        }
    }

    void PickNewRoamTarget()
    {
        // Crée un point aléatoire dans un cercle 2D (x, y)
        Vector2 randomPoint = Random.insideUnitCircle * roamRadius;
        
        // On applique ce point autour de la position du village, en gardant la hauteur (Y) de l'ennemi
        roamTargetPoint = new Vector3(
            villageTransform.position.x + randomPoint.x, 
            transform.position.y, 
            villageTransform.position.z + randomPoint.y
        );
    }

    
    public void TakeDamage(float amount)
    {
        health -= amount;
        if (healthBar != null) healthBar.fillAmount = health / startHealth;

        if (health <= 0) Die();
    }

    void Die()
    {
        GameManager.EnemiesAlive--;
        Destroy(gameObject);
    }
    
}