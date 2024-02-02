using System;
using System.Collections;
using System.Text;
using LightJson;
using Unisave.Foundation;
using Unisave.Sessions;
using Unisave.Utils;
using UnityEngine;
using UnityEngine.Networking;
using Application = UnityEngine.Application;

namespace Unisave.Broadcasting.Sse
{
    // https://javascript.info/server-sent-events
    
    /// <summary>
    /// Represents an SSE connection to the Unisave broadcasting server
    /// </summary>
    public class DefaultSseSocket : MonoBehaviour, ISseSocket
    {
        // interface events
        public event Action<SseEvent> OnEventReceived;
        public event Action OnConnectionLost;
        public event Action OnConnectionRegained;
        
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
        /// Instance of the request that is currently representing
        /// the connection, can be null if no connection exists
        /// </summary>
        public UnityWebRequest RunningRequest { get; private set; }

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

        public DefaultSseSocket()
        {
            state = new SseStateManager(
                invokeConnectionLost: () => OnConnectionLost?.Invoke(),
                invokeConnectionRegained: () => OnConnectionRegained?.Invoke()
            );
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
                StartCoroutine(TheRequestCoroutine());
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
                RunningRequest?.Abort();
                RunningRequest?.Dispose();
                RunningRequest = null;
                ConnectionState = BroadcastingConnection.Disconnected;
            }
        }
        
        /// <summary>
        /// The coroutine that sends the request
        /// representing a single SSE connection
        /// </summary>
        private IEnumerator TheRequestCoroutine()
        {
            if (RunningRequest != null)
                throw new InvalidOperationException(
                    "A request is already running."
                );
            
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

            var downloadHandler = new SseDownloadHandler(HandleEvent);
            
            var url = app.Services.Resolve<ApiUrl>();
            var sessionIdRepo = app.Services.Resolve<ClientSessionIdRepository>();
            
            RunningRequest = new UnityWebRequest(
                url.BroadcastingListen(),
                "POST",
                downloadHandler,
                new UploadHandlerRaw(
                    Encoding.UTF8.GetBytes(
                        new JsonObject {
                            ["gameToken"] = app.Preferences.GameToken,
                            ["editorKey"] = app.Preferences.EditorKey,
                            ["buildGuid"] = ClientIdentity.BuildGuid,
                            ["backendHash"] = app.Preferences.BackendHash,
                            ["sessionId"] = sessionIdRepo.GetSessionId(),
                            ["lastReceivedEventId"] = state.LastReceivedEventId
                        }.ToString()
                    )
                )
            );
            
            RunningRequest.SetRequestHeader("Content-Type", "application/json");
            RunningRequest.SetRequestHeader("Accept", "text/event-stream");
            
            // === LISTEN ===
            
#if UNISAVE_BROADCASTING_DEBUG
            UnityEngine.Debug.Log(
                $"[UnisaveBroadcasting] Starting poll with last " +
                $"received id = {state.LastReceivedEventId}"
            );
#endif
            
            yield return RunningRequest.SendWebRequest();

#if UNISAVE_BROADCASTING_DEBUG
            UnityEngine.Debug.Log($"[UnisaveBroadcasting] Long poll finished.");
#endif
            
            // === HANDLE BREAKAGE ===
            
            if (intendedDisconnection)
            {
                // get rid of the request object
                RunningRequest?.Dispose();
                RunningRequest = null;
                
                // update state
                ConnectionState = BroadcastingConnection.Disconnected;
            }
            else
            {
                Debug.LogWarning(
                    $"[Unisave] Broadcasting client connection broke, " +
                    $"retrying in {state.RetryMilliseconds}ms\n" +
                    $"The reason is: {RunningRequest.error}"
                );
                
                // get rid of the request object
                RunningRequest.Dispose();
                RunningRequest = null;
                
                // update state
                ConnectionState = BroadcastingConnection.Reconnecting;
                
                // retry
                StartCoroutine(TheRequestCoroutine());
            }
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