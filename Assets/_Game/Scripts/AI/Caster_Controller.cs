using UnityEngine;
using System.Collections;

public class Caster_Controller : Shard_Controller
{
    [Header("Caster Settings")]
    public GameObject projectilePrefab;
    public Transform firePoint;

    protected override void Start()
    {
        base.Start(); // Llama al Start del Shard original
    }

    // Sobrescribimos el ataque para que dispare en vez de golpear
    protected override void ExecuteAttack()
    {
        if (anim != null)
        {
            anim.SetTrigger("Attack");
        }

        StartCoroutine(LanzarHechizo());
    }

    IEnumerator LanzarHechizo()
    {
        yield return new WaitForSeconds(0.3f); // Tiempo para que levante la mano

        if (currentTarget != null && projectilePrefab != null && firePoint != null)
        {
            // Instanciar el proyectil
            GameObject projGO = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

            // Si el proyectil tiene un script de lógica, le pasamos el daño y el objetivo
            // Ejemplo: projGO.GetComponent<Projectile>().Setup(currentTarget, attackDamage);
        }
    }
}