using UnityEngine;

public class EvolutionManager : MonoBehaviour
{
    
    public void AddExperience(HeroController hero, float amount)
    {
        hero.experienciaActual += amount;
        
        if (hero.experienciaActual >= hero.experienciaParaSiguienteNivel)
        {
            SubirNivel(hero);
        }
    }

    private void SubirNivel(HeroController hero)
    {
        hero.nivel++;
        hero.experienciaActual = 0;
        hero.experienciaParaSiguienteNivel *= 1.5f; 
        
        // hero.stats.attackDamage += 5f; 
        
        Debug.Log($"¡Héroe {hero.name} subió al nivel {hero.nivel}!");
    }
}