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
    public partial class SseSocket : MonoBehaviour
    {
        /// <summary>
        /// The event ID that's sent when we aren't reconnecting,
        /// but connecting for the first time.
        /// </summary>
        private const int NullEventId = -1;
        
        /// <summary>
        /// Called when a new event arrives
        /// </summary>
        public event Action<SseEvent> OnEventReceived;
        
        /// <summary>
        /// Reference to the client app to resolve connection information
        /// (keys, tokens and hashes)
        /// </summary>
        private ClientApplication app;

        /// <summary>
        /// Instance of the request that is currently representing
        /// the connection, can be null if no connection exists
        /// </summary>
        public UnityWebRequest RunningRequest { get; private set; }

        /// <summary>
        /// Id of the last received SSE event
        /// (updated with each received event,
        /// used during connection establishment)
        /// </summary>
        public int lastReceivedEventId = NullEventId;
        
        /// <summary>
        /// How long to wait for in between during connection retrying
        /// </summary>
        public int retryMilliseconds = 5_000;

        /// <summary>
        /// Status of the connection to the Unisave broadcasting server
        /// </summary>
        public BroadcastingConnection ConnectionState
        {
            get => connectionState;

            private set
            {
                if (connectionState == value)
                    return;
                
                var before = connectionState;
                
                connectionState = value;
                
                #if UNITY_EDITOR
                AppendToDebugLog($"STATE: {connectionState}\n\n");
                #endif
                
                if (connectionState == BroadcastingConnection.Reconnecting)
                    OnConnectionLost?.Invoke();
                
                if (connectionState == BroadcastingConnection.Connected
                    && before == BroadcastingConnection.Reconnecting)
                    OnConnectionRegained?.Invoke();
            }
        }

        // backing field
        private BroadcastingConnection connectionState
            = BroadcastingConnection.Disconnected;

        /// <summary>
        /// Flag to distinguish intended disconnection from a network crash
        /// </summary>
        private bool intendedDisconnection = false;

        /// <summary>
        /// Event called when the connection unexpectedly breaks
        /// </summary>
        public event Action OnConnectionLost;
        
        /// <summary>
        /// Event called when the connection is established again
        /// </summary>
        public event Action OnConnectionRegained;

        /// <summary>
        /// Call this right after this component is created
        /// </summary>
        public void Initialize(ClientApplication app)
        {
            this.app = app;
            lastReceivedEventId = NullEventId;
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
                #if UNITY_EDITOR
                AppendToDebugLog(
                    $"WAITING ON RETRY: {retryMilliseconds}ms\n\n"
                );
                #endif

                yield return new WaitForSeconds(retryMilliseconds / 1000f);
            }
            
            // reset flags
            intendedDisconnection = false;
            
            // === PREPARE THE REQUEST AND HANDLERS ===

            var downloadHandler = new SseDownloadHandler(HandleEvent);
            
            #if UNITY_EDITOR
            downloadHandler.OnDataReceived += AppendToDebugLog;
            #endif
            
            var url = app.Resolve<ApiUrl>();
            var sessionIdRepo = app.Resolve<ClientSessionIdRepository>();
            
            RunningRequest = new UnityWebRequest(
                url.BroadcastingListen(),
                "POST",
                downloadHandler,
                new UploadHandlerRaw(
                    Encoding.UTF8.GetBytes(
                        new JsonObject {
                            ["gameToken"] = app.Preferences.GameToken,
                            ["editorKey"] = app.Preferences.EditorKey,
                            ["buildGuid"] = Application.buildGUID,
                            ["backendHash"] = app.Preferences.BackendHash,
                            ["sessionId"] = sessionIdRepo.GetSessionId(),
                            ["lastReceivedEventId"] = lastReceivedEventId
                        }.ToString()
                    )
                )
            );
            
            RunningRequest.SetRequestHeader("Content-Type", "application/json");
            RunningRequest.SetRequestHeader("Accept", "text/event-stream");
            
            // === LISTEN ===
            
            #if UNITY_EDITOR
            AppendToDebugLog(
                $"SENDING REQUEST, {nameof(lastReceivedEventId)}: " +
                $"{lastReceivedEventId}\n\n"
            );
            #endif
            
            yield return RunningRequest.SendWebRequest();

            #if UNITY_EDITOR
            AppendToDebugLog("REQUEST ENDED\n\n");
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
                    $"retrying in {retryMilliseconds}ms\n" +
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
            if (@event.id != null)
                lastReceivedEventId = (int) @event.id;

            if (@event.retry != null)
                retryMilliseconds = (int) @event.retry;
            
            if (@event.@event == "welcome")
                WelcomeEventReceived();
            
            if (@event.@event == "end-connection")
                EndConnectionReceived();
            
            OnEventReceived?.Invoke(@event);
        }

        /// <summary>
        /// Called when the initial welcome event is received
        /// (this event is sent by the server as the very first event to be
        /// sent on every new SSE connection)
        /// </summary>
        private void WelcomeEventReceived()
        {
            ConnectionState = BroadcastingConnection.Connected;
        }

        /// <summary>
        /// Called when the end-connection event is received
        /// (this means the last received event id should be
        /// reset since we didn't loose any events'
        /// </summary>
        private void EndConnectionReceived()
        {
            lastReceivedEventId = NullEventId;
        }
    }
}