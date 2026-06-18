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
        Retirada,
        Espera,
        RecallBase,
        Rotando,
        HaciendoObjetivos
    }

    public enum DificultadBot { Facil, Normal, Dificil, Pro, Aleatoria }// esto es porque no tengo multiplayer :(

    private EstadoBot estadoActual = EstadoBot.Apagado;
    private DificultadBot dificultadConfigurada = DificultadBot.Aleatoria;
    private float factorAgresividad = 1.0f;

    [Header("Configuración")]
    public HeroRole miRol;
    public Team miEquipo;

    [SerializeField] private float radioDeteccion = 7f;
    [SerializeField] private float distanciaMaximaPersecucion = 12f;
    [SerializeField] private LayerMask capasEnemigas;
    [SerializeField] private float torreSeguridadRadio = 10f;
    [SerializeField] private float umbralVidaBaja = 0.3f;
    [SerializeField] private float umbralFuerzaParaHuir = 0.8f; // Si el enemigo tiene 20% más fuerza, se retira.

    private List<Vector3> misWaypoints = new List<Vector3>();
    private string carrilActual = "mid";

    private NavMeshAgent agent;
    private HeroController controller;
    private HeroRecall recallComponent;
    private Shard_Spawner[] allShardSpawners; // Cache de todos los spawners
    private Dragon_Blue cachedDragon; // Cache del dragón

    private int waypointActualIndex = 0;

    private Transform objetivoActual;

    private float tiempoSiguienteEscaneo = 0f;
    private float intervaloEscaneo = 0.3f;

    private Vector3 puntoRetiradaBase;
    private float tiempoInicioPartida;
    private bool estaEnZonaSegura = true;
    private Collider[] overlapBuffer = new Collider[20]; // Buffer para OverlapSphereNonAlloc

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        controller = GetComponent<HeroController>();
        recallComponent = GetComponent<HeroRecall>();
        tiempoInicioPartida = Time.time;
        allShardSpawners = FindObjectsByType<Shard_Spawner>(FindObjectsSortMode.None); // Cachear spawners
        cachedDragon = FindFirstObjectByType<Dragon_Blue>(); // Cachear el dragón
        
        ConfigurarDificultad();
    }

    public void ActivarBot()
    {
        if (estadoActual != EstadoBot.Apagado)
            return;

        // Determinar carril inicial por rol
        if (miRol == HeroRole.Caster) carrilActual = "mid";
        else if (miRol == HeroRole.Vanguardista) carrilActual = "top";
        else carrilActual = "bot";

        // Asegurarse de que el punto de retirada base esté inicializado antes de buscar rutas
        puntoRetiradaBase = transform.position;

        BuscarRutaYBase();

        if (misWaypoints.Count > 0)
        {
            estadoActual = EstadoBot.Navegando;

            waypointActualIndex = EncontrarWaypointMasCercano();

            if (agent != null && agent.enabled)
                agent.stoppingDistance = 0.1f;

            IniciarNavegacion();
        }
    }

    private void ConfigurarDificultad()
    {
        if (dificultadConfigurada == DificultadBot.Aleatoria)
        {
            factorAgresividad = Random.Range(0.7f, 1.5f);
            intervaloEscaneo = Random.Range(0.1f, 0.5f);
        }
        // A medida que pasa el tiempo, los bots se vuelven más agresivos
        factorAgresividad += (Time.time - tiempoInicioPartida) / 1200f; 
    }

    private int EncontrarWaypointMasCercano()
    {
        int closest = 0;
        float minDist = Mathf.Infinity;
        for (int i = 0; i < misWaypoints.Count; i++)
        {
            float dist = Vector3.Distance(transform.position, misWaypoints[i]);
            if (dist < minDist)
            {
                minDist = dist;
                closest = i;
            }
        }
        return closest;
    }

    public void DesactivarBot()
    {
        estadoActual = EstadoBot.Apagado;

        objetivoActual = null;

        if (controller != null)
            controller.currentTarget = null; // Limpiar el objetivo del controlador

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
        else if (vidaPorcentaje < 0.5f && EvaluarFuerzaEnemiga() > 1.2f)
        {
            estadoActual = EstadoBot.Retirada;
        }
        
        // Si estamos en combate y nos pega una torre, prioridad máxima huir del rango de la torre
        if (CheckTorreEnemigaPegandome()) {
            estadoActual = EstadoBot.Retirada;
        }
    }

    private void BuscarRutaYBase()
    {   
        // Usar el cache de spawners
        Shard_Spawner[] todosLosSpawners = allShardSpawners;

        foreach (Shard_Spawner spawner in todosLosSpawners)
        {
            if (spawner.miEquipo != miEquipo)
                continue;

            string nombre = (spawner.gameObject.name + spawner.transform.parent?.name).ToLower();

            if (nombre.Contains(carrilActual))
            {
                misWaypoints = spawner.ObtenerPuntosDeRuta();

                if (misWaypoints.Count > 0)
                    puntoRetiradaBase = misWaypoints[0]; // El primer waypoint de la ruta es la base de retirada

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
        
        if (estadoActual == EstadoBot.RecallBase) 
            controller.isRecalling = false;
    }

    void Update()
    {
        if (estadoActual == EstadoBot.Apagado)
            return;

        if (agent == null || !agent.enabled)
            return;

        ActualizarEntorno();
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

            case EstadoBot.RecallBase:
                LogicaRecall();
                break;

            case EstadoBot.Rotando:
                LogicaRotacion();
                break;
            
            case EstadoBot.Espera:
                LogicaEspera();
                break;

            case EstadoBot.HaciendoObjetivos:
                LogicaHaciendoObjetivos();
                break;
        }
    }

    private void ActualizarEntorno()
    {
        // Si estamos bajo nuestra torre o lejos de enemigos, estamos seguros
        estaEnZonaSegura = !CheckEnemigosCerca(radioDeteccion * 1.5f);

        // Decisión de Recall
        if (controller.CurrentHealth / controller.stats.maxHealth < umbralVidaBaja && 
            estadoActual != EstadoBot.RecallBase && estaEnZonaSegura)
        {
            estadoActual = EstadoBot.RecallBase;
        }

        // Buscar Objetivos Globales (Dragón) si no hay nada urgente en línea
        if (estadoActual == EstadoBot.Navegando && Time.time % 5f < 0.1f) // Escaneo cada 5 seg
        { // Usar el dragón cacheado
            if (cachedDragon != null && !cachedDragon.IsDead && Vector3.Distance(transform.position, cachedDragon.transform.position) < 25f)
                estadoActual = EstadoBot.HaciendoObjetivos;
        }
    }

    private bool CheckEnemigosCerca(float radio)
    {
        int numColliders = Physics.OverlapSphereNonAlloc(transform.position, radio, overlapBuffer, capasEnemigas);
        for (int i = 0; i < numColliders; i++) {
            if (overlapBuffer[i].TryGetComponent(out HeroController _)) return true; // Solo nos interesan los héroes
        }
        return false;
    }

    private bool CheckTorreEnemigaPegandome()
    {
        int numColliders = Physics.OverlapSphereNonAlloc(transform.position, 12f, overlapBuffer, capasEnemigas);
        for (int i = 0; i < numColliders; i++)
        {
            Collider col = overlapBuffer[i]; // Obtener el collider del buffer
            if (col.name.Contains("Sentinel") || col.name.Contains("Tower"))
            {
                if (!EsSeguroAtacarTorre()) return true; // Si no hay minions, la torre nos pegará a nosotros
            }
        }
        return false;
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
        // Usar OverlapSphereNonAlloc para evitar garbage collection
        int numColliders = Physics.OverlapSphereNonAlloc(transform.position, radioDeteccion, overlapBuffer, capasEnemigas);

        float mejorPuntuacion = -1f;
        Transform objetivoCandidato = null;

        for (int i = 0; i < numColliders; i++) 
        {
            Collider col = overlapBuffer[i];
            if (col == null) continue;
            float puntuacion = 0;
            float distancia = Vector3.Distance(transform.position, col.transform.position);
            
            if (col.TryGetComponent(out HeroController enemyHero))
            {
                if (enemyHero.IsDead) continue;
                
                float fuerzaRelativa = EvaluarFuerzaRelativa(enemyHero);
                if (fuerzaRelativa < umbralFuerzaParaHuir && controller.CurrentHealth / controller.stats.maxHealth < 0.6f)
                {
                    puntuacion = -10f; // Demasiado fuerte, evitar
                }
                else
                {
                    puntuacion = 100f;
                    puntuacion += (1f - (enemyHero.CurrentHealth / enemyHero.stats.maxHealth)) * 80f; // Prioridad Kill
                }
            }
            else if (col.TryGetComponent(out Shard_Controller minion))
            {
                if (minion.IsDead) continue;
                puntuacion = 40f;
                // PRIORIDAD LAST HIT: Mucha puntuación si el minion morirá de un golpe
                if (minion.CurrentHealth <= controller.attackDamage * 1.2f) 
                    puntuacion += 150f; 
            }
            else if (col.TryGetComponent(out Sentinel_Controller tower))
            {
                if (!EsSeguroAtacarTorre()) continue;
                puntuacion = 30f;
            }

            // Penalizar por distancia
            puntuacion -= distancia * 2f;

            if (puntuacion > mejorPuntuacion && puntuacion > 0)
            {
                mejorPuntuacion = puntuacion;
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

    private float EvaluarFuerzaRelativa(HeroController enemigo)
    {
        float miPoder = controller.nivel * (controller.CurrentHealth / controller.stats.maxHealth);
        float suPoder = enemigo.nivel * (enemigo.CurrentHealth / enemigo.stats.maxHealth);
        return miPoder / (suPoder + 0.1f);
    }

    private float EvaluarFuerzaEnemiga()
    {
        // Evalúa si hay muchos aliados vs muchos enemigos cerca
        int numAllies = 0;
        int numEnemies = 0;
        int numColliders = Physics.OverlapSphereNonAlloc(transform.position, radioDeteccion, overlapBuffer);
        for (int i = 0; i < numColliders; i++) {
            if (overlapBuffer[i].TryGetComponent(out HeroController hero)) {
                if (hero.myTeam == miEquipo) numAllies++;
                else numEnemies++;
            }
        }
        return (float)numEnemies / (numAllies + 1); // Si hay más enemigos que aliados, el valor será > 1
    }

    private int ContarEnemigosCerca(float radio)
    {
        int numColliders = Physics.OverlapSphereNonAlloc(transform.position, radio, overlapBuffer, capasEnemigas);
        int enemyHeroCount = 0;
        for (int i = 0; i < numColliders; i++) {
            if (overlapBuffer[i].TryGetComponent(out HeroController enemyHero)) enemyHeroCount++;
        }
        return enemyHeroCount;
    }

    private bool EsSeguroAtacarTorre()
    {
        // Buscar Shards aliados cerca de la posición del bot para que ellos reciban el agro
        int numColliders = Physics.OverlapSphereNonAlloc(transform.position, torreSeguridadRadio, overlapBuffer);
        for (int i = 0; i < numColliders; i++)
        {
            Collider col = overlapBuffer[i];
            if (col.TryGetComponent(out Shard_Controller shard))
            {
                if (shard.myTeam == miEquipo && !shard.IsDead)
                    return true;
            }
        }
        return false;
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

    Vector3 posPropia = new Vector3(transform.position.x, 0, transform.position.z);
    Vector3 posObjetivo = new Vector3(objetivoActual.position.x, 0, objetivoActual.position.z);
    float distanciaObjetivo = Vector3.Distance(posPropia, posObjetivo);

    float rangoEfectivo = controller.AttackRange;

    // Fuera de rango  perseguir
    if (distanciaObjetivo > rangoEfectivo)
    {
        agent.stoppingDistance =
            Mathf.Max(1f, rangoEfectivo * 0.8f);

        agent.SetDestination(
            objetivoActual.position
        );

        // Si somos Caster/Artillero y el enemigo se acerca demasiado, intentar mantener distancia
        if ((miRol == HeroRole.Caster || miRol == HeroRole.Artillero) && distanciaObjetivo < 4f)
        {
            Vector3 dirHuida = (transform.position - objetivoActual.position).normalized;
            agent.SetDestination(transform.position + dirHuida * 3f);
        }
    }
    else
    {
        // Si el ataque está en cooldown, nos movemos un poco para reposicionarnos ORB-WALKING
        // Inteligencia al animal
        if (controller.IsAttacking) 
        {
            if (agent.hasPath) agent.ResetPath();
            agent.velocity = Vector3.zero;
        }
        else 
        {
            // Micro-ajuste de posición hacia el objetivo o lateralmente
            Vector3 stepSide = transform.right * (Random.value > 0.5f ? 1 : -1) * 1.5f;
            agent.SetDestination(transform.position + stepSide);
        }

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

        controller.currentTarget = objetivoActual;

        if (!controller.IsAttacking)
        {
            // Uso de habilidades si el enemigo está bajo de vida
            if (objetivoActual.TryGetComponent(out IDamageable d) && (d.CurrentHealth / d.MaxHealth) < 0.4f)
            {
                controller.ExecuteSkill();
            }
            
            controller.ExecuteAttackBasic();
        }
    }

    // Si hay más de 2 enemigos y estoy solo, retirarse inmediatamente (Gank detection)
    if (ContarEnemigosCerca(radioDeteccion) >= 2)
    {
        estadoActual = EstadoBot.Retirada;
    }

    // Si el enemigo huye a su torre y no es una kill segura,se regresa obvio , perooooo esto deberia varias de la dificultad seleccionada
    if (Vector3.Distance(objetivoActual.position, transform.position) > distanciaMaximaPersecucion)
    {
        estadoActual = EstadoBot.Navegando;
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
            if (controller.CurrentHealth / controller.stats.maxHealth < 0.5f)
                estadoActual = EstadoBot.RecallBase;
            else
                estadoActual = EstadoBot.Navegando;
        }
    }

    private void LogicaRecall()
    {
        if (agent.hasPath) agent.ResetPath();
        
        if (!controller.isRecalling && !controller.IsDead)
        {
            if (recallComponent != null) recallComponent.StartRecall();
        }

        // Si aparece un enemigo mientras recallea en zona no segura, cancelar
        if (CheckEnemigosCerca(radioDeteccion))
        {
            controller.isRecalling = false;
            estadoActual = EstadoBot.Retirada;
        }
    }

    private void LogicaRotacion()
    {
        // Cambiar de carril si el actual es muy difícil o no hay objetivos
        string[] carriles = { "top", "mid", "bot" };
        carrilActual = carriles[Random.Range(0, carriles.Length)];
        BuscarRutaYBase();
        waypointActualIndex = EncontrarWaypointMasCercano();
        estadoActual = EstadoBot.Navegando;
        IniciarNavegacion();
    }

    private void LogicaHaciendoObjetivos()
    {
        if (cachedDragon == null || cachedDragon.IsDead) // Usar el dragón 
        {
            estadoActual = EstadoBot.Navegando;
            return;
        }

        float dist = Vector3.Distance(transform.position, cachedDragon.transform.position);
        if (dist > controller.AttackRange) // Moverse hacia el dragón
        { // Moverse hacia el dragón
            agent.SetDestination(cachedDragon.transform.position);
        }
        else
        {
            if (agent.hasPath) agent.ResetPath();
            transform.LookAt(cachedDragon.transform.position);
            controller.currentTarget = cachedDragon.transform;
            controller.ExecuteAttackBasic();
        }

        if (CheckEnemigosCerca(radioDeteccion)) estadoActual = EstadoBot.Combate;
    }

    private void LogicaEspera()
    {
        // Esperar en arbustos o puntos estratégicos (Simulado) el mapa puede cambiar 
        if (CheckEnemigosCerca(radioDeteccion)) 
            estadoActual = EstadoBot.Combate;
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