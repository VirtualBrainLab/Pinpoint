using System;
using Models.Scene;
using Models.Settings;

namespace Models
{
    /// <summary>
    /// Represents the complete application state for save/load operations.
    /// Contains all persistable state slices.
    /// </summary>
    [Serializable]
    public class SavedState
    {
        public MainState MainState;
        public SceneState SceneState;
        public SettingsState SettingsState;
        public RigState RigState;
        public AtlasSettingsState AtlasSettingsState;
    }
}
