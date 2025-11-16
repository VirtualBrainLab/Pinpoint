using System;
using Models.Scene;
using Models.Settings;

namespace Models
{
    /// <summary>
    /// Represents the application state for save/load operations.
    /// Contains scene, atlas, and rig state slices.
    /// </summary>
    [Serializable]
    public class SavedState
    {
        public SceneState SceneState;
        public RigState RigState;
        public AtlasSettingsState AtlasSettingsState;
    }
}
