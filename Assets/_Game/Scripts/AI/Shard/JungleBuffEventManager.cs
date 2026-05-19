using System.Collections;
using UnityEngine;

public class JungleBuffEventManager : MonoBehaviour
{
    public static JungleBuffEventManager Instance { get; private set; }

    [Header("Configuración del Evento")]
    public float duracionVentaja = 120f;

    public bool VentajaActiva { get; private set; }
    public string EquipoConVentaja { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void IniciarEventoBuff(string nombreEquipo)
    {
       
                if (!VentajaActiva)
        {
            EquipoConVentaja = nombreEquipo;
            StartCoroutine(CronometroVentaja());
        }
 
    }

    private IEnumerator CronometroVentaja()
    {
        VentajaActiva = true;
        Debug.Log($"<color=cyan>[BUFF ACTIVO]</color> ¡La ventaja de 2 minutos ha comenzado para el equipo: {EquipoConVentaja}!");

        yield return new WaitForSeconds(duracionVentaja);

        VentajaActiva = false;
        EquipoConVentaja = "";
        Debug.Log("<color=red>[BUFF TERMINADO]</color> Los 2 minutos expiraron. Spawns desactivados.");
    }
}