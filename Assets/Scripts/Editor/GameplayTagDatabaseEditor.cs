#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Custom inspector for GameplayTagDatabase with hierarchical tree editing
/// </summary>
[CustomEditor(typeof(GameplayTagDatabase))]
public class GameplayTagDatabaseEditor : Editor
{
    private GameplayTagDatabase database;
    private string newTagName = "";
    private Vector2 scrollPosition;
    private HashSet<string> expandedPaths = new HashSet<string>();
    private string selectedTag = null;
    private bool showAddPanel = false;
    private string searchFilter = "";
    
    private GUIStyle headerStyle;
    private GUIStyle selectedStyle;
    
    private void OnEnable()
    {
        database = (GameplayTagDatabase)target;
    }
    
    public override void OnInspectorGUI()
    {
        InitStyles();
        
        // Header
        EditorGUILayout.LabelField("Gameplay Tag Database", headerStyle);
        EditorGUILayout.Space(10);
        
        // Stats
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField($"Total Tags: {database.Tags.Count}");
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(10);
        
        // Toolbar
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        
        if (GUILayout.Button(showAddPanel ? "Hide Add" : "Add Tag", EditorStyles.toolbarButton, GUILayout.Width(80)))
        {
            showAddPanel = !showAddPanel;
        }
        
        if (GUILayout.Button("Expand All", EditorStyles.toolbarButton, GUILayout.Width(80)))
        {
            ExpandAll();
        }
        
        if (GUILayout.Button("Collapse All", EditorStyles.toolbarButton, GUILayout.Width(80)))
        {
            expandedPaths.Clear();
        }
        
        GUILayout.FlexibleSpace();
        
        // Search
        searchFilter = EditorGUILayout.TextField(searchFilter, EditorStyles.toolbarSearchField, GUILayout.Width(200));
        if (GUILayout.Button("", GUI.skin.FindStyle("ToolbarSearchCancelButton")))
        {
            searchFilter = "";
            GUI.FocusControl(null);
        }
        
        EditorGUILayout.EndHorizontal();
        
        // Add panel
        if (showAddPanel)
        {
            DrawAddPanel();
        }
        
        EditorGUILayout.Space(5);
        
        // Tag tree
        EditorGUILayout.LabelField("Tags", EditorStyles.boldLabel);
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.MinHeight(300));
        
        if (string.IsNullOrEmpty(searchFilter))
        {
            DrawTagTree();
        }
        else
        {
            DrawFilteredList();
        }
        
        EditorGUILayout.EndScrollView();
        
        // Selected tag actions
        if (!string.IsNullOrEmpty(selectedTag))
        {
            DrawSelectedTagPanel();
        }
        
