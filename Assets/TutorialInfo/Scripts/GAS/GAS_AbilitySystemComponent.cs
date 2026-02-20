using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime ability instance tracking activation state
/// </summary>
public class GAS_AbilityInstance
{
    public GAS_Ability Definition { get; }
    public GAS_AbilitySystemComponent Owner { get; }
    public bool IsActive { get; private set; }
    public float ActiveTime { get; private set; }
    public float RemainingTime { get; private set; }
    
    public event Action<GAS_AbilityInstance> OnActivated;
    public event Action<GAS_AbilityInstance, bool> OnEnded;
    
    public GAS_AbilityInstance(GAS_Ability def, GAS_AbilitySystemComponent owner)
    {
        Definition = def;
        Owner = owner;
    }
    
    public bool TryActivate()
    {
        Debug.Log($"[GAS] TryActivate: IsActive={IsActive}");
        if (IsActive) return false;
        
        Debug.Log($"[GAS] TryActivate: Checking CanActivate, TagDatabase={Owner.TagDatabase}");
        if (!Definition.CanActivate(Owner, Owner.TagDatabase))
        {
            Debug.LogWarning($"[GAS] TryActivate: CanActivate returned false");
            return false;
        }
        
        Debug.Log($"[GAS] TryActivate: Passed CanActivate check");
        
        // Pay cost
        if (Definition.HasCost)
        {
            Owner.ModifyAttribute(Definition.costAttribute, -Definition.costAmount);
        }
        
        // Apply cooldown
        if (Definition.HasCooldown && Definition.cooldownTag != null && Definition.cooldownTag.IsValid(Owner.TagDatabase))
        {
            Owner.ApplyCooldown(Definition.cooldownTag.tagName, Definition.cooldown);
        }
        
        // Grant tags
        foreach (var tag in Definition.grantTagsWhileActive)
        {
            if (tag != null && tag.IsValid(Owner.TagDatabase))
                Owner.Tags.AddTagByName(tag.tagName);
        }
        
        // Apply activation effects
        foreach (var effect in Definition.effectsOnActivate)
        {
            if (effect != null)
                Owner.ApplyEffectToSelf(effect);
        }
        
        // Activate
        float duration = Definition.OnActivate(Owner);
        IsActive = true;
        ActiveTime = 0f;
        RemainingTime = duration;
        
        OnActivated?.Invoke(this);
        
        // Instant ability ends immediately
        if (duration <= 0)
        {
            End(false);
        }
        
        return true;
    }
    
    public void Tick(float deltaTime)
    {
        if (!IsActive) return;
        
        ActiveTime += deltaTime;
        Definition.OnTick(Owner, deltaTime);
        
        if (RemainingTime > 0)
        {
            RemainingTime -= deltaTime;
            if (RemainingTime <= 0)
            {
                End(false);
            }
        }
    }
    
    public void End(bool cancelled)
    {
        if (!IsActive) return;
        
        IsActive = false;
        
        // Remove granted tags
        foreach (var tag in Definition.grantTagsWhileActive)
        {
            if (tag != null && tag.IsValid(Owner.TagDatabase))
                Owner.Tags.RemoveTagByName(tag.tagName);
        }
        
        // Apply end effects
        foreach (var effect in Definition.effectsOnEnd)
        {
            if (effect != null)
                Owner.ApplyEffectToSelf(effect);
        }
        
        Definition.OnEnd(Owner, cancelled);
        OnEnded?.Invoke(this, cancelled);
    }
    
    public void Cancel() => End(true);
}

/// <summary>
/// Main component that manages abilities, effects, and attributes for an entity.
/// Attach this to any GameObject that needs GAS functionality.
/// Uses data-driven attribute profiles for flexible entity configuration.
/// </summary>
[RequireComponent(typeof(GameplayTagComponent))]
public class GAS_AbilitySystemComponent : MonoBehaviour
{
    [Header("Attribute Profile")]
    [Tooltip("Defines which attributes this entity has (e.g., PlayerProfile, GoblinProfile)")]
    [SerializeField] private GAS_AttributeProfile attributeProfile;
    
    [Header("Starting Abilities")]
    [SerializeField] private List<GAS_Ability> startingAbilities = new List<GAS_Ability>();
    
    // Runtime state - data-driven attribute container
    private GAS_AttributeContainer attributes;
    private List<GAS_ActiveEffect> activeEffects = new List<GAS_ActiveEffect>();
    private List<GAS_AbilityInstance> abilities = new List<GAS_AbilityInstance>();
    private Dictionary<string, GAS_AbilityInstance> abilityMap = new Dictionary<string, GAS_AbilityInstance>();
    
