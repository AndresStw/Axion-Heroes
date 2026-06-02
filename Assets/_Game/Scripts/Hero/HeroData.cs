using UnityEngine;
using AxionHeroes.Gameplay;

[CreateAssetMenu(fileName = "NuevoHeroe", menuName = "AxionHeroes/HeroData")]
public class HeroData : ScriptableObject
{
    [Header("Información Básica")]
    public string heroName;
    public HeroRole role; //tengo que automatizar la toma de rol para cada personaje o algo así, pero por ahora lo dejo manual para poder probar el sistema de roles y luego se puede automatizar o mejorar esa parte.
    public Sprite heroIcon;

    [Header("Atributos de Supervivencia")]
    public float maxHealth;
    public float physicalDefense; // Defensa física
    public float magicalDefense;  // Defensa mágica

    [Header("Atributos de Combate")]
    public float attackRange;
    public float attackSpeed;      // Velocidad de ataque, afecta el tiempo entre ataques básicos.
    public float attackDamage;//levelear el daño  la escala actaual es 12 , pero seria bueno tener 123 según el nivel del héroe o algo así, pero por ahora lo dejo fijo para poder probar el sistema de combate básico y luego se puede agregar esa funcionalidad de leveleo o algo así. tambien tener en cuanta la vida de los shard.

    public float movementSpeed;      // Velocidad de movimiento

    [Header("Habilidades")]
    public float skillCooldown;//
    public float ultiCooldown;


    [Header("Atributos de Progresión")]
    public int nivel;
    public float cooldownReductionPerLevel;//reducción de cooldown por nivel, para que a medida que el héroe suba de nivel pueda usar sus habilidades más seguido.
    public float skillDamage;//daño de la habilidad, también se puede levelear o algo así.

    public float ultiDamage;//daño de la ulti, también se puede levelear.
    public float skillRange;//rango de la habilidad, también se puede levelear.

    public float ultiRange;//rango de la ulti, también se puede levelear o algo así, pero por ahora lo dejo fijo para poder probar el sistema de habilidades y luego se puede agregar esa funcionalidad de leveleo o algo así.

    public float skillManaCost;//costo de mana de la habilidad .  
     public float ultiManaCost;//costo de mana de la ult.

    public float manaRegenRate;//tasa de regeneración de mana.

    public float experiencePoints; // puntos de experiencia que otorga el héroe al morir.


    public float goldValue; // cantidad de oro que otorga el héroe al morir.


    public float attackcooldown; //tiempo de cooldown entre ataques básicos.

    }