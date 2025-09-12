using System.IO;
using UnityEngine;

namespace Utils
{
    public static class EphysLinkConstants
    {
        public const int EPHYS_LINK_MIN_VERSION_MAJOR = 2;
        public const int EPHYS_LINK_MIN_VERSION_MINOR = 1;
        public const int EPHYS_LINK_MIN_VERSION_PATCH = 3;

        public static readonly string EphysLinkMinVersion =
            $"{EPHYS_LINK_MIN_VERSION_MAJOR}.{EPHYS_LINK_MIN_VERSION_MINOR}.{EPHYS_LINK_MIN_VERSION_PATCH}";

        public static readonly string EphysLinkName = $"EphysLink-v{EphysLinkMinVersion}";

        public static readonly string EphysLinkExePath = Path.Combine(
            Application.streamingAssetsPath,
            Path.Combine(EphysLinkName, $"{EphysLinkName}.exe")
        );
    }
}
