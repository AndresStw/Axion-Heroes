using UnityEngine;

public class Shard_Projectile : MonoBehaviour
{
    // Este script se encarga de mover el proyectil hacia el objetivo y aplicar daño al impactar
    private GameObject target;//objetivo al que el proyectil se dirigirá
    private int damage;// Daño que el proyectil infligirá al impactar
    private Shard_Controller.Team team;// Equipo del proyectil para evitar dañar aliados
    public float speed = 10f;// Velocidad del proyectil

    // El método Setup se llama al instanciar el proyectil para configurar su objetivo, daño y equipo
    public void Setup(GameObject _target, int _damage, Shard_Controller.Team _team)
    {
        target = _target;
        damage = _damage;
        team = _team;
    }
    
    void Update()
    {
        if (target == null) { Destroy(gameObject); return; }

        transform.position = Vector3.MoveTowards(transform.position, target.transform.position, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target.transform.position) < 0.2f)
        {
            Impacto();
        }
    }

    void Impacto()
    {
        // Aplicamos daño usando tu lógica existente
        if (target.TryGetComponent(out Shard_Controller enemy))
        {
            enemy.TakeDamage(damage);
        }
        Destroy(gameObject);
    }
}