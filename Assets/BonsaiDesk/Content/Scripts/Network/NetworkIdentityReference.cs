using Mirror;

[System.Serializable]
public class NetworkIdentityReference
{
    /// <summary>
    /// NetworkId for the NetworkIdentity this was initialized for.
    /// </summary>
    public uint NetworkId { get; private set; }

    /// <summary>
    /// NetworkIdentity this referencer is holding a value for.
    /// </summary>
    public NetworkIdentity Value
    {
        get
        {
            //No networkId, therefore is null.
            if (NetworkId == 0)
                return null;
            //If cache isn't set then try to set it now.
            if (_networkIdentityCached == null)
            {
                if (NetworkIdentity.spawned.TryGetValue(NetworkId, out NetworkIdentity nid))
                {
                    _networkIdentityCached = nid;
                }
                else
                {
                    return null;
                }
            }

            return _networkIdentityCached;
        }
    }

    /// <summary>
    /// Cached NetworkIdentity value. Used to prevent excessive dictionary iterations.
    /// </summary>
    private NetworkIdentity _networkIdentityCached = null;

    /// <summary>
    /// 
    /// </summary>
    public NetworkIdentityReference()
    {
    }

    /// <summary>
    /// Initializes with a NetworkIdentity.
    /// </summary>
    /// <param name="networkIdentity"></param>
    public NetworkIdentityReference(NetworkIdentity networkIdentity)
    {
        if (networkIdentity == null)
            return;

        NetworkId = networkIdentity.netId;
        _networkIdentityCached = networkIdentity;
    }

    /// <summary>
    /// Initializes with a NetworkId.
    /// </summary>
    /// <param name="networkId"></param>
    public NetworkIdentityReference(uint networkId)
    {
        NetworkId = networkId;
    }
}


public static class NetworkIdentityReferenceReaderWriter
{
    public static void WriteNetworkIdentityReference(this NetworkWriter writer, NetworkIdentityReference nir)
    {
        //Null NetworkIdentityReference or no NetworkIdentity value.
        if (nir == null || nir.Value == null)
            writer.WriteUInt32(0);
        //Value exist, write netId.
        else
            writer.WriteUInt32(nir.Value.netId);
    }

    public static NetworkIdentityReference ReadNetworkIdentityReference(this NetworkReader reader)
    {
        return new NetworkIdentityReference(reader.ReadUInt32());
    }
}