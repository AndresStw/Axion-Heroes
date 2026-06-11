using UnityEngine;
using AxionHeroes.Gameplay;
using System.Collections.Generic;

public class TeamBase : MonoBehaviour
{
    public Team baseTeam;
    public float healAmountPercent = 0.15f; // 15% por segundo
    public float trueDamage = 500f; // Daño masivo que ignora defensa
    public float attackInterval = 1f;

    private List<HeroController> entitiesInRange = new List<HeroController>();
    private float nextAttackTime;

    void Update()
    {
        HandleBaseLogic();
    }

    private void HandleBaseLogic()
    {
        bool canAttack = Time.time >= nextAttackTime;

        for (int i = entitiesInRange.Count - 1; i >= 0; i--)
        {
            HeroController hero = entitiesInRange[i]; // Obtener el héroe actual
            if (hero == null || hero.IsDead) // Si el héroe es nulo o está muerto, removerlo y continuar
            {
                entitiesInRange.RemoveAt(i);
                continue;
            }

            if (hero.myTeam == baseTeam)
            {
                // Curación aliados
                float heal = hero.stats.maxHealth * healAmountPercent * Time.deltaTime;
                hero.Heal(heal); // Usar el nuevo método Heal()
            }
            else if (hero.myTeam != baseTeam && canAttack) // Si es enemigo y podemos atacar
            {
                // Ataque enemigos (Daño Verdadero)
                Debug.Log($"Base {baseTeam} defendiendo contra {hero.name}");
                hero.TakeDamage(trueDamage);
                nextAttackTime = Time.time + attackInterval;
                break; // Atacar solo a un enemigo por intervalo de ataque
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out HeroController hero))
        {
            if (!entitiesInRange.Contains(hero))
                entitiesInRange.Add(hero);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out HeroController hero))
        {
            entitiesInRange.Remove(hero);
        }
    }
}