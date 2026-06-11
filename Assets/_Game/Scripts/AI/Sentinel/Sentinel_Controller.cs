using UnityEngine;
using AxionHeroes.Gameplay;

public class Sentinel_Controller : MonoBehaviour, IDamageable
{
    [Header("Team")]
    public Team myTeam;
    public string enemyTag;

    [Header("Stats")]
    public float health = 500f; // Cambiado a float
    public float attackRange = 10f;
    public float attackCooldown = 1.5f;
    public float damage = 20f; // Cambiado a float

    // Implementación de IDamageable
    public bool IsDead => health <= 0;
    public float CurrentHealth => health;
    public float MaxHealth => initialHealth;

    [Header("Visual")]
    public GameObject projectilePrefab;
    public Transform firePoint;

    private float attackTimer;

    private GameObject currentTarget;

    private float nextScanTime;
    private const float scanInterval = 0.25f;
    private float initialHealth;

    private void Awake()
{
    initialHealth = health;
    enemyTag =
        myTeam == Team.Blue
        ? "RedTeam"
        : "BlueTeam";
}

    void Update()
    {
        if (health <= 0)
            return;

        if (Time.time >= nextScanTime)
        {
            nextScanTime = Time.time + scanInterval;
            FindEnemy();
        }

        if (currentTarget != null)
        {
            AttackTarget();
        }
    }

    private void FindEnemy()
{
    if (string.IsNullOrEmpty(enemyTag))
    {
        Debug.LogError(
            name + " no tiene configurado enemyTag."
        );
        return;
    }

    Collider[] hits =
        Physics.OverlapSphere(
            transform.position,
            attackRange
        );

    GameObject bestTarget = null;

    float closestDistance = Mathf.Infinity;

    int highestPriority = -1;

    foreach (Collider hit in hits)
    {
        if (!hit.CompareTag(enemyTag))
            continue;

        int priority = 0;

        if (hit.GetComponentInParent<HeroController>())
    priority = 3;

else if (hit.GetComponentInParent<Shard_Controller>())
    priority = 2;

else if (hit.GetComponentInParent<Sentinel_Controller>())
    priority = 1;

        float distance =
            Vector3.Distance(
                transform.position,
                hit.transform.position
            );

        if (priority > highestPriority)
        {
            highestPriority = priority;
            closestDistance = distance;
            bestTarget = hit.gameObject;
        }
        else if (
            priority == highestPriority &&
            distance < closestDistance
        )
        {
            closestDistance = distance;
            bestTarget = hit.gameObject;
        }
    }

    currentTarget = bestTarget;
}

    private void AttackTarget()
    {
        if (currentTarget == null)
            return;

        float distance =
            Vector3.Distance(
                transform.position,
                currentTarget.transform.position
            );

        if (distance > attackRange)
        {
            currentTarget = null;
            return;
        }

        Vector3 dir =
            (currentTarget.transform.position -
             transform.position).normalized;

        dir.y = 0f;

        if (dir != Vector3.zero)
        {
            transform.rotation =
                Quaternion.LookRotation(dir);
        }

        attackTimer += Time.deltaTime;

        if (attackTimer < attackCooldown)
            return;

        attackTimer = 0f;

        AplicarDaño();
    }

    private void AplicarDaño()
    {
        if (currentTarget == null)
            return;

        // Simplificamos la lógica usando la interfaz IDamageable
        if (currentTarget.TryGetComponent(out IDamageable damageableTarget))
        {
            damageableTarget.TakeDamage(damage);
        }
    }

    public void TakeDamage(float damageAmount) // Cambiado a float
    {
        health -= damageAmount;

        if (health <= 0)
        {
            health = 0; // Aseguramos que la salud no baje de 0
            Die();
        }
    }
    private void Die()
    {
        Debug.Log(
            $"Sentinel destruido: {gameObject.name}"
        );

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        Gizmos.DrawWireSphere(
            transform.position,
            attackRange
        );
    }
}