using UnityEngine;
using UnityEngine.AI;

public class Ruk_Controller : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator anim;

    [Header("Combo Settings")]
    public float comboWindow = 1.5f;

    private int currentCombo = 0;
    private float lastClickTime;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();

        // Ajustes PRO del agente (feeling tipo MOBA)
        agent.acceleration = 20f;
        agent.angularSpeed = 720f;
        agent.speed = 5f;
    }

    void Update()
    {
        // 🚫 NO moverse si está atacando
        if (anim.GetCurrentAnimatorStateInfo(0).IsTag("Attack"))
            return;

        // 🎮 Movimiento (WASD para pruebas en PC)
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 move = new Vector3(h, 0, v);

        if (move.magnitude > 0.1f)
        {
            agent.isStopped = false;
            agent.SetDestination(transform.position + move * 2f);
        }

        // 🎬 Animación de movimiento
        anim.SetFloat("Speed", agent.velocity.magnitude);

        // 🔄 Rotación suave tipo MOBA
        if (agent.velocity.magnitude > 0.1f)
        {
            Vector3 dir = agent.velocity.normalized;
            Quaternion rot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * 10f);
        }
    }

    // 🔥 BOTÓN DE ATAQUE
    public void OnAttackButton()
    {
        // Reset combo si se tarda mucho
        if (Time.time - lastClickTime > comboWindow)
        {
            currentCombo = 0;
        }

        currentCombo++;

        if (currentCombo > 3)
            currentCombo = 1;

        lastClickTime = Time.time;

        // 🛑 detener movimiento (pero sin matar la física)
        agent.isStopped = true;

        // 🎬 activar animación
        anim.SetInteger("AttackIndex", currentCombo);
        anim.SetTrigger("BasicAttack");
    }
}