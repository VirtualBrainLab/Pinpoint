using System;
using System.Text;
using UnityEngine.Networking;

namespace Unisave.Broadcasting.Sse
{
    public class SseDownloadHandler : DownloadHandlerScript
    {
        private readonly StringBuilder textStream = new StringBuilder();
        private readonly Action<SseEvent> eventHandler;

        public event Action<string> OnDataReceived;

        public SseDownloadHandler(Action<SseEvent> eventHandler)
        {
            this.eventHandler = eventHandler
                ?? throw new ArgumentNullException(nameof(eventHandler));
        }
        
        protected override bool ReceiveData(byte[] receivedData, int dataLength)
        {
            string stringData = Encoding.UTF8.GetString(
                receivedData,
                0,
                dataLength
            );
            
            textStream.Append(stringData);
            
            ExtractEvents();

            OnDataReceived?.Invoke(stringData);

            // continue receiving data
            return true;
        }

        private void ExtractEvents()
        {
            while (true)
            {
                int length = GetEventLength();
                
                if (length == 0)
                    break;

                char[] rawEvent = new char[length];
                textStream.CopyTo(0, rawEvent, 0, length);
                textStream.Remove(0, length);
                
                HandleEvent(new string(rawEvent));
            }
        }

        private int GetEventLength()
        {
            bool wasNewline = false;

            for (int i = 0; i < textStream.Length; i++)
            {
                if (textStream[i] != '\n')
                {
                    wasNewline = false;
                    continue;
                }

                if (wasNewline)
                {
                    return i + 1;
                }

                wasNewline = true;
            }

            return 0;
        }

        private void HandleEvent(string raw)
        {
            var parsed = SseEvent.Parse(raw);

            eventHandler.Invoke(parsed);
        }
    }
}