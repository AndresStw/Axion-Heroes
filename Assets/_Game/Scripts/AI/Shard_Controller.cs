using UnityEngine.UI;
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class Shard_Controller : MonoBehaviour
{
    public enum Team { Blue, Red }

    [Header("Team Settings")]
    public Team myTeam;
    public string enemyTag;
    public Slider miBarraDeVida;

    [Header("Movement & Path")]
    public List<Vector3> waypoints = new List<Vector3>();
    private int currentWaypointIndex = 0;
    public float detectionRange = 15f;
    public float waypointThreshold = 1.5f;

    [Header("Combat Stats")]
    public int maxHealth = 50;
    public int health;
    public int attackDamage = 10;
    public float attackRange = 3f;

    [Tooltip("A mayor número, más rápido ataca (ej: 1.5)")]
    public float attackSpeed = 1.0f;
    private float attackTimer;
    private NavMeshAgent agent;
    private Animator anim;
    private GameObject currentTarget;

    [Header("Ruta de los Shards (Opcional si se usa Spawner)")]
    public GameObject rutaPadre;
    public bool invertirRuta = false;

    void Start()
    {
        health = maxHealth;
        if (miBarraDeVida != null) miBarraDeVida.maxValue = maxHealth;
        ActualizarVidaUI();

        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();

        if ((waypoints == null || waypoints.Count == 0) && rutaPadre != null)
        {
            waypoints = new List<Vector3>();
            foreach (Transform child in rutaPadre.transform)
            {
                waypoints.Add(child.position);
            }
            if (invertirRuta) waypoints.Reverse();
        }

        if (waypoints != null && waypoints.Count > 0)
        {
            SetNextDestination();
        }
    }

    void Update()
    {
        if (health <= 0) return;

        FindEnemy();

        float speed = agent.velocity.magnitude;
        anim.SetFloat("Speed", speed);

        if (currentTarget != null)
        {
            HandleCombat();
        }
        else
        {
            HandleMovement();
            if (waypoints != null && waypoints.Count > 0 && currentWaypointIndex < waypoints.Count)
            {
                Debug.DrawLine(transform.position, waypoints[currentWaypointIndex], Color.blue);
            }
        }
    }

    // --- FUNCIONES DE MOVIMIENTO ---
    void HandleMovement()
    {
        if (waypoints == null || waypoints.Count == 0) return;

        agent.isStopped = false;

        if (!agent.pathPending && agent.remainingDistance <= waypointThreshold)
        {
            if (currentWaypointIndex < waypoints.Count - 1)
            {
                currentWaypointIndex++;
                SetNextDestination();
            }
        }
    }

    void SetNextDestination()
    {
        if (waypoints == null || waypoints.Count == 0 || currentWaypointIndex >= waypoints.Count) return;
        agent.SetDestination(waypoints[currentWaypointIndex]);
    }

    // --- FUNCIONES DE COMBATE ---
    void FindEnemy()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRange);
        float closestDistance = Mathf.Infinity;
        GameObject closestEnemy = null;

        foreach (Collider hit in hits)
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
        currentTarget = closestEnemy;
    }

    void HandleCombat()
    {
        if (currentTarget == null) return;

        float distance = Vector3.Distance(transform.position, currentTarget.transform.position);
        float buffer = 0.5f;
        float escapeDistance = attackRange + buffer;

        if (distance > escapeDistance)
        {
            agent.isStopped = false;
            agent.SetDestination(currentTarget.transform.position);
            if (attackTimer < 0.8f) attackTimer += Time.deltaTime * 0.5f;
        }
        else
        {
            if (!agent.isStopped) agent.isStopped = true;

            Vector3 direction = (currentTarget.transform.position - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 15f);
            }

            attackTimer += Time.deltaTime * attackSpeed;

            if (attackTimer >= 1.0f)
            {
                ExecuteAttack();
                attackTimer = 0;
            }
        }
    }

    void ExecuteAttack()
    {
        anim.SetTrigger("Attack");
        Shard_Controller enemyShard = currentTarget.GetComponent<Shard_Controller>();
        if (enemyShard != null)
        {
            enemyShard.TakeDamage(attackDamage);
        }
    }

    // --- SALUD Y UI ---
    public void ActualizarVidaUI()
    {
        if (miBarraDeVida != null)
        {
            miBarraDeVida.value = health;
        }
    }

    public void TakeDamage(int damageAmount)
    {
        health -= damageAmount;
        ActualizarVidaUI();
        if (health <= 0) Die();
    }

    void Die()
    {
        if (miBarraDeVida != null) miBarraDeVida.gameObject.SetActive(false);
        anim.SetTrigger("Die");
        agent.isStopped = true;
        if (GetComponent<Collider>()) GetComponent<Collider>().enabled = false;
        Destroy(gameObject, 2.5f);
    }

    // --- GIZMOS ---
    void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Count < 2) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            Gizmos.DrawLine(waypoints[i], waypoints[i + 1]);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}