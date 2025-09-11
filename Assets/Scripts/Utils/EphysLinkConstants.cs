using System.IO;
using UnityEngine;

namespace Utils
{
    public static class EphysLinkConstants
    {
        public static readonly int[] EphysLinkMinVersion = { 2, 1, 2 };

        public static readonly string EphysLinkName =
            $"EphysLink-v{string.Join(".", EphysLinkMinVersion)}";

        public static readonly string EphysLinkExePath = Path.Combine(
            Application.streamingAssetsPath,
            Path.Combine(EphysLinkName, $"{EphysLinkName}.exe")
        );

        public static readonly string EphysLinkMinVersionString =
            $"≥ v{string.Join(".", EphysLinkMinVersion)}";
    }
}
