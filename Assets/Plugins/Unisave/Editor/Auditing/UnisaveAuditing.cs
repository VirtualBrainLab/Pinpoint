using System;
using System.Collections.Generic;
using LightJson;
using Unisave.Facets;
using Unisave.Foundation;
using UnityEngine;
using UnityEngine.Networking;

namespace Unisave.Editor.Auditing
{
    /// <summary>
    /// Tracks important user events to increase their account security
    /// </summary>
    public static class UnisaveAuditing
    {
        public static void EmitEvent(
            string eventType,
            string message,
            Dictionary<string, string> user = null,
            JsonObject data = null,
            string source = null
        )
        {
            try
            {
                if (user == null)
                    user = new Dictionary<string, string>();

                if (!user.ContainsKey("deviceId"))
                    user["deviceId"] = new DeviceIdRepository().GetDeviceId();

                if (data == null)
                    data = new JsonObject();

                if (source == null)
                    source = $"asset/{AssetMeta.Version} Unity Asset";
                
                SendRequest(eventType, message, user, data, source);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private static void SendRequest(
            string eventType,
            string message,
            Dictionary<string, string> user,
            JsonObject data,
            string source
        )
        {
            WWWForm form = new WWWForm();
            
            form.AddField("eventType", eventType);
            form.AddField("message", message);
            
            foreach (var k in user.Keys)
                if (!string.IsNullOrEmpty(user[k]))
                    form.AddField("user[" + k + "]", user[k]);
            
            form.AddField("data", data.ToString(pretty: false));
            form.AddField("source", source);
            
            UnityWebRequest request = UnityWebRequest.Post(BuildUrl(), form);

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            
            op.completed += (_) => {
                if (request.result != UnityWebRequest.Result.Success)
                    Debug.LogError(request.error);
                
                request.Dispose();
            };
        }

        private static string BuildUrl()
        {
            var preferences = UnisavePreferences.Resolve();

            var builder = new UriBuilder(preferences.ServerUrl);
            builder.Host = "api." + builder.Host;
            builder.Path = "auditing/events";
            
            return builder.ToString();
        }
    }
}