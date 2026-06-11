using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class ShardJunglePathfinder : MonoBehaviour
{
    private NavMeshAgent agent;
    private Shard_Controller baseController;
    private Transform[] rutaWaypoints;
    private int indiceActual = 0;
    private bool siguiendoRutaJungle = false;
    private bool enAsaltoFinal = false;
    private GameObject objetivoEstructuraFinal;

    [Header("Configuración de Velocidad Especial")]
    public float velocidadGanker = 10f;

    [Header("Ajuste de Balanceo")]
    public bool seDistraeConJugadores = true;
    public string tagJugadorEnemigo = "Player";

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        baseController = GetComponent<Shard_Controller>();
        enabled = false;
    }

    public void AsignarRutaJungle(Transform[] waypoints)
    {
        if (waypoints == null || waypoints.Length == 0) return;

        rutaWaypoints = waypoints;
        indiceActual = 0;
        siguiendoRutaJungle = true;
        enAsaltoFinal = false;
        objetivoEstructuraFinal = null;
        enabled = true;

        StartCoroutine(IniciarRutaSegura());
    }

    private IEnumerator IniciarRutaSegura()
    {
        yield return null;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.speed = velocidadGanker;

            if (baseController != null)
            {
                baseController.StopAllCoroutines();
            }

            IrAlSiguienteWaypoint();
        }
    }

    void Update()
    {
        if ((!siguiendoRutaJungle && !enAsaltoFinal) || agent == null || !agent.isOnNavMesh) return;

        if (baseController != null)
        {
            GameObject targetActual = baseController.Target;
            bool esObjetivoValido = false;

            if (targetActual != null)
            {
                if (targetActual.name.Contains("Sentinel") || targetActual.name.Contains("Core") || targetActual.name.Contains("Torre"))
                {
                    esObjetivoValido = true;
                    if (enAsaltoFinal) objetivoEstructuraFinal = targetActual;
                }
                else if (!enAsaltoFinal && seDistraeConJugadores && targetActual.CompareTag(tagJugadorEnemigo))
                {
                    esObjetivoValido = true;
                    agent.stoppingDistance = baseController.attackRange;
                }

                if (!esObjetivoValido)
                {
                    if (enAsaltoFinal)
                    {
                        if (objetivoEstructuraFinal == null) ForzarBusquedaDeEstructuraFinal();
                        baseController.SetTarget(objetivoEstructuraFinal);
                    }
                    else baseController.SetTarget(null);
                    agent.stoppingDistance = enAsaltoFinal ? baseController.attackRange : 0f;
                }
            }
            else if (enAsaltoFinal)
            {
                if (objetivoEstructuraFinal == null) ForzarBusquedaDeEstructuraFinal();
                baseController.SetTarget(objetivoEstructuraFinal);
            }
        }

        if (siguiendoRutaJungle)
        {
            if (rutaWaypoints != null && indiceActual < rutaWaypoints.Length)
            {
                agent.SetDestination(rutaWaypoints[indiceActual].position);
            }

            if (!agent.pathPending && agent.remainingDistance <= waypointThresholdOpcional())
            {
                indiceActual++;
                IrAlSiguienteWaypoint();
            }
        }
        else if (enAsaltoFinal)
        {
            if (objetivoEstructuraFinal != null)
            {
                agent.SetDestination(objetivoEstructuraFinal.transform.position);
            }
            else
            {
                ForzarBusquedaDeEstructuraFinal();
            }
        }

        if (agent.speed != velocidadGanker)
        {
            agent.speed = velocidadGanker;
        }
    }

    protected  virtual float waypointThresholdOpcional()
    {
        return baseController != null ? baseController.waypointThreshold : 1.5f;
    }

    private void IrAlSiguienteWaypoint()
    {
        if (indiceActual < rutaWaypoints.Length)
        {
            agent.SetDestination(rutaWaypoints[indiceActual].position);
        }
        else
        {
            siguiendoRutaJungle = false;
            enAsaltoFinal = true;

            if (baseController != null)
            {
                agent.stoppingDistance = baseController.attackRange;
            }

            ForzarBusquedaDeEstructuraFinal();
        }
    }

    private void ForzarBusquedaDeEstructuraFinal()
    {
        GameObject coreEnemigo = GameObject.Find("Core_Enemigo") ?? GameObject.FindGameObjectWithTag("Core") ?? GameObject.Find("Sentinel");

        if (coreEnemigo != null)
            objetivoEstructuraFinal = coreEnemigo;

        if (objetivoEstructuraFinal != null)
        {
            agent.SetDestination(objetivoEstructuraFinal.transform.position);
        }
        else if (rutaWaypoints != null && rutaWaypoints.Length > 0)
        {
            agent.SetDestination(rutaWaypoints[rutaWaypoints.Length - 1].position);
        }
    }
}