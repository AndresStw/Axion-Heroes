using UnityEngine;
using System.Collections.Generic;

public class Sentinel_Controller : MonoBehaviour
{
    public enum Team { Blue, Red }

    [Header("Sentinel Settings")]
    public Team myTeam;
    public string enemyTag; // "RedTeam" o "BlueTeam"
    public int health = 500;
    public float attackRange = 10f;
    public float attackCooldown = 1.5f;
    public int damage = 20;

    [Header("Visuals")]
    public GameObject projectilePrefab; // Opcional: para disparar algo
    public Transform firePoint;

    private float attackTimer;
    private GameObject currentTarget;

    void Update()
    {
        if (health <= 0) return;

        FindEnemy();

        if (currentTarget != null)
        {
            AttackTarget();
        }
    }

    void FindEnemy()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, attackRange);
        float closestDistance = Mathf.Infinity;
        GameObject closestEnemy = null;

        foreach (Collider hit in hits)
        {
            if (hit != null && !string.IsNullOrEmpty(enemyTag))
            {
                if (hit.CompareTag(enemyTag))
                {
                    // Calculamos la distancia real entre la torre y el objetivo
                    float distance = Vector3.Distance(transform.position, hit.transform.position);

                    // Si este enemigo está más cerca que el anterior, lo marcamos como objetivo
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestEnemy = hit.gameObject;
                    }
                }
            }
        }
        currentTarget = closestEnemy;
    }

    void AttackTarget()
    {
        attackTimer += Time.deltaTime;

        if (attackTimer >= attackCooldown)
        {
            attackTimer = 0;

            // Lógica de daño
            var enemy = currentTarget.GetComponent<Shard_Controller>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                Debug.Log("Sentinel atacó a " + currentTarget.name);
                // Aquí podrías instanciar un proyectil si lo tienes
            }
        }
    }

    public void TakeDamage(int damageAmount)
    {
        health -= damageAmount;
        Debug.Log("Sentinel recibió daño. Vida restante: " + health);

        if (health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("Sentinel destruido");
        // Aquí puedes poner efectos de explosión
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}