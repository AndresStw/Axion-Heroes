namespace AxionHeroes.Gameplay
{
    public interface IDamageable
    {
        void TakeDamage(float amount);
        bool IsDead { get; }
        float CurrentHealth { get; }
        float MaxHealth { get; }
    }
}