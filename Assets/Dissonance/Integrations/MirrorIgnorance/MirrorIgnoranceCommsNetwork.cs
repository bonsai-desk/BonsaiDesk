using System;
using System.Collections.Generic;
using Dissonance.Datastructures;
using Dissonance.Networking;
using Dissonance.Extensions;
using JetBrains.Annotations;
using Mirror;
using UnityEngine;

namespace Dissonance.Integrations.MirrorIgnorance
{
    [HelpURL("https://placeholder-software.co.uk/dissonance/docs/Basics/Quick-Start-MirrorIgnorance/")]
    public class MirrorIgnoranceCommsNetwork
        : BaseCommsNetwork<MirrorIgnoranceServer, MirrorIgnoranceClient, MirrorConn, Unit, Unit>
    {
        internal const byte ReliableSequencedChannel = Channels.DefaultReliable;
        internal const byte UnreliableChannel = Channels.DefaultUnreliable;

        private readonly ConcurrentPool<byte[]> _loopbackBuffers = new ConcurrentPool<byte[]>(8, () => new byte[1024]);
        private readonly List<ArraySegment<byte>> _loopbackQueue = new List<ArraySegment<byte>>();

        protected override MirrorIgnoranceServer CreateServer(Unit details)
        {
            return new MirrorIgnoranceServer(this);
        }

        protected override MirrorIgnoranceClient CreateClient(Unit details)
        {
            return new MirrorIgnoranceClient(this);
        }

        protected override void Update()
        {
            if (IsInitialized)
            {
                // Network is considered active if all of:
                // - Network explicitly claims it is active
                // - Server or client explicitly claim they are active
                // - Also if the client is active only say we're active once the client is non-null, has a non-null connection and is "ready"
                var networkActive = NetworkManager.singleton != null
                                 && NetworkManager.singleton.isNetworkActive
                                 && (NetworkServer.active || NetworkClient.active)
                                 && (!NetworkClient.active || (NetworkClient.connection != null && NetworkClient.connection.isReady));

                if (networkActive)
                {
                    // switch to the appropriate mode if we have not already
                    var server = NetworkServer.active;
                    var client = NetworkClient.active;

                    if (Mode.IsServerEnabled() != server || Mode.IsClientEnabled() != client)
                    {
                        if (server && client)
                            RunAsHost(Unit.None, Unit.None);
                        else if (server)
                            RunAsDedicatedServer(Unit.None);
                        else if (client)
                            RunAsClient(Unit.None);
                    }
                }
                else if (Mode != NetworkMode.None)
                {
                    // stop the network if unet has shut down
                    Stop();

                    //Discard looped back packets which haven't been delivered yet
                    _loopbackQueue.Clear();
                }

                //Send looped back packets
                for (var i = 0; i < _loopbackQueue.Count; i++)
                {
                    if (Client != null)
                        Client.NetworkReceivedPacket(_loopbackQueue[i]);

                    // Recycle the packet into the pool of byte buffers
                    // ReSharper disable once AssignNullToNotNullAttribute (Justification: ArraySegment array is not null)
                    _loopbackBuffers.Put(_loopbackQueue[i].Array);
                }
                _loopbackQueue.Clear();
            }

            base.Update();
        }

        protected override void Initialize()
        {
            NetworkServer.ReplaceHandler<DissonanceNetworkMessage>(NullMessageReceivedHandler);

            base.Initialize();
        }

        internal bool PreprocessPacketToClient(ArraySegment<byte> packet, MirrorConn destination)
        {
            //I have no idea if the Mirror/Ignorance handles loopback. Whether it does or does not isn't important though - it's more
            //efficient to handle the loopback special case directly instead of passing through the entire network system!

            //This should never even be called if this peer is not the host!
            if (Server == null)
                throw Log.CreatePossibleBugException("server packet preprocessing running, but this peer is not a server", "8f9dc0a0-1b48-4a7f-9bb6-f767b2542ab1");

            //If there is no local client (e.g. this is a dedicated server) then there can't possibly be loopback
            if (Client == null)
                return false;

            //Is this loopback?
            if (NetworkClient.connection != destination.Connection)
                return false;

            //This is loopback!

            // check that we have a valid local client (in cases of startup or in-progress shutdowns)
            if (Client != null)
            {
                // Don't immediately deliver the packet, add it to a queue and deliver it next frame. This prevents the local client from executing "within" ...
                // ...the local server which can cause confusing stack traces.
                _loopbackQueue.Add(packet.CopyTo(_loopbackBuffers.Get()));
            }

            return true;
        }

        internal bool PreprocessPacketToServer(ArraySegment<byte> packet)
        {
            //I have no idea if the Mirror handles loopback. Whether it does or does not isn't important though - it's more
            //efficient to handle the loopback special case directly instead of passing through the entire network system!

            //This should never even be called if this peer is not a client!
            if (Client == null)
                throw Log.CreatePossibleBugException("client packet processing running, but this peer is not a client", "dd75dce4-e85c-4bb3-96ec-3a3636cc4fbe");

            //Is this loopback?
            if (Server == null)
                return false;

            //This is loopback!

            //Since this is loopback destination == source (by definition)
            Server.NetworkReceivedPacket(new MirrorConn(NetworkClient.connection), packet);

            return true;
        }

        internal static void NullMessageReceivedHandler(NetworkConnection source, DissonanceNetworkMessage msg)
        {
            if (Logs.GetLogLevel(LogCategory.Network) <= LogLevel.Trace)
                Debug.Log("Discarding Dissonance network message");

            msg.Dispose();
        }
    }

