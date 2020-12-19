using Mirror;
using UnityEngine;

namespace Dissonance.Integrations.MirrorIgnorance
{
    /// <summary>
    /// When added to the player prefab, allows Dissonance to automatically track
    /// the location of remote players for positional audio for games using the
    /// Mirror Networking API.
    /// </summary>
    [RequireComponent(typeof (NetworkIdentity))]
    public class MirrorIgnorancePlayer
        : NetworkBehaviour, IDissonancePlayer
    {
        private static readonly Log Log = Logs.Create(LogCategory.Network, "Mirror Player Component");

        private DissonanceComms _comms;

        public bool IsTracking { get; private set; }

        /// <summary>
        /// The name of the player
        /// </summary>
        /// <remarks>
        /// This is a syncvar, this means unity will handle setting this value.
        /// This is important for Join-In-Progress because new clients will join and instantly have the player name correctly set without any effort on our part.
        /// https://docs.unity3d.com/Manual/UNetStateSync.html
        /// </remarks>
        [SyncVar]
        private string _playerId;
        public string PlayerId { get { return _playerId; } }

        public Vector3 Position
        {
            get { return transform.position; }
        }

        public Quaternion Rotation
        {
            get { return transform.rotation; }
        }

        public NetworkPlayerType Type
        {
            get
            {
                if (_comms == null || _playerId == null)
                    return NetworkPlayerType.Unknown;
                return _comms.LocalPlayerName.Equals(_playerId) ? NetworkPlayerType.Local : NetworkPlayerType.Remote;
            }
        }
        
        public void OnDestroy()
        {
            if (_comms != null)
                _comms.LocalPlayerNameChanged -= SetPlayerName;
        }

        public void OnEnable()
        {
            _comms = FindObjectOfType<DissonanceComms>();
        }

        public void OnDisable()
        {
            if (IsTracking)
                StopTracking();
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();

            var comms = FindObjectOfType<DissonanceComms>();
            if (comms == null)
            {
                throw Log.CreateUserErrorException(
                    "cannot find DissonanceComms component in scene",
                    "not placing a DissonanceComms component on a game object in the scene",
                    "https://dissonance.readthedocs.io/en/latest/Basics/Quick-Start-MirrorIgnorance/",
                    "2D90A6C3-5F2B-4859-994C-EBBDDD4A10F4"
                );
            }

            Log.Debug("Tracking `OnStartLocalPlayer` Name={0}", comms.LocalPlayerName);

            // This method is called on the client which has control authority over this object. This will be the local client of whichever player we are tracking.
            if (comms.LocalPlayerName != null)
                SetPlayerName(comms.LocalPlayerName);

            //Subscribe to future name changes (this is critical because we may not have run the initial set name yet and this will trigger that initial call)
            comms.LocalPlayerNameChanged += SetPlayerName;
        }

        private void SetPlayerName(string playerName)
        {
            //We need the player name to be set on all the clients and then tracking to be started (on each client).
            //To do this we send a command from this client, informing the server of our name. The server will pass this on to all the clients (with an RPC)
            // Client -> Server -> Client

            //We need to stop and restart tracking to handle the name change
            if (IsTracking)
                StopTracking();

            //Perform the actual work
            _playerId = playerName;
            StartTracking();

            //Inform the server the name has changed
            if (isLocalPlayer)
                CmdSetPlayerName(playerName);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            //A client is starting. Start tracking if the name has been properly initialised.
            if (!string.IsNullOrEmpty(PlayerId))
                StartTracking();
        }

        /// <summary>
        /// Invoking on client will cause it to run on the server
        /// </summary>
        /// <param name="playerName"></param>
        [Command]
        private void CmdSetPlayerName(string playerName)
        {
            _playerId = playerName;

            //Now call the RPC to inform clients they need to handle this changed value
            RpcSetPlayerName(playerName);
        }

        /// <summary>
        /// Invoking on the server will cause it to run on all the clients
        /// </summary>
        /// <param name="playerName"></param>
        [ClientRpc]
        private void RpcSetPlayerName(string playerName)
        {
            //received a message from server (on all clients). If this is not the local player then apply the change
            if (!isLocalPlayer)
                SetPlayerName(playerName);
        }

        private void StartTracking()
        {
            if (IsTracking)
                throw Log.CreatePossibleBugException("Attempting to start player tracking, but tracking is already started", "31971B1F-52FD-4FCF-89E9-67A17A917921");

            if (_comms != null)
            {
                _comms.TrackPlayerPosition(this);
                IsTracking = true;
            }
        }

        private void StopTracking()
        {
            if (!IsTracking)
                throw Log.CreatePossibleBugException("Attempting to stop player tracking, but tracking is not started", "C7CF0174-0667-4F07-88E3-800ED652142D");

            if (_comms != null)
            {
                _comms.StopTracking(this);
                IsTracking = false;
            }
        }
    }
}