using System;

namespace stardew_medieval_v3.Player;

/// <summary>
/// Manages player stamina. Restored on day advance.
/// </summary>
public class PlayerStats
{
    public float MaxStamina { get; set; } = 100f;
    public float CurrentStamina { get; private set; } = 100f;

    public event Action<float, float>? OnStaminaChanged;

    public bool TrySpendStamina(float amount)
    {
        if (CurrentStamina < amount)
            return false;

        CurrentStamina -= amount;
        OnStaminaChanged?.Invoke(CurrentStamina, MaxStamina);
        return true;
    }

    public void RestoreStamina()
    {
        CurrentStamina = MaxStamina;
        OnStaminaChanged?.Invoke(CurrentStamina, MaxStamina);
    }

    /// <summary>Restore a partial amount of stamina (clamped to max).</summary>
    public void RestoreStamina(float amount)
    {
        CurrentStamina = Math.Clamp(CurrentStamina + amount, 0, MaxStamina);
        OnStaminaChanged?.Invoke(CurrentStamina, MaxStamina);
    }

    public void SetStamina(float value)
    {
        CurrentStamina = Math.Clamp(value, 0, MaxStamina);
        OnStaminaChanged?.Invoke(CurrentStamina, MaxStamina);
    }
}
