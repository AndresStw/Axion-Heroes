using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Ruk_Hero_Controller : MonoBehaviour
{
    [Header("vida")]
    public float maxHealth = 1000f;
    public float currentHealth;

    [Header("Configuración de Movimiento")]
    public LayerMask groundLayer;
    public float moveSpeed = 5f;

    [Header("Referencias")]
    private NavMeshAgent agent;
    private Animator anim;

    [Header("Idle")]
    public float idleSpecialDelay = 10f;
    private float idleTimer;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();

        currentHealth = maxHealth;

        agent.radius = 3.5f;
        agent.speed = moveSpeed;
        agent.acceleration = 20f;
        agent.angularSpeed = 600f; 
    }

    void Update()
    {
        HandleMovementInput();
        UpdateAnimations();
    }

    
    private void HandleMovementInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer))
            {
                agent.SetDestination(hit.point);
                idleTimer = 0; 
            }
        }
    }

    private void UpdateAnimations()
    {
        float currentSpeed = agent.velocity.magnitude;
        anim.SetFloat("Speed", currentSpeed);

        if (currentSpeed < 0.1f)
        {
            idleTimer += Time.deltaTime;
            if (idleTimer >= idleSpecialDelay)
                anim.SetInteger("IdleType", 1);
            else
                anim.SetInteger("IdleType", 0);
        }
        else
        {
            idleTimer = 0;
            anim.SetInteger("IdleType", 0);
        }
    }


    public void ExecuteBasicAttack(int attackID)
    {
        agent.isStopped = true; 
        anim.SetInteger("AttackIndex", attackID);
        anim.SetTrigger("BasicAttack");
    }

    // Ultis (ID: 1, 2, 3)
    public void ExecuteUltimate(int ultiID)
    {
        agent.isStopped = true;
        anim.SetInteger("UltiIndex", ultiID);
        anim.SetTrigger("Ultimate");
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        Debug.Log(gameObject.name + " recibió daño. Vida actual: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        anim.SetTrigger("Die");
        agent.isStopped = true;
        this.enabled = false; 
    }
}