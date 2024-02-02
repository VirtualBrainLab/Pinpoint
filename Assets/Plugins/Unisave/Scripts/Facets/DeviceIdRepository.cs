using System;
using System.Linq;
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
        private const string DeviceIdPlayerPrefsKey = "Unisave.DeviceId";

        public DeviceIdRepository()
        {
            LoadDeviceId();

            LoadDeviceInfo();
        }
        
        #region "Device ID"
        
        private string deviceId;
        
        /// <summary>
        /// Returns the device id
        /// </summary>
        public string GetDeviceId()
        {
            return deviceId;
        }
        
        private void LoadDeviceId()
        {
            deviceId = ObtainDeviceId();

            // cannot be obtained, so try to load it
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                deviceId = PlayerPrefs.GetString(DeviceIdPlayerPrefsKey, null);
                
                // is not stored, so generate random device id
                if (string.IsNullOrWhiteSpace(deviceId))
                {
                    deviceId = Str.Random(32);
                    PlayerPrefs.SetString(DeviceIdPlayerPrefsKey, deviceId);
                    PlayerPrefs.Save();
                }
            }
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

            if (unityGaveUs == null)
                return null;

            // remove non-ascii non-text characters
            // < 32 are control chars
            // > 126 are non-ascii chars (extended latin, currencies, etc.)
            return string.Concat(
                unityGaveUs.Where(c => (c >= 32 && c <= 126))
            );
        }
        
        #endregion
        
        #region "Device Info"

        private JsonObject deviceInfo;

        /// <summary>
        /// Obtains information about the device
        /// </summary>
        public JsonObject GetDeviceInfo()
        {
            return deviceInfo;
        }

        public void LoadDeviceInfo()
        {
            string deviceModel = SystemInfo.deviceModel;
            if (deviceModel == SystemInfo.unsupportedIdentifier)
                deviceModel = null;

            string processorType = SystemInfo.processorType;
            if (processorType == SystemInfo.unsupportedIdentifier)
                processorType = null;

            deviceInfo = new JsonObject()
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
        
        #endregion
    }
}