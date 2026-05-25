using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class VariableJoystick : Joystick
{
    public float MoveThreshold { get { return moveThreshold; } set { moveThreshold = Mathf.Abs(value); } }

    [SerializeField] private float moveThreshold = 1;
    [SerializeField] private JoystickType joystickType = JoystickType.Fixed;

    private Vector2 fixedPosition = Vector2.zero;

    public void SetMode(JoystickType joystickType)
    {
        this.joystickType = joystickType;

        if (background == null) return;

        if (joystickType == JoystickType.Fixed)
        {
            background.anchoredPosition = fixedPosition;
            background.gameObject.SetActive(true);
        }
        else
            background.gameObject.SetActive(false);
    }

    protected override void Start()
    {
        // AUTO-ASIGNACIÓN SEGURA: Buscamos el fondo primero
        if (background == null)
        {
            Transform bgTransform = transform.Find("Background");
            if (bgTransform != null) background = bgTransform.GetComponent<RectTransform>();
        }

        // Ejecutamos la base. El Start() original del asset ya se encarga internamente 
        // de configurar su propia variable 'handle' si encuentra el objeto.
        base.Start();

        if (background != null)
        {
            fixedPosition = background.anchoredPosition;
        }

        SetMode(joystickType);
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (background == null) return;

        if (joystickType != JoystickType.Fixed)
        {
            background.anchoredPosition = ScreenPointToAnchoredPosition(eventData.position);
            background.gameObject.SetActive(true);
        }
        base.OnPointerDown(eventData);
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        if (background == null) return;

        if (joystickType != JoystickType.Fixed)
            background.gameObject.SetActive(false);

        base.OnPointerUp(eventData);
    }

    protected override void HandleInput(float magnitude, Vector2 normalised, Vector2 radius, Camera cam)
    {
        if (background == null) return;

        if (joystickType == JoystickType.Dynamic && magnitude > moveThreshold)
        {
            Vector2 difference = normalised * (magnitude - moveThreshold) * radius;
            background.anchoredPosition += difference;
        }
        base.HandleInput(magnitude, normalised, radius, cam);
    }
}
public enum JoystickType { Fixed, Floating, Dynamic }