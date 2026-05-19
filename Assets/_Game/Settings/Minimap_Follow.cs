using UnityEngine;

public class Minimap_Follow : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Referencia al Transform del jugador principal que el mini-mapa debe rastrear.")]
    private Transform player;

    void LateUpdate()
    {
        if (player == null) return;

        
        Vector3 newPosition = player.position;
        newPosition.y = transform.position.y;

        
        transform.position = newPosition;
    }
}