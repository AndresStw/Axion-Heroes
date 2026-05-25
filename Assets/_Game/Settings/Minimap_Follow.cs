using UnityEngine;

public class Minimap_Follow : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField]
    private Transform player;

    [Header("Límites del Mapa (Bordes)")]
    [SerializeField]
    private float minX = -50f;

    [SerializeField]
    private float maxX = 50f;

    [SerializeField]
    private float minZ = -50f;

    [SerializeField]
    private float maxZ = 50f;

    [Header("Controles de Prueba (Modo sin Héroe)")]
    [SerializeField]
    private float pruebaSpeed = 20f;

    [SerializeField]
    private KeyCode recenterKey = KeyCode.Space;

    private Vector3 currentPos;

    void Start()
    {
        currentPos = transform.position;
    }

    void LateUpdate()
    {
        if (player != null)
        {
            Vector3 targetPosition = player.position;

            if (Input.GetKey(recenterKey))
            {
                CentrarEnHeroe();
                return;
            }

            float clampedX = Mathf.Clamp(targetPosition.x, minX, maxX);
            float clampedZ = Mathf.Clamp(targetPosition.z, minZ, maxZ);

            transform.position = new Vector3(clampedX, transform.position.y, clampedZ);
        }
        else
        {
            float moveX = Input.GetAxis("Horizontal"); // A/D o Flechas Izquierda/Derecha
            float moveZ = Input.GetAxis("Vertical");   // W/S o Flechas Arriba/Abajo

            currentPos.x += moveX * pruebaSpeed * Time.deltaTime;
            currentPos.z += moveZ * pruebaSpeed * Time.deltaTime;

            currentPos.x = Mathf.Clamp(currentPos.x, minX, maxX);
            currentPos.z = Mathf.Clamp(currentPos.z, minZ, maxZ);

            transform.position = new Vector3(currentPos.x, transform.position.y, currentPos.z);
        }
    }

    public void CentrarEnHeroe()
    {
        if (player == null) return;

        float clampedX = Mathf.Clamp(player.position.x, minX, maxX);
        float clampedZ = Mathf.Clamp(player.position.z, minZ, maxZ);

        transform.position = new Vector3(clampedX, transform.position.y, clampedZ);
        currentPos = transform.position; // Sincronizar por si acaso
    }
}