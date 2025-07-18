using System;
using System.IO;
using UnityEngine;
using Utils.Types;

namespace Models.Automation
{
    [Serializable]
    public record EphysLinkState
    {
        public ServerConnectionType SelectedServerConnectionType;

        public int NewScalePathfinderMpmPort = 8080;

        public string CustomServerIpAddress = "localhost";

        public int CustomServerPort = 3000;

        [NonSerialized]
        public EphysLinkConnectionState EphysLinkConnectionState;

        // TODO: Consider moving into a separate constants file.
        #region Ephys Link Info

        [NonSerialized]
        public const string EPHYS_LINK_NAME = "EphysLink-v2.1.0";

        [NonSerialized]
        public readonly int[] EphysLinkMinVersion = { 2, 1, 0 };

        [NonSerialized]
        public readonly string EphysLinkExePath = Path.Combine(
            Application.streamingAssetsPath,
            Path.Combine(EPHYS_LINK_NAME, $"{EPHYS_LINK_NAME}.exe")
        );

        public string EphysLinkMinVersionString => $"≥ v{string.Join(".", EphysLinkMinVersion)}";

        #endregion
    }
}
