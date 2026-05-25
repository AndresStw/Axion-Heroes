using UnityEngine;

public class MobaCamera : MonoBehaviour
{
    [Header("Hero Support")]
    [Tooltip("¡Arrastra a tu héroe aquí en el Inspector!")]
    public Transform playerHero;

    [Header("Configuración de Distancia")]
    public Vector3 offset = new Vector3(0f, 15f, -10f); // Altura y distancia hacia atrás
    public float smoothSpeed = 10f;

    void Start()
    {
        // Ángulo de inclinación clásico de MOBA mirando al campo
        transform.rotation = Quaternion.Euler(55f, 0f, 0f);
    }

    void LateUpdate()
    {
        if (playerHero == null) return;

        // La cámara sigue de forma constante y fluida la posición del héroe
        Vector3 targetPosition = playerHero.position + offset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
    }
}