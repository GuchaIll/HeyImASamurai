#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Searchable hierarchical tag picker window - Unreal style
/// </summary>
public class GameplayTagPickerWindow : EditorWindow
{
    private SerializedProperty targetProperty;
    private GameplayTagDatabase database;
    private string searchText = "";
    private Vector2 scrollPosition;
    private TagTreeNode rootNode;
    private HashSet<string> expandedPaths = new HashSet<string>();
    private string selectedTag;
    private GUIStyle selectedStyle;
    private GUIStyle normalStyle;
    private GUIStyle searchFieldStyle;
    
    public static void Show(SerializedProperty property, GameplayTagDatabase db)
    {
        var window = GetWindow<GameplayTagPickerWindow>(true, "Select Gameplay Tag", true);
        window.targetProperty = property;
        window.database = db;
        window.selectedTag = property.stringValue;
        window.RefreshTree();
        window.minSize = new Vector2(300, 400);
        window.ShowUtility();
    }
    
    private void OnEnable()
    {
        RefreshTree();
    }
    
    private void RefreshTree()
    {
        rootNode = GameplayTagEditorCache.BuildTagTree();
        // Auto-expand to show selected tag
        if (!string.IsNullOrEmpty(selectedTag))
        {
            var parts = selectedTag.Split('.');
            var path = "";
            for (int i = 0; i < parts.Length - 1; i++)
            {
                path = i == 0 ? parts[i] : path + "." + parts[i];
                expandedPaths.Add(path);
            }
        }
    }
    
    private void OnGUI()
    {
        InitStyles();
        
        EditorGUILayout.Space(5);
        
        // Search bar
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUI.SetNextControlName("SearchField");
        searchText = EditorGUILayout.TextField(searchText, EditorStyles.toolbarSearchField);
        if (GUILayout.Button("", GUI.skin.FindStyle("ToolbarSearchCancelButton")))
        {
            searchText = "";
            GUI.FocusControl(null);
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        // Tree view
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        // None option
        DrawTagItem("(None)", "", 0);
        
        if (string.IsNullOrEmpty(searchText))
        {
            // Draw hierarchical tree
            foreach (var child in rootNode.children)
            {
                DrawTreeNode(child);
            }
        }
        else
        {
            // Draw filtered flat list
            DrawFilteredList();
        }
        
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.Space(5);
        
        // Bottom buttons
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Expand All"))
        {
            ExpandAll(rootNode);
        }
        if (GUILayout.Button("Collapse All"))
        {
            expandedPaths.Clear();
        }
        
        GUILayout.FlexibleSpace();
        
        if (GUILayout.Button("Cancel", GUILayout.Width(80)))
        {
            Close();
        }
        
        GUI.enabled = selectedTag != targetProperty.stringValue;
        if (GUILayout.Button("Select", GUILayout.Width(80)))
        {
            ApplySelection();
        }
        GUI.enabled = true;
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
    }
    
    private void InitStyles()
    {
        if (selectedStyle == null)
        {
            selectedStyle = new GUIStyle(EditorStyles.label);
            selectedStyle.normal.background = CreateColorTexture(new Color(0.2f, 0.4f, 0.8f, 0.5f));
            selectedStyle.hover.background = selectedStyle.normal.background;
        }
        
        if (normalStyle == null)
        {
            normalStyle = new GUIStyle(EditorStyles.label);
            normalStyle.hover.background = CreateColorTexture(new Color(0.5f, 0.5f, 0.5f, 0.2f));
        }
    }
    
    private Texture2D CreateColorTexture(Color color)
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return tex;
    }
    
    private void DrawTreeNode(TagTreeNode node)
    {
        bool hasChildren = node.children.Count > 0;
        bool isExpanded = expandedPaths.Contains(node.fullPath);
        
        EditorGUILayout.BeginHorizontal();
        
        // Indent
        GUILayout.Space(node.depth * 20);
        
        // Expand/collapse toggle
        if (hasChildren)
        {
            if (GUILayout.Button(isExpanded ? "▼" : "►", EditorStyles.miniLabel, GUILayout.Width(15)))
            {
                if (isExpanded)
                    expandedPaths.Remove(node.fullPath);
                else
                    expandedPaths.Add(node.fullPath);
            }
        }
        else
        {
            GUILayout.Space(19);
        }
        
        // Tag item
        bool isSelected = selectedTag == node.fullPath;
        var style = isSelected ? selectedStyle : normalStyle;
        
        var rect = GUILayoutUtility.GetRect(new GUIContent(node.name), style, GUILayout.ExpandWidth(true));
        
        if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
        {
            selectedTag = node.fullPath;
            
            if (Event.current.clickCount == 2)
            {
                ApplySelection();
            }
            
            Event.current.Use();
            Repaint();
        }
        
        // Icon for leaf vs parent
        var icon = hasChildren ? EditorGUIUtility.IconContent("d_Folder Icon") : EditorGUIUtility.IconContent("d_FilterByLabel");
        
        GUI.Label(new Rect(rect.x, rect.y, 18, rect.height), icon);
        GUI.Label(new Rect(rect.x + 20, rect.y, rect.width - 20, rect.height), node.name, style);
        
        EditorGUILayout.EndHorizontal();
        
        // Draw children if expanded
        if (hasChildren && isExpanded)
        {
            foreach (var child in node.children)
            {
                DrawTreeNode(child);
            }
        }
    }
    
    private void DrawTagItem(string displayName, string fullPath, int indent)
    {
        EditorGUILayout.BeginHorizontal();
        
        GUILayout.Space(indent * 20);
        
        bool isSelected = selectedTag == fullPath;
        var style = isSelected ? selectedStyle : normalStyle;
        
        var rect = GUILayoutUtility.GetRect(new GUIContent(displayName), style, GUILayout.ExpandWidth(true));
        
        if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
        {
            selectedTag = fullPath;
            
            if (Event.current.clickCount == 2)
            {
                ApplySelection();
            }
            
            Event.current.Use();
            Repaint();
        }
        
        GUI.Label(rect, displayName, style);
        
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawFilteredList()
    {
        string search = searchText.ToLower();
        
        foreach (var tag in database.Tags)
        {
            if (tag.ToLower().Contains(search))
            {
                DrawTagItem(tag, tag, 0);
            }
        }
    }
    
    private void ExpandAll(TagTreeNode node)
    {
        if (!string.IsNullOrEmpty(node.fullPath))
            expandedPaths.Add(node.fullPath);
        
        foreach (var child in node.children)
        {
            ExpandAll(child);
        }
    }
    
    private void ApplySelection()
    {
        if (targetProperty != null)
        {
            targetProperty.stringValue = selectedTag;
            targetProperty.serializedObject.ApplyModifiedProperties();
        }
        Close();
    }
}
#endif
