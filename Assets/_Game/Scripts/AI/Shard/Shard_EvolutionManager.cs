using UnityEngine;

[CreateAssetMenu(fileName = "ShardEvolutionManager", menuName = "AxionHeroes/Shard Evolution Manager")]
public class Shard_EvolutionManager : ScriptableObject
{
    public enum TeamFaction { Blue, Red }
    public TeamFaction faction;

    [Header("Progreso Global (Máx Nivel 8)")]
    public int currentLevel = 1;
    public float currentExp = 0f;
    public float expToNextLevel = 150f;
    public const int MaxLevel = 8;

    [Header("Multiplicadores de Atributos por Nivel")]
    [Tooltip("Porcentaje extra de vida por nivel (0.15 = +15% por nivel)")]
    public float healthMultiplierPerLevel = 0.15f;
    [Tooltip("Porcentaje extra de daño por nivel (0.12 = +12% por nivel)")]
    public float damageMultiplierPerLevel = 0.12f;

    public void ResetManager()
    {
        currentLevel = 1;
        currentExp = 0f;
        expToNextLevel = 150f;
    }

    public void AddExperience(float amount)
    {
        if (currentLevel >= MaxLevel) return;

        currentExp += amount;
        while (currentExp >= expToNextLevel && currentLevel < MaxLevel)
        {
            LevelUp();
        }
    }

    private void LevelUp()
    {
        currentExp -= expToNextLevel;
        currentLevel++;
        expToNextLevel *= 1.4f; // Incremento progresivo de la curva de dificultad
        Debug.Log($"<color=cyan>[EVOLUCIÓN SHARD]</color> ¡La facción {faction} ha subido sus Shards al nivel {currentLevel}!");
    }

    // Fórmulas para retornar los stats inflados según el nivel actual
    public int GetScaledMaxHealth(int baseHealth)
    {
        float bonusFactor = 1f + ((currentLevel - 1) * healthMultiplierPerLevel);
        return Mathf.RoundToInt(baseHealth * bonusFactor);
    }

    public int GetScaledDamage(int baseDamage)
    {
        float bonusFactor = 1f + ((currentLevel - 1) * damageMultiplierPerLevel);
        return Mathf.RoundToInt(baseDamage * bonusFactor);
    }
}