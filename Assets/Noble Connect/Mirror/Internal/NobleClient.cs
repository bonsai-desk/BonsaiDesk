using NobleConnect.Ice;
using System;
using System.Net;
using System.Text;
using UnityEngine;
using Mirror;
using System.Net.Sockets;
using System.Reflection;
using System.Collections.Generic;

#if LITENETLIB_TRANSPORT
using LiteNetLib;
#endif

namespace NobleConnect.Mirror
{
    /// <summary>Adds relay, punchthrough, and port-forwarding support to the Mirror NetworkClient</summary>
    /// <remarks>
    /// Use the Connect method to connect to a host.
    /// </remarks>
    public class NobleClient
    {
        #region Public Properties

        private ConnectionType _latestConnectionType = ConnectionType.NONE;

        /// <summary>You can check this in OnClientConnect(), it will either be Direct, Punchthrough, or Relay.</summary>
        public ConnectionType latestConnectionType {
            get {
                if (baseClient != null) return baseClient.latestConnectionType;
                else return _latestConnectionType;
            }
            set {
                _latestConnectionType = value;
            }
        }

        /// <summary>A convenient way to check if a connection is in progress</summary>
        public bool isConnecting = false;

		#endregion

		#region Internal Properties

        const string TRANSPORT_WARNING_MESSAGE = "You must download and install a UDP transport in order to use Mirror with NobleConnect.\n" +
                                                "I recommend LiteNet: https://github.com/MirrorNetworking/LiteNetLibTransport/";
        /// <summary>Store force relay so that we can pass it on to the iceController</summary>
        public bool ForceRelayOnly;
        /// <summary>A method to call if something goes wrong like reaching ccu or bandwidth limit</summary>
        Action<string> onFatalError = null;

        /// <summary>We store the end point of the local bridge that we connect to because Mirror makes it hard to get the ip and port that a client has connected to for some reason</summary>
        IPEndPoint hostBridgeEndPoint;

        Peer baseClient;
        IceConfig nobleConfig = new IceConfig();

        #endregion Internal Properties

        #region Public Interface

        /// <summary>Initialize the client using NobleConnectSettings. The region used is determined by the Relay Server Address in the NobleConnectSettings.</summary>
        /// <remarks>
        /// The default address is connect.noblewhale.com, which will automatically select the closest 
        /// server based on geographic region.
        /// 
        /// If you would like to connect to a specific region you can use one of the following urls:
        /// <pre>
        ///     us-east.connect.noblewhale.com - Eastern United States
        ///     us-west.connect.noblewhale.com - Western United States
        ///     eu.connect.noblewhale.com - Europe
        ///     ap.connect.noblewhale.com - Asia-Pacific
        ///     sa.connect.noblewhale.com - South Africa
        ///     hk.connect.noblewhale.com - Hong Kong
        /// </pre>
        /// 
        /// Note that region selection will ensure each player connects to the closest relay server, but it does not 
        /// prevent players from connecting across regions. If you want to prevent joining across regions you will 
        /// need to implement that separately (by filtering out unwanted regions during matchmaking for example).
        /// </remarks>
        /// <param name="topo">The HostTopology to use for the NetworkClient. Must be the same on host and client.</param>
        /// <param name="onFatalError">A method to call if something goes horribly wrong.</param>
        /// <param name="allocationResendTimeout">Initial timeout before resending refresh messages. This is doubled for each failed resend.</param>
        /// <param name="maxAllocationResends">Max number of times to try and resend refresh messages before giving up and shutting down the relay connection. If refresh messages fail for 30 seconds the relay connection will be closed remotely regardless of these settings.</param>
        public NobleClient(GeographicRegion region = GeographicRegion.AUTO, Action<string> onFatalError = null, int relayLifetime = 60, int relayRefreshTime = 30, float allocationResendTimeout = .1f, int maxAllocationResends = 8)
        {
            var settings = (NobleConnectSettings)Resources.Load("NobleConnectSettings", typeof(NobleConnectSettings));

            this.onFatalError = onFatalError;
            nobleConfig = new IceConfig
            {
                iceServerAddress = RegionURL.FromRegion(region),
                icePort = settings.relayServerPort,
                maxAllocationRetransmissionCount = maxAllocationResends,
                allocationRetransmissionTimeout = (int)(allocationResendTimeout * 1000),
                allocationLifetime = relayLifetime,
                refreshTime = relayRefreshTime
            };

            if (!string.IsNullOrEmpty(settings.gameID))
            {
                string decodedGameID = Encoding.UTF8.GetString(Convert.FromBase64String(settings.gameID));
                string[] parts = decodedGameID.Split('\n');

                if (parts.Length == 3)
                {
                    nobleConfig.username = parts[1];
                    nobleConfig.password = parts[2];
                    nobleConfig.origin = parts[0];
                }
            }

            Init();
        }

