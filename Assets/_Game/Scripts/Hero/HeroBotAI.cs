using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using AxionHeroes.Gameplay;

public class HeroBotAI : MonoBehaviour
{
    private enum EstadoBot
    {
        Apagado,
        Navegando,
        Combate,
        Retirada
    }

    private EstadoBot estadoActual = EstadoBot.Apagado;

    [Header("Configuración")]
    public HeroRole miRol;
    public Team miEquipo;

    [SerializeField] private float radioDeteccion = 7f;
    [SerializeField] private float distanciaMaximaPersecucion = 12f;
    [SerializeField] private LayerMask capasEnemigas;

    private List<Vector3> misWaypoints = new List<Vector3>();

    private NavMeshAgent agent;
    private HeroController controller;

    private int waypointActualIndex = 0;

    private Transform objetivoActual;

    private float tiempoSiguienteEscaneo = 0f;
    private float intervaloEscaneo = 0.3f;

    private Vector3 puntoRetiradaBase;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        controller = GetComponent<HeroController>();
    }

    public void ActivarBot()
    {
        if (estadoActual != EstadoBot.Apagado)
            return;

        BuscarRutaYBase();

        if (misWaypoints.Count > 0)
        {
            estadoActual = EstadoBot.Navegando;

            waypointActualIndex = 0;

            if (agent != null && agent.enabled)
                agent.stoppingDistance = 0.1f;

            IniciarNavegacion();
        }
    }

    public void DesactivarBot()
    {
        estadoActual = EstadoBot.Apagado;

        objetivoActual = null;

        if (controller != null)
            controller.currentTarget = null;

        if (agent != null && agent.enabled)
            agent.ResetPath();
    }

    public void NotificarDanoRecibido(float vidaPorcentaje)
    {
        if (estadoActual == EstadoBot.Apagado)
            return;

        if (vidaPorcentaje < 0.25f)
        {
            estadoActual = EstadoBot.Retirada;
        }
    }

    private void BuscarRutaYBase()
    {
        Shard_Spawner[] todosLosSpawners =
            FindObjectsByType<Shard_Spawner>(FindObjectsSortMode.None);

        puntoRetiradaBase = transform.position;

        foreach (Shard_Spawner spawner in todosLosSpawners)
        {
            if (spawner.miEquipo != miEquipo)
                continue;

            string nombrePropio =
                spawner.gameObject.name.ToLower();

            string nombrePadre =
                spawner.transform.parent != null
                ? spawner.transform.parent.name.ToLower()
                : "";

            bool esMid =
                nombrePropio.Contains("mid") ||
                nombrePadre.Contains("mid");

            bool esTop =
                nombrePropio.Contains("top") ||
                nombrePadre.Contains("top");

            bool esBotLane =
                nombrePropio.Contains("bot") ||
                nombrePadre.Contains("bot");

            if ((miRol == HeroRole.Caster && esMid) ||
                (miRol == HeroRole.Vanguardista && esTop) ||
                ((miRol == HeroRole.Artillero ||
                  miRol == HeroRole.Operador) && esBotLane))
            {
                misWaypoints = spawner.ObtenerPuntosDeRuta();

                if (misWaypoints.Count > 0)
                    puntoRetiradaBase = misWaypoints[0];

                break;
            }
        }
    }

    private void IniciarNavegacion()
    {
        if (agent == null ||
            !agent.enabled ||
            misWaypoints.Count == 0)
            return;

        agent.SetDestination(
            misWaypoints[waypointActualIndex]
        );
    }

    void Update()
    {
        if (estadoActual == EstadoBot.Apagado)
            return;

        if (agent == null || !agent.enabled)
            return;

        FrecuenciaEscaneoEnemigos();

        switch (estadoActual)
        {
            case EstadoBot.Navegando:
                LogicaNavegacion();
                break;

            case EstadoBot.Combate:
                LogicaCombate();
                break;

            case EstadoBot.Retirada:
                LogicaRetirada();
                break;
        }
    }

    private void FrecuenciaEscaneoEnemigos()
    {
        if (estadoActual == EstadoBot.Retirada)
            return;

        if (Time.time >= tiempoSiguienteEscaneo)
        {
            tiempoSiguienteEscaneo =
                Time.time + intervaloEscaneo;

            BuscarObjetivoMasCercano();
        }
    }

    private void BuscarObjetivoMasCercano()
    {
        Collider[] enemigos =
            Physics.OverlapSphere(
                transform.position,
                radioDeteccion,
                capasEnemigas
            );

        float distanciaMasCercana = Mathf.Infinity;
        Transform objetivoCandidato = null;
        int prioridadMayor = -1;

        foreach (Collider col in enemigos)
        {
            Debug.Log(
    name +
    " detectó -> " +
    col.name +
    " Layer:" +
    LayerMask.LayerToName(col.gameObject.layer)
);
            if (col == null)
                continue;

            float distancia =
                Vector3.Distance(
                    transform.position,
                    col.transform.position
                );

            int prioridad = 0;

            if (col.GetComponent<HeroController>())
                prioridad = 3;
            else if (col.GetComponent<Shard_Controller>())
                prioridad = 2;
            else if (col.GetComponent<Sentinel_Controller>())
                prioridad = 1;

            if (prioridad > prioridadMayor)
            {
                prioridadMayor = prioridad;
                distanciaMasCercana = distancia;
                objetivoCandidato = col.transform;
            }
            else if (
                prioridad == prioridadMayor &&
                distancia < distanciaMasCercana
            )
            {
                distanciaMasCercana = distancia;
                objetivoCandidato = col.transform;
            }
        }

        if (objetivoCandidato != null)
        {
            objetivoActual = objetivoCandidato;

            if (controller != null)
                controller.currentTarget = objetivoCandidato;

            estadoActual = EstadoBot.Combate;
        }
        else if (estadoActual == EstadoBot.Combate)
        {
            objetivoActual = null;

            if (controller != null)
                controller.currentTarget = null;

            estadoActual = EstadoBot.Navegando;

            IniciarNavegacion();
            
        }
    }

    private void LogicaNavegacion()
    {
        if (misWaypoints.Count == 0)
            return;

        if (!agent.pathPending &&
            agent.remainingDistance <=
            agent.stoppingDistance + 0.3f)
        {
            if (waypointActualIndex <
                misWaypoints.Count - 1)
            {
                waypointActualIndex++;

                agent.SetDestination(
                    misWaypoints[waypointActualIndex]
                );
            }
        }
    }

    private void LogicaCombate()
{
    
    if (objetivoActual == null)
    {
        controller.currentTarget = null;

        estadoActual = EstadoBot.Navegando;
        IniciarNavegacion();
        return;
    }

    float distanciaObjetivo =
        Vector3.Distance(
            transform.position,
            objetivoActual.position
        );

    // Fuera de rango → perseguir
    if (distanciaObjetivo > controller.AttackRange)
    {
        agent.stoppingDistance =
            Mathf.Max(1f, controller.AttackRange * 0.8f);

        agent.SetDestination(
            objetivoActual.position
        );

        return;
    }

    // Dentro de rango → detenerse
    if (agent.hasPath)
        agent.ResetPath();

    agent.velocity = Vector3.zero;

    Vector3 dir =
        (objetivoActual.position -
         transform.position).normalized;

    dir.y = 0f;

    if (dir != Vector3.zero)
    {
        Quaternion rotObjetivo =
            Quaternion.LookRotation(dir);

        transform.rotation =
            Quaternion.RotateTowards(
                transform.rotation,
                rotObjetivo,
                720f * Time.deltaTime
            );
    }
    Debug.Log(
    "Distancia: " +
    distanciaObjetivo +
    " | Rango: " +
    controller.AttackRange
);

    controller.currentTarget = objetivoActual;

    if (!controller.IsAttacking)
    {
        Debug.Log(
            name +
            " -> Ejecutando ataque contra: " +
            objetivoActual.name
        );

        controller.ExecuteAttackBasic();
    }
}

    private void LogicaRetirada()
    {
        if (agent.destination != puntoRetiradaBase)
        {
            agent.SetDestination(puntoRetiradaBase);
            agent.stoppingDistance = 0.5f;
        }

        if (!agent.pathPending &&
            agent.remainingDistance <=
            agent.stoppingDistance)
        {
            estadoActual = EstadoBot.Navegando;

            waypointActualIndex = 0;

            agent.stoppingDistance = 0.1f;

            IniciarNavegacion();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            radioDeteccion
        );
    }
}