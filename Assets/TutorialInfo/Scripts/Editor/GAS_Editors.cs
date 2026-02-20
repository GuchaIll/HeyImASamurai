#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom editor for GAS_Effect showing a summary preview
/// </summary>
[CustomEditor(typeof(GAS_Effect))]
public class GAS_EffectEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var effect = (GAS_Effect)target;
        
        // Summary box at top
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Summary", EditorStyles.boldLabel);
        
        // Duration type
        string durationType = effect.IsInstant ? " Instant" : 
                             effect.IsInfinite ? "∞ Infinite" : 
                             $"⏱ {effect.duration}s";
        EditorGUILayout.LabelField("Duration:", durationType);
        
        // Modifiers
        if (effect.modifiers != null && effect.modifiers.Count > 0)
        {
            EditorGUILayout.LabelField("Modifiers:");
            foreach (var mod in effect.modifiers)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField($"• {mod}");
                EditorGUI.indentLevel--;
            }
        }
        
        // Tags
        if (effect.grantTags != null && effect.grantTags.Count > 0)
        {
            string tagStr = string.Join(", ", effect.grantTags);
            EditorGUILayout.LabelField($"Grants: {tagStr}");
        }
        
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);
        
        // Draw default inspector
        DrawDefaultInspector();
    }
}

/// <summary>
/// Custom editor for GAS_Ability showing activation requirements
/// </summary>
[CustomEditor(typeof(GAS_Ability), true)]
public class GAS_AbilityEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var ability = (GAS_Ability)target;
        
        // Summary box
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Ability Summary", EditorStyles.boldLabel);
        
        // Cost
        if (ability.HasCost)
        {
            EditorGUILayout.LabelField($"Cost: {ability.costAmount} {ability.costAttribute}");
        }
        
        // Cooldown
        if (ability.HasCooldown)
        {
            EditorGUILayout.LabelField($"Cooldown: {ability.cooldown}s");
        }
        
        // Effects count
        int effectCount = (ability.effectsOnActivate?.Count ?? 0) + (ability.effectsOnEnd?.Count ?? 0);
        if (effectCount > 0)
        {
            EditorGUILayout.LabelField($"Effects: {effectCount}");
        }
        
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);
        
        DrawDefaultInspector();
    }
}

/// <summary>
/// Custom property drawer for GAS_Modifier
/// </summary>
[CustomPropertyDrawer(typeof(GAS_Modifier))]
public class GAS_ModifierDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        var attrProp = property.FindPropertyRelative("attribute");
        var opProp = property.FindPropertyRelative("operation");
        var valueProp = property.FindPropertyRelative("value");
        
        float width = position.width;
        float x = position.x;
        
        // Attribute (40%)
        EditorGUI.PropertyField(
            new Rect(x, position.y, width * 0.4f - 5, EditorGUIUtility.singleLineHeight),
            attrProp, GUIContent.none);
        
        // Operation (25%)
        x += width * 0.4f;
        EditorGUI.PropertyField(
            new Rect(x, position.y, width * 0.25f - 5, EditorGUIUtility.singleLineHeight),
            opProp, GUIContent.none);
        
        // Value (35%)
        x += width * 0.25f;
        EditorGUI.PropertyField(
            new Rect(x, position.y, width * 0.35f, EditorGUIUtility.singleLineHeight),
            valueProp, GUIContent.none);
        
        EditorGUI.EndProperty();
    }
}
#endif
