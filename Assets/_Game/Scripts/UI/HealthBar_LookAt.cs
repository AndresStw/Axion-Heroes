using UnityEngine;

public class HealthBar_LookAt : MonoBehaviour
{
    private Transform cam;

    void Start()
    {
        cam = Camera.main.transform;
    }

    void LateUpdate() // LateUpdate es mejor para cámaras
    {
        // Hace que la barra mire a la cámara pero se mantenga recta
        transform.LookAt(transform.position + cam.forward);
    }
}