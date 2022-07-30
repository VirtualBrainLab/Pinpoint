using System;
using System.Collections.Generic;
using System.Net.Mime;
using LightJson;
using UnityEngine;
using UnityEngine.Networking;

namespace Unisave.HttpClient
{
    public class HttpClientComponent : MonoBehaviour
    {
        // TODO: display debug log, like SSE socket has

        /// <summary>
        /// Should the game object be destroyed after one performed request?
        /// (used for requests in edit mode)
        /// </summary>
        public bool DestroyImmediateAfterOneRequest { get; set; } = false;
        
        public void SendRequest(
            string method,
            string url,
            Dictionary<string, string> headers,
            JsonObject payload,
            Action<Response> callback
        )
        {
            void HandleRequestResponse(
                UnityWebRequest request,
                DownloadHandlerBuffer downloadHandler
            )
            {
                var contentType = request.GetResponseHeader("Content-Type")
                    ?? "text/plain";

                string body = downloadHandler.text;

                #if UNITY_2020_1_OR_NEWER
                    bool isNetworkError =
                        request.result == UnityWebRequest.Result.ConnectionError;
                #else
                    bool isNetworkError = request.isNetworkError;
                #endif
                
                if (isNetworkError)
                    body = request.error;

                var response = Response.Create(
                    body,
                    new ContentType(contentType).MediaType,
                    (int) request.responseCode
                );
                
                // TODO: don't ignore headers but process them instead

                callback?.Invoke(response);

                if (DestroyImmediateAfterOneRequest)
                    DestroyImmediate(gameObject);
            }

            StartCoroutine(
                AssetHttpClient.TheRequestCoroutine(
                    method, url, headers, payload, HandleRequestResponse
                )
            );
        }
    }
}