        public NobleClient() : base()
        {

        }

        /// <summary>
        /// Initialize the client using NobleConnectSettings but connect to specific relay server address.
        /// This method is useful for selecting the region to connect to at run time when starting the client.
        /// </summary>
        /// <remarks>\copydetails NobleClient::NobleClient(HostTopology,Action)</remarks>
        /// <param name="relayServerAddress">The url or ip of the relay server to connect to</param>
        /// <param name="topo">The HostTopology to use for the NetworkClient. Must be the same on host and client.</param>
        /// <param name="onFatalError">A method to call if something goes horribly wrong.</param>
        /// <param name="allocationResendTimeout">Initial timeout before resending refresh messages. This is doubled for each failed resend.</param>
        /// <param name="maxAllocationResends">Max number of times to try and resend refresh messages before giving up and shutting down the relay connection. If refresh messages fail for 30 seconds the relay connection will be closed remotely regardless of these settings.</param>
        public NobleClient(string relayServerAddress, Action<string> onFatalError = null, int relayLifetime = 60, int relayRefreshTime = 30, float allocationResendTimeout = .1f, int maxAllocationResends = 8)
        {
            var settings = (NobleConnectSettings)Resources.Load("NobleConnectSettings", typeof(NobleConnectSettings));

            this.onFatalError = onFatalError;
            nobleConfig = new IceConfig
            {
                iceServerAddress = relayServerAddress,
                icePort = settings.relayServerPort,
                maxAllocationRetransmissionCount = maxAllocationResends,
                allocationRetransmissionTimeout = (int)(allocationResendTimeout * 1000),
                allocationLifetime = relayLifetime,
                refreshTime = relayRefreshTime
            };

            if (!string.IsNullOrEmpty(settings.gameID))
            {
                string decodedGameID = Encoding.UTF8.GetString(Convert.FromBase64String(settings.gameID));
                string[] parts = decodedGameID.Split('\n');

                if (parts.Length == 3)
                {
                    nobleConfig.username = parts[1];
                    nobleConfig.password = parts[2];
                    nobleConfig.origin = parts[0];
                }
            }

            Init();
        }

        /// <summary>Prepare to connect but don't actually connect yet</summary>
        /// <remarks>
        /// This is used when initializing a client early before connecting. Getting this
        /// out of the way earlier can make the actual connection seem quicker.
        /// </remarks>
        public void PrepareToConnect()
        {
            nobleConfig.forceRelayOnly = ForceRelayOnly;
            baseClient.PrepareToConnect();
        }

        /// <summary>If you are using the NetworkClient directly you must call this method every frame.</summary>
        /// <remarks>
        /// The NobleNetworkManager and NobleNetworkLobbyManager handle this for you but you if you are
        /// using the NobleClient directly you must make sure to call this method every frame.
        /// </remarks>
        public void Update()
        {
			if (baseClient != null) baseClient.Update();
        }

        /// <summary>Connect to the provided host ip and port</summary>
        /// <remarks>
        /// Note that the host address used here should be the one provided to the host by 
        /// the relay server, not the actual ip of the host's computer. You can get this 
        /// address on the host from Server.HostEndPoint.
        /// </remarks>
        /// <param name="hostIP">The IP of the server's HostEndPoint</param>
        /// <param name="hostPort">The port of the server's HostEndPoint</param>
        /// <param name="topo">The HostTopology to use for the NetworkServer.</param>
        public void Connect(string hostIP, ushort hostPort, bool isLANOnly = false)
        {
            Connect(new IPEndPoint(IPAddress.Parse(hostIP), hostPort), isLANOnly);
        }

