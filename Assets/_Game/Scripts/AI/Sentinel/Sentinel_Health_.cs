using UnityEngine;
using UnityEngine.UI;

public class Sentinel_Health : MonoBehaviour
{
    [Header("Estadísticas")]
    public float maxHealth = 500f;
    private float currentHealth;

    [Header("UI en Pantalla (Canvas Overlay)")]
    public GameObject hpSliderPrefab; // Tu prefab 'Sentinel_HP_UI' (el Slider 2D)
    public float margenExtra = 0.5f;   // Un pequeño espacio extra por encima de la cabeza

    private Slider healthSlider;
    private RectTransform sliderRect;
    private Canvas mainCanvas;
    private Camera mainCamera;
    private float alturaCalculada;

    void Start()
    {
        currentHealth = maxHealth;
        mainCamera = Camera.main;

        // 1. TRUCO MAESTRO: Calculamos la altura real del objeto usando su Renderer
        Renderer objRenderer = GetComponentInChildren<Renderer>();
        if (objRenderer != null)
        {
            // Tomamos el punto más alto del Bounds (la caja que envuelve el modelo 3D)
            alturaCalculada = objRenderer.bounds.max.y - transform.position.y;
        }
        else
        {
            // Si por alguna razón no tiene renderer, usamos una altura fija por defecto
            alturaCalculada = 3.0f;
        }

        // 2. Buscar el Canvas principal de la UI
        mainCanvas = FindFirstObjectByType<Canvas>();

        if (mainCanvas != null && hpSliderPrefab != null)
        {
            // 3. Clonamos el Slider dentro del Canvas plano de la pantalla
            GameObject uiGo = Instantiate(hpSliderPrefab, mainCanvas.transform);
            healthSlider = uiGo.GetComponent<Slider>();
            sliderRect = uiGo.GetComponent<RectTransform>();

            if (healthSlider != null)
            {
                healthSlider.minValue = 0;
                healthSlider.maxValue = maxHealth;
                healthSlider.value = currentHealth;
            }
        }
    }

    void LateUpdate()
    {
        if (sliderRect != null && mainCamera != null)
        {
            // 4. Posicionamos la barra en la cabeza usando la altura que calculamos sola
            Vector3 worldPosition = transform.position + Vector3.up * (alturaCalculada + margenExtra);
            Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);

            // Ocultar si está fuera de la pantalla
            if (screenPosition.z < 0)
            {
                healthSlider.gameObject.SetActive(false);
                return;
            }

            healthSlider.gameObject.SetActive(true);
            sliderRect.position = screenPosition;
        }
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (healthSlider != null) healthSlider.value = currentHealth;

        if (currentHealth <= 0) Die();
    }

    void Die()
    {
        if (healthSlider != null) Destroy(healthSlider.gameObject);

        // Como es parte del mapa, desactivamos el script y el objeto visual en vez de destruir todo
        Renderer objRenderer = GetComponentInChildren<Renderer>();
        if (objRenderer != null) objRenderer.enabled = false;

        this.enabled = false;
        Debug.Log("Sentinel derrotada.");
    }
}