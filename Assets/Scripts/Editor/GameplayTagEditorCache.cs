#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Singleton cache for editor-time access to the gameplay tag database.
/// Automatically finds and caches the database asset.
/// </summary>
public static class GameplayTagEditorCache
{
    private static GameplayTagDatabase cachedDatabase;
    private static string[] cachedTagNames;
    private static string[] cachedDisplayNames;
    private static bool isDirty = true;
    
    public static GameplayTagDatabase Database
    {
        get
        {
            if (cachedDatabase == null)
                FindDatabase();
            return cachedDatabase;
        }
    }
    
    public static string[] TagNames
    {
        get
        {
            if (isDirty || cachedTagNames == null)
                RefreshCache();
            return cachedTagNames;
        }
    }
    
    public static string[] DisplayNames
    {
        get
        {
            if (isDirty || cachedDisplayNames == null)
                RefreshCache();
            return cachedDisplayNames;
        }
    }
    
    public static void MarkDirty()
    {
        isDirty = true;
        cachedDatabase = null;
    }
    
    public static void ClearCache()
    {
        MarkDirty();
    }
    
    private static void FindDatabase()
    {
        var guids = AssetDatabase.FindAssets("t:GameplayTagDatabase");
        if (guids.Length > 0)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            cachedDatabase = AssetDatabase.LoadAssetAtPath<GameplayTagDatabase>(path);
            
            if (guids.Length > 1)
            {
                Debug.LogWarning($"[GameplayTags] Multiple tag databases found. Using: {path}");
            }
        }
    }
    
    private static void RefreshCache()
    {
        if (Database == null)
        {
            cachedTagNames = new string[] { "(No Database)" };
            cachedDisplayNames = cachedTagNames;
            return;
        }
        
        var tags = Database.Tags.ToList();
        tags.Sort();
        
        // Add "None" option at the start
        var tagList = new List<string> { "(None)" };
        var displayList = new List<string> { "(None)" };
        
        foreach (var tag in tags)
        {
            tagList.Add(tag);
            
            // Build hierarchical display name
            int depth = tag.Count(c => c == '.');
            string indent = new string('\u00A0', depth * 4); // Non-breaking spaces
            string shortName = tag.Contains('.') ? tag.Substring(tag.LastIndexOf('.') + 1) : tag;
            displayList.Add($"{indent}{shortName}");
        }
        
        cachedTagNames = tagList.ToArray();
        cachedDisplayNames = displayList.ToArray();
        isDirty = false;
    }
    
    public static int GetTagIndex(string tagName)
    {
        if (string.IsNullOrEmpty(tagName))
            return 0;
        
        var tags = TagNames;
        for (int i = 0; i < tags.Length; i++)
        {
            if (tags[i] == tagName)
                return i;
        }
        return 0;
    }
    
    public static string GetTagAtIndex(int index)
    {
        var tags = TagNames;
        if (index <= 0 || index >= tags.Length)
            return string.Empty;
        return tags[index];
    }
    
    /// <summary>
    /// Build a hierarchical tree structure for advanced UI
    /// </summary>
    public static TagTreeNode BuildTagTree()
    {
        var root = new TagTreeNode { name = "Root", fullPath = "", depth = -1 };
        
        if (Database == null)
            return root;
        
        foreach (var tag in Database.Tags)
        {
            var parts = tag.Split('.');
            var current = root;
            var pathBuilder = new System.Text.StringBuilder();
            
            for (int i = 0; i < parts.Length; i++)
            {
                if (i > 0) pathBuilder.Append('.');
                pathBuilder.Append(parts[i]);
                
                var existing = current.children.Find(c => c.name == parts[i]);
                if (existing == null)
                {
                    existing = new TagTreeNode
                    {
                        name = parts[i],
                        fullPath = pathBuilder.ToString(),
                        depth = i,
                        isLeaf = i == parts.Length - 1 && Database.TagExists(pathBuilder.ToString())
                    };
                    current.children.Add(existing);
                }
                current = existing;
            }
        }
        
        return root;
    }
}

public class TagTreeNode
{
    public string name;
    public string fullPath;
    public int depth;
    public bool isLeaf;
    public bool isExpanded;
    public List<TagTreeNode> children = new List<TagTreeNode>();
}
#endif
