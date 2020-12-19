using UnityEngine;

namespace NobleConnect
{
    /// <summary>Settings used by Noble Connect to authenticate with the relay and punchthrough services</summary>
    public class NobleConnectSettings : ScriptableObject
    {
        /// <summary>Used to identify your game and authenticate with the relay servers</summary>
        /// <remarks>
        /// This is populated for you when you go through the setup wizard but you can also set it manually here.
        /// Your game ID is available any time on the dashboard at noblewhale.com
        /// </remarks>
        [Tooltip("Used to identify your game and authenticate with the relay servers")]
        public string gameID;

        [HideInInspector]
        public ushort relayServerPort = 3478;
    }
}
