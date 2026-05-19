using UnityEngine;
using System.Collections.Generic;

public class Sentinel_Controller : MonoBehaviour
{
    public enum Team { Blue, Red }

    [Header("Sentinel Settings")]
    public Team myTeam;
    public string enemyTag; 
    public int health = 500;
    public float attackRange = 10f;
    public float attackCooldown = 1.5f;
    public int damage = 20;

    [Header("Visuals")]
    public GameObject projectilePrefab; 
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
                    
                    float distance = Vector3.Distance(transform.position, hit.transform.position);

                    
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

            
            var enemy = currentTarget.GetComponent<Shard_Controller>();//me falta agregarle a los enemigos  con los script faltantes player, core,etc..
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                Debug.Log("Sentinel atacó a " + currentTarget.name);//despues reemplazar por animacion y proyectil o efecto y visual

            }
        }
    }

    public void TakeDamage(int damageAmount)
    {
        health -= damageAmount;
        Debug.Log("Sentinel recibió daño. Vida restante: " + health); //agregar barra de vida despues y la voz de notificacion de ataque solo la primera vez o con could down para no spamear

        if (health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("Sentinel destruido");//agregar animacion de destruccion y efectos visuales despues y la voz de notificacion de destruccion

        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}