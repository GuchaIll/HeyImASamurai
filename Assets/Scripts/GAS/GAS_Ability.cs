using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base ability definition. Create subclasses for custom ability logic.
/// 
/// Simple abilities can just use data (cost, cooldown, effects).
/// Complex abilities can override OnActivate, OnTick, OnEnd.
/// </summary>
[CreateAssetMenu(menuName = "GAS/Ability")]
public class GAS_Ability : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Unique tag for this ability")]
    public GameplayTagReference abilityTag;
    
    [Tooltip("Display name for UI")]
    public string displayName;
    
    [Tooltip("Icon for UI")]
    public Sprite icon;
    
    [Tooltip("Description for UI")]
    [TextArea(2, 4)]
    public string description;
    
    [Header("Activation")]
    [Tooltip("Tags required to activate")]
    public GameplayTagQuery requireTags;
    
    [Tooltip("Can't activate if target has these tags")]
    public GameplayTagQuery blockedByTags;
    
    [Header("Tags Granted")]
    [Tooltip("Tags added while ability is active")]
    public List<GameplayTagReference> grantTagsWhileActive = new List<GameplayTagReference>();
    
    [Header("Cost")]
    [Tooltip("Attribute used as cost (e.g., Mana, Stamina reference)")]
    public GAS_AttributeDefinition costAttribute;
    
    [Tooltip("Amount to consume")]
    public float costAmount = 0f;
    
    [Header("Cooldown")]
    [Tooltip("Cooldown duration in seconds (0 = no cooldown)")]
    public float cooldown = 0f;
    
    [Tooltip("Tag applied during cooldown")]
    public GameplayTagReference cooldownTag;
    
    [Header("Effects")]
    [Tooltip("Effects applied when ability activates")]
    public List<GAS_Effect> effectsOnActivate = new List<GAS_Effect>();
    
    [Tooltip("Effects applied when ability ends")]
    public List<GAS_Effect> effectsOnEnd = new List<GAS_Effect>();
    
    // Convenience
    public bool HasCost => costAttribute != null && costAmount > 0;
    public bool HasCooldown => cooldown > 0;
    
    /// <summary>
    /// Check if ability can be activated
    /// </summary>
    public virtual bool CanActivate(GAS_AbilitySystemComponent owner, GameplayTagDatabase db)
    {
        if (owner == null)
        {
            Debug.LogWarning("[GAS] CanActivate: owner is null");
            return false;
        }
        if (db == null)
        {
            Debug.LogWarning("[GAS] CanActivate: TagDatabase is null - make sure GameplayTagComponent has a database assigned!");
            return false;
        }
        
        // Check cooldown
        if (HasCooldown && IsOnCooldown(owner, db))
        {
            Debug.Log("[GAS] CanActivate: On cooldown");
            return false;
        }
        
        // Check cost
        if (HasCost && !CanPayCost(owner))
        {
            Debug.Log("[GAS] CanActivate: Can't pay cost");
            return false;
        }
        
        // Check tag requirements
        if (!CheckTagRequirements(owner, db))
        {
            Debug.Log("[GAS] CanActivate: Tag requirements not met");
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Check if ability is on cooldown
    /// </summary>
    public bool IsOnCooldown(GAS_AbilitySystemComponent owner, GameplayTagDatabase db)
    {
        if (!HasCooldown || cooldownTag == null || !cooldownTag.IsValid(db))
            return false;
        
        return owner.Tags.HasTag(cooldownTag.ToRuntimeTag(db));
    }
    
    /// <summary>
    /// Check if owner can pay the cost
    /// </summary>
    public bool CanPayCost(GAS_AbilitySystemComponent owner)
    {
        if (!HasCost) return true;
        return owner.GetAttributeValue(costAttribute) >= costAmount;
    }
    
    /// <summary>
    /// Check tag requirements
    /// </summary>
    public bool CheckTagRequirements(GAS_AbilitySystemComponent owner, GameplayTagDatabase db)
    {
        var tags = owner.Tags.GetContainer();
        
        // Check required tags
        if (requireTags != null && !requireTags.IsEmpty)
        {
            if (!requireTags.Matches(tags, db))
                return false;
        }
        
        // Check blocked tags
        if (blockedByTags != null && !blockedByTags.IsEmpty)
        {
            if (blockedByTags.Matches(tags, db))
                return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Called when ability activates. Override for custom logic.
    /// Return the ability duration (0 = instant, >0 = lasts that long)
    /// </summary>
    public virtual float OnActivate(GAS_AbilitySystemComponent owner)
    {
        // Default: apply activation effects and return 0 (instant)
        return 0f;
    }
    
    /// <summary>
    /// Called every frame while ability is active. Override for channeled/held abilities.
    /// </summary>
    public virtual void OnTick(GAS_AbilitySystemComponent owner, float deltaTime)
    {
        // Default: nothing
    }
    
    /// <summary>
    /// Called when ability ends. Override for cleanup.
    /// </summary>
    public virtual void OnEnd(GAS_AbilitySystemComponent owner, bool wasCancelled)
    {
        // Default: nothing
    }
}
