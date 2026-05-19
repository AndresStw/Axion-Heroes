using UnityEngine;

/// <summary>
/// Controla los iconos del mini-mapa para que se vean desde el cielo.
/// Automatiza colores por bando y asegura un sprite por defecto si no le pones uno.
/// </summary>
public class MinimapIcon : MonoBehaviour
{
    public enum TeamType { Azul_Aliado, Rojo_Enemigo, Neutral_Personalizado }

    [Header("Configuración de Equipo")]
    [SerializeField]
    [Tooltip("Asigna el bando de esta entidad para pintar el icono automáticamente.")]
    private TeamType equipo = TeamType.Azul_Aliado;

    [Header("Configuración del Icono")]
    [SerializeField]
    [Tooltip("El componente Sprite Renderer que contiene la imagen del icono.")]
    private SpriteRenderer spriteRenderer;

    [SerializeField]
    [Tooltip("El sprite por defecto (un círculo blanco común) por si se me olvida ponerle un icono personalizado a las torres o dragones.")]
    private Sprite defaultCircleSprite;

    [SerializeField]
    [Tooltip("Altura fija sobre el objeto para evitar que el terreno tape el icono en la cámara cenital.")]
    private float heightOffset = 15f;

    void Start()
    {
        // Forzar que este objeto use la capa exclusiva del mini-mapa para que la cámara principal no lo renderice
        gameObject.layer = LayerMask.NameToLayer("Minimap");

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        // SISTEMA POR DEFECTO: Si no le asigné ningún sprite en el componente, le metemos el círculo base
        if (spriteRenderer != null && spriteRenderer.sprite == null)
        {
            if (defaultCircleSprite != null)
            {
                spriteRenderer.sprite = defaultCircleSprite;
            }
            else
            {
                Debug.LogWarning("Oye bro, acuérdate de asignar el defaultCircleSprite en el Inspector para que funcione el respaldo.");
            }
        }

        // Alinear el transform mirando al cielo al iniciar
        ConfigurarTransform();

        // Pintar según el bando elegido (Azul o Rojo)
        AsignarColorPorEquipo();
    }

    void LateUpdate()
    {
        // Forzar rotación fija hacia el cielo, por si el personaje o la torre rotan en el mapa
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        // Seguir al padre manteniendo la altura para que el mini-mapa no haga cosas raras
        if (transform.parent != null)
        {
            Vector3 newPos = transform.parent.position;
            newPos.y += heightOffset;
            transform.position = newPos;
        }
    }

    private void ConfigurarTransform()
    {
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        Vector3 localPos = Vector3.zero;
        localPos.y = heightOffset;
        transform.localPosition = localPos;
    }

    private void AsignarColorPorEquipo()
    {
        // Swicth para pintar automáticamente el icono según el equipo, si es neutral conserva su color original
        switch (equipo)
        {
            case TeamType.Azul_Aliado:
                SetIconColor(Color.blue);
                break;

            case TeamType.Rojo_Enemigo:
                SetIconColor(Color.red);
                break;

            case TeamType.Neutral_Personalizado:
                // No hace nada, deja el color que ya tenga por defecto el sprite original de la jungla
                break;
        }
    }

    // Método por si el spawner necesita cambiar el bando del bicho en tiempo real
    public void SetTeam(TeamType nuevoEquipo)
    {
        equipo = nuevoEquipo;
        AsignarColorPorEquipo();
    }

    // Método público por si quiero cambiar el icono desde otro script (ej: cuando una torre es destruida y cambia a icono de ruinas)
    public void ChangeIconSprite(Sprite newSprite)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = newSprite;
        }
    }

    // Método para cambiar el color del sprite de forma manual si se necesita, probar
    public void SetIconColor(Color color)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }
    }
}