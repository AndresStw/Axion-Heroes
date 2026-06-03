using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using AxionHeroes.Gameplay;

public class HeroController : MonoBehaviour
{
    private NavMeshAgent agent;
    
    private Animator anim;
    private HeroBotAI botAI;

    [Header("Referencia de Datos")]
    public HeroData stats;

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

    [Header(" Sistema de Combate - Ataques Automáticos ")]
    [SerializeField] private float normalAttackCooldown = 1.0f;
    [SerializeField] private float quickAttackCooldown = 0.5f;
    [SerializeField] private float burstAttackCooldown = 3.0f;
    [SerializeField] private int attacksBeforeBurst = 5;
    [SerializeField] private float detectionRange = 15f; // Rango de detección de enemigos
    [SerializeField] private bool autoAttackEnabled = false; // Ataque automático activado
    
    private int attackCount = 0;
    private bool isInBurstMode = false;
    private float nextAttackTime = 0f;
    private bool isAttacking = false;
    private Coroutine autoAttackRoutine;

    [Header(" Habilidades & Ulti ")]
    private float nextSkillTime = 0f;
    private float nextUltiTime = 0f;
 
    [Header("")]
    public float AttackRange => stats.attackRange; 
    public bool IsAttacking => isAttacking;
    public bool IsDead => isDead;
    public float CurrentHealth => currentHealth;

    [Header("Referencias de Combate")]
    public Transform currentTarget;
    public float attackDamage = 50f;
    public float porcentaje = 1.0f;
    public EvolutionManager myEvolutionManager;
    public float expParaEvolucionPorTorre = 50f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        botAI = GetComponent<HeroBotAI>();