        /// <summary>Connect to the provided HostEndPoint</summary>
        /// <remarks>
        /// Note that the host address used here should be the one provided to the host by 
        /// the relay server, not the actual ip of the host's computer. You can get this 
        /// address on the host from Server.HostEndPoint.
        /// </remarks>
        /// <param name="hostEndPoint">The HostEndPoint of the server to connect to</param>
        /// <param name="hostPort">The port of the server's HostEndPoint</param>
        /// <param name="topo">The HostTopology to use for the NetworkServer.</param>
        public void Connect(IPEndPoint hostEndPoint, bool isLANOnly = false)
        {
            if (isConnecting || isConnected) return;
            isConnecting = true;

            if (isLANOnly)
            {
                SetConnectPort((ushort)hostEndPoint.Port);
                NetworkClient.Connect(hostEndPoint.Address.ToString());
            }
            else
            {
                if (baseClient == null)
                {
                    Init();
                }
                baseClient.InitializeClient(hostEndPoint, OnReadyToConnect);
            }
        }

        public void SetConnectPort(ushort port)
        {
            bool hasUDP = false;
            var transportType = Transport.activeTransport.GetType();

#if LITENETLIB_TRANSPORT
            if (transportType == typeof(LiteNetLibTransport))
            {
                hasUDP = true;
                var liteNet = (LiteNetLibTransport)Transport.activeTransport;
                liteNet.port = (ushort)port;
            }
#endif
#if IGNORANCE
            if (transportType.IsSubclassOf(typeof(IgnoranceThreaded)) || 
                transportType == typeof(IgnoranceThreaded))
            {
                hasUDP = true;
                var ignorance = (IgnoranceThreaded)Transport.activeTransport;
                ignorance.CommunicationPort = port;
            }
#endif
            if (!hasUDP)
            {
                throw new Exception(TRANSPORT_WARNING_MESSAGE);
            }
        }

        /// <summary>Shut down the client and clean everything up.</summary>
        /// <remarks>
        /// You can call this method if you are totally done with a client and don't plan
        /// on using it to connect again.
        /// </remarks>
        public void Shutdown()
        {
            if (baseClient != null)
            {
                baseClient.CleanUpEverything();
                baseClient.Dispose();
                baseClient = null;
            }


            NetworkClient.Shutdown();
        }

