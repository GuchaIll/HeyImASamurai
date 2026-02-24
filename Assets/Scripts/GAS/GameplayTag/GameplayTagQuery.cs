using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Type of tag query operation
/// </summary>
public enum TagQueryType
{
    /// <summary>Must match ALL child expressions</summary>
    All,
    /// <summary>Must match ANY child expression</summary>
    Any,
    /// <summary>Must match NONE of the child expressions</summary>
    None,
    /// <summary>Leaf node - checks for a specific tag (or its children)</summary>
    Tag,
    /// <summary>Leaf node - exact tag match (no hierarchical check)</summary>
    ExactTag
}

/// <summary>
/// Query node for building complex tag match expressions.
/// Similar to Unreal Engine's FGameplayTagQuery.
/// </summary>
[Serializable]
public class GameplayTagQueryNode
{
    [SerializeField] private TagQueryType queryType = TagQueryType.Tag;
    [SerializeField] private GameplayTagReference tag;
    [SerializeField] private List<GameplayTagQueryNode> children = new List<GameplayTagQueryNode>();
    
    public TagQueryType QueryType => queryType;
    public GameplayTagReference Tag => tag;
    public List<GameplayTagQueryNode> Children => children;
    
    /// <summary>
    /// Create a leaf node that checks for a specific tag (hierarchically)
    /// </summary>
    public static GameplayTagQueryNode MakeTag(string tagName)
    {
        return new GameplayTagQueryNode
        {
            queryType = TagQueryType.Tag,
            tag = new GameplayTagReference(tagName)
        };
    }
    
    /// <summary>
    /// Create a leaf node that checks for an exact tag match
    /// </summary>
    public static GameplayTagQueryNode MakeExactTag(string tagName)
    {
        return new GameplayTagQueryNode
        {
            queryType = TagQueryType.ExactTag,
            tag = new GameplayTagReference(tagName)
        };
    }
    
    /// <summary>
    /// Create an ALL node (AND logic)
    /// </summary>
    public static GameplayTagQueryNode MakeAll(params GameplayTagQueryNode[] nodes)
    {
        var node = new GameplayTagQueryNode { queryType = TagQueryType.All };
        node.children.AddRange(nodes);
        return node;
    }
    
    /// <summary>
    /// Create an ANY node (OR logic)
    /// </summary>
    public static GameplayTagQueryNode MakeAny(params GameplayTagQueryNode[] nodes)
    {
        var node = new GameplayTagQueryNode { queryType = TagQueryType.Any };
        node.children.AddRange(nodes);
        return node;
    }
    
    /// <summary>
    /// Create a NONE node (NOT logic)
    /// </summary>
    public static GameplayTagQueryNode MakeNone(params GameplayTagQueryNode[] nodes)
    {
        var node = new GameplayTagQueryNode { queryType = TagQueryType.None };
        node.children.AddRange(nodes);
        return node;
    }
    
    /// <summary>
    /// Evaluate this query against a tag container
    /// </summary>
    public bool Evaluate(GameplayTagContainer container, GameplayTagDatabase database)
    {
        if (container == null || database == null)
            return false;
        
        switch (queryType)
        {
            case TagQueryType.Tag:
                return EvaluateTagNode(container, database, hierarchical: true);
            
            case TagQueryType.ExactTag:
                return EvaluateTagNode(container, database, hierarchical: false);
            
            case TagQueryType.All:
                return EvaluateAllNode(container, database);
            
            case TagQueryType.Any:
                return EvaluateAnyNode(container, database);
            
            case TagQueryType.None:
                return EvaluateNoneNode(container, database);
            
            default:
                return false;
        }
    }
    
    private bool EvaluateTagNode(GameplayTagContainer container, GameplayTagDatabase database, bool hierarchical)
    {
        if (tag == null || !tag.IsValid(database))
            return false;
        
        var runtimeTag = tag.ToRuntimeTag(database);
        if (runtimeTag.id < 0)
            return false;
        
        if (hierarchical)
            return container.HasTag(runtimeTag, database);
        else
            return container.HasTagExact(runtimeTag);
    }
    
    private bool EvaluateAllNode(GameplayTagContainer container, GameplayTagDatabase database)
    {
        if (children.Count == 0)
            return true;
        
        foreach (var child in children)
        {
            if (!child.Evaluate(container, database))
                return false;
        }
        return true;
    }
    
    private bool EvaluateAnyNode(GameplayTagContainer container, GameplayTagDatabase database)
    {
        if (children.Count == 0)
            return false;
        
        foreach (var child in children)
        {
            if (child.Evaluate(container, database))
                return true;
        }
        return false;
    }
    
    private bool EvaluateNoneNode(GameplayTagContainer container, GameplayTagDatabase database)
    {
        foreach (var child in children)
        {
            if (child.Evaluate(container, database))
                return false;
        }
        return true;
    }
    
