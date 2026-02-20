using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pure data container for runtime attribute values.
/// ECS-style: holds only data, minimal logic.
/// </summary>
[Serializable]
public class GAS_AttributeContainer
{
    [Serializable]
    public struct AttributeState
    {
        public GAS_AttributeDefinition definition;
        public float baseValue;
        public float currentValue;
        
        public AttributeState(GAS_AttributeDefinition def, float baseVal)
        {
            definition = def;
            baseValue = baseVal;
            currentValue = baseVal;
        }
    }
    
    [SerializeField]
    private List<AttributeState> states = new List<AttributeState>();
    
    // Fast lookup cache (built at runtime)
    [NonSerialized]
    private Dictionary<GAS_AttributeDefinition, int> indexCache;
    
    // String-based lookup cache
    [NonSerialized]
    private Dictionary<string, GAS_AttributeDefinition> idCache;
    
    [NonSerialized]
    private bool cacheBuilt;
    
    public IReadOnlyList<AttributeState> States => states;
    
    public event Action<GAS_AttributeDefinition, float, float> OnValueChanged;
    
    private void EnsureCache()
    {
        if (cacheBuilt && indexCache != null) return;
        
        indexCache = new Dictionary<GAS_AttributeDefinition, int>();
        idCache = new Dictionary<string, GAS_AttributeDefinition>(StringComparer.OrdinalIgnoreCase);
        
        for (int i = 0; i < states.Count; i++)
        {
            if (states[i].definition != null)
            {
                indexCache[states[i].definition] = i;
                
                // Add to ID lookup (by attributeId and by asset name)
                if (!string.IsNullOrEmpty(states[i].definition.attributeId))
                    idCache[states[i].definition.attributeId] = states[i].definition;
                if (!string.IsNullOrEmpty(states[i].definition.name))
                    idCache[states[i].definition.name] = states[i].definition;
            }
        }
        cacheBuilt = true;
    }
    
    /// <summary>
    /// Find an attribute definition by its ID or name (case-insensitive)
    /// </summary>
    public GAS_AttributeDefinition FindByID(string attributeId)
    {
        if (string.IsNullOrEmpty(attributeId)) return null;
        EnsureCache();
        return idCache.TryGetValue(attributeId, out var def) ? def : null;
    }
    
    /// <summary>
    /// Check if container has this attribute by ID
    /// </summary>
    public bool Has(string attributeId)
    {
        return FindByID(attributeId) != null;
    }
    
    /// <summary>
    /// Check if container has this attribute
    /// </summary>
    public bool Has(GAS_AttributeDefinition attr)
    {
        if (attr == null) return false;
        EnsureCache();
        return indexCache.ContainsKey(attr);
    }
    
    /// <summary>
    /// Get current value of an attribute
    /// </summary>
    public float Get(GAS_AttributeDefinition attr)
    {
        if (attr == null) return 0f;
        EnsureCache();
        
        if (indexCache.TryGetValue(attr, out int idx))
            return states[idx].currentValue;
        
        return attr.defaultValue;
    }
    
    /// <summary>
    /// Get current value of an attribute by ID (e.g., "Health", "Mana")
    /// </summary>
    public float Get(string attributeId)
    {
        var attr = FindByID(attributeId);
        return attr != null ? Get(attr) : 0f;
    }
    
    /// <summary>
    /// Get base value of an attribute
    /// </summary>
    public float GetBase(GAS_AttributeDefinition attr)
    {
        if (attr == null) return 0f;
        EnsureCache();
        
        if (indexCache.TryGetValue(attr, out int idx))
            return states[idx].baseValue;
        
        return attr.defaultValue;
    }
    
    /// <summary>
    /// Set the base value (and current) of an attribute
    /// </summary>
    public void SetBase(GAS_AttributeDefinition attr, float value)
    {
        if (attr == null) return;
        EnsureCache();
        
        if (indexCache.TryGetValue(attr, out int idx))
        {
            var state = states[idx];
            state.baseValue = value;
            state.currentValue = value;
            states[idx] = state;
        }
        else
        {
            states.Add(new AttributeState(attr, value));
            indexCache[attr] = states.Count - 1;
        }
    }
    
    /// <summary>
    /// Set current value (with clamping)
    /// </summary>
    public void SetCurrent(GAS_AttributeDefinition attr, float value)
    {
        if (attr == null) return;
        EnsureCache();
        
        if (!indexCache.TryGetValue(attr, out int idx))
            return;
        
        var state = states[idx];
        float oldValue = state.currentValue;
        
        // Clamp using linked max attribute if available
        float? maxVal = null;
        if (attr.maxAttribute != null && Has(attr.maxAttribute))
            maxVal = Get(attr.maxAttribute);
        
        state.currentValue = attr.Clamp(value, maxVal);
        states[idx] = state;
        
        if (!Mathf.Approximately(oldValue, state.currentValue))
            OnValueChanged?.Invoke(attr, oldValue, state.currentValue);
    }
    
    /// <summary>
    /// Modify current value by delta
    /// </summary>
    public void Modify(GAS_AttributeDefinition attr, float delta)
    {
        SetCurrent(attr, Get(attr) + delta);
    }
    
    /// <summary>
    /// Modify base value by delta
    /// </summary>
    public void ModifyBase(GAS_AttributeDefinition attr, float delta)
    {
        if (attr == null) return;
        EnsureCache();
        
        if (indexCache.TryGetValue(attr, out int idx))
        {
            var state = states[idx];
            state.baseValue += delta;
            state.currentValue = state.baseValue;
            states[idx] = state;
        }
    }
    
    /// <summary>
    /// Reset current values to base values
    /// </summary>
    public void ResetToBase()
    {
        for (int i = 0; i < states.Count; i++)
        {
            var state = states[i];
            state.currentValue = state.baseValue;
            states[i] = state;
        }
    }
    
    /// <summary>
    /// Get all attributes that match a category
    /// </summary>
    public List<AttributeState> GetByCategory(string category)
    {
        var result = new List<AttributeState>();
        foreach (var state in states)
        {
            if (state.definition != null && state.definition.category == category)
                result.Add(state);
        }
        return result;
    }
    
    /// <summary>
    /// Clear all attributes
    /// </summary>
    public void Clear()
    {
        states.Clear();
        indexCache?.Clear();
        cacheBuilt = false;
    }
    
    /// <summary>
    /// Copy all values from another container
    /// </summary>
    public void CopyFrom(GAS_AttributeContainer other)
    {
        Clear();
        foreach (var state in other.states)
        {
            SetBase(state.definition, state.baseValue);
        }
    }
}
