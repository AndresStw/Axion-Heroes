using UnityEngine.UI;
using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class Shard_Controller : MonoBehaviour
{
    public enum Team { Blue, Red }

    
    public Team myTeam;
    public string enemyTag;
    public Slider miBarraDeVida;

   
    public List<Vector3> waypoints = new List<Vector3>();
    private int currentWaypointIndex = 0;
    public float detectionRange = 15f;
    public float waypointThreshold = 1.5f;

    
    public int maxHealth = 50;
    public int health;
    public int attackDamage = 10;
    public float attackRange = 3f;

   
    public float attackSpeed = 1.0f;
    private float attackTimer;
    protected NavMeshAgent agent;
    protected Animator anim;
    protected GameObject currentTarget;
    private bool isDead = false;

    
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

       
        FindEnemy();

        
        if (anim != null)
        {
            anim.SetFloat("Speed", agent.velocity.sqrMagnitude);
        }

      
        if (currentTarget != null)
        {
           
            HandleCombat();
        }
        else
        {
            
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
        int highestPriority = -1; 

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag(enemyTag))
            {
                float distance = Vector3.Distance(transform.position, hit.transform.position);
                int currentPriority = 0;

               
                if (hit.TryGetComponent(out Shard_Controller es)) currentPriority = 3; 
                else if (hit.TryGetComponent(out Sentinel_Controller sn)) currentPriority = 2;
                else currentPriority = 1; 

                
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
       
        }
        else
        {
            currentTarget = null;
        }
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
                else
                {
                    agent.SetDestination(currentTarget.transform.position);
                }
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

      
        if (currentTarget.TryGetComponent(out Shard_Controller enemyShard))// Prioridad 1: Atacar a otros Shards
        {
            enemyShard.TakeDamage(danoFinal);
        }
        
        else if (currentTarget.TryGetComponent(out Sentinel_Controller enemySentinel)) // Prioridad 2: Atacar a Sentinels
        {
            enemySentinel.TakeDamage(danoFinal);
        }
      
        //else if (currentTarget.TryGetComponent(out Red_Core enemyCore)) // Prioridad 3: Atacar a Red Core o nexo 
        //{
        //    enemyCore.TakeDamage(danoFinal);
        //} // Falta agregar lógica para atacar a jugadores  y haciendo que si el jugador ataca al minion , el minion lo priorice por sobre los sentinels y otros shards
        else
        {
          
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