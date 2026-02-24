using UnityEngine;

[System.Serializable]
public class GameplayTagReference
{
    [SerializeField]
    public string tagName;
    
    public GameplayTagReference() { }
    
    public GameplayTagReference(string name)
    {
        tagName = name;
    }
    
    /// <summary>
    /// Convert to runtime tag using the database
    /// </summary>
    public GameplayTag ToRuntimeTag(GameplayTagDatabase db)
    {
        if (db == null || string.IsNullOrEmpty(tagName))
            return new GameplayTag { id = -1 };
        
        return db.GetRuntimeTag(tagName);
    }
    
    public bool IsValid(GameplayTagDatabase db)
    {
        return db != null && db.TagExists(tagName);
    }
    
    public override string ToString() => tagName ?? "(none)";
    
    public static implicit operator string(GameplayTagReference tagRef) => tagRef?.tagName;
}