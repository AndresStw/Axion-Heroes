using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using AxionHeroes.Gameplay;

public class HeroBotAI : MonoBehaviour
{
    private enum EstadoBot { Apagado, Navegando, Combate, Retirada }
    private EstadoBot estadoActual = EstadoBot.Apagado;

    public HeroRole miRol;
    public Team miEquipo;

    [SerializeField] private float radioDeteccion = 7f;
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
        BuscarRutaYBase();

        if (misWaypoints.Count > 0)
        {
            estadoActual = EstadoBot.Navegando;
            waypointActualIndex = 0;
            if (agent != null && agent.enabled) agent.stoppingDistance = 0.1f;
            IniciarNavegacion();
        }
    }

    public void DesactivarBot()
    {
        estadoActual = EstadoBot.Apagado;
        objetivoActual = null;
    }

    public void NotificarDanoRecibido(float vidaPorcentaje)
    {
        if (estadoActual != EstadoBot.Apagado && vidaPorcentaje < 0.25f)
        {
            estadoActual = EstadoBot.Retirada;
        }
    }

    private void BuscarRutaYBase()
    {
        Shard_Spawner[] todosLosSpawners = FindObjectsByType<Shard_Spawner>(FindObjectsSortMode.None);
        puntoRetiradaBase = transform.position;

        foreach (Shard_Spawner spawner in todosLosSpawners)
        {
            if (spawner.miEquipo == this.miEquipo)
            {
                string nombrePropio = spawner.gameObject.name.ToLower();
                string nombrePadre = spawner.transform.parent != null ? spawner.transform.parent.name.ToLower() : "";

                bool esMid = nombrePropio.Contains("mid") || nombrePadre.Contains("mid");
                bool esTop = nombrePropio.Contains("top") || nombrePadre.Contains("top");
                bool esBotLane = nombrePropio.Contains("bot") || nombrePadre.Contains("bot");

                if ((miRol == HeroRole.Caster && esMid) ||
                    (miRol == HeroRole.Vanguardista && esTop) ||
                    ((miRol == HeroRole. Artillero || miRol == HeroRole.Operador) && esBotLane))
                {
                    misWaypoints = spawner.ObtenerPuntosDeRuta();
                    if (misWaypoints.Count > 0) puntoRetiradaBase = misWaypoints[0];
                    break;
                }
            }
        }
    }

    void IniciarNavegacion()
    {
        if (agent == null || !agent.enabled || misWaypoints.Count == 0) return;
        agent.SetDestination(misWaypoints[waypointActualIndex]);
    }

    void Update()
    {
        if (estadoActual == EstadoBot.Apagado || agent == null || !agent.enabled) return;

        FrecuenciaEscaneoEnemigos();

        switch (estadoActual)
        {
            case EstadoBot.Navegando:
                LógicaNavegacion();
                break;
            case EstadoBot.Combate:
                LógicaCombate();
                break;
            case EstadoBot.Retirada:
                LógicaRetirada();
                break;
        }
    }

    private void FrecuenciaEscaneoEnemigos()
    {
        if (estadoActual == EstadoBot.Retirada) return;

        if (Time.time >= tiempoSiguienteEscaneo)
        {
            tiempoSiguienteEscaneo = Time.time + intervaloEscaneo;
            BuscarObjetivoMasCercano();
        }
    }

    private void BuscarObjetivoMasCercano()
    {
        Collider[] enemigos = Physics.OverlapSphere(transform.position, radioDeteccion, capasEnemigas);
        float distanciaMasCercana = Mathf.Infinity;
        Transform objetivoCandidato = null;

        foreach (Collider col in enemigos)
        {
            float dist = Vector3.Distance(transform.position, col.transform.position);
            if (dist < distanciaMasCercana)
            {
                distanciaMasCercana = dist;
                objetivoCandidato = col.transform;
            }
        }

        if (objetivoCandidato != null)
        {
            objetivoActual = objetivoCandidato;
            estadoActual = EstadoBot.Combate;
        }
        else if (estadoActual == EstadoBot.Combate)
        {
            objetivoActual = null;
            estadoActual = EstadoBot.Navegando;
            IniciarNavegacion();
        }
    }

    private void LógicaNavegacion()
    {
        if (misWaypoints.Count == 0) return;

        float distanciaWaypoint = Vector3.Distance(transform.position, misWaypoints[waypointActualIndex]);

        if (distanciaWaypoint < 2.0f)
        {
            agent.acceleration = 15f;
        }
        else
        {
            agent.acceleration = 30f;
        }

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.3f)
        {
            if (waypointActualIndex < misWaypoints.Count - 1)
            {
                waypointActualIndex++;
                agent.SetDestination(misWaypoints[waypointActualIndex]);
            }
            else
            {
                estadoActual = EstadoBot.Apagado;
            }
        }
    }

    private void LógicaCombate()
    {
        if (objetivoActual == null)
        {
            estadoActual = EstadoBot.Navegando;
            IniciarNavegacion();
            return;
        }

        float distanciaAlObjetivo = Vector3.Distance(transform.position, objetivoActual.position);

        if (distanciaAlObjetivo <= controller.AttackRange)
        {
            if (agent.hasPath) agent.ResetPath();

            Vector3 dir = (objetivoActual.position - transform.position).normalized;
            dir.y = 0f;
            if (dir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 15f);
            }

            if (!controller.IsAttacking)
            {
                controller.ExecuteAttackBasic();
            }
        }
        else
        {
            agent.SetDestination(objetivoActual.position);
        }
    }

    private void LógicaRetirada()
    {
        if (agent.destination != puntoRetiradaBase)
        {
            agent.SetDestination(puntoRetiradaBase);
            agent.stoppingDistance = 0.5f;
        }

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            estadoActual = EstadoBot.Navegando;
            waypointActualIndex = 0;
            agent.stoppingDistance = 0.1f;
            IniciarNavegacion();
        }
    }
}