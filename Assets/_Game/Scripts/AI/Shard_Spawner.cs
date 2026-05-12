using UnityEngine;
using UnityEngine.AI; // Añadimos esto para que reconozca NavMeshAgent más fácil
using System.Collections;
using System.Collections.Generic;

public class Shard_Spawner : MonoBehaviour
{
    [Header("Configuración de Oleada")]
    public GameObject meleePrefab;
    public GameObject casterPrefab;
    public GameObject siegePrefab;
    public GameObject extraPrefab;

    public int shardsPerWave = 4;
    public float timeBetweenShards = 1.5f;
    public float waveInterval = 30f;

    [Header("Ruta de los Shards")]
    public GameObject rutaPadre;

    [Header("Configuración de Equipo y Tags")]
    public bool invertirRuta = false;
    public string teamTag = "BlueTeam";
    public string enemyTag = "RedTeam";
    public string teamLayer = "BlueTeam";

    void Start()
    {
        StartCoroutine(SpawnWaves());
    }

    IEnumerator SpawnWaves()
    {
        while (true)
        {
            for (int i = 0; i < shardsPerWave; i++)
            {
                SpawnShard(i);
                yield return new WaitForSeconds(timeBetweenShards);
            }
            yield return new WaitForSeconds(waveInterval);
        }
    }

    void SpawnShard(int index)
    {
        GameObject prefabAInstanciar = meleePrefab;
        if (index == 1 && casterPrefab != null) prefabAInstanciar = casterPrefab;
        else if (index == 2 && siegePrefab != null) prefabAInstanciar = siegePrefab;
        else if (index == 3 && extraPrefab != null) prefabAInstanciar = extraPrefab;

        if (prefabAInstanciar == null) return;

        GameObject newShard = Instantiate(prefabAInstanciar, transform.position, transform.rotation);

        // CONFIGURAR TAG Y LAYER
        newShard.tag = teamTag;
        newShard.layer = LayerMask.NameToLayer(teamLayer);

        // CORRECCIÓN DE PRIORIDAD (Random con Mayúscula y Coma)
        UnityEngine.AI.NavMeshAgent agent = newShard.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            agent.avoidancePriority = Random.Range(40, 61);
        }

        Shard_Controller controller = newShard.GetComponent<Shard_Controller>();

        if (controller != null)
        {
            controller.enemyTag = enemyTag;

            if (rutaPadre != null)
            {
                List<Vector3> posicionesFijas = new List<Vector3>();
                foreach (Transform child in rutaPadre.transform)
                {
                    posicionesFijas.Add(child.position);
                }

                if (invertirRuta) posicionesFijas.Reverse();
                controller.waypoints = posicionesFijas;
            }
        }
    }
}