        if (anim == null)
        {
            Debug.LogError($"[{name}] Animator no encontrado.");
        }
        else
        {
            if (!HasParameter("Attack"))
            {
                Debug.LogWarning($"[{name}] No existe Trigger Attack.");
            }
            if (!HasParameter("AttackQuick"))
            {
                Debug.LogWarning($"[{name}] No existe Trigger AttackQuick.");
            }
            if (!HasParameter("Burst"))
            {
                Debug.LogWarning($"[{name}] No existe Trigger Burst para la ráfaga.");
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
        
        // Busca automáticamente enemigos si el ataque automático está habilitado
        if (autoAttackEnabled)
        {
            BuscarEnemigoCercano();
        }
    }

    private struct InputData
    {
        public float h;
        public float v;
        public bool tieneInput;
    }

    private InputData ObtenerInput()
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
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Alternar ataque automático
            ToggleAutoAttack();
        }
        if (Input.GetKeyDown(KeyCode.Q) && Time.time >= nextSkillTime) ExecuteSkill();
        if (Input.GetKeyDown(KeyCode.R) && Time.time >= nextUltiTime) ExecuteUltimate();
    }

    /// <summary>
    /// Alterna el ataque automático ON/OFF
    /// </summary>
    public void ToggleAutoAttack()
    {
        if (isDead) return;

        autoAttackEnabled = !autoAttackEnabled;

        if (autoAttackEnabled)
        {
            Debug.Log(name + " - ATAQUE AUTOMÁTICO ACTIVADO");
            BuscarEnemigoCercano();
            if (autoAttackRoutine != null)
                StopCoroutine(autoAttackRoutine);
            autoAttackRoutine = StartCoroutine(AutoAttackRoutine());
        }
        else
        {
            Debug.Log(name + " - ATAQUE AUTOMÁTICO DESACTIVADO");
            if (autoAttackRoutine != null)
            {
                StopCoroutine(autoAttackRoutine);
                autoAttackRoutine = null;
            }
            currentTarget = null;
            attackCount = 0;
        }
    }

    /// <summary>
    /// Busca el enemigo con menor vida dentro del rango de detección
    /// </summary>
    private void BuscarEnemigoCercano()
    {
        Collider[] enemigos = Physics.OverlapSphere(transform.position, detectionRange);
        Transform mejorObjetivo = null;
        float menorVida = float.MaxValue;

        foreach (Collider col in enemigos)
        {
            // Busca Shards
            if (col.TryGetComponent(out Shard_Controller shard))
            {
                if (shard.GetHealth() < menorVida)
                {
                    menorVida = shard.GetHealth();
                    mejorObjetivo = col.transform;
                }
            }
            // Busca Sentinels
            else if (col.TryGetComponent(out Sentinel_Controller sentinel))
            {
                if (sentinel.GetHealth() < menorVida)
                {
                    menorVida = sentinel.GetHealth();
                    mejorObjetivo = col.transform;
                }
            }
            // Busca Héroes enemigos
            else if (col.TryGetComponent(out HeroController hero))
            {
                if (hero != this && !hero.IsDead && hero.CurrentHealth < menorVida)
                {
                    menorVida = hero.CurrentHealth;
                    mejorObjetivo = col.transform;
                }
            }
        }

        currentTarget = mejorObjetivo;
    }

    /// <summary>
    /// Rutina de ataque automático continuo
    /// </summary>
    private IEnumerator AutoAttackRoutine()
    {
        while (autoAttackEnabled && !isDead)
        {
            // Si no hay objetivo, busca uno
            if (currentTarget == null)
            {
                BuscarEnemigoCercano();
            }

            // Si hay objetivo y está en rango, ataca
            if (currentTarget != null)
            {
                float distancia = Vector3.Distance(transform.position, currentTarget.position);
                if (distancia <= stats.attackRange)
                {
                    // Rota hacia el objetivo
                    Vector3 direccion = (currentTarget.position - transform.position).normalized;
                    Quaternion targetRotation = Quaternion.LookRotation(direccion);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 15f);

                    // Ejecuta el ataque
                    ExecuteAutoAttack();
                }
                else
                {
                    // Se mueve hacia el objetivo si está fuera de rango
                    if (agent != null && agent.enabled)
                    {
                        agent.SetDestination(currentTarget.position);
                    }
                }
            }

            yield return null;
        }
    }

    /// <summary>
    /// Ejecuta un ataque automático del combo
    /// </summary>
    private void ExecuteAutoAttack()
    {
        if (Time.time < nextAttackTime)
            return;

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

        attackCount++;
        nextAttackTime = Time.time + (attackCount == 1 ? normalAttackCooldown : quickAttackCooldown);
    }

    private void PlayBasicAttackFull()
    {
        isAttacking = true;

        if (anim != null)
        {
            anim.ResetTrigger("Attack");
            anim.ResetTrigger("AttackQuick");
            anim.SetTrigger("Attack");
        }

        Debug.Log(name + " realiza un ataque COMPLETO con un arma (Automático)");
        AplicarDanoAlObjetivo();
    }

    private void PlayBasicAttackQuick()
    {
        isAttacking = true;

        if (anim != null)
        {
            anim.ResetTrigger("Attack");
            anim.ResetTrigger("AttackQuick");
            anim.SetTrigger("AttackQuick");
        }

        Debug.Log(name + " realiza ataque RÁPIDO automático (Combo: " + attackCount + ")");
        AplicarDanoAlObjetivo();
    }

    private void TriggerBurstAttack()
    {
        if (isInBurstMode) return;

        isAttacking = true;

        if (anim != null)
        {
            anim.ResetTrigger("Attack");
            anim.ResetTrigger("AttackQuick");
            anim.ResetTrigger("Burst");
            anim.SetTrigger("Burst");
        }

        Debug.Log(name + " ¡Activó la RÁFAGA automática con ambas manos!");
        AplicarDanoAlObjetivo();

        isInBurstMode = true;
        attackCount = 0;
        nextAttackTime = Time.time + burstAttackCooldown;

        StartCoroutine(ResetBurstMode());
    }

    private IEnumerator ResetBurstMode()
    {
        yield return new WaitForSeconds(1.0f);
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

    private void AplicarDanoRaycastSimulado()
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
        autoAttackEnabled = false;
        if (autoAttackRoutine != null)
        {
            StopCoroutine(autoAttackRoutine);
            autoAttackRoutine = null;
        }
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
        attackCount = 0;
        isInBurstMode = false;
        autoAttackEnabled = false;

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

    public void LevelExperience(float cantidad)
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
