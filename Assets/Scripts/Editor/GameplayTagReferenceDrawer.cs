#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;

[CustomPropertyDrawer(typeof(GameplayTagReference))]
public class GameplayTagReferenceDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        var tagNameProp = property.FindPropertyRelative("tagName");
        
        var db = GameplayTagEditorCache.Database;
        
        if (db == null)
        {
            // Show warning and text field if no database
            var warningRect = new Rect(position.x, position.y, position.width - 80, position.height);
            var buttonRect = new Rect(position.xMax - 75, position.y, 75, position.height);
            
            EditorGUI.PropertyField(warningRect, tagNameProp, label);
            
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.yellow;
            if (GUI.Button(buttonRect, "Create DB"))
            {
                CreateDefaultDatabase();
            }
            GUI.backgroundColor = oldColor;
            
            EditorGUI.EndProperty();
            return;
        }
        
        var tagNames = GameplayTagEditorCache.TagNames;
        var displayNames = GameplayTagEditorCache.DisplayNames;
        
        int currentIndex = GameplayTagEditorCache.GetTagIndex(tagNameProp.stringValue);
        
        // Draw label
        var labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height);
        var popupRect = new Rect(position.x + EditorGUIUtility.labelWidth + 2, position.y, 
                                  position.width - EditorGUIUtility.labelWidth - 22, position.height);
        var searchRect = new Rect(position.xMax - 18, position.y, 18, position.height);
        
        EditorGUI.LabelField(labelRect, label);
        
        // Draw popup with hierarchical display names
        int newIndex = EditorGUI.Popup(popupRect, currentIndex, displayNames);
        
        if (newIndex != currentIndex)
        {
            tagNameProp.stringValue = GameplayTagEditorCache.GetTagAtIndex(newIndex);
        }
        
        // Search button for advanced picker
        if (GUI.Button(searchRect, EditorGUIUtility.IconContent("d_Search Icon"), EditorStyles.iconButton))
        {
            GameplayTagPickerWindow.Show(tagNameProp, db);
        }
        
        // Show warning if tag doesn't exist
        if (!string.IsNullOrEmpty(tagNameProp.stringValue) && 
            !db.TagExists(tagNameProp.stringValue))
        {
            var warningRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight, 
                                        position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.HelpBox(warningRect, $"Tag '{tagNameProp.stringValue}' not found in database!", MessageType.Warning);
        }
        
        EditorGUI.EndProperty();
    }
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var tagNameProp = property.FindPropertyRelative("tagName");
        var db = GameplayTagEditorCache.Database;
        
        if (db != null && !string.IsNullOrEmpty(tagNameProp.stringValue) && !db.TagExists(tagNameProp.stringValue))
        {
            return EditorGUIUtility.singleLineHeight * 2 + 2;
        }
        
        return EditorGUIUtility.singleLineHeight;
    }
    
    private void CreateDefaultDatabase()
    {
        var db = ScriptableObject.CreateInstance<GameplayTagDatabase>();
        
        // Create default hierarchy
        var defaultTags = new string[]
        {
            "State",
            "State.Movement",
            "State.Movement.Idle",
            "State.Movement.Walking",
            "State.Movement.Running",
            "State.Movement.Jumping",
            "State.Combat",
            "State.Combat.Attacking",
            "State.Combat.Blocking",
            "State.Status",
            "State.Status.Stunned",
            "State.Status.Invulnerable",
            "Ability",
            "Ability.Active",
            "Ability.Cooldown",
            "Event",
            "Event.Damage",
            "Event.Heal"
        };
        
        foreach (var tag in defaultTags)
        {
            db.AddTag(tag);
        }
        
        string path = "Assets/GameplayTagDatabase.asset";
        AssetDatabase.CreateAsset(db, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        GameplayTagEditorCache.MarkDirty();
        
        Debug.Log($"Created GameplayTagDatabase at {path}");
        Selection.activeObject = db;
    }
}
#endif