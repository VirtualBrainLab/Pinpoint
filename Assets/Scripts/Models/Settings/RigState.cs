using System;

namespace Models.Settings
{
    [Serializable]
    public record RigState
    {
        public bool WellVisible = false;
        public bool RigWidefieldVisible = false;
        public bool MouseSkullVisible = false;
        public bool RatSkullVisible = false;
        public bool IblCenterVisible = false;
        public bool IblFrontVisible = false;
        public bool IblBackVisible = false;
        public bool UclaVisible = false;
    }
}
