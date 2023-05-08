using System;
using LightJson;
using LightJson.Serialization;
using Unisave.Serialization;
using UnityEngine;

namespace Unisave.Sessions
{
    /// <summary>
    /// Stores session id that is used for connecting to the server
    /// </summary>
    public class ClientSessionIdRepository
    {
        /*
         * PlayerPrefs?
         * ------------
         * 
         * The unity editor needs to preserve the session id in between
         * play mode restarts. This is how it does that.
         * However the build shouldn't do that! It would cause:
         * - no logout on game close
         * - two instances of the same app on one PC sharing one session id
         *     -> problems with broadcasting connection, etc...
         */
        
        /// <summary>
        /// In how many minutes the session id expires
        /// </summary>
        private const double ExpiryMinutes = 30;

        private const string PlayerPrefsKey = "Unisave.SessionId";
        
        private string id;
        private bool loaded;
        
        /// <summary>
        /// Returns the stored session id
        /// </summary>
        public string GetSessionId()
        {
            if (!loaded)
            {
                id = LoadFromPlayerPrefs();
                loaded = true;
            }

            return id;
        }
        
        /// <summary>
        /// Sets the session ID to be remembered
        /// </summary>
        public void StoreSessionId(string sessionId)
        {
            id = sessionId;
            loaded = true;
            
            RememberInPlayerPrefs();
        }

        private void RememberInPlayerPrefs()
        {
            #if UNITY_EDITOR
                string keySuffix = ":" + Application.dataPath; // to work with project cloners    
                
                PlayerPrefs.SetString(PlayerPrefsKey + keySuffix, new JsonObject()
                    .Add("StoredAt", Serializer.ToJson(DateTime.UtcNow))
                    .Add("SessionId", id)
                    .ToString()
                );
                PlayerPrefs.Save();
            #endif
        }

        private string LoadFromPlayerPrefs()
        {
            #if UNITY_EDITOR

                string keySuffix = ":" + Application.dataPath; // to work with project cloners
            
                var raw = PlayerPrefs.GetString(PlayerPrefsKey + keySuffix);
                JsonObject json = null;

                if (string.IsNullOrWhiteSpace(raw))
                    raw = "{}";

                try
                {
                    json = JsonReader.Parse(raw);
                }
                catch (JsonParseException e)
                {
                    Debug.LogException(e);
                }
                
                if (json == null)
                    json = new JsonObject();
                
                // check expiry
                var storedAt = Serializer.FromJson<DateTime>(json["StoredAt"]);
                if ((DateTime.UtcNow - storedAt).TotalMinutes > ExpiryMinutes)
                    return null;

                return json["SessionId"].AsString;
            
            #else

                return null;
            
            #endif
        }
    }
}