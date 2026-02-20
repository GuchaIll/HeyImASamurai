using System;

[Serializable]
public struct GameplayTag :IEquatable<GameplayTag>
{
    public int id;

    public bool Equals(GameplayTag other) => id == other.id;
    public override int GetHashCode() => id;
}