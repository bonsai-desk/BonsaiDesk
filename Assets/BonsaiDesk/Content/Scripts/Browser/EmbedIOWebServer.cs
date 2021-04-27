using System;
using EmbedIO;
using EmbedIO.Actions;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using Swan.Logging;
using UnityEngine;

namespace Bonsai.Web
{
    public class EmbedIOWebServer : IBonsaiWebServer
    {
        private WebServer _server;

        public void Start()
        {
            var htmlRoot = Application.streamingAssetsPath + "/build";
            const string url = "http://localhost:9696/";
            _server = CreateWebServer(url, htmlRoot);
            _server.RunAsync();
        }

        public void Shutdown()
        {
            Debug.Log("Shutting down web server");
            _server.Dispose();
        }

        public void Pause()
        {
            //
        }

        public void Resume()
        {
            //
        }

        private static WebServer CreateWebServer(string url, string htmlRoot)
        {
            var server = new WebServer(o => o
                        .WithUrlPrefix(url)
                        .WithMode(HttpListenerMode.EmbedIO))
                        .WithLocalSessionManager()
                        .WithStaticFolder("/", htmlRoot, true)
                        //.WithWebApi("/ui", m => m.WithController<InterfaceController>())
                        .WithModule(new ActionModule("/", HttpVerbs.Any, ctx => ctx.SendDataAsync(new {Message = "Error"})));

            server.StateChanged += (s, e) => $"WebServer New State - {e.NewState}".Info();

            return server;
        }
    }

   //public sealed class InterfaceController : WebApiController
   //{
   //    [Route(HttpVerbs.Get, "/ping")]
   //    public string GetPing()
   //    {
   //        return "pong";
   //    }
   //}
}