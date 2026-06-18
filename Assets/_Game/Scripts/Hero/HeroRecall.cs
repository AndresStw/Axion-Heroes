using UnityEngine;
using System.Collections;

public class HeroRecall : MonoBehaviour
{
    private HeroController controller;
    [SerializeField] private float recallTime = 4f;
    private Coroutine recallCoroutine;
    private Vector3 spawnPoint;

    void Start()
    {
        controller = GetComponent<HeroController>();

        // punto de spawn inicial base
        spawnPoint = transform.position; 
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B) && !controller.IsDead)
        {
            if (controller.isRecalling)
            {
                CancelRecall(); // Condición 4: Si presiona de nuevo, se cancela
            }
            else
            {
                StartRecall();
            }
        }
    }

    public void StartRecall()
    {
        if (recallCoroutine != null) StopCoroutine(recallCoroutine);
        recallCoroutine = StartCoroutine(RecallRoutine());
    }

    private IEnumerator RecallRoutine()
    {
        controller.isRecalling = true;
        Debug.Log("Iniciando Regreso a Base...");
        
        yield return new WaitForSeconds(recallTime);

        // Teletransporte exitoso
        controller.isRecalling = false;
        transform.position = spawnPoint;
        if (TryGetComponent(out UnityEngine.AI.NavMeshAgent agent))
        {
            agent.Warp(spawnPoint);
        }
        Debug.Log("Regreso a Base completado.");
    }

    public void CancelRecall()
    {
        if (controller.isRecalling)
        {
            controller.isRecalling = false;
            if (recallCoroutine != null) StopCoroutine(recallCoroutine);
            Debug.Log("Recall Cancelado.");
        }
    }
}