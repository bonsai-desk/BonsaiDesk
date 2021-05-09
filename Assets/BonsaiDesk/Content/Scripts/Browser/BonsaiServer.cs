using UnityEngine;

namespace Bonsai.Web
{
    public class BonsaiServer : MonoBehaviour
    {
        private IBonsaiWebServer _server;

        private void Start()
        {
        #if UNITY_EDITOR
            _server = new EmbedIOWebServer();
        #elif UNITY_ANDROID && !UNITY_EDITOR
            _server = new NanoWebServer();
        #endif
            _server.Start();
        }

        private void OnApplicationQuit()
        {
            _server?.Shutdown();
        }

        void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                _server?.Pause();
            }
            else
            {
                _server?.Resume();
            }
        }
    }
}