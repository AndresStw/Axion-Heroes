using UnityEngine;

public class Ruk_Combat : MonoBehaviour
{
    [Header("Ajustes de Daño")]
    public int attackDamage = 50;
    public float attackRadius = 5f; 
    public LayerMask enemyLayers;   

    [Header("Punto de Impacto")]
    public Transform attackPoint;   

   
    public void ExecuteHit()
    {
        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, attackRadius, enemyLayers);

        foreach (Collider enemy in hitEnemies)
        {
            if (enemy.TryGetComponent(out Sentinel_Controller sentinel))
            {
                sentinel.TakeDamage(attackDamage);
            }

            if (enemy.TryGetComponent(out Shard_Controller EnemyShard))
            {
                EnemyShard.TakeDamage(attackDamage);
            }
        }

        Debug.Log("Ruk lanzó un ataque de área.");
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
    }
}