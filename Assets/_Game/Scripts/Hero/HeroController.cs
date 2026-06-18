using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using AxionHeroes.Gameplay;

// tengo que analizar si dejar todo en ingles o español, por ahora como estoy solo se queda en dos versiones Es-In , por comodidad 
public class HeroController : MonoBehaviour, IDamageable
{
    private NavMeshAgent agent;// verificar si lo puedo actualizar a algo mejor  leer unityDocuentacion
    
    private Animator anim;
    private HeroBotAI botAI;

    private HeroRecall recallComponent;

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
    public Team myTeam;//en otros script esta como mieEquipo,hacer una validacion y dejarla en un solo idioma
    public bool esBot = false;
    public float tiempoParaAFK = 10f;
    private float tiempoInactivo = 0f;

    [Header("Estado del Héroe ")]
    private float currentHealth;
    private bool isDead = false;
    public bool isRecalling = false;

    [Header("Movimiento")]
    [SerializeField] private VariableJoystick mobileJoystick;

    [Header(" Sistema de Combate - Ataques Automáticos ")]
    [SerializeField] private float normalAttackCooldown = 1.0f;
    [SerializeField] private float quickAttackCooldown = 0.5f;
    [SerializeField] private float burstAttackCooldown = 3.0f;
    [SerializeField] private int attacksBeforeBurst = 5;
    [SerializeField] private float detectionRange = 15f; 
    [SerializeField] private bool autoAttackEnabled = false; 
    
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
    public float MaxHealth => stats.maxHealth; 

    [Header("Referencias de Combate")]
    public Transform currentTarget; 
    public float attackDamage = 50f; 
    public float porcentaje = 1.0f;
    public EvolutionManager myEvolutionManager; 
    [SerializeField] private Transform teamRespawnPoint; 
    public float expParaEvolucionPorTorre = 50f;

   
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int HealthHash = Animator.StringToHash("Health");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int DieHash = Animator.StringToHash("Die");

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        botAI = GetComponent<HeroBotAI>();
        recallComponent = GetComponent<HeroRecall>();

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

        // Resetear el estado de ataque si el cooldown ya pasó
        ResetearEstadoAtaqueSimulado();

        if (input.tieneInput)
        {
            // Cancelar Recall si hay movimiento manual
            if (isRecalling)
            {
                isRecalling = false;
                if (recallComponent != null) recallComponent.CancelRecall();
            }

            // El movimiento manual interrumpe el desplazamiento del auto-ataque pero no lo apaga
            if (autoAttackEnabled && agent != null && agent.enabled && agent.hasPath)
                agent.ResetPath();
        }

        if (esBot)
        {
            if (input.tieneInput)
            {
                DesactivarIA();
            }
            else
            {
                HandleBotAnimations();
                // Actualizar float de vida para animaciones de la IA
                anim.SetFloat(HealthHash, currentHealth / stats.maxHealth);
                return;
            }
        }

