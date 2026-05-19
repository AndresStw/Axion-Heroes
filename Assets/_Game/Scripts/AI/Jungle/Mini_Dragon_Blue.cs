using UnityEngine;

public class MiniDragon_Blue : Dragon_Blue
{
    [Header("Mini Dragon Stats")]
    [SerializeField] private float miniHealth = 1200f;
    [SerializeField] private float miniDamage = 60f;
    [SerializeField] private float miniMoveSpeed = 4f;
    [SerializeField] private float miniAttackSpeed = 1.5f;

    protected override void Start()
    {
        // Aplicamos stats del mini dragon
        health = miniHealth;
        maxHealth = health;

        damage = miniDamage;

        moveSpeed = miniMoveSpeed;
        attackSpeed = miniAttackSpeed;

        // Ejecutamos la lógica base
        base.Start();

        // Actualizar velocidad del NavMesh
        if (agent != null)
        {
            agent.speed = moveSpeed;
        }

        // Actualizar velocidad de animaciones
        if (animator != null)
        {
            animator.speed = attackSpeed;
        }

        Debug.Log("Mini Dragon Azul inicializado.");
    }

    protected override void Attack()
    {
        base.Attack();

        // efectos especiales futuros
        Debug.Log("Mini Dragon ataca rápido.");
    }

    protected override void Die()
    {
        Debug.Log("Mini Dragon Azul derrotado.");

        base.Die();
    }
}