    // Cached component
    private GameplayTagComponent tagComponent;
    
    // Properties
    public GameplayTagComponent Tags => tagComponent;
    public GameplayTagDatabase TagDatabase => tagComponent?.Database;
    public GAS_AttributeProfile Profile => attributeProfile;
    public GAS_AttributeContainer Attributes => attributes;
    public IReadOnlyList<GAS_ActiveEffect> ActiveEffects => activeEffects;
    public IReadOnlyList<GAS_AbilityInstance> Abilities => abilities;
    
    // Events - now use GAS_AttributeDefinition instead of enum
    public event Action<GAS_AttributeDefinition, float, float> OnAttributeChanged;
    public event Action<GAS_ActiveEffect> OnEffectApplied;
    public event Action<GAS_ActiveEffect> OnEffectRemoved;
    public event Action<GAS_AbilityInstance> OnAbilityActivated;
    public event Action<GAS_AbilityInstance> OnAbilityEnded;
    
    #region Lifecycle
    
    protected virtual void Awake()
    {
        tagComponent = GetComponent<GameplayTagComponent>();
        InitializeAttributes();
    }
    
    protected virtual void Start()
    {
        // Grant starting abilities
        foreach (var ability in startingAbilities)
        {
            if (ability != null)
                GrantAbility(ability);
        }
    }
    
    protected virtual void Update()
    {
        float dt = Time.deltaTime;
        
        // Update effects
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            var effect = activeEffects[i];
            effect.Tick(dt);
            
            if (effect.IsExpired)
            {
                RemoveEffect(effect);
            }
        }
        
        // Update active abilities
        foreach (var ability in abilities)
        {
            if (ability.IsActive)
            {
                ability.Tick(dt);
            }
        }
        
