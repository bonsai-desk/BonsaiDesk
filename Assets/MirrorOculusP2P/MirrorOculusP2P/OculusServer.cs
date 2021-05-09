using System;
using System.Collections.Generic;
using Oculus.Platform;
using Oculus.Platform.Models;
using Debug = UnityEngine.Debug;

namespace Mirror.OculusP2P
{
    public class OculusServer : OculusCommon, IServer
    {
        private List<(int, byte[], int)> _messages = new List<(int, byte[], int)>();
        private event Action<int> OnConnected;
        private event Action<int, byte[], int> OnReceivedData;
        private event Action<int> OnDisconnected;
        private event Action<int, Exception> OnReceivedError;

        private readonly BidirectionalDictionary<ulong, int> _oculusIDToMirrorID;
        private readonly int _maxConnections;
        private int _nextConnectionID;

        private OculusServer(int maxConnections)
        {
            _maxConnections = maxConnections;
            _oculusIDToMirrorID = new BidirectionalDictionary<ulong, int>();
            _nextConnectionID = 1;
            Net.SetPeerConnectRequestCallback(OnPeerConnectRequest);
            Net.SetConnectionStateChangedCallback(OnConnectionStatusChanged);
        }

        public static OculusServer CreateServer(OculusTransport transport, int maxConnections)
        {
            OculusServer s = new OculusServer(maxConnections);

            s.OnConnected += (id) => transport.OnServerConnected.Invoke(id);
            s.OnDisconnected += (id) => transport.OnServerDisconnected.Invoke(id);
            s.OnReceivedData += (id, data, ch) => transport.OnServerDataReceived.Invoke(id, new ArraySegment<byte>(data), ch);
            s.OnReceivedError += (id, exception) => transport.OnServerError.Invoke(id, exception);

            if (!Core.IsInitialized())
            {
                OculusLogError("Oculus platform not initialized.");
            }

            return s;
        }

        private void OnPeerConnectRequest(Message<NetworkingPeer> message)
        {
            var oculusId = message.Data.ID;
            if (_oculusIDToMirrorID.TryGetValue(oculusId, out int _))
            {
                OculusLogError($"Incoming connection {oculusId} already exists");
            }
            else
            {
                if (_oculusIDToMirrorID.Count >= _maxConnections)
                {
                    OculusLog($"Incoming connection {oculusId} would exceed max connection count. Rejecting.");
                }
                else
                {
                    OculusLog($"Accept connection {oculusId}");
                    Net.Accept(oculusId);
                }
            }
        }

        private void OnConnectionStatusChanged(Message<NetworkingPeer> message)
        {
            var oculusId = message.Data.ID;
            switch (message.Data.State)
            {
                case PeerConnectionState.Unknown:
                    break;
                case PeerConnectionState.Connected:
                    int connectionId = _nextConnectionID++;
                    _oculusIDToMirrorID.Add(oculusId, connectionId);
                    OnConnected.Invoke(connectionId);
                    OculusLog($"Client with OculusID {oculusId} connected. Assigning connection id {connectionId}");

                    break;
                case PeerConnectionState.Timeout:
                    break;
                case PeerConnectionState.Closed:
                    if (_oculusIDToMirrorID.TryGetValue(oculusId, out int connId))
                    {
                        InternalDisconnect(connId, oculusId);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void InternalDisconnect(int connId, ulong userId)
        {
            ClearMessagesFor(connId);
            if (_oculusIDToMirrorID.TryGetValue(userId, out int _))
            {
                OculusLog($"Internal disconnect ({connId})");
                _oculusIDToMirrorID.Remove(connId);
                OnDisconnected.Invoke(connId);
            }
            else
            {
                OculusLogWarning($"Nothing to disconnect");
            }
        }

        private void ClearMessagesFor(int connId)
        {
            var prunedMessages = new List<(int, byte[], int)>();
            for (var i = 0; i < _messages.Count; i++)
            {
                if (connId != _messages[i].Item1)
                {
                    prunedMessages.Add(_messages[i]);
                }
            }

            _messages = prunedMessages;

        }

        public bool Disconnect(int connectionId)
        {
            if (_oculusIDToMirrorID.TryGetValue(connectionId, out ulong userId))
            {
                OculusLog($"Closing connection {connectionId}");
                Net.Close(userId);
                //_oculusIDToMirrorID.Remove(connectionId);
            }
            else
            {
                OculusLogWarning("Trying to disconnect unknown connection id: " + connectionId);
            }

            return true;
        }

        public void FlushData()
        {
            for (var i = 0; i < _messages.Count; i++)
            {
                var (connectionId, data, channelId) = _messages[i];
                if (_oculusIDToMirrorID.TryGetValue(connectionId, out ulong userId))
                {
                    var sent = SendPacket(userId, data, channelId);

                    if (!sent)
                    {
                        OculusLogWarning("Failed to send packet");
                        
                        // todo
                        Net.Close(userId);
                        
                        InternalDisconnect(connectionId, userId);
                    }
                }
                else
                {
                    OculusLogError("Trying to send on unknown connection: " + connectionId);
                    OnReceivedError.Invoke(connectionId, new Exception("ERROR Unknown Connection"));
                }
            }
            _messages.Clear();
        }

        public void ReceiveData()
        {
            Packet packet;
            while ((packet = Net.ReadPacket()) != null)
            {
                if (_oculusIDToMirrorID.TryGetValue(packet.SenderID, out int connId))
                {
                    (byte[] data, int ch) = ProcessPacket(packet);
                    OnReceivedData(connId, data, ch);
                }
                else
                {
                    Debug.LogWarning("Ignoring packet from sender not in dictionary");
                }
            }
        }
        
        public void Send(int connectionId, byte[] data, int channelId)
        {
            if (_oculusIDToMirrorID.TryGetValue(connectionId, out ulong _))
            {
                _messages.Add((connectionId, data, channelId));
            }
            else
            {
                OculusLogWarning($"Ignoring attempt to add message for unknown id {connectionId}");
            }
        }

        public string ServerGetClientAddress(int connectionId)
        {
            if (_oculusIDToMirrorID.TryGetValue(connectionId, out ulong userId))
            {
                return userId.ToString();
            }
            else
            {
                OculusLogError("Trying to get info on unknown connection: " + connectionId);
                OnReceivedError.Invoke(connectionId, new Exception("ERROR Unknown Connection"));
                return string.Empty;
            }
        }

        public void Shutdown()
        {
            Net.SetPeerConnectRequestCallback(_ => { });
            Net.SetConnectionStateChangedCallback(_ => { });
            DisposeAllPackets();
        }

        #region Logging

        private static void OculusLog(string msg)
        {
            Debug.Log("<color=orange>OculusServer: </color>: " + msg);
        }

        private static void OculusLogWarning(string msg)
        {
            Debug.LogWarning("<color=orange>OculusServer: </color>: " + msg);
        }

        private static void OculusLogError(string msg)
        {
            Debug.LogError("<color=orange>OculusServer: </color>: " + msg);
        }

        #endregion
    }
}