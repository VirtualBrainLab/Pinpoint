using System;
using System.Text;
using UnityEngine.Networking;
using UnityEngine.Scripting;

namespace Unisave.Broadcasting.Sse
{
    public class SseDownloadHandler : DownloadHandlerScript
    {
        private readonly StreamToEventsSlicer slicer = new StreamToEventsSlicer();

        public SseDownloadHandler(Action<SseEvent> eventHandler)
        {
            if (eventHandler == null)
                throw new ArgumentNullException(nameof(eventHandler));
            
            slicer.OnEventReceived += eventHandler.Invoke;
        }
        
        [RequiredMember] // do not IL2CPP strip this away if the class is used
        protected override bool ReceiveData(byte[] receivedData, int dataLength)
        {
            string stringData = Encoding.UTF8.GetString(
                receivedData,
                0,
                dataLength
            );
            
#if UNISAVE_BROADCASTING_DEBUG
            UnityEngine.Debug.Log(
                "[UnisaveBroadcasting] Received data:\n" + stringData
            );
#endif
            
            slicer.ReceiveChunk(stringData);

            // continue receiving data
            return true;
        }
    }
}