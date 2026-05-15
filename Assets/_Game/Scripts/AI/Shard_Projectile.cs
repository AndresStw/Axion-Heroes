using UnityEngine;

public class Shard_Projectile : MonoBehaviour
{
    public float speed = 10f;
    public int damage = 20;
    public float lifetime = 3f;

    private GameObject target;

    public void SetTarget(GameObject t)
    {
        target = t;
    }

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        
        Vector3 dir = (target.transform.position - transform.position).normalized;
        transform.position += dir * speed * Time.deltaTime;

        
        transform.rotation = Quaternion.LookRotation(dir);

       
        float distance = Vector3.Distance(transform.position, target.transform.position);

        if (distance < 0.5f)
        {
            Impacto();
        }
    }

    void Impacto()
    {
        if (target == null) return;

       
        if (target.TryGetComponent(out Shard_Controller enemyShard))
        {
            enemyShard.TakeDamage(damage);
        }
       
        else if (target.TryGetComponent(out Sentinel_Controller enemySentinel))
        {
            enemySentinel.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}