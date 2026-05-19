using System.Collections;
using UnityEngine;

public class MidGankSpawner : MonoBehaviour
{
    [Header("Configuración del Equipo")]
    [Tooltip("Definir si este spawn pertenece al equipo Red o Blue.")]
    public string equipoDeEsteSpawn = "Blue";

    [Header("Configuración del Tiempo (Sincronizado con Oleadas)")]
    [Tooltip("Cada cuántos segundos sale una oleada normal en tu juego.")]
    public float tiempoEntreOleadas = 30f;

    [Tooltip("Tiempo de retraso antes de que salga la primera oleada al iniciar la partida.")]
    public float retrasoPrimerSpawn = 5f;

    [Header("Configuración de la Fila (Gank)")]
    [Tooltip("Segundos de espera entre la aparición de cada minion para avanzar en fila india (Ej: 1.5).")]
    public float retrasoEntreMinions = 1.5f;

    [Header("Referencias de Spawn (Multi-Prefab)")]
    [Tooltip("Arrastra aquí tus 3 prefabs en orden de salida: Melee 1, Melee 2, Siege.")]
    public GameObject[] prefabsGankers;

    [Header("Ruta de Navegación")]
    [Tooltip("Lista de Waypoints que llevan directo al Sentinel enemigo.")]
    public Transform[] waypointsJungle;

    void Start()
    {
        StartCoroutine(BucleRelojDeOleadas());
    }

    private IEnumerator BucleRelojDeOleadas()
    {
        yield return new WaitForSeconds(retrasoPrimerSpawn);

        while (true)
        {
            VerificarYSpawnearGankers();
            yield return new WaitForSeconds(tiempoEntreOleadas);
        }
    }

    private void VerificarYSpawnearGankers()
    {
        if (JungleBuffEventManager.Instance != null)
        {
            if (JungleBuffEventManager.Instance.VentajaActiva)
            {
                if (JungleBuffEventManager.Instance.EquipoConVentaja == equipoDeEsteSpawn)
                {
                    StartCoroutine(InstanciarOleadaEnFila());
                }
                else
                {
                    Debug.Log($"[Mid Spawner] Oleada ignorada: El buff lo tiene el equipo enemigo.");
                }
            }
        }
        else
        {
            Debug.LogError("[Mid Spawner] ERROR CRÍTICO: No se encuentra 'JungleBuffEventManager'.");
        }
    }

    private IEnumerator InstanciarOleadaEnFila()
    {
        if (prefabsGankers == null || prefabsGankers.Length == 0)
        {
            Debug.LogWarning($"[Mid Spawner] No hay prefabs asignados en el spawner de gankeo.");
            yield break;
        }

        for (int i = 0; i < prefabsGankers.Length; i++)
        {
            GameObject prefabActual = prefabsGankers[i];

            if (prefabActual != null)
            {
                 GameObject ganker = Instantiate(prefabActual, transform.position, transform.rotation);

                ShardJunglePathfinder pathfinder = ganker.GetComponent<ShardJunglePathfinder>();
                if (pathfinder == null)
                {
                    pathfinder = ganker.AddComponent<ShardJunglePathfinder>();
                }
           
                pathfinder.AsignarRutaJungle(waypointsJungle);

                Debug.Log($"[Mid Spawner] {prefabActual.name} enviado (Unidad {i + 1}/{prefabsGankers.Length}).");
            }

            if (i < prefabsGankers.Length - 1)
            {
                yield return new WaitForSeconds(retrasoEntreMinions);
            }
        }

        Debug.Log($"<color=green>[SPAWN COMPLETO]</color> Los Shards avanzan correctamente en fila por la jungla.");
    }
}