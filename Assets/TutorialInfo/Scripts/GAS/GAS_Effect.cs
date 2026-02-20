using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines what an effect does when applied.
/// Duration: 0 = instant, -1 = infinite, >0 = timed
/// 
/// Examples:
/// - Damage: duration=0, modifier=Health -50
/// - Poison: duration=5, modifier=Health -10, grantTags=["Status.Poisoned"]
/// - Speed Buff: duration=10, modifier=MoveSpeed ×1.5, grantTags=["Buff.Speed"]
/// - Stun: duration=2, grantTags=["State.Stunned"]
/// </summary>
[CreateAssetMenu(menuName = "GAS/Effect")]
public class GAS_Effect : ScriptableObject
{
    [Header("Timing")]
    [Tooltip("0 = instant, -1 = infinite, >0 = duration in seconds")]
    public float duration = 0f;
    
    [Header("Attribute Changes")]
    [Tooltip("Modifiers applied by this effect")]
    public List<GAS_Modifier> modifiers = new List<GAS_Modifier>();
    
    [Header("Tag Changes")]
    [Tooltip("Tags granted while this effect is active")]
    public List<GameplayTagReference> grantTags = new List<GameplayTagReference>();
    
    [Tooltip("Tags removed when this effect is applied")]
    public List<GameplayTagReference> removeTags = new List<GameplayTagReference>();
    
    [Header("Requirements")]
    [Tooltip("Effect only applies if target has these tags")]
    public GameplayTagQuery requireTags;
    
    [Tooltip("Effect won't apply if target has these tags")]
    public GameplayTagQuery blockIfHasTags;
    
    [Header("Stacking")]
    [Tooltip("Can multiple instances of this effect stack?")]
    public bool canStack = false;
    
    [Tooltip("Maximum number of stacks (if canStack is true)")]
    public int maxStacks = 1;
    
    [Header("Periodic (for DoT/HoT)")]
    [Tooltip("If true, modifiers are applied every 'period' seconds")]
    public bool isPeriodic = false;
    
    [Tooltip("Time between periodic applications")]
    public float period = 1f;
    
    // Convenience properties
    public bool IsInstant => duration == 0f;
    public bool IsInfinite => duration < 0f;
    public bool HasDuration => duration > 0f;
    
    /// <summary>
    /// Check if this effect can be applied to a target
    /// </summary>
    public bool CanApplyTo(GameplayTagContainer tags, GameplayTagDatabase db)
    {
        // Check required tags
        if (requireTags != null && !requireTags.IsEmpty)
        {
            if (!requireTags.Matches(tags, db))
                return false;
        }
        
        // Check blocking tags
        if (blockIfHasTags != null && !blockIfHasTags.IsEmpty)
        {
            if (blockIfHasTags.Matches(tags, db))
                return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Calculate the total value change for an attribute from all modifiers.
    /// Applies in order: Override -> Multiply -> Add
    /// </summary>
    public float CalculateModifiedValue(GAS_AttributeDefinition attr, float baseValue, int stacks = 1)
    {
        float result = baseValue;
        bool hadOverride = false;
        float additive = 0f;
        float multiplicative = 1f;
        
        foreach (var mod in modifiers)
        {
            if (mod.attribute != attr) continue;
            
            float scaledValue = mod.value * stacks;
            
            switch (mod.operation)
            {
                case ModifierOp.Override:
                    if (!hadOverride)
                    {
                        result = mod.value; // Override doesn't scale with stacks
                        hadOverride = true;
                    }
                    break;
                case ModifierOp.Multiply:
                    multiplicative *= (1 + (mod.value - 1) * stacks); // Scale the bonus part
                    break;
                case ModifierOp.Add:
                    additive += scaledValue;
                    break;
            }
        }
        
        if (!hadOverride)
        {
            result = (baseValue + additive) * multiplicative;
        }
        
        return result;
    }
    
    /// <summary>
    /// Get summary text of what this effect does
    /// </summary>
    public string GetSummary()
    {
        var parts = new List<string>();
        
        // Duration
        if (IsInstant) parts.Add("Instant");
        else if (IsInfinite) parts.Add("Permanent");
        else parts.Add($"{duration}s");
        
        // Modifiers
        foreach (var mod in modifiers)
        {
            parts.Add(mod.ToString());
        }
        
        // Tags
        if (grantTags.Count > 0)
            parts.Add($"Grants: {string.Join(", ", grantTags)}");
        
        return string.Join(" | ", parts);
    }
}
