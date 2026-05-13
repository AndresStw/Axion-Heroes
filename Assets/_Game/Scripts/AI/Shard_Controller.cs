using UnityEngine.UI;
using UnityEngine;
using UnityEngine.AI;
using System.Collections;
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
    protected NavMeshAgent agent;
    protected Animator anim;
    protected GameObject currentTarget;
    private bool isDead = false;

    [Header("Ruta de los Shards")]
    public GameObject rutaPadre;
    public bool invertirRuta = false;

   protected virtual void Start()
    {
        health = maxHealth;
        if (miBarraDeVida != null)
        {
            miBarraDeVida.maxValue = maxHealth;
            miBarraDeVida.value = health;
        }

        ActualizarVidaUI();
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();

        if (anim != null)
        {
            anim.ResetTrigger("Die");
            if (HasParameter("Attack", anim)) anim.ResetTrigger("Attack");
            if (HasParameter("Attack_2", anim)) anim.ResetTrigger("Attack_2");
        }

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
        if (isDead) return;

        // 1. ESCANEO CONSTANTE
        FindEnemy();

        // 2. ANIMACIÓN (Siempre sincronizada)
        if (anim != null)
        {
            anim.SetFloat("Speed", agent.velocity.sqrMagnitude);
        }

        // 3. MÁQUINA DE ESTADOS (Prioridad Vertical)
        if (currentTarget != null)
        {
            // ESTADO DE COMBATE: Ignora Waypoints por completo
            HandleCombat();
        }
        else
        {
            // ESTADO DE PATRULLA: Solo si no hay amenazas

            // Limpieza de estado de frenado
            if (agent.isActiveAndEnabled && agent.isStopped)
            {
                agent.isStopped = false;
            }

            HandleMovement();
        }
    }

    bool HasParameter(string paramName, Animator animator)
    {
        if (animator == null) return false;
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }

    void HandleMovement()
    {
        if (waypoints == null || waypoints.Count == 0) return;

        agent.isStopped = false;

        if (HasParameter("Attack", anim)) anim.ResetTrigger("Attack");
        if (HasParameter("Attack_2", anim)) anim.ResetTrigger("Attack_2");

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
        if (agent.isActiveAndEnabled) agent.SetDestination(waypoints[currentWaypointIndex]);
    }
    void FindEnemy()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRange);

        GameObject bestTarget = null;
        float closestDistance = Mathf.Infinity;
        int highestPriority = -1; // -1: nada, 1: Torre, 2: Héroe, 3: Shard/Minion

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag(enemyTag))
            {
                float distance = Vector3.Distance(transform.position, hit.transform.position);
                int currentPriority = 0;

                // --- SISTEMA DE PRIORIDADES TIPO MLBB ---
                if (hit.TryGetComponent(out Shard_Controller es)) currentPriority = 3; // Prioridad 1: Minions
                else if (hit.TryGetComponent(out Sentinel_Controller sn)) currentPriority = 2; // Prioridad 2: Héroes
                else currentPriority = 1; // Prioridad 3: Estructuras/Torres

                // Lógica de elección:
                // Si encontramos algo de mayor prioridad, lo elegimos sin importar la distancia (dentro del rango)
                // Si la prioridad es igual, elegimos el más cercano.
                if (currentPriority > highestPriority)
                {
                    highestPriority = currentPriority;
                    closestDistance = distance;
                    bestTarget = hit.gameObject;
                }
                else if (currentPriority == highestPriority && distance < closestDistance)
                {
                    closestDistance = distance;
                    bestTarget = hit.gameObject;
                }
            }
        }

        if (bestTarget != null)
        {
            currentTarget = bestTarget;
            // No lo frenamos aquí, dejamos que HandleCombat decida cuándo frenar según el AttackRange
        }
        else
        {
            currentTarget = null;
        }
    }
    void HandleCombat()
    {
        if (currentTarget == null) return;

        // Calculamos la distancia al centro para la lógica de ataque
        float distance = Vector3.Distance(transform.position, currentTarget.transform.position);

        if (distance > attackRange + 0.2f)
        {
            if (agent.isActiveAndEnabled)
            {
                agent.isStopped = false;

                // --- MEJORA DE POSICIONAMIENTO ---
                // En lugar de ir todos al centro, buscamos el punto más cercano en el borde del enemigo
                Collider enemyCollider = currentTarget.GetComponent<Collider>();
                if (enemyCollider != null)
                {
                    // Esto hace que cada Shard elija "su propio punto" en la circunferencia de la torre
                    Vector3 puntoEnElBorde = enemyCollider.ClosestPoint(transform.position);
                    agent.SetDestination(puntoEnElBorde);
                }
                else
                {
                    agent.SetDestination(currentTarget.transform.position);
                }
            }
            attackTimer = 0f;
        }
        else
        {
            // FRENADO TOTAL (Mantenemos tu lógica de inercia cero)
            if (agent.isActiveAndEnabled && !agent.isStopped)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }

            // ROTACIÓN AGRESIVA (Mantenemos tu rotación suave de 25f)
            Vector3 direction = (currentTarget.transform.position - transform.position).normalized;
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 25f);
            }

            // ATAQUE INSTANTÁNEO Y CICLO (Mantenemos tu sistema de attackTimer)
            if (attackTimer == 0f)
            {
                ExecuteAttack();
                attackTimer = 0.001f;
            }

            attackTimer += Time.deltaTime * attackSpeed;

            if (attackTimer >= 1.0f)
            {
                ExecuteAttack();
                attackTimer = 0.001f;
            }
        }
    }

    protected virtual void ExecuteAttack()
    {
        if (anim == null) return;

        if (HasParameter("Attack_2", anim))
        {
            anim.SetTrigger("Attack_2");
            StartCoroutine(SecuenciaDeDanoDoble());
        }
        else
        {
            anim.SetTrigger("Attack");
            StartCoroutine(SecuenciaDeDanoSimple());
        }
    }

    IEnumerator SecuenciaDeDanoDoble()
    {
        yield return new WaitForSeconds(0.3f); // Ajustado para menos delay
        AplicarDanoProporcional(0.5f);
        yield return new WaitForSeconds(0.4f);
        AplicarDanoProporcional(0.5f);
    }

    IEnumerator SecuenciaDeDanoSimple()
    {
        yield return new WaitForSeconds(0.3f); // Ajustado para menos delay
        AplicarDanoProporcional(1.0f);
    }

    void AplicarDanoProporcional(float porcentaje)
    {
        if (isDead || currentTarget == null) return;
        int danoFinal = Mathf.RoundToInt(attackDamage * porcentaje);

        // 1. Daño a otros Shards (Minions)
        if (currentTarget.TryGetComponent(out Shard_Controller enemyShard))
        {
            enemyShard.TakeDamage(danoFinal);
        }
        // 2. Daño a la Sentinel o Torres 
        // (Si ambos usan el script Sentinel_Controller, esto cubrirá a los dos)
        else if (currentTarget.TryGetComponent(out Sentinel_Controller enemySentinel))
        {
            enemySentinel.TakeDamage(danoFinal);
        }
        // 3. Daño a Estructuras (Si usas un script diferente para las torres)
        else if (currentTarget.TryGetComponent(out Sentinel_Controller enemyTower))
        {
            enemyTower.TakeDamage(danoFinal);
        }
        else
        {
            // Esto te avisará en la consola si le estás pegando a algo que no tiene script
            Debug.LogWarning("Atacando a " + currentTarget.name + " pero no tiene un script de daño compatible.");
        }
    }

    public void TakeDamage(int damageAmount)
    {
        if (isDead) return;
        health -= damageAmount;
        ActualizarVidaUI();
        if (health <= 0) Die();
    }

    public void ActualizarVidaUI()
    {
        if (miBarraDeVida != null) miBarraDeVida.value = health;
    }
    void Die()
    {
        if (isDead) return;
        isDead = true;

        StopAllCoroutines();
        if (miBarraDeVida != null) miBarraDeVida.gameObject.SetActive(false);

        if (anim != null)
        {
            anim.SetTrigger("Die");
            if (HasParameter("Attack", anim)) anim.ResetTrigger("Attack");
            if (HasParameter("Attack_2", anim)) anim.ResetTrigger("Attack_2");
        }

        if (GetComponent<Collider>()) GetComponent<Collider>().enabled = false;
        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        // Ahora sí, llamamos a la corrutina que está dentro de la misma clase
        StartCoroutine(EsperarYDestruir());
    }

    IEnumerator EsperarYDestruir()
    {
        yield return new WaitForSeconds(0.1f);

        float tiempoDeEspera = 2.0f;

        if (anim != null)
        {
            AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("Die"))
            {
                tiempoDeEspera = stateInfo.length;
            }
        }

        yield return new WaitForSeconds(tiempoDeEspera + 1.5f);

        Destroy(gameObject);
    }
}