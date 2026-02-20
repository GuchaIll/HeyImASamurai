using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(menuName = "Gameplay Tags/Tag Database")]
public class GameplayTagDatabase : ScriptableObject
{
    [SerializeField]
    private List<string> tags = new List<string>();
    
    public IReadOnlyList<string> Tags => tags;
    
    // Runtime lookups
    private Dictionary<string, int> tagToId;
    private Dictionary<int, string> idToTag;
    private Dictionary<int, List<int>> parentMap; // tagId -> list of ancestor tagIds
    private Dictionary<int, List<int>> childMap;  // tagId -> list of descendant tagIds
    private bool isInitialized;
    
    public bool IsInitialized => isInitialized;
    
    public void Initialize()
    {
        tagToId = new Dictionary<string, int>();
        idToTag = new Dictionary<int, string>();
        
        for (int i = 0; i < tags.Count; i++)
        {
            tagToId[tags[i]] = i;
            idToTag[i] = tags[i];
        }
        
        BuildHierarchyInternal();
        isInitialized = true;
    }
    
    /// <summary>
    /// Rebuild the hierarchy maps. Call after modifying tags in editor.
    /// </summary>
    public void BuildHierarchy()
    {
        isInitialized = false;
        EnsureInitialized();
    }
    
    public void EnsureInitialized()
    {
        if (!isInitialized)
            Initialize();
    }
    
    private void OnEnable()
    {
        isInitialized = false;
    }
    
    public bool TryGetId(string tagName, out int id)
    {
        EnsureInitialized();
        return tagToId.TryGetValue(tagName, out id);
    }
    
    public int GetId(string tagName)
    {
        EnsureInitialized();
        return tagToId.TryGetValue(tagName, out int id) ? id : -1;
    }
    
    public string GetTagName(int id)
    {
        EnsureInitialized();
        return idToTag.TryGetValue(id, out string name) ? name : null;
    }
    
    public GameplayTag GetRuntimeTag(string tagName)
    {
        return new GameplayTag { id = GetId(tagName) };
    }
    
    public bool TagExists(string tagName)
    {
        EnsureInitialized();
        return tagToId.ContainsKey(tagName);
    }
    
    private void BuildHierarchyInternal()
    {
        parentMap = new Dictionary<int, List<int>>();
        childMap = new Dictionary<int, List<int>>();
        
        // Initialize child map
        for (int i = 0; i < tags.Count; i++)
        {
            childMap[i] = new List<int>();
        }
        
        // Build parent relationships
        for (int i = 0; i < tags.Count; i++)
        {
            var tagName = tags[i];
            var parents = new List<int>();
            
            while (tagName.Contains("."))
            {
                tagName = tagName.Substring(0, tagName.LastIndexOf('.'));
                if (tagToId.TryGetValue(tagName, out int parentId))
                {
                    parents.Add(parentId);
                    // Also add this tag as a child of the parent
                    childMap[parentId].Add(i);
                }
            }
            parentMap[i] = parents;
        }
    }
    
    /// <summary>
    /// Check if childId is a descendant of parentId in the tag hierarchy
    /// </summary>
    public bool IsChildOf(int childId, int parentId)
    {
        EnsureInitialized();
        if (parentMap.TryGetValue(childId, out var parents))
        {
            return parents.Contains(parentId);
        }
        return false;
    }
    
    /// <summary>
    /// Get all ancestor tag IDs for a given tag
    /// </summary>
    public IReadOnlyList<int> GetParentIds(int tagId)
    {
        EnsureInitialized();
        return parentMap.TryGetValue(tagId, out var parents) ? parents : new List<int>();
    }
    
    /// <summary>
    /// Get all descendant tag IDs for a given tag
    /// </summary>
    public IReadOnlyList<int> GetChildIds(int tagId)
    {
        EnsureInitialized();
        return childMap.TryGetValue(tagId, out var children) ? children : new List<int>();
    }
    
    /// <summary>
    /// Get tags formatted for hierarchical display
    /// </summary>
    public List<string> GetHierarchicalDisplayNames()
    {
        var result = new List<string>();
        foreach (var tag in tags)
        {
            int depth = tag.Count(c => c == '.');
            result.Add(new string(' ', depth * 2) + tag.Split('.').Last());
        }
        return result;
    }
    
    /// <summary>
    /// Get tags sorted hierarchically
    /// </summary>
    public List<string> GetSortedTags()
    {
        return tags.OrderBy(t => t).ToList();
    }
    
#if UNITY_EDITOR
    public void AddTag(string tagName)
    {
        if (!tags.Contains(tagName))
        {
            tags.Add(tagName);
            tags.Sort();
            isInitialized = false;
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
    
    public void RemoveTag(string tagName)
    {
        if (tags.Remove(tagName))
        {
            isInitialized = false;
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
    
    public void SetTags(List<string> newTags)
    {
        tags = new List<string>(newTags);
        tags.Sort();
        isInitialized = false;
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}