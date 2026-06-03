using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using AxionHeroes.Gameplay;

public class HeroController : MonoBehaviour
{
    private NavMeshAgent agent;//para el movimiento del héroe, aunque también se puede mover con transform.Translate o algo así, pero el NavMeshAgent ya me da la ventaja de poder navegar por el map[...]
    
    private Animator anim;
    private HeroBotAI botAI;

    [Header("Referencia de Datos")]
    public HeroData stats;//scriptable object con las estadísticas del héroe, como vida, daño, velocidad de movimiento, etc.

    [Header("Respawn Config")]
    [SerializeField] private float baseRespawnTime = 6f;
    [SerializeField] private float timeIncrementPerLevel = 2f;
    private float currentRespawnDuration = 0f;

    [Header("Experiencia y Nivel")]
    public int nivel = 1;
    public float experienciaActual = 0f;
    public float experienciaParaSiguienteNivel = 100f;
    public float maxLevel = 25f;

    [Header(" Configuración de Rol & IA ")]
    public bool esBot = false;
    public float tiempoParaAFK = 10f;
    private float tiempoInactivo = 0f;

    [Header("Estado del Héroe ")]
    private float currentHealth;
    private bool isDead = false;

    [Header("Movimiento")]
    [SerializeField] private VariableJoystick mobileJoystick;

    [Header(" Sistema de Combate - Ataques Básicos ")]
    [SerializeField] private float normalAttackCooldown = 1.0f; // Enfriamiento del ataque básico normal
    [SerializeField] private float quickAttackCooldown = 0.5f; // Enfriamiento de ataques consecutivos
    [SerializeField] private float burstAttackCooldown = 3.0f; // Enfriamiento de la ráfaga
    [SerializeField] private int attacksBeforeBurst = 5; // Número de básicos consecutivos antes de activar la ráfaga
    
    private int attackCount = 0; // Conteo de ataques consecutivos
    private bool isInBurstMode = false; // Indica si está en modo ráfaga
    private float nextAttackTime = 0f; // Temporizador para ataques
    private bool isAttacking = false;

    [Header(" Habilidades & Ulti ")]
    private float nextSkillTime = 0f;
    private float nextUltiTime = 0f;
 
    [Header("")]
    public float AttackRange => stats.attackRange; 
    public bool IsAttacking => isAttacking;
    public bool IsDead => isDead;
    public float CurrentHealth => currentHealth;


    [Header("Referencias de Combate")]
    public Transform currentTarget; // El enemigo al que el bot/héroe apunta
    public float attackDamage = 50f; // Puedes sacar esto de 'stats' si prefieres
    public float porcentaje = 1.0f; // Multiplicador de daño
    public EvolutionManager myEvolutionManager; // Asumiendo que este es tu sistema de exp
    public float expParaEvolucionPorTorre = 50f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        botAI = GetComponent<HeroBotAI>();

        if (anim == null)
        {
            Debug.LogError(
                $"[{name}] Animator no encontrado."
            );
        }
        else
        {
            if (!HasParameter("Attack"))
            {
                Debug.LogWarning(
                    $"[{name}] No existe Trigger Attack."
                );
            }
            if (!HasParameter("Burst"))
            {
                Debug.LogWarning(
                    $"[{name}] No existe Trigger Burst para la ráfaga."
                );
            }
        }

        currentHealth = stats.maxHealth;

        if (agent != null)
        {
            agent.acceleration = 30f;
            agent.angularSpeed = 1000f;
            agent.speed = stats.movementSpeed;
            agent.stoppingDistance = 0.1f;
        }

