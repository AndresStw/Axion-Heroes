using UnityEngine;
using System.Collections;

public class Caster_Controller : Shard_Controller
{
    
    public GameObject projectilePrefab;
    public Transform firePointR;
    public Transform firePointL;
    private bool dispararDerecha = true;

    protected override void Start()
    {
        base.Start(); 
    }

    
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
        // Esperar un porcentaje del tiempo de ataque para sincronizar con la animación de lanzamiento
        yield return new WaitForSeconds((1f / attackSpeed) * 0.3f); 

        if (currentTarget != null && projectilePrefab != null && firePointL != null && firePointR != null && !IsDead)
        {
            Transform puntoActual = dispararDerecha ? firePointR : firePointL;

            Vector3 direccion = (currentTarget.transform.position - puntoActual.position).normalized;

            GameObject proj = Instantiate(projectilePrefab, puntoActual.position, Quaternion.identity);
            Shard_Projectile projectileScript = proj.GetComponent<Shard_Projectile>();

            if (projectileScript != null) projectileScript.SetTarget(currentTarget, attackDamage); // Pasar el daño del shard


            proj.transform.rotation = Quaternion.LookRotation(direccion);

            dispararDerecha = !dispararDerecha;
        }
    }
}