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
            ConfigurarColisionesAliadas();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void ConfigurarColisionesAliadas()
    {
        int blueLayer = LayerMask.NameToLayer("BlueTeam");
        int redLayer = LayerMask.NameToLayer("RedTeam");

        if (blueLayer != -1) 
            Physics.IgnoreLayerCollision(blueLayer, blueLayer, true);
        
        if (redLayer != -1) 
            Physics.IgnoreLayerCollision(redLayer, redLayer, true);
            
        Debug.Log("<color=green>[Physics]</color> Colisiones entre aliados desactivadas.");
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