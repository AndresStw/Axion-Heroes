using UnityEngine;
using UnityEngine.InputSystem;

public class MobaCamera : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 30f;
    public float edgeSize = 15f;

    [Header("Zoom")]
    public float zoomSpeed = 20f;
    public float minHeight = 10f;
    public float maxHeight = 80f;

    [Header("Map Bounds")]
    public float minX = -250f;
    public float maxX = 250f;
    public float minZ = -250f;
    public float maxZ = 250f;

    void Update()
    {
        MoveCamera();
        ZoomCamera();
    }

    void MoveCamera()
    {
        Vector3 move = Vector3.zero;

        // WASD
        if (Keyboard.current.wKey.isPressed)
            move += Vector3.forward;

        if (Keyboard.current.sKey.isPressed)
            move += Vector3.back;

        if (Keyboard.current.aKey.isPressed)
            move += Vector3.left;

        if (Keyboard.current.dKey.isPressed)
            move += Vector3.right;

        // Edge scrolling
        Vector2 mousePos = Mouse.current.position.ReadValue();

        if (mousePos.x >= Screen.width - edgeSize)
            move += Vector3.right;

        if (mousePos.x <= edgeSize)
            move += Vector3.left;

        if (mousePos.y >= Screen.height - edgeSize)
            move += Vector3.forward;

        if (mousePos.y <= edgeSize)
            move += Vector3.back;

        transform.position += move.normalized * moveSpeed * Time.deltaTime;

        // Clamp map bounds
        Vector3 pos = transform.position;

        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.z = Mathf.Clamp(pos.z, minZ, maxZ);

        transform.position = pos;
    }

    void ZoomCamera()
    {
        float scroll = Mouse.current.scroll.ReadValue().y;

        Vector3 pos = transform.position;

        pos.y -= scroll * zoomSpeed * Time.deltaTime;

        pos.y = Mathf.Clamp(pos.y, minHeight, maxHeight);

        transform.position = pos;
    }
}