        /// <summary>Clean up and free resources. Called automatically when garbage collected.</summary>
        /// <remarks>
        /// You shouldn't need to call this directly. It will be called automatically when an unused
        /// NobleClient is garbage collected or when shutting down the application.
        /// </remarks>
        /// <param name="disposing"></param>
        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                NetworkClient.Shutdown();
                if (baseClient != null) baseClient.Dispose();
            }
            isConnecting = false;
        }
        public void Dispose()
        {
            Dispose(true);
        }

        #endregion Public Interface

        #region Internal Methods

        /// <summary>Initialize the NetworkClient and NobleConnect client</summary>
        private void Init()
        {
            var platform = Application.platform;
            nobleConfig.useSimpleAddressGathering = (platform == RuntimePlatform.IPhonePlayer || platform == RuntimePlatform.Android) && !Application.isEditor;
            nobleConfig.onOfferFailed = CancelConnection;
            nobleConfig.onFatalError = OnFatalError;
            nobleConfig.forceRelayOnly = ForceRelayOnly;

            baseClient = new Peer(nobleConfig);

            NetworkClient.ReplaceHandler<ConnectMessage>(OnClientConnect, false);
            NetworkClient.ReplaceHandler<DisconnectMessage>(OnClientDisconnect, false);
        }

        /// <summary>Finish the disconnect process and clean everything up</summary>
        private void CancelConnection()
        {
            NetworkClient.Disconnect();
            InvokeHandler(new DisconnectMessage(), 0);

            isConnecting = false;
        }

        /// <summary>
        /// This function invokes the registered handler function for a message.
        /// <para>Network connections used by the NetworkClient and NetworkServer use this function for handling network messages.</para>
        /// </summary>
        /// <typeparam name="T">The message type to unregister.</typeparam>
        /// <param name="msg">The message object to process.</param>
        /// <returns>Returns true if the handler was successfully invoked</returns>
        public bool InvokeHandler<T>(T msg, int channelId) where T : NetworkMessage
        {
            using (PooledNetworkWriter writer = NetworkWriterPool.GetWriter())
            {
                // if it is a value type,  just use typeof(T) to avoid boxing
                // this works because value types cannot be derived
                // if it is a reference type (for example NetworkMessage),
                // ask the message for the real type
                int msgType = MessagePacker.GetId(default(T) != null ? typeof(T) : msg.GetType());

                MessagePacker.Pack(msg, writer);
                ArraySegment<byte> segment = writer.ToArraySegment();
                using (PooledNetworkReader networkReader = NetworkReaderPool.GetReader(segment))
                    return InvokeHandler(msgType, networkReader, channelId);
            }
        }

        internal bool InvokeHandler(int msgType, NetworkReader reader, int channelId)
        {
            var messageHandlersProperty = typeof(NetworkClient).GetField("handlers", BindingFlags.Static | BindingFlags.NonPublic);
            var messageHandlers = (Dictionary<int, NetworkMessageDelegate>)messageHandlersProperty.GetValue(null);
            if (messageHandlers.TryGetValue(msgType, out NetworkMessageDelegate msgDelegate))
            {
                msgDelegate(null, reader, channelId);
                return true;
            }
            Logger.Log("Unknown message ID " + msgType + " " + this + ". May be due to no existing RegisterHandler for this message.");
            return false;
        }

        #endregion Internal Methods

        #region Handlers

        /// <summary>Called when a fatal error occurs.</summary>
        /// <remarks>
        /// This usually means that the ccu or bandwidth limit has been exceeded. It will also
        /// happen if connection is lost to the relay server for some reason.
        /// </remarks>
        /// <param name="errorString">A string with more info about the error</param>
        private void OnFatalError(string errorString)
        {
            Logger.Log("Shutting down because of a fatal error: " + errorString, Logger.Level.Fatal);
            CancelConnection();
            if (onFatalError != null) onFatalError(errorString);
        }

        /// <summary>Called when Noble Connect has selected a candidate pair to use to connect to the host.</summary>
        /// <param name="bridgeEndPoint">The EndPoint to connect to</param>
        private void OnReadyToConnect(IPEndPoint bridgeEndPoint, IPEndPoint bridgeEndPointIPv6)
        {
            if (Socket.OSSupportsIPv6 && bridgeEndPointIPv6 != null)
            {
                hostBridgeEndPoint = bridgeEndPointIPv6;
                SetConnectPort((ushort)bridgeEndPointIPv6.Port);
                NetworkClient.Connect(bridgeEndPointIPv6.Address.ToString());
            }
            else
            {
                hostBridgeEndPoint = bridgeEndPoint;
                SetConnectPort((ushort)bridgeEndPoint.Port);
                NetworkClient.Connect(bridgeEndPoint.Address.ToString());
            }
        }

        /// <summary>Called on the client upon succesfully connecting to a host</summary>
        /// <remarks>
        /// We clean some ice stuff up here.
        /// </remarks>
        /// <param name="message"></param>
        private void OnClientConnect(NetworkConnection conn, ConnectMessage message)
        {
            // This happens when connecting in LAN only mode, which is always direct
            if (baseClient == null) latestConnectionType = ConnectionType.DIRECT;

            isConnecting = false;
            if (baseClient != null)
            {
                baseClient.FinalizeConnection(hostBridgeEndPoint);
            }
        }

        /// <summary>Called on the client upon disconnecting from a host</summary>
        /// <remarks>
        /// Some memory and ports are freed here.
        /// </remarks>
        /// <param name="message"></param>
        private void OnClientDisconnect(NetworkConnection conn, DisconnectMessage message)
        {
            if (baseClient != null) baseClient.EndSession(hostBridgeEndPoint);
        }

        #endregion Handlers

        #region Mirror NetworkClient public interface