        if (esBot)
        {
            ActivarIA();
        }
    }

    void Update()
    {
        if (isDead) return;

        InputData input = ObtenerInput();

        if (esBot)
        {
            if (input.tieneInput)
            {
                DesactivarIA();
            }
            else
            {
                HandleBotAnimations();
                return;
            }
        }

        VerificarInactividadAFK(input);
        HandleMovement(input);
        HandleKeyboardInput();
        ResetearEstadoAtaqueSimulado();
    }

    private struct InputData
    {
        public float h;
        public float v;
        public bool tieneInput;
    }

    private InputData ObtenerInput()//joystick
    {
        InputData data = new InputData();
        data.h = Input.GetAxisRaw("Horizontal");
        data.v = Input.GetAxisRaw("Vertical");

        if (mobileJoystick != null && (mobileJoystick.Horizontal != 0f || mobileJoystick.Vertical != 0f))
        {
            data.h = mobileJoystick.Horizontal;
            data.v = mobileJoystick.Vertical;
        }

        data.tieneInput = Mathf.Abs(data.h) > 0.1f || Mathf.Abs(data.v) > 0.1f || Input.anyKey;
        return data;
    }

    private void VerificarInactividadAFK(InputData input)
    {
        if (!input.tieneInput)
        {
            tiempoInactivo += Time.deltaTime;

            if (tiempoInactivo >= tiempoParaAFK)
            {
                esBot = true;
                ActivarIA();
            }
        }
        else
        {
            tiempoInactivo = 0f;
        }
    }

    private bool HasParameter(string paramName)
    {
        if (anim == null) return false;

        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.name == paramName)
                return true;
        }

        return false;
    }

    private void ActivarIA()
    {
        if (botAI == null) botAI = gameObject.AddComponent<HeroBotAI>();
        botAI.miRol = stats.role; 
        botAI.ActivarBot();
    }

    private void DesactivarIA()
    {
        esBot = false;
        tiempoInactivo = 0f;
        if (botAI != null) botAI.DesactivarBot();
        if (agent != null && agent.enabled) agent.ResetPath();
    }

    private void HandleBotAnimations()
    {
        if (agent == null || !agent.enabled)
            return;

        SetAnimatorFloatSafe(
            "Speed",
            agent.velocity.sqrMagnitude > 0.01f ? 1f : 0f
        );
    }

    private void HandleMovement(InputData input)
    {
        if (isAttacking) return;

        Vector3 movementDirection = new Vector3(input.h, 0f, input.v).normalized;

        if (movementDirection.magnitude >= 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movementDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 15f);

            if (agent != null && agent.enabled)
            {
                agent.velocity = movementDirection * stats.movementSpeed;
            }

            SetAnimatorFloatSafe("Speed", 1f);
        }
        else
        {
            if (agent != null && agent.enabled)
            {
                agent.velocity = Vector3.zero;
                if (agent.hasPath) agent.ResetPath();
            }

            SetAnimatorFloatSafe("Speed", 0f);
        }

        float healthNormalized = currentHealth / stats.maxHealth;
        SetAnimatorFloatSafe("Health", healthNormalized);
    }

    private void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.Space)) ExecuteAttackBasic();
        if (Input.GetKeyDown(KeyCode.Q) && Time.time >= nextSkillTime) ExecuteSkill();
        if (Input.GetKeyDown(KeyCode.R) && Time.time >= nextUltiTime) ExecuteUltimate();
    }

    /// <summary>
    /// Ejecuta el ataque básico con sistema de combos y ráfagas
    /// Primer ataque: animación completa
    /// Ataques consecutivos: animación acortada para fluidez
    /// Después de N ataques: activa ráfaga con ambas armas
    /// </summary>
    public void ExecuteAttackBasic()
    {
        Debug.Log(name + " -> ExecuteAttackBasic llamado");

        if (isDead)
        {
            Debug.Log("Muerto");
            return;
        }

        if (Time.time < nextAttackTime)
        {
            Debug.Log("En cooldown");
            return;
        }

        // Verifica si se activa la ráfaga
        if (attackCount >= attacksBeforeBurst)
        {
            TriggerBurstAttack();
            return;
        }

        // Dispara la animación según si es el ataque completo o recortado
        if (attackCount == 0)
        {
            PlayBasicAttackFull();
        }
        else
        {
            PlayBasicAttackQuick();
        }

        // Incrementa el contador de ataques consecutivos
        attackCount++;
        nextAttackTime = Time.time + (attackCount == 1 ? normalAttackCooldown : quickAttackCooldown);
        
        // Resetea el conteo si el jugador es demasiado lento
        StartCoroutine(ResetAttackCombo());
    }

    private void PlayBasicAttackFull()
    {
        isAttacking = true;

        if (anim != null)
        {
            anim.ResetTrigger("Attack");
            anim.SetTrigger("Attack");
        }

        Debug.Log(name + " realiza un ataque completo con un arma");

        // Aplica el daño al objetivo si existe
        AplicarDanoAlObjetivo();
    }

    private void PlayBasicAttackQuick()
    {
        isAttacking = true;

        if (anim != null)
        {
            anim.ResetTrigger("Attack");
            anim.SetTrigger("Attack");
        }

        Debug.Log(name + " realiza ataque consecutivo fluido con un arma (Combo: " + attackCount + ")");

        // Aplica el daño al objetivo si existe
        AplicarDanoAlObjetivo();
    }

    private void TriggerBurstAttack()
    {
        if (isInBurstMode) return;

        isAttacking = true;

        if (anim != null)
        {
            anim.ResetTrigger("Attack");
            anim.ResetTrigger("Burst");
            anim.SetTrigger("Burst");
        }

        Debug.Log(name + " ¡Activó la ráfaga con ambas manos!");

        // Aplica daño inmediato
        AplicarDanoAlObjetivo();

        // Reinicia el contador y el cooldown para ataques
        isInBurstMode = true;
        attackCount = 0;
        nextAttackTime = Time.time + burstAttackCooldown;

        // Salir de modo ráfaga después de la animación
        StartCoroutine(ResetBurstMode());
    }

    private IEnumerator ResetAttackCombo()
    {
        yield return new WaitForSeconds(1.5f); // Define el tiempo límite para continuar un combo
        if (attackCount > 0)
        {
            attackCount = 0; // Si se supera este tiempo, se reinicia el contador
            Debug.Log(name + " - Combo reseteado por inactividad");
        }
    }

    private IEnumerator ResetBurstMode()
    {
        yield return new WaitForSeconds(1.0f); // Ajusta la duración de la ráfaga
        isInBurstMode = false;
        isAttacking = false;
        Debug.Log(name + " - Modo ráfaga terminado");
    }

    private void AplicarDanoAlObjetivo()
    {
        if (currentTarget == null)
            return;

        int danoFinal = Mathf.RoundToInt(attackDamage * porcentaje);

        if (currentTarget.TryGetComponent(out Shard_Controller enemyShard))
        {
            enemyShard.TakeDamage(danoFinal);
            Debug.Log("Golpeó shard: " + enemyShard.name);
        }
        else if (currentTarget.TryGetComponent(out Sentinel_Controller enemySentinel))
        {
            enemySentinel.TakeDamage(danoFinal);
            Debug.Log("Golpeó sentinel: " + enemySentinel.name);
        }
        else if (currentTarget.TryGetComponent(out HeroController enemyHero))
        {
            enemyHero.TakeDamage(danoFinal);
            Debug.Log("Golpeó héroe: " + enemyHero.name);
        }
    }

    private void AplicarDanoRaycastSimulado()//debug para simular el ataque básico mientras no tengo los personajes ni las animaciones definitivas, luego se puede reemplazar por la lógica real de d[...]
    {
       
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, transform.forward, out hit, stats.attackRange))
        {
            Debug.Log($"[GOLPE SIMULADO] Impactó a: {hit.collider.name} infligiendo {stats.attackDamage} de daño.");
        }
    }

    private void ResetearEstadoAtaqueSimulado()
    {
        if (Time.time >= nextAttackTime && isAttacking)
        {
            isAttacking = false;
        }
    }

    private void SetAnimatorFloatSafe(string paramName, float value)
    {
        if (anim == null) return;
        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.name == paramName)
            {
                anim.SetFloat(paramName, value);
                return;
            }
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, stats.maxHealth);

        if (botAI != null && esBot)
        {
            botAI.NotificarDanoRecibido(currentHealth / stats.maxHealth);
        }

        if (currentHealth <= 0) Die();
    }

    private void Die()
    {
        isDead = true;
        if (botAI != null) botAI.DesactivarBot();
        if (agent != null)
        {
            agent.ResetPath();
            agent.enabled = false;
        }
        if (anim != null) anim.SetTrigger("Die");

        StartCoroutine(RespawnTimer());
    }

    private IEnumerator RespawnTimer()
    {
        currentRespawnDuration = baseRespawnTime + (nivel * timeIncrementPerLevel);
        Debug.Log($"[RESPAWN] Héroe eliminado. Reaparición en {currentRespawnDuration} segundos (Nivel {nivel}).");

        yield return new WaitForSeconds(currentRespawnDuration);
        Respawn();
    }

    private void Respawn()
    {
        isDead = false;
        currentHealth = stats.maxHealth;
        tiempoInactivo = 0f;
        attackCount = 0; // Resetea el combo al respawnear
        isInBurstMode = false;

        transform.position = Vector3.zero; 

        if (agent != null) agent.enabled = true;

        if (anim != null)
        {
            anim.Rebind();
            anim.Update(0f);
        }

        if (esBot) ActivarIA();
        Debug.Log("[RESPAWN] El héroe ha vuelto a la batalla.");
    }

    public void LevelExperience(float cantidad)//Experimental
    {
        if (nivel >= maxLevel) return;

        experienciaActual += cantidad;
        while (experienciaActual >= experienciaParaSiguienteNivel)
        {
            SubirNivel();
            if (nivel >= maxLevel)
            {
                experienciaActual = 0f;
                break;
            }
        }
    }

    private void SubirNivel()
    {
        experienciaActual -= experienciaParaSiguienteNivel;
        nivel++;
        experienciaParaSiguienteNivel *= 1.5f;
        Debug.Log("¡Subiste al nivel " + nivel + "!");
    }

    public void ExecuteSkill() { if (!isDead && Time.time >= nextSkillTime) { nextSkillTime = Time.time + stats.skillCooldown; if (anim != null) anim.SetTrigger("DoSkill"); } }
    public void ExecuteUltimate() { if (!isDead && Time.time >= nextUltiTime) { nextUltiTime = Time.time + stats.ultiCooldown; if (anim != null) anim.SetTrigger("DoUlti"); } }
}
