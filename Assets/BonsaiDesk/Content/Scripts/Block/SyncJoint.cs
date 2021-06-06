using System;
using UnityEngine;

public readonly struct SyncJoint : IEquatable<SyncJoint>
{
    public readonly bool connected;

    public readonly NetworkIdentityReference attachedTo;
    
    //the rotation of the block being attached. this is used while connecting the joint, but then is overriden by the actual rotation of the blockObject transform
    public readonly byte localRotation;
    //SyncBlock rotation of the bearing. in theory, this could be calculated in ConnectJoint, but this makes sure it works even if
    //the Blocks dict or MeshBlocks dict is not missing the bearing block
    public readonly byte bearingLocalRotation;
    public readonly Vector3Int attachedToMeAtCoord;
    public readonly Vector3Int otherBearingCoord;

    public SyncJoint(NetworkIdentityReference attachedTo, byte localRotation, byte bearingLocalRotation, Vector3Int attachedToMeAtCoord,
        Vector3Int otherBearingCoord)
    {
        connected = true;
        this.attachedTo = attachedTo;
        this.localRotation = localRotation;
        this.bearingLocalRotation = bearingLocalRotation;
        this.attachedToMeAtCoord = attachedToMeAtCoord;
        this.otherBearingCoord = otherBearingCoord;
    }

    public SyncJoint(SyncJoint oldJoint, NetworkIdentityReference newNetIdRef)
    {
        connected = true;
        attachedTo = newNetIdRef;
        localRotation = oldJoint.localRotation;
        bearingLocalRotation = oldJoint.bearingLocalRotation;
        attachedToMeAtCoord = oldJoint.attachedToMeAtCoord;
        otherBearingCoord = oldJoint.otherBearingCoord;
    }

    // public SyncJoint(NetworkIdentityReference attachedTo, Vector3 positionLocalToAttachedTo, Quaternion rotationLocalToAttachedTo,
    //     Vector3Int attachedToMeAtCoord, Vector3Int otherBearingCoord, Vector3 axis, Vector3 anchor, Vector3 connectedAnchor)
    // {
    //     connected = true;
    //     this.attachedTo = attachedTo;
    //     this.positionLocalToAttachedTo = positionLocalToAttachedTo;
    //     this.rotationLocalToAttachedTo = rotationLocalToAttachedTo;
    //     this.attachedToMeAtCoord = attachedToMeAtCoord;
    //     this.otherBearingCoord = otherBearingCoord;
    //     this.axis = axis;
    //     this.anchor = anchor;
    //     this.connectedAnchor = connectedAnchor;
    // }
    //
    // public SyncJoint(SyncJoint oldJoint, NetworkIdentityReference newNetIdRef)
    // {
    //     connected = true;
    //     attachedTo = newNetIdRef;
    //     positionLocalToAttachedTo = oldJoint.positionLocalToAttachedTo;
    //     rotationLocalToAttachedTo = oldJoint.rotationLocalToAttachedTo;
    //     attachedToMeAtCoord = oldJoint.attachedToMeAtCoord;
    //     otherBearingCoord = oldJoint.otherBearingCoord;
    //     axis = oldJoint.axis;
    //     anchor = oldJoint.anchor;
    //     connectedAnchor = oldJoint.connectedAnchor;
    // }

    public bool Equals(SyncJoint other)
    {
        var attachedToSame = true;
        if (attachedTo != null && other.attachedTo != null)
        {
            attachedToSame = attachedTo.NetworkId == other.attachedTo.NetworkId;
        }

        return connected == other.connected && (attachedTo == null) == (other.attachedTo == null) && attachedToSame && localRotation == other.localRotation &&
               bearingLocalRotation == other.bearingLocalRotation && attachedToMeAtCoord == other.attachedToMeAtCoord &&
               otherBearingCoord == other.otherBearingCoord;
    }

    public override bool Equals(System.Object obj)
    {
        return obj is SyncJoint c && this == c;
    }

    public override int GetHashCode()
    {
        uint attachToId = 0;
        if (attachedTo != null)
        {
            attachToId = attachedTo.NetworkId;
        }

        return connected.GetHashCode() ^ attachToId.GetHashCode() ^ localRotation.GetHashCode() ^ bearingLocalRotation.GetHashCode() ^ attachedToMeAtCoord.GetHashCode() ^
               otherBearingCoord.GetHashCode();
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