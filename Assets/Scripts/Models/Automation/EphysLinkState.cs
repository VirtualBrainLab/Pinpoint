using System;
using System.IO;
using UnityEngine;

namespace Models.Automation
{
    [Serializable]
    public record EphysLinkState
    {
        public PlatformType SelectedPlatformType;

        public int NewScalePathfinderMpmPort = 8080;

        public string CustomServerIpAddress = "localhost";

        public int CustomServerPort = 3000;

        public bool IsConnected;

        // TODO: Consider moving into a separate constants file.
        #region Ephys Link Info

        [NonSerialized]
        private const string EPHYS_LINK_NAME = "EphysLink-v2.1.0b1";

        [NonSerialized]
        private static string EphysLinkExePath =
            Path.Combine(
                Application.streamingAssetsPath,
                Path.Combine(EPHYS_LINK_NAME, $"{EPHYS_LINK_NAME}.exe")
            );
        
        [NonSerialized]
        private static readonly int[] EPHYS_LINK_MIN_VERSION = { 2, 1, 0 };

        [NonSerialized]
        public static readonly string EPHYS_LINK_MIN_VERSION_STRING =
            $"≥ v{string.Join(".", EPHYS_LINK_MIN_VERSION)}";

        #endregion
    }

    public enum PlatformType
    {
        SensapexUmp,
        NewScalePathfinderMpm,
        Custom,
    }
}
