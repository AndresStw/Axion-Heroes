using UnityEngine;

public class ExpHeroe : MonoBehaviour
{
    public int nivel = 1;
    public float experienciaActual = 0f;
    public float experienciaParaSiguienteNivel = 100f;

    public void GanarExperiencia(float cantidad)
    {
        experienciaActual += cantidad;
        while (experienciaActual >= experienciaParaSiguienteNivel)
        {
            SubirNivel();
        }
    }

    private void SubirNivel()
    {
        experienciaActual -= experienciaParaSiguienteNivel;
        nivel++;
        experienciaParaSiguienteNivel *= 1.5f; // Incrementa la experiencia requerida para el siguiente nivel
        Debug.Log("¡Subiste al nivel " + nivel + "!");
    }
}