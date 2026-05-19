using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class Dragon_Blue : MonoBehaviour // hijo de jungleBuffEventManager recordar que toca crear un script para el mini dragon azul,
                                         // que herede de este script, para que tenga las mismas funciones pero con diferentes stats.
{
    [SerializeField] protected Transform spawnPoint;

    protected NavMeshAgent agent; // para mas inteligencia del dragon, para que pueda navegar mejor el mapa, evitar obstáculos
                                  // protected para que el mini dragon azul tambien pueda usarlo sin problemas

    protected enum DragonState
    {
        Idle,
        Attacking,
        Leashing,
        Dead,
        Spawned,
        Chasing
    }

    [SerializeField] protected DragonState state;

    [SerializeField] protected Animator animator;

    [SerializeField] protected Transform target;

    //private GameObject currentTarget;

    protected Dragon_Spawn mySpawner;

    [Header("Dragon Stats")]
    [SerializeField] protected float health = 3000f;
    protected float maxHealth;

    [SerializeField] protected float damage = 150f;

    [SerializeField] protected float regenRate = 200f;

    [Header("CombatStats")]
    [SerializeField] protected float attackRange = 3f;

    [SerializeField] protected float attackCooldown = 2f;

    [SerializeField] protected float attackSpeed = 1f;

    [SerializeField] protected float attackRadius = 1f;

    [SerializeField] protected LayerMask playerLayer;

    protected float currentAttackCooldown;

    [SerializeField] protected float detectionRange = 8f;

    [Header("Movement Stats")]
    [SerializeField] protected float moveSpeed = 2f;

    [Header("Respawn and Spawn Stats")]
    [SerializeField] protected float respawnTime = 120f;

    [SerializeField] protected float spawnTime = 0f;

    [Header("Lishing Stats")]
    [SerializeField] protected float lishingRange = 15f; // subir un poco este rango probablemente
    [SerializeField] protected float lishingSpeed = 3f;

    [Header("otras")]
    [SerializeField] protected bool isDead = false;

    [Header("Attack Variations_Prueba")]
    protected int attackVariation = 0;

    protected virtual void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // validar si el dragon tiene NavMeshAgent
        if (agent != null)
        {
            agent.speed = moveSpeed;

            // para que no se pegue completamente al jugador
            agent.stoppingDistance = attackRange - 0.3f;

            // el dragon rota manualmente con LookAt
            agent.updateRotation = false;
        }

        maxHealth = health;

        // Modifica la velocidad del set de animaciones
        if (animator != null)
            animator.speed = attackSpeed;

        StartCoroutine(SpawnDragon());
    }

    protected virtual void Update()
    {
        if (isDead)
            return;

        if (currentAttackCooldown > 0)
            currentAttackCooldown -= Time.deltaTime;

        switch (state)
        {
            case DragonState.Idle:
                Idle();
                break;

            case DragonState.Attacking:
                Attack();
                break;

            case DragonState.Leashing:
                Leashing();
                break;

            case DragonState.Chasing:
                Chase();
                break;
        }

        //Nunca colocar un Debug.Log dentro de update, porque se va a spamear el log
    }

    protected virtual IEnumerator SpawnDragon()
    {
        yield return new WaitForSeconds(spawnTime);

        if (spawnPoint != null)
            transform.position = spawnPoint.position;

        state = DragonState.Idle;

        Debug.Log("Dragon Azul ha aparecido en el mapa.");
    }

    protected virtual IEnumerator RespawnDragon()
    {
        yield return new WaitForSeconds(respawnTime);

        health = maxHealth;

        isDead = false;

        if (spawnPoint != null)
            transform.position = spawnPoint.position;

        state = DragonState.Idle;

        if (animator != null)
            animator.SetBool("isIdle", true);

        Debug.Log("Dragon Azul ha respawneado.");
    }

    protected virtual void Leashing()
    {
        if (spawnPoint == null || agent == null)
            return;

        agent.isStopped = false;

        // velocidad especial mientras vuelve a casa
        agent.speed = lishingSpeed;

        agent.SetDestination(spawnPoint.position);

        // Rotar suavemente hacia el punto de spawn mientras regresa
        transform.LookAt(new Vector3(
            spawnPoint.position.x,
            transform.position.y,
            spawnPoint.position.z));

        // Se cura continuamente frame a frame mientras regresa a su base
        if (health < maxHealth)
        {
            // revisar el tiempo de regeneracion para que sea continuo y no de golpe seria raro
            health += regenRate * Time.deltaTime;

            if (health > maxHealth)
                health = maxHealth;
        }

        // Si ya llegó prácticamente al spawn
        if (!agent.pathPending && agent.remainingDistance <= 0.5f)
        {
            target = null;

            state = DragonState.Idle;

            if (animator != null)
                animator.SetBool("isIdle", true);
        }
    }

    protected virtual void Attack()
    {
        if (target == null)
        {
            state = DragonState.Leashing;
            return;
        }

        // detener movimiento mientras ataca
        if (agent != null)
            agent.isStopped = true;

        // Validar si el objetivo se movió fuera del rango de ataque
        if (Vector3.Distance(transform.position, target.position) > attackRange)
        {
            state = DragonState.Chasing;
            return;
        }

        if (currentAttackCooldown > 0)
            return;

        // Orientar al dragón de frente a su víctima al golpear
        transform.LookAt(new Vector3(
            target.position.x,
            transform.position.y,
            target.position.z));

        // definimos la secuencia de ataque del dragon, primero el Attack_1, luego el Attack_2
        if (attackVariation == 0)
        {
            if (animator != null)
                animator.SetTrigger("Attack_1");

            attackVariation = 1;

            Debug.Log("Attack_1.");
        }
        else
        {
            if (animator != null)
                animator.SetTrigger("Attack_2");

            attackVariation = 0;

            Debug.Log("Attack_2.");

            // misma logica de ataque para el dragon azul y el mini dragon azul
        }

        currentAttackCooldown = attackCooldown;
    }

    protected virtual void Idle()
    {
        if (agent != null)
            agent.isStopped = true;

        if (animator != null)
            animator.SetBool("isIdle", true);

        // solo voy a dejar que ataque si atacan
        // no quiero que ataque si estan cerca
        // Cambio: Se eliminó el Physics.OverlapSphere de aquí para que no sea agresivo por cercanía.
    }

    protected virtual void Die()
    {
        state = DragonState.Dead;

        if (agent != null)
            agent.isStopped = true;

        if (animator != null)
            animator.SetTrigger("Die");

        StartCoroutine(RespawnDragon());

        Debug.Log("Dragon Azul ha sido derrotado." + respawnTime);

        if (mySpawner != null)
            mySpawner.OnDragonDeath();
    }

    protected virtual void Chase()
    {
        if (target != null)
        {
            // Comprobar si el dragón se alejó demasiado de su zona
            if (Vector3.Distance(transform.position, spawnPoint.position) > lishingRange)
            {
                target = null;

                state = DragonState.Leashing;

                return;
            }

            if (agent != null)
            {
                agent.isStopped = false;

                // vuelve a velocidad normal
                agent.speed = moveSpeed;

                // perseguir usando NavMesh
                agent.SetDestination(target.position);
            }

            // mirar al jugador mientras persigue
            transform.LookAt(new Vector3(
                target.position.x,
                transform.position.y,
                target.position.z));

            // Si entra en la distancia de ataque cuerpo a cuerpo
            if (Vector3.Distance(transform.position, target.position) <= attackRange)
            {
                state = DragonState.Attacking;
            }
        }
        else
        {
            state = DragonState.Leashing;
        }
    }

    // parámetro opcional 'attacker' para saber quién inició la pelea
    public virtual void TakeDamage(float damageAmount, Transform attacker = null)
    {
        if (isDead)
            return;

        health -= damageAmount;

        // Si recibimos la referencia de quién nos atacó, la guardamos como el objetivo principal
        if (attacker != null)
        {
            target = attacker;
        }

        // Si es atacado mientras está quieto, se defiende de inmediato
        // Agragamos primero prioridad, puede ser al que mas daño tenga o mas vida
        if (state == DragonState.Idle || state == DragonState.Leashing)
        {
            if (target != null)
            {
                state = DragonState.Chasing;

                if (animator != null)
                    animator.SetBool("isIdle", false);
            }
        }

        if (health <= 0)
        {
            health = 0;

            isDead = true;

            Die();
        }
    }

    // función para que el spawner le asigne su referencia al dragon
    public void AssignSpawner(Dragon_Spawn spawner, Transform spawnTransform)
    {
        mySpawner = spawner;

        spawnPoint = spawnTransform;
    }

    protected virtual void OnDrawGizmosSelected()
    {
        if (spawnPoint != null)
        {
            Gizmos.color = Color.blue;

            Gizmos.DrawWireSphere(spawnPoint.position, lishingRange);

            // dibujar un circulito para que se vea el rango del area de lishing del dragon
        }

        // área de detección visual
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // rango de ataque
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}