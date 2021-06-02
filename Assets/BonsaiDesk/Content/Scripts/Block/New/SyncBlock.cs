using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public readonly struct SyncBlock : IEquatable<SyncBlock>
{
    public readonly string name;
    public readonly byte rotation;

    public SyncBlock(string name, byte rotation)
    {
        this.name = name;
        this.rotation = rotation;
    }

    public bool Equals(SyncBlock other)
    {
        return name == other.name && rotation == other.rotation;
    }

    public override bool Equals(System.Object obj)
    {
        return obj is SyncBlock c && this == c;
    }
    
    public override int GetHashCode()
    {
        return name.GetHashCode() ^ rotation.GetHashCode();
    }
    
    public static bool operator ==(SyncBlock x, SyncBlock y)
    {
        return x.name == y.name && x.rotation == y.rotation;
    }
    
    public static bool operator !=(SyncBlock x, SyncBlock y)
    {
        return !(x == y);
    }
}