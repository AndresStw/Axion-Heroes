using System.Collections;
using UnityEngine;

public class JungleBossTest : MonoBehaviour
{
    [Header("Configuración de Prueba")]
    [Tooltip("Tiempo en segundos que tardará el dragón en morir automáticamente desde que inicia el juego.")]
    public float tiempoParaMorir = 10f;

    [Tooltip("El equipo que recibirá el buff en esta prueba ('Blue' o 'Red').")]
    public string equipoGanadorPrueba = "Blue";

    void Start()
    {
        StartCoroutine(TemporizadorMuerteSimulada());
    }

    private IEnumerator TemporizadorMuerteSimulada()
    {
        Debug.Log($"[Jungle Boss Test] El dragón está vivo. Morirá automáticamente en {tiempoParaMorir} segundos para activar el evento.");

        yield return new WaitForSeconds(tiempoParaMorir);

        SimularMuerteYDispararEvento();
    }

    private void SimularMuerteYDispararEvento()
    {
        Debug.Log($"[Jungle Boss Test] ¡El dragón ha muerto! Otorgando buff al equipo: {equipoGanadorPrueba}.");

        if (JungleBuffEventManager.Instance != null)
        {
            JungleBuffEventManager.Instance.IniciarEventoBuff(equipoGanadorPrueba);
        }
        else
        {
            Debug.LogError("[Jungle Boss Test] No se encontró el 'JungleBuffEventManager' en la escena. Asegúrate de tenerlo creado en un GameObject.");
        }

        Destroy(gameObject);
    }
 
}