    /// <summary>
    /// Get a human-readable description of this query
    /// </summary>
    public string GetDescription(int indent = 0)
    {
        string prefix = new string(' ', indent * 2);
        
        switch (queryType)
        {
            case TagQueryType.Tag:
                return $"{prefix}Has tag: {tag?.tagName ?? "(none)"}";
            
            case TagQueryType.ExactTag:
                return $"{prefix}Has exact tag: {tag?.tagName ?? "(none)"}";
            
            case TagQueryType.All:
                var allLines = new List<string> { $"{prefix}ALL of:" };
                foreach (var child in children)
                    allLines.Add(child.GetDescription(indent + 1));
                return string.Join("\n", allLines);
            
            case TagQueryType.Any:
                var anyLines = new List<string> { $"{prefix}ANY of:" };
                foreach (var child in children)
                    anyLines.Add(child.GetDescription(indent + 1));
                return string.Join("\n", anyLines);
            
            case TagQueryType.None:
                var noneLines = new List<string> { $"{prefix}NONE of:" };
                foreach (var child in children)
                    noneLines.Add(child.GetDescription(indent + 1));
                return string.Join("\n", noneLines);
            
            default:
                return $"{prefix}Unknown query type";
        }
    }
}

/// <summary>
/// High-level tag query wrapper for easy serialization and usage.
/// Similar to Unreal's FGameplayTagQuery.
/// </summary>
[Serializable]
public class GameplayTagQuery
{
    [SerializeField] private GameplayTagQueryNode rootNode;
    
    public GameplayTagQueryNode RootNode => rootNode;
    
    /// <summary>
    /// Create an empty query (always matches)
    /// </summary>
    public GameplayTagQuery()
    {
        rootNode = null;
    }
    
    /// <summary>
    /// Create a query with a root node
    /// </summary>
    public GameplayTagQuery(GameplayTagQueryNode root)
    {
        rootNode = root;
    }
    
    /// <summary>
    /// Evaluate this query against a tag container
    /// </summary>
    public bool Matches(GameplayTagContainer container, GameplayTagDatabase database)
    {
        if (rootNode == null)
            return true; // Empty query matches everything
        
        return rootNode.Evaluate(container, database);
    }
    
    /// <summary>
    /// Check if this query is empty
    /// </summary>
    public bool IsEmpty {
        get {
            if (rootNode == null)
                return true;
            // Treat queryType None with no children as empty
            if (rootNode.QueryType == TagQueryType.None && (rootNode.Children == null || rootNode.Children.Count == 0))
                return true;
            return false;
        }
    }
    
    /// <summary>
    /// Get description of this query
    /// </summary>
    public string GetDescription()
    {
        if (rootNode == null)
            return "(Empty query - matches all)";
        return rootNode.GetDescription();
    }
    
    // Builder methods for common query patterns
    
    /// <summary>
    /// Create a query that requires ALL specified tags
    /// </summary>
    public static GameplayTagQuery RequireAll(params string[] tagNames)
    {
        var nodes = new GameplayTagQueryNode[tagNames.Length];
        for (int i = 0; i < tagNames.Length; i++)
            nodes[i] = GameplayTagQueryNode.MakeTag(tagNames[i]);
        
        return new GameplayTagQuery(GameplayTagQueryNode.MakeAll(nodes));
    }
    
    /// <summary>
    /// Create a query that requires ANY of the specified tags
    /// </summary>
    public static GameplayTagQuery RequireAny(params string[] tagNames)
    {
        var nodes = new GameplayTagQueryNode[tagNames.Length];
        for (int i = 0; i < tagNames.Length; i++)
            nodes[i] = GameplayTagQueryNode.MakeTag(tagNames[i]);
        
        return new GameplayTagQuery(GameplayTagQueryNode.MakeAny(nodes));
    }
    
    /// <summary>
    /// Create a query that blocks if ANY of the specified tags are present
    /// </summary>
    public static GameplayTagQuery BlockAny(params string[] tagNames)
    {
        var nodes = new GameplayTagQueryNode[tagNames.Length];
        for (int i = 0; i < tagNames.Length; i++)
            nodes[i] = GameplayTagQueryNode.MakeTag(tagNames[i]);
        
        return new GameplayTagQuery(GameplayTagQueryNode.MakeNone(nodes));
    }
    
    /// <summary>
    /// Create a query requiring specific tags and blocking others
    /// </summary>
    public static GameplayTagQuery RequireAndBlock(string[] requiredTags, string[] blockedTags)
    {
        var requiredNodes = new GameplayTagQueryNode[requiredTags.Length];
        for (int i = 0; i < requiredTags.Length; i++)
            requiredNodes[i] = GameplayTagQueryNode.MakeTag(requiredTags[i]);
        
        var blockedNodes = new GameplayTagQueryNode[blockedTags.Length];
        for (int i = 0; i < blockedTags.Length; i++)
            blockedNodes[i] = GameplayTagQueryNode.MakeTag(blockedTags[i]);
        
        return new GameplayTagQuery(
            GameplayTagQueryNode.MakeAll(
                GameplayTagQueryNode.MakeAll(requiredNodes),
                GameplayTagQueryNode.MakeNone(blockedNodes)
            )
        );
    }
}