        // Apply changes
        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
        }
    }
    
    private void InitStyles()
    {
        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 16;
            headerStyle.alignment = TextAnchor.MiddleCenter;
        }
        
        if (selectedStyle == null)
        {
            selectedStyle = new GUIStyle(EditorStyles.label);
            selectedStyle.normal.background = CreateColorTexture(new Color(0.2f, 0.4f, 0.8f, 0.3f));
        }
    }
    
    private Texture2D CreateColorTexture(Color color)
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return tex;
    }
    
    private void DrawAddPanel()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        EditorGUILayout.LabelField("Add New Tag", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        
        // If a tag is selected, show option to add as child
        if (!string.IsNullOrEmpty(selectedTag))
        {
            EditorGUILayout.LabelField($"Parent: {selectedTag}", GUILayout.Width(200));
        }
        
        newTagName = EditorGUILayout.TextField("Tag Name:", newTagName);
        
        EditorGUILayout.EndHorizontal();
        
        // Preview
        string fullTagName = string.IsNullOrEmpty(selectedTag) ? newTagName : $"{selectedTag}.{newTagName}";
        
        if (!string.IsNullOrEmpty(newTagName))
        {
            EditorGUILayout.LabelField($"Full Path: {fullTagName}", EditorStyles.miniLabel);
        }
        
        EditorGUILayout.BeginHorizontal();
        
        GUI.enabled = !string.IsNullOrEmpty(newTagName) && IsValidTagName(newTagName);
        
        if (GUILayout.Button("Add Tag"))
        {
            AddTag(fullTagName);
            newTagName = "";
            GUI.FocusControl(null);
        }
        
        GUI.enabled = true;
        
        if (!string.IsNullOrEmpty(selectedTag) && GUILayout.Button("Add as Root"))
        {
            AddTag(newTagName);
            newTagName = "";
            GUI.FocusControl(null);
        }
        
        EditorGUILayout.EndHorizontal();
        
        // Validation message
        if (!string.IsNullOrEmpty(newTagName) && !IsValidTagName(newTagName))
        {
            EditorGUILayout.HelpBox("Tag name can only contain letters, numbers, and underscores.", MessageType.Warning);
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private bool IsValidTagName(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        
        foreach (char c in name)
        {
            if (!char.IsLetterOrDigit(c) && c != '_')
                return false;
        }
        return true;
    }
    
    private void DrawTagTree()
    {
        var rootNode = BuildTree();
        
        foreach (var child in rootNode.children)
        {
            DrawTreeNode(child, 0);
        }
        
        if (rootNode.children.Count == 0)
        {
            EditorGUILayout.HelpBox("No tags defined. Use 'Add Tag' to create tags.", MessageType.Info);
        }
    }
    
    private TagTreeNode BuildTree()
    {
        var root = new TagTreeNode { name = "Root", fullPath = "", depth = -1 };
        var nodeMap = new Dictionary<string, TagTreeNode>();
        nodeMap[""] = root;
        
        var sortedTags = database.Tags.OrderBy(t => t).ToList();
        
        foreach (var tag in sortedTags)
        {
            var parts = tag.Split('.');
            var parentPath = "";
            
            for (int i = 0; i < parts.Length; i++)
            {
                var currentPath = i == 0 ? parts[i] : parentPath + "." + parts[i];
                
                if (!nodeMap.ContainsKey(currentPath))
                {
                    var node = new TagTreeNode
                    {
                        name = parts[i],
                        fullPath = currentPath,
                        depth = i
                    };
                    
                    nodeMap[currentPath] = node;
                    nodeMap[parentPath].children.Add(node);
                }
                
                parentPath = currentPath;
            }
        }
        
        return root;
    }
    
    private void DrawTreeNode(TagTreeNode node, int depth)
    {
        bool hasChildren = node.children.Count > 0;
        bool isExpanded = expandedPaths.Contains(node.fullPath);
        bool isSelected = selectedTag == node.fullPath;
        bool isLeaf = database.Tags.Contains(node.fullPath);
        
        EditorGUILayout.BeginHorizontal();
        
        // Indent
        GUILayout.Space(depth * 20);
        
        // Expand/collapse
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
        
        // Selection rect
        var style = isSelected ? selectedStyle : EditorStyles.label;
        var rect = GUILayoutUtility.GetRect(new GUIContent(node.name), style, GUILayout.ExpandWidth(true));
        
        if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
        {
            if (selectedTag == node.fullPath)
                selectedTag = null; // Deselect
            else
                selectedTag = node.fullPath;
            
            Event.current.Use();
            Repaint();
        }
        
        // Icon
        var icon = hasChildren 
            ? EditorGUIUtility.IconContent("d_Folder Icon") 
            : EditorGUIUtility.IconContent("d_FilterByLabel");
        
        GUI.Label(new Rect(rect.x, rect.y, 18, rect.height), icon);
        
        // Label with virtual indicator
        string label = node.name;
        if (!isLeaf && !hasChildren)
            label += " (virtual)";
        
        GUI.Label(new Rect(rect.x + 20, rect.y, rect.width - 20, rect.height), label, style);
        
        EditorGUILayout.EndHorizontal();
        
        // Children
        if (hasChildren && isExpanded)
        {
            foreach (var child in node.children)
            {
                DrawTreeNode(child, depth + 1);
            }
        }
    }
    
    private void DrawFilteredList()
    {
        string search = searchFilter.ToLower();
        var matches = database.Tags.Where(t => t.ToLower().Contains(search)).OrderBy(t => t).ToList();
        
        foreach (var tag in matches)
        {
            bool isSelected = selectedTag == tag;
            var style = isSelected ? selectedStyle : EditorStyles.label;
            
            var rect = GUILayoutUtility.GetRect(new GUIContent(tag), style);
            
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                selectedTag = isSelected ? null : tag;
                Event.current.Use();
                Repaint();
            }
            
            GUI.Label(rect, tag, style);
        }
        
        if (matches.Count == 0)
        {
            EditorGUILayout.HelpBox($"No tags matching '{searchFilter}'", MessageType.Info);
        }
    }
    
    private void DrawSelectedTagPanel()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        EditorGUILayout.LabelField("Selected Tag", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(selectedTag);
        
        EditorGUILayout.Space(5);
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Copy Path"))
        {
            EditorGUIUtility.systemCopyBuffer = selectedTag;
        }
        
        if (database.Tags.Contains(selectedTag))
        {
            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("Delete Tag"))
            {
                if (EditorUtility.DisplayDialog("Delete Tag", 
                    $"Are you sure you want to delete '{selectedTag}'?\n\nThis will also remove all child tags.", 
                    "Delete", "Cancel"))
                {
                    DeleteTagAndChildren(selectedTag);
                    selectedTag = null;
                }
            }
            GUI.backgroundColor = Color.white;
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
    }
    
    private void AddTag(string tagPath)
    {
        if (string.IsNullOrEmpty(tagPath)) return;
        
        Undo.RecordObject(database, "Add Gameplay Tag");
        database.AddTag(tagPath);
        database.BuildHierarchy();
        GameplayTagEditorCache.ClearCache();
        EditorUtility.SetDirty(database);
    }
    
    private void DeleteTagAndChildren(string tagPath)
    {
        Undo.RecordObject(database, "Delete Gameplay Tag");
        
        // Find and remove all matching tags
        var toRemove = database.Tags.Where(t => t == tagPath || t.StartsWith(tagPath + ".")).ToList();
        foreach (var tag in toRemove)
        {
            database.RemoveTag(tag);
        }
        
        database.BuildHierarchy();
        GameplayTagEditorCache.ClearCache();
        EditorUtility.SetDirty(database);
    }
    
    private void ExpandAll()
    {
        foreach (var tag in database.Tags)
        {
            var parts = tag.Split('.');
            var path = "";
            for (int i = 0; i < parts.Length; i++)
            {
                path = i == 0 ? parts[i] : path + "." + parts[i];
                expandedPaths.Add(path);
            }
        }
    }
    
    // Helper class for tree building
    private class TagTreeNode
    {
        public string name;
        public string fullPath;
        public int depth;
        public List<TagTreeNode> children = new List<TagTreeNode>();
    }
}
#endif
