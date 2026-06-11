using UnityEngine.UI;
using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using AxionHeroes.Gameplay;

public class Shard_Controller : MonoBehaviour, IDamageable
{
    [Header("Configuración de Grupo")]
    public Team myTeam;
    public string enemyTag;
    [SerializeField] private Shard_EvolutionManager myEvolutionManager; // Asignar el ScriptableObject de su color

    [Header("UI Componentes")]
    public Slider miBarraDeVida;
    [SerializeField] private Text textNivelUI; // Componente de texto opcional para mostrar el nivel (1-8) sobre su cabeza, mas adelante hacemos una ui exclusiva para esto

    [Header("Navegación & IA")]
    public List<Vector3> waypoints = new List<Vector3>();//se puede mejorar
    private int currentWaypointIndex = 0;
    public float detectionRange = 12f;
    [SerializeField] private float leashRange = 8f; 
    public float waypointThreshold = 1.5f;

    [Header("Stats Base (Nivel 1)")]
    public int baseMaxHealth = 50;
    public int baseAttackDamage = 10;

    [HideInInspector] public float maxHealth;
    [HideInInspector] public float currentHealthValue;
    [HideInInspector] public float attackDamage;

    public float attackRange = 3f;
    public float attackSpeed = 1.0f;
    private float attackTimer;

    private float scanTimer;
    private const float SCAN_INTERVAL = 0.2f; // Intervalo de escaneo para buscar enemigos
    private Collider[] scanBuffer = new Collider[10]; // Buffer para OverlapSphereNonAlloc

    protected NavMeshAgent agent; 
    protected Animator anim;
    protected GameObject currentTarget;

    public GameObject Target => currentTarget;
    public void SetTarget(GameObject target) => currentTarget = target;

    private bool hasAttackParam, hasAttack2Param;

    private bool isDead = false;
    public float MaxHealth => maxHealth; // Implementación de IDamageable
    public bool IsDead => isDead;
    public float CurrentHealth => currentHealthValue;

    [Header("Configuración de Ruta")]
    public GameObject rutaPadre;
    public bool invertirRuta = false;

    [Header("Sistema de Experiencia Balanceado")]
    public int expOtorgadaAlHeroeEnemigo = 35;     // Exp que le da al Héroe enemigo cuando este minion muere
    public float expParaEvolucionPorTorre = 100f;  // Exp global que gana la facción por tumbar un Sentinel
    public float expParaEvolucionPorHeroe = 150f;  // Exp global que gana la facción por matar un Héroe enemigo

    protected virtual void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();

        // ESCALADO GLOBAL: Ajustar estadísticas según la evolución del grupo 
        if (myEvolutionManager != null)
        {
            maxHealth = myEvolutionManager.GetScaledMaxHealth(baseMaxHealth);
            attackDamage = myEvolutionManager.GetScaledDamage(baseAttackDamage);
        }
        else
        {
            maxHealth = baseMaxHealth;
            attackDamage = baseAttackDamage;
        }

        currentHealthValue = maxHealth;

        ActualizarVidaUI();

        if (anim != null)
        {
            hasAttackParam = HasParameter("Attack", anim);
            hasAttack2Param = HasParameter("Attack_2", anim);
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

        if (waypoints != null && waypoints.Count > 0) SetNextDestination();
    }

    void Update()
    {
        if (isDead) return;

        scanTimer -= Time.deltaTime;
        if (scanTimer <= 0)
        {
            FindEnemy();
            scanTimer = SCAN_INTERVAL;
        }

        if (anim != null) anim.SetFloat("Speed", agent.velocity.sqrMagnitude);

        if (currentTarget != null) HandleCombat();
        else
        {
            if (agent.isActiveAndEnabled && agent.isStopped) agent.isStopped = false;
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

        if (hasAttackParam) anim.ResetTrigger("Attack");
        if (hasAttack2Param) anim.ResetTrigger("Attack_2");

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
#region 
    void FindEnemy()
    {
        if (currentTarget != null && waypoints.Count > 0)
        {
            Vector3 puntoLineaActual = waypoints[currentWaypointIndex];
            float distanciaAlCarril = Vector3.Distance(currentTarget.transform.position, puntoLineaActual);

            if (distanciaAlCarril > leashRange)
            {
                currentTarget = null; 
                if (agent.isActiveAndEnabled) agent.SetDestination(puntoLineaActual);
                return;
            }
        }

        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, detectionRange, scanBuffer);
        GameObject bestTarget = null;
        float closestDistance = Mathf.Infinity;
        int highestPriority = -1; 

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = scanBuffer[i];
            if (hit.CompareTag(enemyTag))
            {
                float distance = Vector3.Distance(transform.position, hit.transform.position);
                int currentPriority = 0;

                if (hit.TryGetComponent(out Shard_Controller _)) currentPriority = 3;
                else if (hit.TryGetComponent(out Sentinel_Controller _)) currentPriority = 2; 
                else currentPriority = 1; 

                if (currentPriority == 1 && waypoints.Count > 0)
                {
                    if (Vector3.Distance(hit.transform.position, waypoints[currentWaypointIndex]) > leashRange) continue; 
                }

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

        if (bestTarget != null) currentTarget = bestTarget;
        else if (currentTarget == null && waypoints.Count > 0) SetNextDestination();
    }
    #endregion
    #region Mecanicas de combate
    void HandleCombat()
    {
        if (currentTarget == null) return;

        float distance = Vector3.Distance(transform.position, currentTarget.transform.position);

        if (distance > attackRange + 0.2f)
        {
            if (agent.isActiveAndEnabled)
            {
                agent.isStopped = false;
                Collider enemyCollider = currentTarget.GetComponent<Collider>();
                if (enemyCollider != null)
                {
                    Vector3 puntoEnElBorde = enemyCollider.ClosestPoint(transform.position);
                    agent.SetDestination(puntoEnElBorde);
                }
                else agent.SetDestination(currentTarget.transform.position);
            }
            attackTimer = 0f;
        } // Si está fuera de rango, no debería atacar, resetear timer
        else
        {
            if (agent.isActiveAndEnabled && !agent.isStopped)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }

            Vector3 direction = (currentTarget.transform.position - transform.position).normalized;
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 25f);
            }

            if (attackTimer <= 0f) // Si el timer es 0 o negativo, podemos atacar
            {
                ExecuteAttack();
                attackTimer = 1.0f / attackSpeed; // Reiniciar el timer con el cooldown basado en attackSpeed
            }
            else
            {
                attackTimer -= Time.deltaTime; // Decrementar el timer
            }
        }
    }

