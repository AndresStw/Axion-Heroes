using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using AxionHeroes.Gameplay;

public class HeroController : MonoBehaviour, IDamageable
{
    private NavMeshAgent agent;//para el movimiento del héroe, aunque también se puede mover con transform.Translate o algo así, pero el NavMeshAgent ya me da la ventaja de poder navegar por el mapa sin preocuparme por obstáculos o cosas así, y también me facilita la implementación de la IA del bot luego.
    
    private Animator anim;
    private HeroBotAI botAI;

    private HeroRecall recallComponent;

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
    public Team myTeam;
    public bool esBot = false;
    public float tiempoParaAFK = 10f;
    private float tiempoInactivo = 0f;

    [Header("Estado del Héroe ")]
    private float currentHealth;
    private bool isDead = false;
    public bool isRecalling = false;

    [Header("Movimiento")]
    [SerializeField] private VariableJoystick mobileJoystick;

    [Header(" Sistema de Combate (Simulado mientras tengo los personajes )")]
    private float tiempoSiguienteAtaque = 0f;
    private bool isAttacking = false;

    [Header(" Habilidades & Ulti ")]
    private float nextSkillTime = 0f;
    private float nextUltiTime = 0f;
 
    [Header("")]
    public float AttackRange => stats.attackRange; 
    public bool IsAttacking => isAttacking;
    public bool IsDead => isDead;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => stats.maxHealth; // Implementación de IDamageable


    [Header("Referencias de Combate")]
    public Transform currentTarget; // El enemigo al que el bot/héroe apunta
    public float attackDamage = 50f; // Puedes sacar esto de 'stats' si prefieres
    public float porcentaje = 1.0f; // Multiplicador de daño
    public EvolutionManager myEvolutionManager; // Asumiendo que este es tu sistema de exp
    [SerializeField] private Transform teamRespawnPoint; // Punto de respawn para el equipo
    public float expParaEvolucionPorTorre = 50f;

    // Caching de hashes para optimización
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

        // Cancelar Recall si hay movimiento significativo
        if (isRecalling && input.tieneInput)
        {
            isRecalling = false;
            if (recallComponent != null) recallComponent.CancelRecall();
        }

        // Siempre resetear el estado de ataque (cooldowns) para que el bot pueda atacar de nuevo
        ResetearEstadoAtaqueSimulado();

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
        if (botAI != null && botAI.isActiveAndEnabled) botAI.DesactivarBot(); // Asegurarse de que el botAI esté activo
        if (agent != null && agent.enabled) agent.ResetPath();
    }

    private void HandleBotAnimations()
{
    if (agent == null || !agent.enabled)
        return;
    anim.SetFloat(SpeedHash, agent.velocity.sqrMagnitude > 0.01f ? 1f : 0f);
}
    private void HandleMovement(InputData input)
    {
        if (isAttacking) return;

        Vector3 movementDirection = new Vector3(input.h, 0f, input.v).normalized;

        if (movementDirection.magnitude >= 0.1f)
        {
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
        if (Input.GetKeyDown(KeyCode.Space)) ExecuteAttackBasic();
        if (Input.GetKeyDown(KeyCode.Q) && Time.time >= nextSkillTime) ExecuteSkill();
        if (Input.GetKeyDown(KeyCode.R) && Time.time >= nextUltiTime) ExecuteUltimate();
    }

   public void ExecuteAttackBasic()
{
    Debug.Log(name + " -> ExecuteAttackBasic llamado");

    if (isDead)
    {
        Debug.Log("Muerto");
        return;
    }
    if (Time.time < tiempoSiguienteAtaque)
        return;

    isAttacking = true;

    Debug.Log(name + " ATACA");

    if (anim != null)
    {
        anim.SetTrigger(AttackHash);
    }

    tiempoSiguienteAtaque =
        Time.time + (1f / stats.attackSpeed);

    if (currentTarget == null)
        return;

    float danoFinal = attackDamage * porcentaje;

    if (currentTarget.TryGetComponent(out IDamageable damageable))
    {
        damageable.TakeDamage(danoFinal);
        Debug.Log($"[{name}] Golpeó a {currentTarget.name} infligiendo {danoFinal} de daño.");
    }
    else if (currentTarget.TryGetComponent(out Sentinel_Controller enemySentinel))
    {
        // Casos especiales como estructuras que quizás no usan la interfaz aún
        enemySentinel.TakeDamage(danoFinal);
    }
}    private void AplicarDanoRaycastSimulado()//debug para simular el ataque básico mientras no tengo los personajes ni las animaciones definitivas, luego se puede reemplazar por la lógica real de daño que quiera implementar.
    {
       
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, transform.forward, out hit, stats.attackRange))
        {
            Debug.Log($"[GOLPE SIMULADO] Impactó a: {hit.collider.name} infligiendo {stats.attackDamage} de daño.");
        }
    }

    private void ResetearEstadoAtaqueSimulado()
{
    if (Time.time >= tiempoSiguienteAtaque)
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