    public struct MirrorConn
        : IEquatable<MirrorConn>
    {
        public readonly NetworkConnection Connection;

        public MirrorConn(NetworkConnection connection)
            : this()
        {
            Connection = connection;
        }

        public override int GetHashCode()
        {
            return Connection.GetHashCode();
        }

        public override string ToString()
        {
            return Connection.ToString();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is MirrorConn && Equals((MirrorConn)obj);
        }

        public bool Equals(MirrorConn other)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            // ReSharper disable HeuristicUnreachableCode
            if (Connection == null)
            {
                if (other.Connection == null)
                    return true;
                return false;
            }
            // ReSharper restore HeuristicUnreachableCode

            return Connection.Equals(other.Connection);
        }
    }

    internal static class DissonanceNetworkMessageExtensions
    {
        internal const int BufferLength = 1024;
        internal static readonly ConcurrentPool<byte[]> SerializationBuffers = new ConcurrentPool<byte[]>(8, () => new byte[BufferLength]);

        public static void Serialize([NotNull] this NetworkWriter writer, DissonanceNetworkMessage value)
        {
            writer.WriteUInt16((ushort)value.Data.Count);
            writer.WriteBytes(value.Data.Array, value.Data.Offset, value.Data.Count);

            //Recyle array now that it has been serialized
            // ReSharper disable once AssignNullToNotNullAttribute (Justification: can't be null)
            SerializationBuffers.Put(value.Data.Array);
        }

        public static DissonanceNetworkMessage Deserialize([NotNull] this NetworkReader reader)
        {
            var arr = SerializationBuffers.Get();

            var length = reader.ReadUInt16();
            for (var i = 0; i < length; i++)
                arr[i] = reader.ReadByte();

            return new DissonanceNetworkMessage(new ArraySegment<byte>(arr, 0, length));
        }
    }

    internal struct DissonanceNetworkMessage
        : NetworkMessage, IDisposable
    {
        public ArraySegment<byte> Data;

        public DissonanceNetworkMessage(ArraySegment<byte> packet)
        {
            //We are not allowed to keep a reference to `packet` beyond this point, immediately copy it into a temporary buffer
            Data = packet.CopyTo(DissonanceNetworkMessageExtensions.SerializationBuffers.Get());
        }

        public void Dispose()
        {
            var arr = Data.Array;
            if (arr != null && arr.Length == DissonanceNetworkMessageExtensions.BufferLength)
            {
                DissonanceNetworkMessageExtensions.SerializationBuffers.Put(arr);
                Data = new ArraySegment<byte>(Array.Empty<byte>(), 0, 0);
            }
        }
    }
}
