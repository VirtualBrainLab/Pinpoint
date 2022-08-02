using System;
using LightJson;
using Unisave.Utils;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unisave.Facets
{
    /// <summary>
    /// Stores/obtains device id that is used for connecting to the server
    /// </summary>
    public class DeviceIdRepository
    {
        /// <summary>
        /// Where is the device id stored if when it cannot be obtained
        /// from Unity (say on WebGL platform)
        /// </summary>
        private const string PlayerPrefsKey = "Unisave.DeviceId";
        
        private string id;
        private bool loaded;
        
        /// <summary>
        /// Returns the device id
        /// </summary>
        public string GetDeviceId()
        {
            if (!loaded)
                LoadDeviceId();

            return id;
        }

        /// <summary>
        /// Obtains information about the device
        /// </summary>
        public JsonObject GetDeviceInfo()
        {
            string deviceModel = SystemInfo.deviceModel;
            if (deviceModel == SystemInfo.unsupportedIdentifier)
                deviceModel = null;

            string processorType = SystemInfo.processorType;
            if (processorType == SystemInfo.unsupportedIdentifier)
                processorType = null;

            return new JsonObject()
                // general
                .Add(
                    "platform",
                    Enum.GetName(typeof(RuntimePlatform), Application.platform)
                )
                .Add("deviceModel", deviceModel)
                
                // graphics
                .Add("graphicsDeviceName", SystemInfo.graphicsDeviceName)
                .Add("graphicsDeviceID", SystemInfo.graphicsDeviceID)
                .Add("graphicsDeviceVendorID", SystemInfo.graphicsDeviceVendorID)
                .Add("graphicsMemorySize", SystemInfo.graphicsMemorySize)
                .Add(
                    "graphicsDeviceType",
                    Enum.GetName(typeof(GraphicsDeviceType), SystemInfo.graphicsDeviceType)
                )
                
                // CPU & RAM
                .Add("systemMemorySize", SystemInfo.systemMemorySize)
                .Add("processorCount", SystemInfo.processorCount)
                .Add("processorFrequency", SystemInfo.processorFrequency)
                .Add("processorType", processorType);
        }
        
        private void LoadDeviceId()
        {
            id = ObtainDeviceId();

            // cannot be obtained, so try to load it
            if (string.IsNullOrWhiteSpace(id))
            {
                id = PlayerPrefs.GetString(PlayerPrefsKey, null);
                
                // is not stored, so generate random device id
                if (string.IsNullOrWhiteSpace(id))
                {
                    id = Str.Random(32);
                    PlayerPrefs.SetString(PlayerPrefsKey, id);
                    PlayerPrefs.Save();
                }
            }

            // now the id should be available in the private field
            loaded = true;
        }

        /// <summary>
        /// Obtains the device id from Unity
        /// and returns null when it cannot be done
        /// </summary>
        private string ObtainDeviceId()
        {
            var unityGaveUs = SystemInfo.deviceUniqueIdentifier;

            if (unityGaveUs == SystemInfo.unsupportedIdentifier)
                return null;

            return unityGaveUs;
        }
    }
}