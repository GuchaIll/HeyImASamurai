using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bundles attributes for an entity type.
/// Create different profiles: PlayerProfile, GoblinProfile, DragonProfile, etc.
/// 
/// Each profile defines which attributes an entity has and their base values.
/// </summary>
[CreateAssetMenu(menuName = "GAS/Attribute Profile")]
public class GAS_AttributeProfile : ScriptableObject
{
    [Serializable]
    public struct AttributeEntry
    {
        public GAS_AttributeDefinition attribute;
        public float baseValue;
        
        public AttributeEntry(GAS_AttributeDefinition attr, float value)
        {
            attribute = attr;
            baseValue = value;
        }
    }
    
    [Tooltip("Name of this profile (Player, Goblin, Dragon, etc.)")]
    public string profileName;
    
    [Tooltip("Optional parent profile to inherit from")]
    public GAS_AttributeProfile parentProfile;
    
    [Tooltip("Attributes included in this profile")]
    public List<AttributeEntry> attributes = new List<AttributeEntry>();
    
    /// <summary>
    /// Create a runtime container from this profile
    /// </summary>
    public GAS_AttributeContainer CreateContainer()
    {
        var container = new GAS_AttributeContainer();
        
        // First apply parent profile
        if (parentProfile != null)
        {
            foreach (var entry in parentProfile.attributes)
            {
                if (entry.attribute != null)
                    container.SetBase(entry.attribute, entry.baseValue);
            }
        }
        
        // Then override with this profile's values
        foreach (var entry in attributes)
        {
            if (entry.attribute != null)
                container.SetBase(entry.attribute, entry.baseValue);
        }
        
        return container;
    }
    
    /// <summary>
    /// Check if this profile (or parent) has an attribute
    /// </summary>
    public bool HasAttribute(GAS_AttributeDefinition attr)
    {
        foreach (var entry in attributes)
            if (entry.attribute == attr) return true;
        
        return parentProfile != null && parentProfile.HasAttribute(attr);
    }
    
    /// <summary>
    /// Get base value for an attribute
    /// </summary>
    public float GetBaseValue(GAS_AttributeDefinition attr)
    {
        foreach (var entry in attributes)
            if (entry.attribute == attr) return entry.baseValue;
        
        if (parentProfile != null)
            return parentProfile.GetBaseValue(attr);
        
        return attr?.defaultValue ?? 0f;
    }
    
    /// <summary>
    /// Get all attributes (including inherited)
    /// </summary>
    public List<AttributeEntry> GetAllAttributes()
    {
        var result = new Dictionary<GAS_AttributeDefinition, float>();
        
        // Collect from parent first
        if (parentProfile != null)
        {
            foreach (var entry in parentProfile.GetAllAttributes())
                result[entry.attribute] = entry.baseValue;
        }
        
        // Override with own values
        foreach (var entry in attributes)
        {
            if (entry.attribute != null)
                result[entry.attribute] = entry.baseValue;
        }
        
        var list = new List<AttributeEntry>();
        foreach (var kvp in result)
            list.Add(new AttributeEntry(kvp.Key, kvp.Value));
        
        return list;
    }
    
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(profileName))
            profileName = name;
    }
}
