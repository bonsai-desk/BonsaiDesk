﻿using System;
using UnityEngine;

public readonly struct SyncJoint : IEquatable<SyncJoint>
{
    public readonly bool connected;

    public readonly NetworkIdentityReference attachedTo;

    //the 2 blockObjects must be aligned before attaching the joint. This is the position of the single block relative to the attachedTo for use in alligning
    public readonly Vector3 positionLocalToAttachedTo;
    public readonly Quaternion rotationLocalToAttachedTo; //same as positionLocalToAttachedTo but for rotation
    public readonly Vector3Int attachedToMeAtCoord;
    public readonly Vector3Int otherBearingCoord;
    public readonly Vector3 axis;
    public readonly Vector3 anchor;
    public readonly Vector3 connectedAnchor;

    public SyncJoint(NetworkIdentityReference attachedTo, Vector3 positionLocalToAttachedTo, Quaternion rotationLocalToAttachedTo,
        Vector3Int attachedToMeAtCoord, Vector3Int otherBearingCoord, Vector3 axis, Vector3 anchor, Vector3 connectedAnchor)
    {
        connected = true;
        this.attachedTo = attachedTo;
        this.positionLocalToAttachedTo = positionLocalToAttachedTo;
        this.rotationLocalToAttachedTo = rotationLocalToAttachedTo;
        this.attachedToMeAtCoord = attachedToMeAtCoord;
        this.otherBearingCoord = otherBearingCoord;
        this.axis = axis;
        this.anchor = anchor;
        this.connectedAnchor = connectedAnchor;
    }

    public SyncJoint(SyncJoint oldJoint, NetworkIdentityReference newNetIdRef)
    {
        connected = true;
        attachedTo = newNetIdRef;
        positionLocalToAttachedTo = oldJoint.positionLocalToAttachedTo;
        rotationLocalToAttachedTo = oldJoint.rotationLocalToAttachedTo;
        attachedToMeAtCoord = oldJoint.attachedToMeAtCoord;
        otherBearingCoord = oldJoint.otherBearingCoord;
        axis = oldJoint.axis;
        anchor = oldJoint.anchor;
        connectedAnchor = oldJoint.connectedAnchor;
    }

    public bool Equals(SyncJoint other)
    {
        var attachedToSame = true;
        if (attachedTo != null && other.attachedTo != null)
        {
            attachedToSame = attachedTo.NetworkId == other.attachedTo.NetworkId;
        }

        return connected == other.connected && (attachedTo == null) == (other.attachedTo == null) && attachedToSame &&
               positionLocalToAttachedTo == other.positionLocalToAttachedTo && rotationLocalToAttachedTo == other.rotationLocalToAttachedTo &&
               attachedToMeAtCoord == other.attachedToMeAtCoord && otherBearingCoord == other.otherBearingCoord && axis == other.axis &&
               anchor == other.anchor && connectedAnchor == other.connectedAnchor;
    }

    public override bool Equals(System.Object obj)
    {
        return obj is SyncJoint c && this == c;
    }

    public override int GetHashCode()
    {
        var attachToHashCode = 0;
        if (attachedTo != null)
        {
            attachToHashCode = (int) attachedTo.NetworkId;
        }

        return connected.GetHashCode() ^ attachToHashCode ^ positionLocalToAttachedTo.GetHashCode() ^ rotationLocalToAttachedTo.GetHashCode() ^
               attachedToMeAtCoord.GetHashCode() ^ otherBearingCoord.GetHashCode() ^ axis.GetHashCode() & anchor.GetHashCode() ^ connectedAnchor.GetHashCode();
    }

    public static bool operator ==(SyncJoint x, SyncJoint y)
    {
        return x.Equals(y);
    }

    public static bool operator !=(SyncJoint x, SyncJoint y)
    {
        return !(x == y);
    }
}