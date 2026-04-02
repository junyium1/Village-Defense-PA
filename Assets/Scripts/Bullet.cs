using UnityEngine;

public class Bullet : MonoBehaviour
{
    private Transform target;
    public float speed = 0.5f;
    public GameObject impactEffect;

    void Start()
    {
        Destroy(gameObject, 5f);
    }
    public void SeekEnemy(Transform _target)
    {
        target = _target;
    }

    void HitTarget()
    {
        Debug.Log("cc"); 
        GameObject particleIns = (GameObject)Instantiate(impactEffect, transform.position, transform.rotation);
        Destroy(particleIns, 2f);
        Destroy(gameObject);
    }
    
    void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }
        Vector3 dirToEnemy = target.position - transform.position;
        float distanceThisFrame = speed * Time.deltaTime;

        if (dirToEnemy.magnitude <= distanceThisFrame)
        {
            HitTarget();
            return;
        }
        transform.Translate(dirToEnemy.normalized * distanceThisFrame, Space.World);

    }
}