#endregion
#region Ejecucion ataques

    protected virtual void ExecuteAttack()
    {
        if (anim == null) return; // Asegurarse de que el animador existe

        if (hasAttack2Param)
        {
            anim.SetTrigger("Attack_2");
            StartCoroutine(SecuenciaDeDanoDoble());
        }
        else if (hasAttackParam) // Usar Attack si Attack_2 no está disponible
        {
            anim.SetTrigger("Attack");
            StartCoroutine(SecuenciaDeDanoSimple());
        }
        // Si no tiene ninguna animación de ataque, no hacer nada o loggear una advertencia
    }

    IEnumerator SecuenciaDeDanoDoble()
    {
        yield return new WaitForSeconds(0.3f);
        AplicarDanoProporcional(0.5f);
        yield return new WaitForSeconds(0.4f);
        AplicarDanoProporcional(0.5f);
    }

    IEnumerator SecuenciaDeDanoSimple()
    {
        yield return new WaitForSeconds(0.3f); 
        AplicarDanoProporcional(1.0f);
    }

    void AplicarDanoProporcional(float porcentaje)
    {
        if (isDead || currentTarget == null) return;
        float danoFinal = attackDamage * porcentaje;

        if (currentTarget.TryGetComponent(out IDamageable targetDamageable)) // Usar IDamageable para estructuras también
        {
            // Si el golpe destruye una torre enemiga (asumiendo que Sentinel_Controller implementa IDamageable)
            if (targetDamageable is Sentinel_Controller enemySentinel && enemySentinel.CurrentHealth <= danoFinal && myEvolutionManager != null)
            {
                myEvolutionManager.AddExperience(expParaEvolucionPorTorre);
            }
            targetDamageable.TakeDamage(danoFinal);
        }
        // else if (currentTarget.TryGetComponent(out Sentinel_Controller enemySentinel)) // Esto ya no sería necesario si Sentinel_Controller implementa IDamageable
        // {
        //     // Si el golpe destruye la torre enemiga, sumamos experiencia evolutiva a toda nuestra facción
        //     if (enemySentinel.health <= danoFinal && myEvolutionManager != null)
        //     {
        //         myEvolutionManager.AddExperience(expParaEvolucionPorTorre);
        //     }
        //     enemySentinel.TakeDamage(danoFinal);
        // }
    }

    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;
        currentHealthValue -= damageAmount;
        ActualizarVidaUI();
        if (currentHealthValue <= 0) Die();
    }

    public void ActualizarVidaUI()
    {
        if (miBarraDeVida != null)
        {
            miBarraDeVida.maxValue = maxHealth;
            miBarraDeVida.value = currentHealthValue;
        }

        if (textNivelUI != null && myEvolutionManager != null) 
        {
            textNivelUI.text = "Nv. " + myEvolutionManager.currentLevel;
        }
    }
#endregion
    void Die()
    {
        if (isDead) return;
        isDead = true;

        RepartirExperienciaAHeroesCercanos();

        StopAllCoroutines();
        if (miBarraDeVida != null) miBarraDeVida.gameObject.SetActive(false);

        if (anim != null)
        {
            anim.SetTrigger("Die");
            if (hasAttackParam) anim.ResetTrigger("Attack");
            if (hasAttack2Param) anim.ResetTrigger("Attack_2");
        }

        if (GetComponent<Collider>()) GetComponent<Collider>().enabled = false;
        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }
        StartCoroutine(EsperarYDestruir());
    }

    private void RepartirExperienciaAHeroesCercanos()
    {
        // Al morir, busca héroes enemigos en el área para darles experiencia directa en su HeroController
        Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRange);
        foreach (Collider col in colliders)
        {
            if (col.TryGetComponent(out IDamageable damageable) && col.CompareTag(enemyTag))
            {
                // Si es un HeroController, le damos experiencia
                if (damageable is HeroController hero)
                {
                    hero.LevelExperience(expOtorgadaAlHeroeEnemigo);
                    Debug.Log($"[EXP] Otorgada {expOtorgadaAlHeroeEnemigo} de experiencia al héroe: {col.name}");
                }
            }
        }
    }

    IEnumerator EsperarYDestruir()
    {
        yield return new WaitForSeconds(0.1f);
        float tiempoDeEspera = 2.0f;

        if (anim != null)
        {
            AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("Die")) tiempoDeEspera = stateInfo.length;
        }

        yield return new WaitForSeconds(tiempoDeEspera + 1.5f);
        Destroy(gameObject);
    }
}