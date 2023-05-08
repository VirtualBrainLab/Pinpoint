using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using LightJson;
using Unisave.Foundation;
using UnityEngine;
using UnityEngine.Networking;

namespace Unisave.HttpClient
{
    /// <summary>
    /// Http client for the Unisave asset
    /// (the server-side one cannot be used, because it's synchronous)
    /// </summary>
    public class AssetHttpClient
    {
        private ClientApplication app;
        
        public AssetHttpClient(ClientApplication app)
        {
            this.app = app;
        }
        
        private HttpClientComponent ResolveComponent()
        {
            if (app.InEditMode)
            {
                // create a game object for the lifetime of the request
                // and then destroy it immediately
                var go = new GameObject(
                    "UnisaveHttpClient",
                    typeof(HttpClientComponent)
                );
                var component = go.GetComponent<HttpClientComponent>();
                component.DestroyImmediateAfterOneRequest = true;
                return component;
            }
            else
            {
                return app.GameObject.GetComponent<HttpClientComponent>();
            }
        }
        
        public void Get(string url, Action<Response> callback)
        {
            Send("GET", url, null, null, callback);
        }
        
        public void Post(string url, JsonObject payload, Action<Response> callback)
        {
            Send("POST", url, null, payload, callback);
        }

        private void Send(
            string method,
            string url,
            Dictionary<string, string> headers,
            JsonObject payload,
            Action<Response> callback
        )
        {
            // if there's no callback, just send the request synchronously
            // and don't wait for anything. This is needed for requests
            // that need to be fired during scene tear down.
            if (callback == null)
            {
                var c = TheRequestCoroutine(method, url, headers, payload, null);
                while (c.MoveNext()) {}
                return;
            }
        
            // execute regularly with the callback,
            // via a MonoBehaviour that can run the coroutine asynchronously
            ResolveComponent().SendRequest(
                method, url, headers, payload, callback
            );
        }
        
        /// <summary>
        /// The core logic that sends the request.
        /// It has to send the request but do nothing to the response.
        /// </summary>
        internal static IEnumerator TheRequestCoroutine(
            string method,
            string url,
            Dictionary<string, string> headers,
            JsonObject payload,
            Action<UnityWebRequest, DownloadHandlerBuffer> callback
        )
        {
            // TODO: enforce SSL certificates
            
            using var downloadHandler = new DownloadHandlerBuffer();

            using var uploadHandler = (payload == null) ? null :
                new UploadHandlerRaw(
                    Encoding.UTF8.GetBytes(payload.ToString())
                );

            using UnityWebRequest request = new UnityWebRequest(
                url,
                method,
                downloadHandler,
                uploadHandler
            );
            
            if (headers != null)
                foreach (var pair in headers)
                    request.SetRequestHeader(pair.Key, pair.Value);

            if (payload != null)
            {
                request.SetRequestHeader("Accept", "application/json");
                request.SetRequestHeader("Content-Type", "application/json");
            }

            yield return request.SendWebRequest();
            
            callback?.Invoke(request, downloadHandler);
        }
    }
}