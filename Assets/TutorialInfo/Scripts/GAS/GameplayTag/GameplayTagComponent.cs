using UnityEngine;
using System.Collections.Generic;

public class GameplayTagComponent : MonoBehaviour
{
    [SerializeField]
    private GameplayTagDatabase database;
    
    [SerializeField]
    private List<GameplayTagReference> initialTags = new List<GameplayTagReference>();
    
    private GameplayTagContainer tagContainer;
    
    public GameplayTagContainer TagContainer
    {
        get
        {
            if (tagContainer == null)
                InitializeContainer();
            return tagContainer;
        }
    }
    
    public GameplayTagDatabase Database => database;
    
    private void Awake()
    {
        InitializeContainer();
    }
    
    private void InitializeContainer()
    {
        tagContainer = new GameplayTagContainer();
        
        if (database != null)
        {
            database.EnsureInitialized();
            foreach (var tagRef in initialTags)
            {
                if (!string.IsNullOrEmpty(tagRef.tagName))
                {
                    var runtimeTag = database.GetRuntimeTag(tagRef.tagName);
                    if (runtimeTag.id >= 0)
                        tagContainer.AddTag(runtimeTag);
                }
            }
        }
    }
    
    public bool HasTag(GameplayTag tag)
    {
        return TagContainer.HasTag(tag, database);
    }
    
    public bool HasTagExact(GameplayTag tag)
    {
        return TagContainer.HasTagExact(tag);
    }
    
    public void AddTag(GameplayTag tag)
    {
        TagContainer.AddTag(tag);
    }
    
    public void RemoveTag(GameplayTag tag)
    {
        TagContainer.RemoveTag(tag);
    }
    
    public void AddTagByName(string tagName)
    {
        if (database != null && database.TryGetId(tagName, out int id))
        {
            TagContainer.AddTag(new GameplayTag { id = id });
        }
    }
    
    public void RemoveTagByName(string tagName)
    {
        if (database != null && database.TryGetId(tagName, out int id))
        {
            TagContainer.RemoveTag(new GameplayTag { id = id });
        }
    }
    
    /// <summary>
    /// Get the underlying container for query operations
    /// </summary>
    public GameplayTagContainer GetContainer()
    {
        return TagContainer;
    }
}