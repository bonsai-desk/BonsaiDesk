using System;
using System.Collections.Generic;
using Dissonance.Networking;
using Dissonance.Networking.Server;
using JetBrains.Annotations;
using Mirror;

namespace Dissonance.Integrations.MirrorIgnorance
{
    public class MirrorIgnoranceServer
        : BaseServer<MirrorIgnoranceServer, MirrorIgnoranceClient, MirrorConn>
    {
        #region fields and properties
        [NotNull] private readonly MirrorIgnoranceCommsNetwork _network;

        private readonly List<NetworkConnection> _addedConnections = new List<NetworkConnection>();
        #endregion

        #region constructors
        public MirrorIgnoranceServer([NotNull] MirrorIgnoranceCommsNetwork network)
        {
            if (network == null)
                throw new ArgumentNullException("network");

            _network = network;
        }
        #endregion

        public override void Connect()
        {
            NetworkServer.ReplaceHandler<DissonanceNetworkMessage>(OnMessageReceived);

            base.Connect();
        }

        private void OnMessageReceived(NetworkConnection source, DissonanceNetworkMessage msg)
        {
            using (msg)
                NetworkReceivedPacket(new MirrorConn(source), msg.Data);
        }

        protected override void AddClient([NotNull] ClientInfo<MirrorConn> client)
        {
            base.AddClient(client);

            //Add this player to the list of known connections (do not add the local player)
            if (client.PlayerName != _network.PlayerName)
                _addedConnections.Add(client.Connection.Connection);
        }

        public override void Disconnect()
        {
            base.Disconnect();

            NetworkServer.ReplaceHandler<DissonanceNetworkMessage>(MirrorIgnoranceCommsNetwork.NullMessageReceivedHandler);
        }

        protected override void ReadMessages()
        {
            //Messages are received in an event handler, so we don't need to do any work to read events
        }

        public override ServerState Update()
        {
            // The only way to get an event regarding disconnections from Mirror is to be a NetworkManager. We aren't a
            // NetworkManager and don't want to be because it would make setting up the Mirror integration significantly
            // more complex. Instead we'll have to poll for disconnections.
            for (var i = _addedConnections.Count - 1; i >= 0; i--)
            {
                var conn = _addedConnections[i];
                if (!IsConnected(conn))
                {
                    ClientDisconnected(new MirrorConn(_addedConnections[i]));
                    _addedConnections.RemoveAt(i);
                }
            }

            return base.Update();
        }

        private static bool IsConnected([NotNull] NetworkConnection conn)
        {
            return conn.isReady && NetworkServer.connections.ContainsKey(conn.connectionId);
        }

        #region send
        protected override void SendReliable(MirrorConn connection, ArraySegment<byte> packet)
        {
            if (!Send(packet, connection, MirrorIgnoranceCommsNetwork.ReliableSequencedChannel))
                FatalError("Failed to send reliable packet (unknown Mirror error)");
        }

        protected override void SendUnreliable(MirrorConn connection, ArraySegment<byte> packet)
        {
            Send(packet, connection, MirrorIgnoranceCommsNetwork.UnreliableChannel);
        }

        /// <summary>
        /// Send a packet
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="connection"></param>
        /// <param name="channel"></param>
        /// <returns>false if there was an error during sending, otherwise true</returns>
        private bool Send(ArraySegment<byte> packet, MirrorConn connection, byte channel)
        {
            if (_network.PreprocessPacketToClient(packet, connection))
                return true;

            // We don't consider sending to a disconnected connection a failure.
            // It could easily be caused by a race (i.e. they only just disconnected) and we don't really care if packets to non-clients get lost!
            if (!IsConnected(connection.Connection))
                return true;

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse (Justification it shouldn't be null, but sanity check anyway)
            // ReSharper disable HeuristicUnreachableCode
            if (connection.Connection == null)
            {
                Log.Error("Cannot send to a null destination");
                return false;
            }
            // ReSharper restore HeuristicUnreachableCode

            connection.Connection.Send(new DissonanceNetworkMessage(packet), channel);
            return true;
        }
        #endregion
    }
}
