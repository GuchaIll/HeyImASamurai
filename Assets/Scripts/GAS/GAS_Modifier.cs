using System;
using UnityEngine;

/// <summary>
/// How a modifier affects the attribute value
/// </summary>
public enum ModifierOp
{
    /// <summary>Add value to base (base + value)</summary>
    Add,
    /// <summary>Multiply base by value (base * value)</summary>
    Multiply,
    /// <summary>Replace base entirely (= value)</summary>
    Override
}

/// <summary>
/// A single modification to an attribute.
/// Used by GAS_Effect to define what changes it applies.
/// </summary>
[Serializable]
public class GAS_Modifier
{
    [Tooltip("Which attribute to modify (ScriptableObject reference)")]
    public GAS_AttributeDefinition attribute;
    
    [Tooltip("How to apply the value")]
    public ModifierOp operation;
    
    [Tooltip("The value to apply")]
    public float value;
    
    public GAS_Modifier() { }
    
    public GAS_Modifier(GAS_AttributeDefinition attr, ModifierOp op, float val)
    {
        attribute = attr;
        operation = op;
        value = val;
    }
    
    /// <summary>
    /// Apply this modifier to a base value
    /// </summary>
    public float Apply(float baseValue)
    {
        return operation switch
        {
            ModifierOp.Add => baseValue + value,
            ModifierOp.Multiply => baseValue * value,
            ModifierOp.Override => value,
            _ => baseValue
        };
    }
    
    /// <summary>
    /// Create a damage modifier (subtracts from attribute)
    /// </summary>
    public static GAS_Modifier Damage(GAS_AttributeDefinition healthAttr, float amount) 
        => new GAS_Modifier(healthAttr, ModifierOp.Add, -amount);
    
    /// <summary>
    /// Create a heal modifier (adds to attribute)
    /// </summary>
    public static GAS_Modifier Heal(GAS_AttributeDefinition healthAttr, float amount) 
        => new GAS_Modifier(healthAttr, ModifierOp.Add, amount);
    
    /// <summary>
    /// Create a multiplier buff modifier
    /// </summary>
    public static GAS_Modifier Buff(GAS_AttributeDefinition attr, float multiplier) 
        => new GAS_Modifier(attr, ModifierOp.Multiply, multiplier);
    
    public override string ToString()
    {
        string opStr = operation switch
        {
            ModifierOp.Add => value >= 0 ? $"+{value}" : $"{value}",
            ModifierOp.Multiply => $"×{value}",
            ModifierOp.Override => $"={value}",
            _ => value.ToString()
        };
        return $"{attribute} {opStr}";
    }
}
