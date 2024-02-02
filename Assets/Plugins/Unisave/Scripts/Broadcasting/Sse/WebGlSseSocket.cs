#if UNITY_WEBGL
using System;
using System.Collections;
using System.Runtime.InteropServices;
using LightJson;
using Unisave.Foundation;
using Unisave.Sessions;
using Unisave.Utils;
using UnityEngine;
using UnityEngine.Scripting;
using Application = UnityEngine.Application;

namespace Unisave.Broadcasting.Sse
{
    /// <summary>
    /// SSE Socket used in WebGl, it interacts with native Javascript and that
    /// in turn uses the browser "fetch()" API to perform long-polling SSE requests
    /// </summary>
    public class WebGlSseSocket : MonoBehaviour, ISseSocket
    {
        // interface events
        public event Action<SseEvent> OnEventReceived;
        public event Action OnConnectionLost;
        public event Action OnConnectionRegained;
        
        [DllImport("__Internal")]
        private static extern void JS_WebGlSseSocket_StartPoll(
            string gameObjectName, string url, string jsonBody
        );
        
        [DllImport("__Internal")]
        private static extern void JS_WebGlSseSocket_AbortPoll();
        
        /// <summary>
        /// Reference to the client app to resolve connection information
        /// (keys, tokens and hashes)
        /// </summary>
        private ClientApplication app;
        
        /// <summary>
        /// Internal state of the SSE socket
        /// </summary>
        public readonly SseStateManager state;
        
        /// <summary>
        /// State of the connection to the Unisave broadcasting server
        /// </summary>
        public BroadcastingConnection ConnectionState
        {
            get => state.GetConnectionState();
            private set => state.SetConnectionState(value);
        }
        
        /// <summary>
        /// Flag to distinguish intended disconnection from a network crash
        /// </summary>
        private bool intendedDisconnection = false;

        private readonly StreamToEventsSlicer slicer = new StreamToEventsSlicer();

        public WebGlSseSocket()
        {
            state = new SseStateManager(
                invokeConnectionLost: () => OnConnectionLost?.Invoke(),
                invokeConnectionRegained: () => OnConnectionRegained?.Invoke()
            );

            slicer.OnEventReceived += HandleEvent;
        }
        
        /// <summary>
        /// Call this right after this component is created
        /// </summary>
        public void Initialize(ClientApplication app)
        {
            this.app = app;
        }
        
        /// <summary>
        /// Enabling the component acts as a wanted connection / reconnection
        /// </summary>
        private void OnEnable()
        {
            if (ConnectionState == BroadcastingConnection.Disconnected)
            {
                // update state
                ConnectionState = BroadcastingConnection.Connecting;
                
                // connect
                StartCoroutine(StartPollCoroutine());
            }
        }
        
        /// <summary>
        /// Disabling the component acts as a wanted disconnection
        /// </summary>
        private void OnDisable()
        {
            if (ConnectionState != BroadcastingConnection.Disconnected)
            {
                intendedDisconnection = true;
                JS_WebGlSseSocket_AbortPoll();
                slicer.ClearBuffer();
                ConnectionState = BroadcastingConnection.Disconnected;
            }
        }

        private IEnumerator StartPollCoroutine()
        {
            // skip a frame so that the Initialize
            // method gets called
            yield return null;
            
            // handle retrying
            if (ConnectionState == BroadcastingConnection.Reconnecting)
            {
#if UNISAVE_BROADCASTING_DEBUG
                UnityEngine.Debug.Log(
                    $"[UnisaveBroadcasting] Waiting on retry {state.RetryMilliseconds}ms"
                );
#endif

                yield return new WaitForSeconds(state.RetryMilliseconds / 1000f);
            }
            
            // reset flags
            intendedDisconnection = false;
            
            // === PREPARE THE REQUEST AND HANDLERS ===
            
            var url = app.Services.Resolve<ApiUrl>();
            var sessionIdRepo = app.Services.Resolve<ClientSessionIdRepository>();
            
            slicer.ClearBuffer();
            
#if UNISAVE_BROADCASTING_DEBUG
            UnityEngine.Debug.Log(
                $"[UnisaveBroadcasting] Starting poll with last " +
                $"received id = {state.LastReceivedEventId}"
            );
#endif

            JS_WebGlSseSocket_StartPoll(
                gameObjectName: gameObject.name,
                url: url.BroadcastingListen(),
                jsonBody: new JsonObject {
                    ["gameToken"] = app.Preferences.GameToken,
                    ["editorKey"] = app.Preferences.EditorKey,
                    ["buildGuid"] = ClientIdentity.BuildGuid,
                    ["backendHash"] = app.Preferences.BackendHash,
                    ["sessionId"] = sessionIdRepo.GetSessionId(),
                    ["lastReceivedEventId"] = state.LastReceivedEventId
                }.ToString()
            );
        }

        /// <summary>
        /// Called by the javascript when the poll finishes
        /// (empty string = no error)
        /// </summary>
        [Preserve]
        public void JsCallback_OnDone(string error)
        {
#if UNISAVE_BROADCASTING_DEBUG
            UnityEngine.Debug.Log($"[UnisaveBroadcasting] Long poll finished.");
#endif
            
            if (intendedDisconnection)
            {
                // update state
                ConnectionState = BroadcastingConnection.Disconnected;
            }
            else
            {
                if (string.IsNullOrEmpty(error))
                    error = "The HTTP request ended successfully.";
                
                Debug.LogWarning(
                    $"[Unisave] Broadcasting client connection broke, " +
                    $"retrying in {state.RetryMilliseconds}ms\n" +
                    $"The reason is: {error}"
                );
                
                // update state
                ConnectionState = BroadcastingConnection.Reconnecting;
                
                // retry
                StartCoroutine(StartPollCoroutine());
            }
        }

        /// <summary>
        /// Called by the javascript when the next chunk of data arrives
        /// </summary>
        [Preserve]
        public void JsCallback_OnChunk(string chunk)
        {
#if UNISAVE_BROADCASTING_DEBUG
            UnityEngine.Debug.Log($"[UnisaveBroadcasting] Received chunk:\n" + chunk);
#endif
            
            slicer.ReceiveChunk(chunk);
        }
        
        /// <summary>
        /// Called when an SSE event arrives over the connection
        /// </summary>
        /// <param name="event"></param>
        private void HandleEvent(SseEvent @event)
        {
#if UNISAVE_BROADCASTING_DEBUG
            UnityEngine.Debug.Log(
                $"[UnisaveBroadcasting] Emitting event: {@event.@event} {@event.jsonData}"
            );
#endif
            
            state.ObserveReceivedEvent(@event);
            
            OnEventReceived?.Invoke(@event);
        }
    }
}
#endif