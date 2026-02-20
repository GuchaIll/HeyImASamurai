using UnityEngine;

/// <summary>
/// Example: Dash ability with stamina cost and cooldown
/// Uses data-driven approach - assign attribute references in inspector
/// </summary>
[CreateAssetMenu(menuName = "GAS/Abilities/Dash")]
public class Ability_Dash : GAS_Ability
{
    [Header("Dash Settings")]
    public float dashDistance = 5f;
    public float dashDuration = 0.2f;
    
    public override float OnActivate(GAS_AbilitySystemComponent owner)
    {
        // Get movement direction (forward if no input)
        var rb = owner.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 dashDir = owner.transform.forward;
            rb.linearVelocity = dashDir * (dashDistance / dashDuration);
        }
        
        return dashDuration;
    }
    
    public override void OnEnd(GAS_AbilitySystemComponent owner, bool wasCancelled)
    {
        var rb = owner.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Reset velocity after dash
            rb.linearVelocity = Vector3.zero;
        }
    }
}

/// <summary>
/// Example: Heal ability that restores health over time
/// Demonstrates data-driven attribute references
/// </summary>
[CreateAssetMenu(menuName = "GAS/Abilities/Heal")]
public class Ability_Heal : GAS_Ability
{
    [Header("Heal Settings")]
    public float healAmount = 30f;
    public float healDuration = 3f;
    
    [Header("Attribute References")]
    [Tooltip("The health attribute to heal")]
    public GAS_AttributeDefinition healthAttribute;
    [Tooltip("The max health attribute for clamping")]
    public GAS_AttributeDefinition maxHealthAttribute;
    
    private float healPerTick;
    
    public override float OnActivate(GAS_AbilitySystemComponent owner)
    {
        healPerTick = healAmount / healDuration;
        return healDuration;
    }
    
    public override void OnTick(GAS_AbilitySystemComponent owner, float deltaTime)
    {
        if (healthAttribute == null) return;
        
        float heal = healPerTick * deltaTime;
        float current = owner.GetAttributeValue(healthAttribute);
        float max = maxHealthAttribute != null ? owner.GetAttributeValue(maxHealthAttribute) : float.MaxValue;
        
        if (current < max)
        {
            owner.ModifyAttribute(healthAttribute, Mathf.Min(heal, max - current));
        }
    }
}

/// <summary>
/// Example: Basic attack ability with data-driven attributes
/// </summary>
[CreateAssetMenu(menuName = "GAS/Abilities/Basic Attack")]
public class Ability_BasicAttack : GAS_Ability
{
    [Header("Attack Settings")]
    public float baseDamage = 10f;
    public float range = 2f;
    public LayerMask targetMask;
    
    [Header("Attribute References")]
    [Tooltip("Attacker's power attribute (optional)")]
    public GAS_AttributeDefinition attackPowerAttribute;
    [Tooltip("Target's defense attribute (optional)")]
    public GAS_AttributeDefinition defenseAttribute;
    [Tooltip("Target's health attribute")]
    public GAS_AttributeDefinition targetHealthAttribute;
    
    public override float OnActivate(GAS_AbilitySystemComponent owner)
    {
        // Find targets in range
        var hits = Physics.OverlapSphere(owner.transform.position, range, targetMask);
        
        foreach (var hit in hits)
        {
            if (hit.gameObject == owner.gameObject) continue;
            
            var target = hit.GetComponent<GAS_AbilitySystemComponent>();
            if (target != null && targetHealthAttribute != null)
            {
                // Scale damage by attack power (if attribute assigned)
                float attackPower = attackPowerAttribute != null ? owner.GetAttributeValue(attackPowerAttribute) : 0f;
                float defense = defenseAttribute != null ? target.GetAttributeValue(defenseAttribute) : 0f;
                float finalDamage = (baseDamage + attackPower) - defense;
                
                if (finalDamage > 0)
                {
                    target.ModifyAttribute(targetHealthAttribute, -finalDamage);
                }
            }
        }
        
        return 0f; // Instant
    }
}
