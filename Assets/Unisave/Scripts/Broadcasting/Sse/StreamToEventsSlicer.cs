using System;
using System.Text;

namespace Unisave.Broadcasting.Sse
{
    /// <summary>
    /// Slices a text stream into SSE events
    /// </summary>
    public class StreamToEventsSlicer
    {
        private readonly StringBuilder buffer = new StringBuilder();

        /// <summary>
        /// Fired when a new event is received
        /// </summary>
        public event Action<SseEvent> OnEventReceived; 

        /// <summary>
        /// Call this whenever new data arrives. It automatically triggers
        /// the event for new sse events.
        /// </summary>
        /// <param name="chunk"></param>
        public void ReceiveChunk(string chunk)
        {
            buffer.Append(chunk);
            
            ExtractEvents();
        }

        /// <summary>
        /// Resets the slicer to the default state
        /// </summary>
        public void ClearBuffer()
        {
            buffer.Clear();
        }
        
        private void ExtractEvents()
        {
            while (true)
            {
                int length = GetEventLength();
                
                if (length == 0)
                    break;

                char[] rawEvent = new char[length];
                buffer.CopyTo(0, rawEvent, 0, length);
                buffer.Remove(0, length);
                
                HandleEvent(new string(rawEvent));
            }
        }

        private int GetEventLength()
        {
            bool wasNewline = false;

            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i] != '\n')
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
            SseEvent parsed = SseEvent.Parse(raw);

            OnEventReceived?.Invoke(parsed);
        }
    }
}