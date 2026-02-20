# Gameplay Ability System (GAS) - Complete Beginner's Guide

Welcome! This guide will teach you how to use the Gameplay Ability System (GAS) in your Unity project. This system is inspired by Unreal Engine's GAS and lets you create flexible abilities, effects, and character stats without writing code for every new feature.

---

## Table of Contents

1. [What is GAS?](#what-is-gas)
2. [Core Concepts](#core-concepts)
3. [Quick Start Tutorial](#quick-start-tutorial)
4. [Creating Attributes](#creating-attributes)
5. [Creating Attribute Profiles](#creating-attribute-profiles)
6. [Creating Effects](#creating-effects)
7. [Creating Abilities](#creating-abilities)
8. [Setting Up a Character](#setting-up-a-character)
9. [Using GAS in Scripts](#using-gas-in-scripts)
10. [Working with Tags](#working-with-tags)
11. [Common Recipes](#common-recipes)
12. [Troubleshooting](#troubleshooting)

---

## What is GAS?

The Gameplay Ability System is a framework for handling:

- **Attributes** - Stats like Health, Mana, Stamina, Attack Power, etc.
- **Effects** - Buffs, debuffs, damage, healing, status effects
- **Abilities** - Actions characters can perform (spells, attacks, dashes)
- **Tags** - Labels that track state (stunned, poisoned, on cooldown)

### Why Use GAS?

- **No code needed** for most content - create abilities and effects in the Unity Editor
- **Data-driven** - designers can tweak values without programmer help
- **Flexible** - different characters can have completely different attributes
- **Modular** - mix and match abilities, effects, and attributes

---

## Core Concepts

### The Big Picture

```
┌─────────────────────────────────────────────────────────────┐
│                    GAS_AbilitySystemComponent               │
│  (Attach this to any character - player, enemy, NPC)        │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌─────────────────┐  ┌─────────────────┐  ┌────────────┐  │
│  │   Attributes    │  │     Effects     │  │  Abilities │  │
│  │  (Health: 100)  │  │  (Burning DoT)  │  │  (Fireball)│  │
│  │  (Mana: 50)     │  │  (Speed Buff)   │  │  (Dash)    │  │
│  └─────────────────┘  └─────────────────┘  └────────────┘  │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │                    Tags                              │   │
│  │  ["State.Stunned", "Buff.Speed", "Ability.Cooldown"] │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

### Key Components

| Component | What It Does | Example |
|-----------|--------------|---------|
| **GAS_AttributeDefinition** | Defines one stat type | "Health", "Mana" |
| **GAS_AttributeProfile** | Bundles attributes for a character type | "PlayerProfile" with Health, Mana, Stamina |
| **GAS_Effect** | Modifies attributes or grants tags | Poison: -5 Health/sec for 10 seconds |
| **GAS_Ability** | An action a character can perform | Dash, Fireball, Heal |
| **GAS_AbilitySystemComponent** | The main component on characters | Manages everything above |

---

## Quick Start Tutorial

Let's create a simple player with Health that can be damaged. This takes about 5 minutes!

### Step 1: Create the Health Attribute

1. In Project window, **Right-click → Create → GAS → Attribute Definition**
2. Name it `Attr_Health`
3. Configure it:
   - **Attribute ID**: `Health`
   - **Display Name**: `Health`
   - **Default Value**: `100`
   - **Has Min Value**: ✓ (checked)
   - **Min Value**: `0`

### Step 2: Create a Max Health Attribute

1. **Right-click → Create → GAS → Attribute Definition**
2. Name it `Attr_MaxHealth`
3. Configure:
   - **Attribute ID**: `MaxHealth`
   - **Display Name**: `Max Health`
   - **Default Value**: `100`

### Step 3: Link Health to Max Health

1. Select `Attr_Health`
2. In **Max Attribute** field, drag in `Attr_MaxHealth`
   - This ensures Health can never exceed MaxHealth!

### Step 4: Create a Player Profile

1. **Right-click → Create → GAS → Attribute Profile**
2. Name it `Profile_Player`
3. Add attributes:
   - Click **+** under Attributes
   - Drag `Attr_Health` → Set **Base Value** to `100`
   - Click **+** again
   - Drag `Attr_MaxHealth` → Set **Base Value** to `100`

### Step 5: Set Up the Player

1. Create a **Cube** in the scene (or use your player object)
2. Add component: **GAS Ability System Component**
3. Add component: **Gameplay Tag Component** (required for GAS)
4. On GAS Ability System Component:
   - Drag `Profile_Player` into **Attribute Profile**
5. On Gameplay Tag Component:
   - Create or assign a **Gameplay Tag Database** (see [Working with Tags](#working-with-tags))

### Step 6: Test It!

Create a simple test script:

```csharp
using UnityEngine;

public class DamageTest : MonoBehaviour
{
    public float damageAmount = 10f;
    
    private GAS_AbilitySystemComponent gas;
    
    void Start()
    {
        gas = GetComponent<GAS_AbilitySystemComponent>();
    }
    
    void Update()
    {
        // Press Space to take damage
        if (Input.GetKeyDown(KeyCode.Space))
        {
            gas.ModifyAttribute("Health", -damageAmount);
            Debug.Log($"Health: {gas.GetAttributeValue("Health")}");
        }
    }
}
```

Press Play and hit Space - watch the Health decrease!

---

## Creating Attributes

Attributes are the building blocks of your RPG stats.

### How to Create an Attribute

1. **Right-click in Project → Create → GAS → Attribute Definition**
2. Name it descriptively (e.g., `Attr_Mana`, `Attr_Stamina`)

### Attribute Settings Explained

| Setting | What It Does | Example |
|---------|--------------|---------|
| **Attribute ID** | Unique identifier (auto-fills from filename) | `Health` |
| **Display Name** | Shown in UI | `Hit Points` |
| **Default Value** | Value if not specified in a profile | `100` |
| **Has Min Value** | Enable minimum clamping | ✓ for Health (can't go negative) |
| **Min Value** | Minimum allowed value | `0` |
| **Has Max Value** | Enable maximum clamping | Usually unchecked (use Max Attribute instead) |
| **Max Value** | Maximum allowed value | `999` |
| **Max Attribute** | Another attribute that caps this one | Health → MaxHealth |
| **Category** | For organizing in editor | `"Vitals"`, `"Combat"` |

### Common Attribute Patterns

#### Health/Max Health Pattern
```
Attr_Health:
  - Min Value: 0
  - Max Attribute: Attr_MaxHealth

Attr_MaxHealth:
  - Default Value: 100
```

#### Resource with Regeneration
```
Attr_Mana:
  - Min Value: 0
  - Max Attribute: Attr_MaxMana

Attr_MaxMana:
  - Default Value: 50

Attr_ManaRegen:
  - Default Value: 5 (per second)
```

#### Combat Stats
```
Attr_AttackPower:    Default: 10
Attr_Defense:        Default: 5
Attr_CritChance:     Default: 0.05 (5%)
Attr_CritMultiplier: Default: 2.0 (200% damage)
```

---

## Creating Attribute Profiles

Profiles bundle attributes together for different entity types.

### Why Use Profiles?

- **Player** needs: Health, Mana, Stamina, Experience
- **Goblin** needs: Health, AttackPower (no mana!)
- **Dragon** needs: Health, Rage, FireBreath cooldown

Each gets a different profile!

### Creating a Profile

1. **Right-click → Create → GAS → Attribute Profile**
2. Name it (e.g., `Profile_Goblin`)
3. Add attributes and set their **base values**:

```
Profile_Goblin:
  - Attr_Health: 30
  - Attr_MaxHealth: 30
  - Attr_AttackPower: 5
  - Attr_Defense: 2
```

### Profile Inheritance

Profiles can inherit from a parent! Great for shared stats.

**Example:**
```
Profile_BaseEnemy (parent):
  - Attr_Health: 50
  - Attr_MaxHealth: 50

Profile_Goblin (inherits from BaseEnemy):
  - Attr_AttackPower: 5  ← adds this
  - Attr_Health: 30      ← overrides parent's 50

Profile_Orc (inherits from BaseEnemy):
  - Attr_AttackPower: 15
  - Attr_Health: 100
```

To set inheritance:
1. Select child profile
2. Drag parent profile into **Parent Profile** field

---

## Creating Effects

Effects are temporary (or permanent) modifications to attributes and tags.

### Basic Effect: Instant Damage

1. **Right-click → Create → GAS → Effect**
2. Name it `Effect_Damage_50`
3. Settings:
   - **Duration**: `0` (instant)
   - **Modifiers**: Click **+**
     - **Attribute**: `Attr_Health`
     - **Operation**: `Add`
     - **Value**: `-50`

### Duration Effect: Speed Buff

1. Create new Effect: `Effect_SpeedBuff`
2. Settings:
   - **Duration**: `10` (lasts 10 seconds)
   - **Modifiers**: Click **+**
     - **Attribute**: `Attr_MoveSpeed`
     - **Operation**: `Multiply`
     - **Value**: `1.5` (50% faster)
   - **Grant Tags**: Click **+**
     - Add tag `Buff.Speed`

### Periodic Effect: Poison DoT

1. Create: `Effect_Poison`
2. Settings:
   - **Duration**: `5` (5 seconds total)
   - **Is Periodic**: ✓
   - **Period**: `1` (tick every 1 second)
   - **Modifiers**:
     - **Attribute**: `Attr_Health`
     - **Operation**: `Add`
     - **Value**: `-10` (10 damage per tick)
   - **Grant Tags**: `Status.Poisoned`

### Modifier Operations Explained

| Operation | Formula | Example |
|-----------|---------|---------|
| **Add** | base + value | Health 100 + (-50) = 50 |
| **Multiply** | base × value | Speed 5 × 1.5 = 7.5 |
| **Override** | = value | Set Health to exactly 100 |

### Effect Requirements

Effects can require or block based on tags:

```
Effect_Heal:
  - Require Tags: (none) 
  - Block If Has Tags: "Status.Unhealable"
  
  → Heal won't work on targets with "Status.Unhealable" tag
```

### Stacking Effects

```
Effect_Bleed:
  - Can Stack: ✓
  - Max Stacks: 5
  - Modifiers: Health -5 per stack
  
  → At 5 stacks, deals -25 Health per tick!
```

---

## Creating Abilities

Abilities are actions characters can perform.

### Simple Ability: Fireball

1. **Right-click → Create → GAS → Ability**
2. Name it `Ability_Fireball`
3. Settings:
   - **Display Name**: `Fireball`
   - **Cost Attribute**: `Attr_Mana`
   - **Cost Amount**: `25`
   - **Cooldown**: `2` (seconds)
   - **Cooldown Tag**: `Ability.Fireball.Cooldown`
   - **Effects On Activate**: Add your damage effect

### Ability with Custom Logic

For complex abilities, create a script:

```csharp
using UnityEngine;

[CreateAssetMenu(menuName = "GAS/Abilities/Teleport")]
public class Ability_Teleport : GAS_Ability
{
    public float teleportDistance = 10f;
    
    public override float OnActivate(GAS_AbilitySystemComponent owner)
    {
        // Teleport forward
        owner.transform.position += owner.transform.forward * teleportDistance;
        
        // Play effects, sounds, etc.
        
        return 0f; // 0 = instant ability
    }
}
```

### Ability Lifecycle

| Method | When It's Called | Use For |
|--------|------------------|---------|
| `CanActivate()` | Before activation | Custom activation checks |
| `OnActivate()` | When ability starts | Main ability logic |
| `OnTick()` | Every frame while active | Channeled abilities |
| `OnEnd()` | When ability finishes | Cleanup |

### Return Values from OnActivate

- Return `0` → Instant ability (ends immediately)
- Return `> 0` → Duration ability (OnTick called until time expires)
- Return `-1` → Infinite duration (must be cancelled manually)

---

## Setting Up a Character

### Required Components

Every character using GAS needs:

1. **GameplayTagComponent** - Manages tags
2. **GAS_AbilitySystemComponent** - Manages abilities/effects/attributes

### Setup Checklist

```
□ Add GameplayTagComponent
  □ Assign Gameplay Tag Database
  
□ Add GAS_AbilitySystemComponent  
  □ Assign Attribute Profile
  □ (Optional) Add Starting Abilities
```

### Example: Complete Enemy Setup

```
Enemy GameObject:
├── GameplayTagComponent
│   └── Database: MainTagDatabase
│   └── Initial Tags: ["Enemy", "Faction.Hostile"]
│
└── GAS_AbilitySystemComponent
    └── Attribute Profile: Profile_Goblin
    └── Starting Abilities:
        ├── Ability_BasicAttack
        └── Ability_Flee
```

---

## Using GAS in Scripts

### Getting the Component

```csharp
// In a MonoBehaviour
GAS_AbilitySystemComponent gas;

void Start()
{
    gas = GetComponent<GAS_AbilitySystemComponent>();
}
```

### Working with Attributes

```csharp
// Simple string-based access (recommended!)
float health = gas.GetAttributeValue("Health");
float mana = gas.GetAttributeValue("Mana");

// Modify by string ID
gas.ModifyAttribute("Health", -25f);  // Take 25 damage
gas.ModifyAttribute("Mana", 10f);     // Restore 10 mana

// Set value directly
gas.SetAttributeBaseValue("Health", 100f);

// Check if entity has an attribute
if (gas.HasAttribute("Health"))
{
    // This entity has health
}

// Get base value (without temporary effect modifiers)
float baseHealth = gas.GetAttributeBaseValue("Health");

// Alternative: Use ScriptableObject references (for type safety)
public GAS_AttributeDefinition healthAttr;  // Assign in Inspector
float health = gas.GetAttributeValue(healthAttr);
gas.ModifyAttribute(healthAttr, -25f);

// Get ratio (useful for UI bars)
float healthPercent = gas.GetAttributeRatio(healthAttr);  // Returns 0.0 to 1.0
```

**Note:** String lookups match against both the `attributeId` and asset name, and are case-insensitive. So `"health"`, `"Health"`, and `"HEALTH"` all work!

### Applying Effects

```csharp
public GAS_Effect damageEffect;
public GAS_Effect buffEffect;

// Apply effect to self
gas.ApplyEffectToSelf(damageEffect);

// Apply effect to another target
GAS_AbilitySystemComponent target = enemy.GetComponent<GAS_AbilitySystemComponent>();
gas.ApplyEffectToTarget(buffEffect, target);

// Check active effects
foreach (var effect in gas.ActiveEffects)
{
    Debug.Log($"Active: {effect.Definition.name}, Time Left: {effect.RemainingTime}");
}

// Remove all effects
gas.RemoveAllEffects();
```

### Using Abilities

```csharp
public GAS_Ability fireballAbility;

// Grant an ability (do once, e.g., when learning a skill)
gas.GrantAbility(fireballAbility);

// Try to activate by ability reference
if (gas.TryActivateAbility(fireballAbility))
{
    Debug.Log("Fireball cast!");
}

// Try to activate by tag name
gas.TryActivateAbility("Ability.Fireball");

// Check if ability is available
string abilityTag = "Ability.Fireball";
bool hasAbility = gas.HasAbility(abilityTag);
bool isActive = gas.IsAbilityActive(abilityTag);

// Cancel ability
gas.CancelAbility("Ability.Fireball");
gas.CancelAllAbilities();

// Remove ability (e.g., unlearning)
gas.RemoveAbility(fireballAbility);
```

### Listening to Events

```csharp
void OnEnable()
{
    gas.OnAttributeChanged += HandleAttributeChanged;
    gas.OnEffectApplied += HandleEffectApplied;
    gas.OnEffectRemoved += HandleEffectRemoved;
    gas.OnAbilityActivated += HandleAbilityActivated;
    gas.OnAbilityEnded += HandleAbilityEnded;
}

void OnDisable()
{
    gas.OnAttributeChanged -= HandleAttributeChanged;
    gas.OnEffectApplied -= HandleEffectApplied;
    gas.OnEffectRemoved -= HandleEffectRemoved;
    gas.OnAbilityActivated -= HandleAbilityActivated;
    gas.OnAbilityEnded -= HandleAbilityEnded;
}

void HandleAttributeChanged(GAS_AttributeDefinition attr, float oldValue, float newValue)
{
    Debug.Log($"{attr.displayName}: {oldValue} → {newValue}");
    
    // Check for death
    if (attr.attributeId == "Health" && newValue <= 0)
    {
        Die();
    }
}

void HandleEffectApplied(GAS_ActiveEffect effect)
{
    Debug.Log($"Effect applied: {effect.Definition.name}");
}

void HandleEffectRemoved(GAS_ActiveEffect effect)
{
    Debug.Log($"Effect removed: {effect.Definition.name}");
}

void HandleAbilityActivated(GAS_AbilityInstance ability)
{
    Debug.Log($"Ability used: {ability.Definition.displayName}");
}

void HandleAbilityEnded(GAS_AbilityInstance ability)
{
    Debug.Log($"Ability ended: {ability.Definition.displayName}");
}
```

---

## Working with Tags

Tags are labels that track state. GAS uses the Gameplay Tag system for requirements and blocking.

### Setting Up the Tag Database

1. **Right-click → Create → GameplayTag Database**
2. Name it `MainTagDatabase`
3. Add your tags (they're hierarchical with dots):

```
State
State.Stunned
State.Moving
State.Attacking

Status
Status.Poisoned
Status.Burning
Status.Frozen

Buff
Buff.Speed
Buff.Strength
Buff.Shield

Ability
Ability.Cooldown
Ability.Fireball.Cooldown
Ability.Dash.Cooldown
```

### Using Tags in Effects

**Grant Tags** - Added when effect is applied:
```
Effect_Stun:
  - Duration: 3
  - Grant Tags: ["State.Stunned"]
  
  → Target gets "State.Stunned" for 3 seconds
```

**Remove Tags** - Removed when effect is applied:
```
Effect_Cleanse:
  - Duration: 0 (instant)
  - Remove Tags: ["Status.Poisoned", "Status.Burning"]
  
  → Instantly removes poison and burning
```

### Using Tags in Abilities

**Require Tags** - Must have these tags to activate:
```
Ability_FinishingBlow:
  - Require Tags: Enemy has "State.Stunned"
  
  → Can only use on stunned enemies
```

**Blocked By Tags** - Can't activate if these tags present:
```
Ability_Dash:
  - Blocked By Tags: "State.Stunned", "State.Rooted"
  
  → Can't dash while stunned or rooted
```

**Cooldown Tags** - Applied during cooldown:
```
Ability_Fireball:
  - Cooldown: 5
  - Cooldown Tag: "Ability.Fireball.Cooldown"
  
  → Can't cast again until tag is removed (5 seconds)
```

### Working with Tags in Code

```csharp
GameplayTagComponent tags = GetComponent<GameplayTagComponent>();

// Add a tag
tags.AddTagByName("State.Stunned");

// Remove a tag
tags.RemoveTagByName("State.Stunned");

// Check if has tag (includes hierarchy - "State" matches "State.Stunned")
bool isStunned = tags.HasTag(stunnedTag);

// Check exact tag (no hierarchy)
bool hasExactTag = tags.HasTagExact(stunnedTag);
```

---

## Common Recipes

### Recipe: Damage Over Time (DoT)

```
Effect_Poison:
  Duration: 10
  Is Periodic: ✓
  Period: 1
  Modifiers:
    - Health, Add, -5
  Grant Tags: Status.Poisoned
```

### Recipe: Heal Over Time (HoT)

```
Effect_Regeneration:
  Duration: 15
  Is Periodic: ✓
  Period: 1
  Modifiers:
    - Health, Add, +10
  Grant Tags: Buff.Regenerating
```

### Recipe: Percentage Damage

```
Effect_ExecuteDamage:
  Duration: 0 (instant)
  Modifiers:
    - Health, Multiply, 0.5  ← Cuts health in half!
```

### Recipe: Stacking Bleed

```
Effect_Bleed:
  Duration: 5
  Can Stack: ✓
  Max Stacks: 5
  Is Periodic: ✓
  Period: 1
  Modifiers:
    - Health, Add, -3 (per stack)
  Grant Tags: Status.Bleeding
```

### Recipe: Shield/Barrier

Create a new attribute:
```
Attr_Shield:
  Min Value: 0
```

Create absorb effect:
```
Effect_Shield:
  Duration: 10
  Modifiers:
    - Shield, Add, 50
  Grant Tags: Buff.Shielded
```

In your damage code:
```csharp
void TakeDamage(float amount)
{
    float shield = gas.GetAttributeValue("Shield");
    
    if (shield > 0)
    {
        float absorbed = Mathf.Min(shield, amount);
        gas.ModifyAttribute("Shield", -absorbed);
        amount -= absorbed;
    }
    
    if (amount > 0)
    {
        gas.ModifyAttribute("Health", -amount);
    }
}
```

### Recipe: Ability with Charges

```csharp
[CreateAssetMenu(menuName = "GAS/Abilities/Blink")]
public class Ability_Blink : GAS_Ability
{
    public int maxCharges = 3;
    public float chargeRegenTime = 5f;
    
    [System.NonSerialized]
    private int currentCharges;
    
    public override bool CanActivate(GAS_AbilitySystemComponent owner, GameplayTagDatabase db)
    {
        return currentCharges > 0 && base.CanActivate(owner, db);
    }
    
    public override float OnActivate(GAS_AbilitySystemComponent owner)
    {
        currentCharges--;
        // Start recharge coroutine...
        return 0f;
    }
}
```

---

## Troubleshooting

### "Attribute not found" / Value always 0

**Cause:** Attribute not in the profile, or profile not assigned.

**Fix:**
1. Check **GAS_AbilitySystemComponent** has a profile assigned
2. Check the profile includes your attribute
3. Make sure attribute ScriptableObject is saved

### "Effect not applying"

**Cause:** Tag requirements not met.

**Fix:**
1. Check effect's **Require Tags** - target must have these
2. Check effect's **Block If Has Tags** - target must NOT have these
3. Verify tag database is assigned to GameplayTagComponent

### "Ability won't activate"

**Checklist:**
1. Is ability granted? (`gas.GrantAbility(ability)`)
2. Is it on cooldown? (check for cooldown tag)
3. Can owner pay the cost? (check attribute value)
4. Are tag requirements met?

**Debug with:**
```csharp
Debug.Log($"Has ability: {gas.HasAbility(tagName)}");
Debug.Log($"Mana: {gas.GetAttributeValue(\"Mana\")}");
Debug.Log($"Has cooldown tag: {tags.HasTagByName(\"Ability.Cooldown\")}");
```

### "Tags not working"

**Cause:** GameplayTagComponent missing or database not assigned.

**Fix:**
1. Add **GameplayTagComponent** to the GameObject
2. Assign your **GameplayTagDatabase** to it
3. Make sure tags exist in the database

### Effect modifiers not stacking correctly

**Order of operations:**
1. Override (replaces value)
2. Multiply (multiplies result)
3. Add (adds to result)

If you have both `Multiply × 2` and `Add + 10`:
- Base: 100
- After multiply: 200
- After add: 210

### Performance Tips

1. **Cache references** - Don't call `GetComponent` every frame
2. **Use events** - React to changes instead of polling
3. **Limit active effects** - Consider max effect counts for enemies
4. **Profile profiles** - Keep attribute counts reasonable

---

## Quick Reference

### Create Menu Paths

| Asset | Menu Path |
|-------|-----------|
| Attribute | Create → GAS → Attribute Definition |
| Profile | Create → GAS → Attribute Profile |
| Effect | Create → GAS → Effect |
| Ability | Create → GAS → Ability |
| Tag Database | Create → GameplayTag Database |

### Key Methods

```csharp
// Attributes (string-based - recommended!)
gas.GetAttributeValue("Health")
gas.GetAttributeBaseValue("Health")
gas.SetAttributeBaseValue("Health", 100f)
gas.ModifyAttribute("Health", -25f)
gas.HasAttribute("Health")

// Attributes (ScriptableObject reference - for type safety)
gas.GetAttributeValue(healthAttr)
gas.GetAttributeRatio(healthAttr)

// Effects
gas.ApplyEffectToSelf(effect)
gas.ApplyEffectToTarget(effect, target)
gas.RemoveEffect(activeEffect)
gas.RemoveAllEffects()

// Abilities
gas.GrantAbility(ability)
gas.RemoveAbility(ability)
gas.TryActivateAbility(ability)
gas.TryActivateAbility("tag.name")
gas.CancelAbility("tag.name")
gas.HasAbility("tag.name")
gas.IsAbilityActive("tag.name")

// Tags
tags.AddTagByName("Tag.Name")
tags.RemoveTagByName("Tag.Name")
tags.HasTag(runtimeTag)
```

### Events

```csharp
gas.OnAttributeChanged += (attr, oldVal, newVal) => { };
gas.OnEffectApplied += (effect) => { };
gas.OnEffectRemoved += (effect) => { };
gas.OnAbilityActivated += (ability) => { };
gas.OnAbilityEnded += (ability) => { };
```

---

## Need More Help?

- Check the example abilities in `GAS/Abilities/ExampleAbilities.cs`
- Look at the source code - it's well documented!
- Experiment in a test scene before adding to your main project

Happy game making! 🎮
