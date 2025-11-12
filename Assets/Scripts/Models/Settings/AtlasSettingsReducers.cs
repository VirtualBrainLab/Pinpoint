using Models;
using Unity.AppUI.Redux;
using UnityEngine;

namespace Models.Settings
{
    public static class AtlasSettingsReducers
    {
        public static AtlasSettingsState SetAtlasNameReducer(AtlasSettingsState state, IAction<string> action)
        {
            return state with { AtlasName = action.payload };
        }

        public static AtlasSettingsState SetAtlasTransformNameReducer(AtlasSettingsState state, IAction<string> action)
        {
            return state with { AtlasTransformName = action.payload };
        }

        public static AtlasSettingsState SetReferenceCoordReducer(AtlasSettingsState state, IAction<Vector3> action)
        {
            return state with { ReferenceCoord = action.payload };
        }

        public static AtlasSettingsState ToggleShow3DSlicesReducer(AtlasSettingsState state, IAction<bool> action)
        {
            return state with { Show3DSlices = !state.Show3DSlices };
        }
    }

    public static class AtlasSettingsActions
    {
        public static readonly ActionCreator<string> SET_ATLAS_NAME =
 $"{SliceNames.ATLAS_SETTINGS_SLICE}/SetAtlasName";

        public static readonly ActionCreator<string> SET_ATLAS_TRANSFORM_NAME =
            $"{SliceNames.ATLAS_SETTINGS_SLICE}/SetAtlasTransformName";

        public static readonly ActionCreator<Vector3> SET_REFERENCE_COORD =
       $"{SliceNames.ATLAS_SETTINGS_SLICE}/SetReferenceCoord";

        public static readonly ActionCreator<bool> TOGGLE_SHOW_3D_SLICES =
            $"{SliceNames.ATLAS_SETTINGS_SLICE}/ToggleShow3DSlices";
    }
}