        VerificarInactividadAFK(input);
        HandleMovement(input);
        HandleKeyboardInput();
        
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
            tiempoInactivo = 0f;//obviamente este tiempo aumentarlo 1min o 30seg dependiendo
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
        if (botAI != null && botAI.isActiveAndEnabled) botAI.DesactivarBot(); // Asegurarse de que el botAI esté activo
        if (agent != null && agent.enabled) agent.ResetPath();
    }

    private void HandleBotAnimations()
    {
        if (agent == null || !agent.enabled)
            return;

        float speed = agent.velocity.sqrMagnitude > 0.01f ? 1f : 0f;
        anim.SetFloat(SpeedHash, speed);
    }

    private void HandleMovement(InputData input)
    {
        if (isAttacking) return;

        Vector3 movementDirection = new Vector3(input.h, 0f, input.v).normalized;

        if (movementDirection.magnitude >= 0.1f)
        {
            // Corregir , la anim si ataca no se mueve , deberia ser si ataco ir me moviendo pero mas lento , o no se
            if (isAttacking) return;

            Quaternion targetRotation = Quaternion.LookRotation(movementDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 15f);

            if (agent != null && agent.enabled) agent.Move(movementDirection * stats.movementSpeed * Time.deltaTime);
            anim.SetFloat(SpeedHash, 1f);
        }
        else
        {
            anim.SetFloat(SpeedHash, 0f);
        }

        float healthNormalized = currentHealth / stats.maxHealth;
        anim.SetFloat(HealthHash, healthNormalized);
    }

    private void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Alternarnador de ataques
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

    public void ExecuteAttackBasic()
    {
        if (isDead || Time.time < nextAttackTime) return;
        ExecuteAutoAttack();
    }

    /// <summary>
    /// 1.prioridad busca el enemigo con menor vida dentro del rango de detección ,para futuros posibles agregar, prioridad escojible
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
                if (shard.myTeam != myTeam && shard.GetHealth() < menorVida)
                {
                    menorVida = shard.GetHealth();
                    mejorObjetivo = col.transform;
                }
            }
            // Busca Sentinels
            else if (col.TryGetComponent(out Sentinel_Controller sentinel))
            {
                if (sentinel.myTeam != myTeam && !sentinel.IsDead && sentinel.CurrentHealth < menorVida)
                {
                    menorVida = sentinel.CurrentHealth;
                    mejorObjetivo = col.transform;
                }
            }
            // Busca Héroes enemigos
            else if (col.TryGetComponent(out HeroController hero))
            {
                if (hero != this && hero.myTeam != myTeam && !hero.IsDead && hero.CurrentHealth < menorVida)
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
                    // Rota hacia el objetivo, en navmesh lo aumento a 1000 o dependiendo de como lo vea 
                    Vector3 direccion = (currentTarget.position - transform.position).normalized;
                    Quaternion targetRotation = Quaternion.LookRotation(direccion);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 15f);

                  
                    ExecuteAutoAttack();
                }
                else
                {
                    // Se mueve hacia el objetivo si está fuera de rango, rango de seguimiento 
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
    /// Ejecuta un ataque automático del combo, recordar pasar esto a un enum o scriponjet porque no todos los ataquen van a ser iguales para los heroes , entonces creo algo que permita editar los combos 
    /// </summary>
    private void ExecuteAutoAttack()
    {
        if (Time.time < nextAttackTime)
            return;

        // Verifica
        if (attackCount >= attacksBeforeBurst)
        {
            TriggerBurstAttack();
            return;
        }

        // Dispara la animación 
        if (attackCount == 0)
        {
            PlayBasicAttackFull();
        }
        else
        {
            PlayBasicAttackQuick();
        }

        attackCount++;
        nextAttackTime = Time.time + (attackCount <= 1 ? normalAttackCooldown : quickAttackCooldown);
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

    /// <summary>
    /// Lógica central de combate: aplica daño al objetivo actual.
    /// Se simplifica usando la interfaz IDamageable para funcionar con cualquier entidad.
    /// </summary>
    private void AplicarDanoAlObjetivo()
    {
        if (currentTarget == null) return;

        float danoFinal = attackDamage * porcentaje;

        // Intentamos obtener la interfaz IDamageable que comparten Shards, Sentinels y Heroes
        if (currentTarget.TryGetComponent(out IDamageable victim))
        {
            victim.TakeDamage(danoFinal);
            
            // Si el objetivo muere, limpiamos la referencia para buscar uno nuevo
            if (victim.IsDead) currentTarget = null;
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

        if (isRecalling)
        {
            isRecalling = false;
            if (recallComponent != null) recallComponent.CancelRecall();
        }

        currentHealth = Mathf.Clamp(currentHealth, 0, stats.maxHealth);

        if (currentHealth <= 0) Die();
    }

    public void Heal(float amount)
    {
        currentHealth += amount;
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
        if (anim != null) anim.SetTrigger(DieHash);

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
        
        if (autoAttackRoutine != null)
        {
            StopCoroutine(autoAttackRoutine);
            autoAttackRoutine = null;
        }
        autoAttackEnabled = false;
        
        if (teamRespawnPoint != null)
            transform.position = teamRespawnPoint.position;
        else
            transform.position = Vector3.zero; // Fallback

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
