using System;
using UnityEngine;

namespace Bonsai.Web
{
    public class NanoWebServer : IBonsaiWebServer
    {
        private AndroidJavaObject _server;

        public void Start()
        {
            const string className = "com.example.bonsai.NanoWebServer";
            const string methodName = "main";
            try
            {
                using (var nanoClass = new AndroidJavaClass(className))
                {
                    object[] args = {"build"};
                    _server = nanoClass.CallStatic<AndroidJavaObject>(methodName, args);
                }
            }

            catch (Exception ex)
            {
                Debug.LogWarning($"{className}.{methodName} Exception:{ex}");
            }
        }

        public void Shutdown()
        {
            _server?.Call("ShutDown");
        }

        public void Pause()
        {
            _server?.Call("Pause");
        }
        
        public void Resume()
        {
            _server?.Call("Resume");
        }
    }
}