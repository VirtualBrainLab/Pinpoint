using System;
using System.Text;
using LightJson;
using LightJson.Serialization;

namespace Unisave.Broadcasting.Sse
{
    /// <summary>
    /// Represents a single event received via the SSE protocol
    /// https://javascript.info/server-sent-events
    /// </summary>
    public struct SseEvent
    {
        /// <summary>
        /// Event ID that represents the "null" value
        /// </summary>
        public const int NullEventId = -1;
        
        /// <summary>
        /// Optional event name
        /// </summary>
        public string @event;
        
        /// <summary>
        /// Event string data
        /// </summary>
        public string data;

        /// <summary>
        /// Event data parsed as JSON object
        /// </summary>
        public JsonObject jsonData;
        
        /// <summary>
        /// Event id
        /// </summary>
        public int id;
        
        /// <summary>
        /// Recommended retry delay in milliseconds
        /// (retry, when the connection unexpectedly breaks)
        /// </summary>
        public int? retry;

        /// <summary>
        /// Parse the event from the text received over HTTP
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static SseEvent Parse(string text)
        {
            string[] lines = text.Split(
                new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.RemoveEmptyEntries
            );
            
            var message = new SseEvent {
                data = null,
                @event = null,
                id = NullEventId,
                retry = null
            };
            
            StringBuilder data = new StringBuilder();

            foreach (var line in lines)
            {
                if (line.StartsWith("id: "))
                    message.id = int.Parse(line.Substring(4));
                else if (line.StartsWith("event: "))
                    message.@event = line.Substring(7);
                else if (line.StartsWith("retry: "))
                    message.retry = int.Parse(line.Substring(7));
                else if (line.StartsWith("data: "))
                    data.Append(line.Substring(6));
            }

            message.data = data.ToString();

            if (!string.IsNullOrWhiteSpace(message.data))
            {
                message.jsonData = JsonReader.Parse(message.data).AsJsonObject;
            }

            return message;
        }
    }
}