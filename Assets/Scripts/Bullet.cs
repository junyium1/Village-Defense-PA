using Game;
using Game.Defenses;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    private Transform target;
    private float damage;
    public float speed = 0.5f;
    public GameObject impactEffect;

    public void Init(Transform _target, float _damage, float _speed)
    {
        target = _target;
        damage = _damage;
        speed = _speed;
        Destroy(gameObject, 5f);
    }

    void HitTarget()
    {
        Health hp = target.GetComponent<Health>();
        if (hp != null) hp.TakeDamage(damage);
        
        // TODO implement status effect cf StatusEffectManager.cs 
        // StatusEffectManager status = _target.GetComponent<StatusEffectManager>();
        // if (status != null && _statusEffect != StatusEffect.None)
        //     status.Apply(_statusEffect, _statusDuration);
        
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
