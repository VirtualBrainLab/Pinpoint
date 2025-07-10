using System;

namespace Models.Automation
{
    [Serializable]
    public record EphysLinkState
    {
        public PlatformType SelectedPlatformType;

        public int NewScalePathfinderMpmPort = 8080;

        public string CustomServerAddress = "localhost";

        public int CustomServerPort = 3000;

        public bool IsConnected;
    }

    public enum PlatformType
    {
        SensapexUmp,
        NewScalePathfinderMpm,
        Custom,
    }
}
