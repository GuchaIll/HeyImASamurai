using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class GameplayTagContainer
{
    [SerializeField]
    private List<int> tagIds = new List<int>();
    
    // Runtime lookup for fast queries
    private HashSet<int> tagSet;
    
    public IReadOnlyList<int> Tags => tagIds;
    
    public void EnsureInitialized()
    {
        if (tagSet == null)
            tagSet = new HashSet<int>(tagIds);
    }
    
    public void AddTag(GameplayTag tag)
    {
        EnsureInitialized();
        if (tagSet.Add(tag.id))
            tagIds.Add(tag.id);
    }
    
    public void RemoveTag(GameplayTag tag)
    {
        EnsureInitialized();
        if (tagSet.Remove(tag.id))
            tagIds.Remove(tag.id);
    }
    
    public bool HasTagExact(GameplayTag tag)
    {
        EnsureInitialized();
        return tagSet.Contains(tag.id);
    }
    
    public bool HasTag(GameplayTag tag, GameplayTagDatabase db)
    {
        EnsureInitialized();
        if (tagSet.Contains(tag.id))
            return true;
        
        // Check if any owned tag is a child of the queried tag
        foreach (var ownedId in tagIds)
        {
            if (db.IsChildOf(ownedId, tag.id))
                return true;
        }
        return false;
    }
    
    public bool HasAny(GameplayTagContainer other)
    {
        EnsureInitialized();
        other.EnsureInitialized();
        
        foreach (var t in other.tagIds)
        {
            if (tagSet.Contains(t))
                return true;
        }
        return false;
    }
    
    public bool HasAny(GameplayTagContainer other, GameplayTagDatabase db)
    {
        EnsureInitialized();
        other.EnsureInitialized();
        
        foreach (var otherId in other.tagIds)
        {
            if (HasTag(new GameplayTag { id = otherId }, db))
                return true;
        }
        return false;
    }
    
    public bool HasAll(GameplayTagContainer other)
    {
        EnsureInitialized();
        other.EnsureInitialized();
        
        foreach (var t in other.tagIds)
        {
            if (!tagSet.Contains(t))
                return false;
        }
        return true;
    }
    
    public bool HasAll(GameplayTagContainer other, GameplayTagDatabase db)
    {
        EnsureInitialized();
        other.EnsureInitialized();
        
        foreach (var otherId in other.tagIds)
        {
            if (!HasTag(new GameplayTag { id = otherId }, db))
                return false;
        }
        return true;
    }
    
    public void Clear()
    {
        tagIds.Clear();
        tagSet?.Clear();
    }
    
    public int Count => tagIds.Count;
}