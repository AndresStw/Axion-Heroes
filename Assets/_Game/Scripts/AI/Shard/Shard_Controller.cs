using UnityEngine.UI;
using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class Shard_Controller : MonoBehaviour
{
    public enum Team { Blue, Red }

    [Header("Configuración de Facción")]
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
    
    [HideInInspector] public int maxHealth;
    [HideInInspector] public int health;
    [HideInInspector] public int attackDamage;
    
    public float attackRange = 3f;
    public float attackSpeed = 1.0f;
    private float attackTimer;

    protected NavMeshAgent agent; 
    protected Animator anim;
    protected GameObject currentTarget;
    private bool isDead = false;

    [Header("Configuración de Ruta")]
    public GameObject rutaPadre;
    public bool invertirRuta = false;

    [Header("Sistema de Experiencia Balanceado")]
    public int expOtorgadaAlHeroeEnemigo = 35;     // Exp que le da al Héroe enemigo cuando este minion muere
    public float expParaEvolucionPorTorre = 100f;  // Exp global que gana la facción por tumbar un Sentinel
    public float expParaEvolucionPorHeroe = 150f;  // Exp global que gana la facción por matar un Héroe enemigo

    protected virtual void Start()
    {
        // ESCALADO GLOBAL: Ajustar estadísticas según la evolución de la facción
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

        if (waypoints != null && waypoints.Count > 0) SetNextDestination();
    }

    void Update()
    {
        if (isDead) return;

        FindEnemy();

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

        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRange);
        GameObject bestTarget = null;
        float closestDistance = Mathf.Infinity;
        int highestPriority = -1; 

        foreach (Collider hit in hits)
        {
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
        }
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
        int danoFinal = Mathf.RoundToInt(attackDamage * porcentaje);

        if (currentTarget.TryGetComponent(out Shard_Controller enemyShard))
        {
            enemyShard.TakeDamage(danoFinal);
        }
        else if (currentTarget.TryGetComponent(out Sentinel_Controller enemySentinel))
        {
            // Si el golpe destruye la torre enemiga, sumamos experiencia evolutiva a toda nuestra facción
            if (enemySentinel.health <= danoFinal && myEvolutionManager != null)
            {
                myEvolutionManager.AddExperience(expParaEvolucionPorTorre);
            }
            enemySentinel.TakeDamage(danoFinal);
        }
        else if (currentTarget.TryGetComponent(out HeroController enemyHero))
        {
           
            if (enemyHero.AttackRange > 0) // Validación rápida de existencia de la referencia del héroe
            {
                // Asumiendo que añado un campo de vida pública o  vía método en HeroController
                // Si matamos al héroe enemigo:
                // if(heroeMuere) myEvolutionManager.AddExperience(expParaEvolucionPorHeroe);
            }
            enemyHero.TakeDamage(danoFinal);
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
        if (textNivelUI != null && myEvolutionManager != null) 
        {
            textNivelUI.text = "Nv. " + myEvolutionManager.currentLevel;
        }
    }

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
            if (HasParameter("Attack", anim)) anim.ResetTrigger("Attack");
            if (HasParameter("Attack_2", anim)) anim.ResetTrigger("Attack_2");
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
            if (col.TryGetComponent(out HeroController hero))
            {
                // Validación para asegurar que solo le de experiencia al héroe que es del bando contrario
                if (col.CompareTag(enemyTag))
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