#if !DOXYGEN_SHOULD_SKIP_THIS
        /// The rest of this is just a wrapper for Mirror's NetworkClient

        /// <summary>
        /// The NetworkConnection object this client is using.
        /// </summary>
        public NetworkConnection connection => NetworkClient.connection;

        /// <summary>
        /// active is true while a client is connecting/connected
        /// (= while the network is active)
        /// </summary>
        public bool active => NetworkClient.active;

        /// <summary>
        /// This gives the current connection status of the client.
        /// </summary>
        public bool isConnected => NetworkClient.isConnected;

        /// <summary>
        /// NetworkClient can connect to local server in host mode too
        /// </summary>
        public bool isLocalClient => NetworkClient.isLocalClient;

        /// <summary>
        /// Connect client to a NetworkServer instance.
        /// </summary>
        /// <param name="address"></param>
        public void Connect(string address)
        {
            NetworkClient.Connect(address);
        }

        /// <summary>
        /// Connect client to a NetworkServer instance.
        /// </summary>
        /// <param name="uri">Address of the server to connect to</param>
        public void Connect(Uri uri)
        {
            NetworkClient.Connect(uri);
        }

        public void ConnectHost()
        {
            NetworkClient.ConnectHost();
        }

        /// <summary>
        /// connect host mode
        /// </summary>
        public void ConnectLocalServer()
        {
            NetworkClient.ConnectLocalServer();
        }

        /// <summary>
        /// disconnect host mode. this is needed to call DisconnectMessage for
        /// the host client too.
        /// </summary>
        public void DisconnectLocalServer()
        {
            NetworkClient.DisconnectLocalServer();
        }

        /// <summary>
        /// Disconnect from server.
        /// <para>The disconnect message will be invoked.</para>
        /// </summary>
        public void Disconnect()
        {
            NetworkClient.Disconnect();
        }

        /// <summary>
        /// This sends a network message with a message Id to the server. This message is sent on channel zero, which by default is the reliable channel.
        /// <para>The message must be an instance of a class derived from MessageBase.</para>
        /// <para>The message id passed to Send() is used to identify the handler function to invoke on the server when the message is received.</para>
        /// </summary>
        /// <typeparam name="T">The message type to unregister.</typeparam>
        /// <param name="message"></param>
        /// <param name="channelId"></param>
        public void Send<T>(T message, int channelId = Channels.DefaultReliable) where T : NetworkMessage
        {
            NetworkClient.Send<T>(message, channelId);
        }

        /// <summary>
        /// Register a handler for a particular message type.
        /// <para>There are several system message types which you can add handlers for. You can also add your own message types.</para>
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <param name="handler">Function handler which will be invoked when this message type is received.</param>
        /// <param name="requireAuthentication">True if the message requires an authenticated connection</param>
        public void RegisterHandler<T>(Action<NetworkConnection, T> handler, bool requireAuthentication = true) where T : NetworkMessage
        {
            int msgType = MessagePacker.GetId<T>();
            if (typeof(T) == typeof(ConnectMessage))
            {
                NetworkClient.RegisterHandler<ConnectMessage>((conn, msg) => {
                    OnClientConnect(conn, msg);
                    handler(conn, (T)(msg as NetworkMessage));
                },
                    requireAuthentication
                );
            }
            else if (typeof(T) == typeof(DisconnectMessage))
            {
                NetworkClient.RegisterHandler<DisconnectMessage>((conn, msg) => {
                    OnClientDisconnect(conn, msg);
                    handler(conn, (T)(msg as NetworkMessage));
                },
                    requireAuthentication
                );
            }
            else
            {
                NetworkClient.RegisterHandler<T>(handler, requireAuthentication);
            }
        }

        /// <summary>
        /// Register a handler for a particular message type.
        /// <para>There are several system message types which you can add handlers for. You can also add your own message types.</para>
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <param name="handler">Function handler which will be invoked when this message type is received.</param>
        /// <param name="requireAuthentication">True if the message requires an authenticated connection</param>
        public void RegisterHandler<T>(Action<T> handler, bool requireAuthentication = true) where T : NetworkMessage
        {
            int msgType = MessagePacker.GetId<T>();
            if (typeof(T) == typeof(ConnectMessage))
            {
                NetworkClient.RegisterHandler<ConnectMessage>((conn, msg) => {
                    OnClientConnect(conn, msg);
                    handler((T)(msg as NetworkMessage));
                },
                    requireAuthentication
                );
            }
            else if (typeof(T) == typeof(DisconnectMessage))
            {
                NetworkClient.RegisterHandler<DisconnectMessage>((conn, msg) => {
                    OnClientDisconnect(conn, msg);
                    handler((T)(msg as NetworkMessage));
                },
                    requireAuthentication
                );
            }
            else
            {
                NetworkClient.RegisterHandler<T>(handler, requireAuthentication);
            }
        }

        /// <summary>
        /// Replaces a handler for a particular message type.
        /// <para>See also <see cref="RegisterHandler{T}(Action{NetworkConnection, T}, bool)">RegisterHandler(T)(Action(NetworkConnection, T), bool)</see></para>
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <param name="handler">Function handler which will be invoked when this message type is received.</param>
        /// <param name="requireAuthentication">True if the message requires an authenticated connection</param>
        public void ReplaceHandler<T>(Action<NetworkConnection, T> handler, bool requireAuthentication = true) where T : NetworkMessage
        {
            int msgType = MessagePacker.GetId<T>();
            if (typeof(T) == typeof(ConnectMessage))
            {
                NetworkClient.ReplaceHandler<ConnectMessage>((conn, msg) => {
                    OnClientConnect(conn, msg);
                    handler(connection, (T)(msg as NetworkMessage));
                },
                    requireAuthentication
                );
            }
            else if (typeof(T) == typeof(DisconnectMessage))
            {
                NetworkClient.ReplaceHandler<DisconnectMessage>((conn, msg) => {
                    OnClientDisconnect(conn, msg);
                    handler(connection, (T)(msg as NetworkMessage));
                },
                    requireAuthentication
                );
            }
            else
            {
                NetworkClient.RegisterHandler<T>(handler, requireAuthentication);
            }
        }

        /// <summary>
        /// Replaces a handler for a particular message type.
        /// <para>See also <see cref="RegisterHandler{T}(Action{NetworkConnection, T}, bool)">RegisterHandler(T)(Action(NetworkConnection, T), bool)</see></para>
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <param name="handler">Function handler which will be invoked when this message type is received.</param>
        /// <param name="requireAuthentication">True if the message requires an authenticated connection</param>
        public void ReplaceHandler<T>(Action<T> handler, bool requireAuthentication = true) where T : NetworkMessage
        {
            int msgType = MessagePacker.GetId<T>();
            if (typeof(T) == typeof(ConnectMessage))
            {
                NetworkClient.ReplaceHandler<ConnectMessage>((conn, msg) => {
                    OnClientConnect(conn, msg);
                    handler((T)(msg as NetworkMessage));
                },
                    requireAuthentication
                );
            }
            else if (typeof(T) == typeof(DisconnectMessage))
            {
                NetworkClient.ReplaceHandler<DisconnectMessage>((conn, msg) => {
                    OnClientDisconnect(conn, msg);
                    handler((T)(msg as NetworkMessage));
                },
                    requireAuthentication
                );
            }
            else
            {
                NetworkClient.RegisterHandler<T>(handler, requireAuthentication);
            }
        }

        /// <summary>
        /// Unregisters a network message handler.
        /// </summary>
        /// <typeparam name="T">The message type to unregister.</typeparam>
        public bool UnregisterHandler<T>() where T : NetworkMessage
        {
            return NetworkClient.UnregisterHandler<T>();
        }
    }

    #endif

    #endregion
}
