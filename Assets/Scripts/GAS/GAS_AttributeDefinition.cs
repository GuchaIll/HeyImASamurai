using UnityEngine;

/// <summary>
/// Defines a single attribute type. Create ScriptableObject assets for each attribute.
/// Examples: Health, Mana, Rage, Corruption, etc.
/// 
/// Data-driven approach - no code changes needed to add new attributes.
/// Different entity types can have different attributes.
/// </summary>
[CreateAssetMenu(menuName = "GAS/Attribute Definition")]
public class GAS_AttributeDefinition : ScriptableObject
{
    [Tooltip("Unique identifier for this attribute")]
    public string attributeId;
    
    [Tooltip("Display name for UI")]
    public string displayName;
    
    [Tooltip("Default value when not specified in a profile")]
    public float defaultValue = 100f;
    
    [Header("Clamping")]
    [Tooltip("Minimum allowed value")]
    public bool hasMinValue = true;
    public float minValue = 0f;
    
    [Tooltip("Maximum allowed value")]
    public bool hasMaxValue = false;
    public float maxValue = 100f;
    
    [Tooltip("Optional: Attribute that caps this value (e.g., MaxHealth caps Health)")]
    public GAS_AttributeDefinition maxAttribute;
    
    [Header("UI & Organization")]
    [Tooltip("Category for grouping in editor")]
    public string category = "Misc";
    
    [Tooltip("Icon for UI display")]
    public Sprite icon;
    
    [Tooltip("Description for tooltips")]
    [TextArea(2, 4)]
    public string description;
    
    /// <summary>
    /// Clamp a value according to this attribute's rules
    /// </summary>
    public float Clamp(float value, float? linkedMax = null)
    {
        float min = hasMinValue ? minValue : float.NegativeInfinity;
        float max = linkedMax ?? (hasMaxValue ? maxValue : float.PositiveInfinity);
        return Mathf.Clamp(value, min, max);
    }
    
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(attributeId))
            attributeId = name;
        if (string.IsNullOrEmpty(displayName))
            displayName = name;
    }
    
    public override string ToString() => displayName ?? attributeId ?? name;
    
    // Equality based on attributeId for dictionary lookups
    public override int GetHashCode() => attributeId?.GetHashCode() ?? 0;
    
    public override bool Equals(object obj)
    {
        if (obj is GAS_AttributeDefinition other)
            return attributeId == other.attributeId;
        return false;
    }
}
