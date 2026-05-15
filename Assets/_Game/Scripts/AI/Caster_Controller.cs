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
        yield return new WaitForSeconds(1f);

        if (currentTarget != null && projectilePrefab != null && firePointL != null && firePointR != null)
        {
            Transform puntoActual = dispararDerecha ? firePointR : firePointL;

            Vector3 direccion = (currentTarget.transform.position - puntoActual.position).normalized;

            GameObject proj = Instantiate(projectilePrefab, puntoActual.position, Quaternion.identity);

            proj.GetComponent<Shard_Projectile>().SetTarget(currentTarget);


            proj.transform.rotation = Quaternion.LookRotation(direccion);

            dispararDerecha = !dispararDerecha;
        }
    }
}