using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using AxionHeroes.Gameplay;

public class HeroController : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator anim;
    private HeroBotAI botAI;

    [Header("Respawn Config")]
    [SerializeField] private float baseRespawnTime = 6f;
    [SerializeField] private float timeIncrementPerLevel = 2f;
    private float currentRespawnDuration = 0f;

    [Header("Experiencia y Nivel")]
    public int nivel = 1;
    public float experienciaActual = 0f;
    public float experienciaParaSiguienteNivel = 100f;
    public float maxLevel = 25f;

    [Header("--- Configuración de Rol e IA ---")]
    public HeroRole miRol = HeroRole.Fighter;
    public bool esBot = false;
    public float tiempoParaAFK = 10f;
    private float tiempoInactivo = 0f;

    [Header("--- Stats del Héroe ---")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;
    private bool isDead = false;

    [Header("Movimiento")]
    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] private VariableJoystick mobileJoystick;

    [Header(" Sistema de Combate (Simulado)")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private float velocidadAtaque = 1f;
    private float tiempoSiguienteAtaque = 0f;
    private bool isAttacking = false;

    [Header("--- Habilidades & Ulti ---")]
    [SerializeField] private float skillCooldown = 5f;
    [SerializeField] private float ultiCooldown = 12f;
    private float nextSkillTime = 0f;
    private float nextUltiTime = 0f;

    public float AttackRange => attackRange;
    public bool IsAttacking => isAttacking;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        botAI = GetComponent<HeroBotAI>();

        currentHealth = maxHealth;

        if (agent != null)
        {
            agent.acceleration = 30f;
            agent.angularSpeed = 1000f;
            agent.speed = movementSpeed;
            agent.stoppingDistance = 0.1f;
        }

        if (esBot && botAI != null)
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

    private void ActivarIA()
    {
        if (botAI == null) botAI = gameObject.AddComponent<HeroBotAI>();
        botAI.miRol = this.miRol;
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
        if (agent != null && agent.enabled)
        {
            if (agent.velocity.magnitude > 0.1f) SetAnimatorFloatSafe("Speed", 1f);
            else SetAnimatorFloatSafe("Speed", 0f);
        }
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
                agent.velocity = movementDirection * movementSpeed;
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

        float healthNormalized = currentHealth / maxHealth;
        SetAnimatorFloatSafe("Health", healthNormalized);
    }

    private void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.Space)) ExecuteAttackBasic();
        if (Input.GetKeyDown(KeyCode.Q) && Time.time >= nextSkillTime) ExecuteSkill();
        if (Input.GetKeyDown(KeyCode.R) && Time.time >= nextUltiTime) ExecuteUltimate();
    }

    public void ExecuteAttackBasic()
    {
        if (isDead || Time.time < tiempoSiguienteAtaque) return;

        isAttacking = true;
        tiempoSiguienteAtaque = Time.time + velocidadAtaque;

        if (agent != null && agent.enabled) agent.ResetPath();

        if (anim != null) anim.SetTrigger("DoAttack");

        AplicarDanoRaycastSimulado();
    }

    private void AplicarDanoRaycastSimulado()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, transform.forward, out hit, attackRange))
        {
            Debug.Log($"[GOLPE SIMULADO] Impactó a: {hit.collider.name} infligiendo {attackDamage} de daño.");
        }
    }

    private void ResetearEstadoAtaqueSimulado()
    {
        if (isAttacking && Time.time >= tiempoSiguienteAtaque - (velocidadAtaque * 0.3f))
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
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (botAI != null && esBot)
        {
            botAI.NotificarDanoRecibido(currentHealth / maxHealth);
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
        currentHealth = maxHealth;
        tiempoInactivo = 0f;

        transform.position = Vector3.zero; // Reemplazar en el futuro con la posición exacta de  Fuente/Base

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

    public void ExecuteSkill() { if (!isDead && Time.time >= nextSkillTime) { nextSkillTime = Time.time + skillCooldown; if (anim != null) anim.SetTrigger("DoSkill"); } }
    public void ExecuteUltimate() { if (!isDead && Time.time >= nextUltiTime) { nextUltiTime = Time.time + ultiCooldown; if (anim != null) anim.SetTrigger("DoUlti"); } }
}