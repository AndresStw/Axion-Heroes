using UnityEngine;
using System.Collections;

public class Dragon_Spawn : MonoBehaviour
{
    [SerializeField] private GameObject dragonPrefab; // Reference to the dragon prefab
    [SerializeField] private float respawnTime = 120f; // vamos a dejar que este script se encargue del spawn y respawn del dragon, para que el dragon azul tenga su propio script de comportamiento, y el spawn se encargue solo de eso, asi no se mezcla la logica del dragon con la logica del spawn, probar

    private GameObject currentDragon; // aca se guarda la referencia al dragon que esta actualmente en el mapa

    void Start()
    {
        SpawnDragon();
    }

    void SpawnDragon()
    {
        if (currentDragon == null) // Verificar si no hay un dragón actualmente en el mapa
        {
            // Instanciamos al dragón directamente en la posición de este objeto Spawner
            currentDragon = Instantiate(dragonPrefab, transform.position, Quaternion.identity);

            // COMUNICACIÓN: Le pasamos este Spawner y la posición al script del dragón para que funcione el Leashing
            Dragon_Blue dragonScript = currentDragon.GetComponent<Dragon_Blue>();
            if (dragonScript != null)
            {
                dragonScript.AssignSpawner(this, transform);
            }

            Debug.Log("Dragon Azul ha aparecido en el mapa."); // o sonido mas adelante quiero sonido de batalla o alerta , el log es para saber si aparece 
        }
    }

    // Este método es público para que el script del Dragón lo pueda activar al llegar a 0 de vida
    public void OnDragonDeath()
    {
        currentDragon = null; // Liberamos la referencia del dragón viejo
        StartCoroutine(RespawnDragon());
    }

    IEnumerator RespawnDragon()
    {
        yield return new WaitForSeconds(respawnTime);
        SpawnDragon();
        Debug.Log("Dragon Azul ha respawneado.");
    }
}