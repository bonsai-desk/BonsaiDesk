using System;
using Dissonance.Networking;
using JetBrains.Annotations;
using Mirror;

namespace Dissonance.Integrations.MirrorIgnorance
{
    public class MirrorIgnoranceClient
        : BaseClient<MirrorIgnoranceServer, MirrorIgnoranceClient, MirrorConn>
    {
        #region fields and properties
        private readonly MirrorIgnoranceCommsNetwork _network;
        #endregion

        #region constructors
        public MirrorIgnoranceClient([NotNull] MirrorIgnoranceCommsNetwork network)
            : base(network)
        {
            if (network == null)
                throw new ArgumentNullException("network");

            _network = network;
        }
        #endregion

        #region connect/disconnect
        public override void Connect()
        {
            //we handle loopback explicitly, so if the server is locally hosted we don't need to register the network handler
            //This is important because otherwise we'd overwrite the server message handler!
            if (!_network.Mode.IsServerEnabled())
            {
                NetworkClient.ReplaceHandler<DissonanceNetworkMessage>(OnMessageReceived);
            }
            else
                Log.Debug("Not binding network handler (server is running locally)");

            Connected();
        }

        public override void Disconnect()
        {
            // Bind a handler to discard all Dissonance messages to the local client (if the server is not handling them).
            // Don't bother if client isn't null, because by definition we can't receive any messages then!
            if (!_network.Mode.IsServerEnabled())
            {
                NetworkClient.ReplaceHandler<DissonanceNetworkMessage>(MirrorIgnoranceCommsNetwork.NullMessageReceivedHandler);
            }

            base.Disconnect();
        }
        #endregion

        #region send/receive
        private void OnMessageReceived(NetworkConnection source, DissonanceNetworkMessage msg)
        {
            using (msg)
                NetworkReceivedPacket(msg.Data);
        }

        protected override void ReadMessages()
        {
            //Messages are received in an event handler, so we don't need to do any work to read events
        }

        protected override void SendReliable(ArraySegment<byte> packet)
        {
            //Send packet to the server, if it fails for some reason then instantly kill the client
            if (!Send(packet, MirrorIgnoranceCommsNetwork.ReliableSequencedChannel))
                FatalError("Failed to send reliable packet (unknown Mirror error)");
        }

        protected override void SendUnreliable(ArraySegment<byte> packet)
        {
            Send(packet, MirrorIgnoranceCommsNetwork.UnreliableChannel);
        }

        private bool Send(ArraySegment<byte> packet, byte channel)
        {
            if (_network.PreprocessPacketToServer(packet))
                return true;

            NetworkClient.connection.Send(new DissonanceNetworkMessage(packet), channel);
            return true;
        }
        #endregion
    }
}
