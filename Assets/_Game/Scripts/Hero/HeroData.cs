using UnityEngine;
using AxionHeroes.Gameplay;

[CreateAssetMenu(fileName = "NuevoHeroe", menuName = "AxionHeroes/HeroData")]
public class HeroData : ScriptableObject
{
    [Header("Información Básica")]
    public string heroName;
    public HeroRole role;

    [Header("Atributos de Supervivencia")]
    public float maxHealth;
    public float physicalDefense; // Defensa física
    public float magicalDefense;  // Defensa mágica

    [Header("Atributos de Combate")]
    public float attackRange;
    public float attackDamage;
    public float attackSpeed;        // Velocidad de ataque
    public float movementSpeed;      // Velocidad de movimiento

    [Header("Habilidades")]
    public float skillCooldown;
    public float ultiCooldown;
}