        // Note: Effect modifiers are applied dynamically in GetAttributeValue()
        // No need for per-frame recalculation
    }
    
    #endregion
    
    #region Attributes
    
    private void InitializeAttributes()
    {
        if (attributeProfile != null)
        {
            attributes = attributeProfile.CreateContainer();
            
            // Forward attribute change events
            attributes.OnValueChanged += (attr, oldVal, newVal) => 
                OnAttributeChanged?.Invoke(attr, oldVal, newVal);
        }
        else
        {
            // Create empty container if no profile assigned
            attributes = new GAS_AttributeContainer();
            Debug.LogWarning($"[GAS] {name} has no AttributeProfile assigned!", this);
        }
    }
    
    /// <summary>
    /// Get the current (modified) value of an attribute
    /// </summary>
    public float GetAttributeValue(GAS_AttributeDefinition attr)
    {
        if (attributes == null || attr == null) return 0f;
        
        float baseVal = attributes.Get(attr);
        float finalVal = baseVal;
        
        // Apply modifiers from active effects
        foreach (var effect in activeEffects)
        {
            finalVal = effect.GetModifiedValue(attr, finalVal);
        }
        
        return finalVal;
    }
    
    /// <summary>
    /// Get the current (modified) value of an attribute by ID (e.g., "Health", "Mana")
    /// </summary>
    public float GetAttributeValue(string attributeId)
    {
        var attr = attributes?.FindByID(attributeId);
        return attr != null ? GetAttributeValue(attr) : 0f;
    }
    
    /// <summary>
    /// Get the base (unmodified by effects) value of an attribute
    /// </summary>
    public float GetAttributeBaseValue(GAS_AttributeDefinition attr)
    {
        return attributes?.Get(attr) ?? 0f;
    }
    
    /// <summary>
    /// Get the base (unmodified by effects) value of an attribute by ID
    /// </summary>
    public float GetAttributeBaseValue(string attributeId)
    {
        var attr = attributes?.FindByID(attributeId);
        return attr != null ? GetAttributeBaseValue(attr) : 0f;
    }
    
    /// <summary>
    /// Set the base value of an attribute directly
    /// </summary>
    public void SetAttributeBaseValue(GAS_AttributeDefinition attr, float value)
    {
        attributes?.SetCurrent(attr, value);
    }
    
    /// <summary>
    /// Set the base value of an attribute by ID
    /// </summary>
    public void SetAttributeBaseValue(string attributeId, float value)
    {
        var attr = attributes?.FindByID(attributeId);
        if (attr != null) SetAttributeBaseValue(attr, value);
    }
    
    /// <summary>
    /// Modify an attribute by a delta amount
    /// </summary>
    public void ModifyAttribute(GAS_AttributeDefinition attr, float delta)
    {
        if (attributes == null || attr == null) return;
        float current = attributes.Get(attr);
        attributes.SetCurrent(attr, current + delta);
    }
    
    /// <summary>
    /// Modify an attribute by ID (e.g., "Health", "Mana")
    /// </summary>
    public void ModifyAttribute(string attributeId, float delta)
    {
        var attr = attributes?.FindByID(attributeId);
        if (attr != null) ModifyAttribute(attr, delta);
    }
    
    /// <summary>
    /// Check if this entity has a specific attribute
    /// </summary>
    public bool HasAttribute(GAS_AttributeDefinition attr)
    {
        return attributes?.Has(attr) ?? false;
    }
    
    /// <summary>
    /// Check if this entity has a specific attribute by ID
    /// </summary>
    public bool HasAttribute(string attributeId)
    {
        return attributes?.Has(attributeId) ?? false;
    }
    
    /// <summary>
    /// Get attribute as a normalized 0-1 ratio (e.g., Health / MaxHealth)
    /// </summary>
    public float GetAttributeRatio(GAS_AttributeDefinition attr)
    {
        if (attributes == null || attr == null) return 0f;
        
        if (!attributes.Has(attr)) return 0f;
        
        // If there's a linked max attribute, use its value
        if (attr.maxAttribute != null)
        {
            float maxVal = GetAttributeValue(attr.maxAttribute);
            return maxVal > 0 ? GetAttributeValue(attr) / maxVal : 0f;
        }
        
        // Otherwise use the definition's max if defined
        if (attr.hasMaxValue && attr.maxValue > 0)
        {
            return GetAttributeValue(attr) / attr.maxValue;
        }
        
        return 0f;
    }
    
    #endregion
    
    #region Effects
    
    public GAS_ActiveEffect ApplyEffectToSelf(GAS_Effect effect)
    {
        return ApplyEffect(effect, this, this);
    }
    
    public GAS_ActiveEffect ApplyEffectToTarget(GAS_Effect effect, GAS_AbilitySystemComponent target)
    {
        return ApplyEffect(effect, this, target);
    }
    
    private GAS_ActiveEffect ApplyEffect(GAS_Effect effect, GAS_AbilitySystemComponent source, GAS_AbilitySystemComponent target)
    {
        if (effect == null || target == null) return null;
        
        // Check requirements
        if (!effect.CanApplyTo(target.Tags.GetContainer(), target.TagDatabase))
            return null;
        
        // Handle stacking
        GAS_ActiveEffect existing = null;
        if (!effect.canStack)
        {
            existing = target.activeEffects.Find(e => e.Definition == effect);
            if (existing != null)
            {
                existing.Refresh();
                return existing;
            }
        }
        else
        {
            existing = target.activeEffects.Find(e => e.Definition == effect && e.Stacks < effect.maxStacks);
            if (existing != null)
            {
                existing.AddStacks(1);
                existing.Refresh();
                return existing;
            }
        }
        
        // Create new effect
        var activeEffect = new GAS_ActiveEffect(effect, source, target);
        
        // Handle instant effects
        if (effect.IsInstant)
        {
            // Apply modifiers immediately to base values
            foreach (var mod in effect.modifiers)
            {
                if (mod.operation == ModifierOp.Add)
                {
                    target.ModifyAttribute(mod.attribute, mod.value);
                }
                else
                {
                    float current = target.GetAttributeValue(mod.attribute);
                    float newVal = mod.Apply(current);
                    target.SetAttributeBaseValue(mod.attribute, newVal);
                }
            }
            
            // Grant/remove tags
            foreach (var tag in effect.grantTags)
            {
                if (tag != null && tag.IsValid(target.TagDatabase))
                    target.Tags.AddTagByName(tag.tagName);
            }
            foreach (var tag in effect.removeTags)
            {
                if (tag != null && tag.IsValid(target.TagDatabase))
                    target.Tags.RemoveTagByName(tag.tagName);
            }
            
            return activeEffect;
        }
        
        // Duration effect - add to active list
        target.activeEffects.Add(activeEffect);
        
        // Grant tags
        foreach (var tag in effect.grantTags)
        {
            if (tag != null && tag.IsValid(target.TagDatabase))
                target.Tags.AddTagByName(tag.tagName);
        }
        
        // Remove tags
        foreach (var tag in effect.removeTags)
        {
            if (tag != null && tag.IsValid(target.TagDatabase))
                target.Tags.RemoveTagByName(tag.tagName);
        }
        
        // Handle periodic
        if (effect.isPeriodic)
        {
            activeEffect.OnPeriodicTick += OnPeriodicTick;
        }
        
        target.OnEffectApplied?.Invoke(activeEffect);
        return activeEffect;
    }
    
    private void OnPeriodicTick(GAS_ActiveEffect effect)
    {
        // Apply modifiers each tick
        foreach (var mod in effect.Definition.modifiers)
        {
            if (mod.operation == ModifierOp.Add)
            {
                effect.Target.ModifyAttribute(mod.attribute, mod.value * effect.Stacks);
            }
        }
    }
    
    public void RemoveEffect(GAS_ActiveEffect effect)
    {
        if (!activeEffects.Remove(effect)) return;
        
        // Remove granted tags
        foreach (var tag in effect.Definition.grantTags)
        {
            if (tag != null && tag.IsValid(TagDatabase))
                Tags.RemoveTagByName(tag.tagName);
        }
        
        effect.OnPeriodicTick -= OnPeriodicTick;
        OnEffectRemoved?.Invoke(effect);
    }
    
    public void RemoveAllEffects()
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            RemoveEffect(activeEffects[i]);
        }
    }
    
    /// <summary>
    /// Apply a cooldown tag that auto-removes after duration
    /// </summary>
    public void ApplyCooldown(string cooldownTagName, float duration)
    {
        Tags.AddTagByName(cooldownTagName);
        StartCoroutine(RemoveCooldownAfter(cooldownTagName, duration));
    }
    
    private System.Collections.IEnumerator RemoveCooldownAfter(string tagName, float duration)
    {
        yield return new WaitForSeconds(duration);
        Tags.RemoveTagByName(tagName);
    }
    
    #endregion
    
    #region Abilities
    
    public GAS_AbilityInstance GrantAbility(GAS_Ability ability)
    {
        if (ability == null) return null;
        
        string key = ability.abilityTag?.tagName ?? ability.name;
        Debug.Log($"[GAS] GrantAbility: Registering ability with key='{key}' (tag='{ability.abilityTag?.tagName}', name='{ability.name}')");
        
        if (abilityMap.ContainsKey(key)) return abilityMap[key];
        
        var instance = new GAS_AbilityInstance(ability, this);
        abilities.Add(instance);
        abilityMap[key] = instance;
        
        instance.OnActivated += (a) => OnAbilityActivated?.Invoke(a);
        instance.OnEnded += (a, _) => OnAbilityEnded?.Invoke(a);
        
        return instance;
    }
    
    public void RemoveAbility(GAS_Ability ability)
    {
        if (ability == null) return;
        
        string key = ability.abilityTag?.tagName ?? ability.name;
        if (abilityMap.TryGetValue(key, out var instance))
        {
            if (instance.IsActive)
                instance.Cancel();
            
            abilities.Remove(instance);
            abilityMap.Remove(key);
        }
    }
    
    public bool TryActivateAbility(string abilityTagName)
    {
        Debug.Log($"[GAS] TryActivateAbility called with key: '{abilityTagName}'");
        Debug.Log($"[GAS] Available abilities: {string.Join(", ", abilityMap.Keys)}");
        
        if (abilityMap.TryGetValue(abilityTagName, out var instance))
        {
            Debug.Log($"[GAS] Found ability instance, attempting activation...");
            bool result = instance.TryActivate();
            Debug.Log($"[GAS] TryActivate returned: {result}");
            return result;
        }
        Debug.LogWarning($"[GAS] No ability found with key: '{abilityTagName}'");
        return false;
    }
    
    public bool TryActivateAbility(GAS_Ability ability)
    {
        string key = ability.abilityTag?.tagName ?? ability.name;
        return TryActivateAbility(key);
    }
    
    public void CancelAbility(string abilityTagName)
    {
        if (abilityMap.TryGetValue(abilityTagName, out var instance))
        {
            instance.Cancel();
        }
    }
    
    public void CancelAllAbilities()
    {
        foreach (var ability in abilities)
        {
            if (ability.IsActive)
                ability.Cancel();
        }
    }
    
    public bool HasAbility(string abilityTagName)
    {
        return abilityMap.ContainsKey(abilityTagName);
    }
    
    public bool IsAbilityActive(string abilityTagName)
    {
        return abilityMap.TryGetValue(abilityTagName, out var instance) && instance.IsActive;
    }
    
    #endregion
}
