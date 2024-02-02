using System;
using LightJson;
using UnityEngine;

namespace Unisave.Logging
{
    public static class LogPrinter
    {
        public static void PrintLogsFromFacetCall(JsonValue rawLogs)
        {
            if (!rawLogs.IsJsonArray)
                return;

            foreach (var item in rawLogs.AsJsonArray)
            {
                if (!item.IsJsonObject)
                    continue;
                
                JsonObject record = item.AsJsonObject;

                // parse time
                DateTime? time = item["time"].AsDateTime;
                time = time?.ToLocalTime();
                string timeString = time?.ToLongTimeString() ?? "just now";
                
                // parse remaining values
                string level = item["level"].AsString;
                string message = item["message"].AsString;
                JsonValue context = item["context"];

                string messageToPrint =
                    $"[{timeString}] [SERVER.{level.ToUpper()}] {message}\n\n" +
                    $"Context: {context.ToString(true)}\n\n";

                switch (item["level"].AsString)
                {
                    case "info":
                        Debug.Log(messageToPrint);
                        break;
                    
                    case "warning":
                        Debug.LogWarning(messageToPrint);
                        break;
                    
                    default:
                        Debug.LogError(messageToPrint);
                        break;
                }
            }
        }
    }
}