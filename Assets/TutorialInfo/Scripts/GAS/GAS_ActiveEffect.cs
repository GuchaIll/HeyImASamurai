using System;
using UnityEngine;

/// <summary>
/// Runtime instance of an active effect.
/// Created when a GAS_Effect is applied to a target.
/// </summary>
public class GAS_ActiveEffect
{
    /// <summary>The effect definition</summary>
    public GAS_Effect Definition { get; }
    
    /// <summary>Who applied this effect (can be null for environment effects)</summary>
    public GAS_AbilitySystemComponent Source { get; }
    
    /// <summary>Who has this effect</summary>
    public GAS_AbilitySystemComponent Target { get; }
    
    /// <summary>Time remaining in seconds (for duration effects)</summary>
    public float TimeRemaining { get; private set; }
    
    /// <summary>Current stack count</summary>
    public int Stacks { get; private set; }
    
    /// <summary>Time until next periodic tick</summary>
    public float NextPeriodicTime { get; private set; }
    
    /// <summary>Has this effect expired?</summary>
    public bool IsExpired => !Definition.IsInfinite && TimeRemaining <= 0;
    
    /// <summary>Normalized progress (0 = just applied, 1 = expired)</summary>
    public float Progress => Definition.HasDuration 
        ? 1f - (TimeRemaining / Definition.duration) 
        : 0f;
    
    // Events
    public event Action<GAS_ActiveEffect> OnExpired;
    public event Action<GAS_ActiveEffect> OnStackChanged;
    public event Action<GAS_ActiveEffect> OnPeriodicTick;
    
    public GAS_ActiveEffect(GAS_Effect def, GAS_AbilitySystemComponent source, GAS_AbilitySystemComponent target, int initialStacks = 1)
    {
        Definition = def;
        Source = source;
        Target = target;
        TimeRemaining = def.duration;
        Stacks = Mathf.Clamp(initialStacks, 1, def.canStack ? def.maxStacks : 1);
        NextPeriodicTime = def.period;
    }
    
    /// <summary>
    /// Update the effect timer. Call from AbilitySystemComponent.Update()
    /// </summary>
    public void Tick(float deltaTime)
    {
        if (Definition.IsInstant || IsExpired)
            return;
        
        // Update duration
        if (Definition.HasDuration)
        {
            TimeRemaining -= deltaTime;
            if (TimeRemaining <= 0)
            {
                TimeRemaining = 0;
                OnExpired?.Invoke(this);
            }
        }
        
        // Handle periodic effects
        if (Definition.isPeriodic && !IsExpired)
        {
            NextPeriodicTime -= deltaTime;
            if (NextPeriodicTime <= 0)
            {
                NextPeriodicTime = Definition.period;
                OnPeriodicTick?.Invoke(this);
            }
        }
    }
    
    /// <summary>
    /// Add stacks (up to max)
    /// </summary>
    public void AddStacks(int count = 1)
    {
        if (!Definition.canStack) return;
        
        int oldStacks = Stacks;
        Stacks = Mathf.Min(Stacks + count, Definition.maxStacks);
        
        if (Stacks != oldStacks)
            OnStackChanged?.Invoke(this);
    }
    
    /// <summary>
    /// Remove stacks
    /// </summary>
    public void RemoveStacks(int count = 1)
    {
        int oldStacks = Stacks;
        Stacks = Mathf.Max(Stacks - count, 0);
        
        if (Stacks != oldStacks)
            OnStackChanged?.Invoke(this);
    }
    
    /// <summary>
    /// Reset duration to full
    /// </summary>
    public void Refresh()
    {
        TimeRemaining = Definition.duration;
    }
    
    /// <summary>
    /// Force the effect to expire immediately
    /// </summary>
    public void ForceExpire()
    {
        if (Definition.IsInfinite)
        {
            OnExpired?.Invoke(this);
        }
        else
        {
            TimeRemaining = 0;
            OnExpired?.Invoke(this);
        }
    }
    
    /// <summary>
    /// Calculate modified value for an attribute considering stacks
    /// </summary>
    public float GetModifiedValue(GAS_AttributeDefinition attr, float baseValue)
    {
        return Definition.CalculateModifiedValue(attr, baseValue, Stacks);
    }
    
    public override string ToString()
    {
        string stackStr = Stacks > 1 ? $" x{Stacks}" : "";
        string timeStr = Definition.HasDuration ? $" ({TimeRemaining:F1}s)" : "";
        return $"{Definition.name}{stackStr}{timeStr}";
    }
}
