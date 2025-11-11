using Models;
using Unity.AppUI.Redux;

namespace Models.Settings
{
    public static class RigReducers
    {
        public static RigState ToggleWellReducer(RigState state, IAction<bool> action)
        {
            return state with { WellVisible = !state.WellVisible };
        }

        public static RigState ToggleRigWidefieldReducer(RigState state, IAction<bool> action)
        {
            return state with { RigWidefieldVisible = !state.RigWidefieldVisible };
        }

        public static RigState ToggleMouseSkullReducer(RigState state, IAction<bool> action)
        {
            return state with { MouseSkullVisible = !state.MouseSkullVisible };
        }

        public static RigState ToggleRatSkullReducer(RigState state, IAction<bool> action)
        {
            return state with { RatSkullVisible = !state.RatSkullVisible };
        }

        public static RigState ToggleIblCenterReducer(RigState state, IAction<bool> action)
        {
            return state with { IblCenterVisible = !state.IblCenterVisible };
        }

        public static RigState ToggleIblFrontReducer(RigState state, IAction<bool> action)
        {
            return state with { IblFrontVisible = !state.IblFrontVisible };
        }

        public static RigState ToggleIblBackReducer(RigState state, IAction<bool> action)
        {
            return state with { IblBackVisible = !state.IblBackVisible };
        }

        public static RigState ToggleUclaReducer(RigState state, IAction<bool> action)
        {
            return state with { UclaVisible = !state.UclaVisible };
        }
    }

    public static class RigActions
    {
        public static readonly ActionCreator<bool> TOGGLE_WELL =
            $"{SliceNames.RIG_SLICE}/ToggleWell";

        public static readonly ActionCreator<bool> TOGGLE_RIG_WIDEFIELD =
            $"{SliceNames.RIG_SLICE}/ToggleRigWidefield";

        public static readonly ActionCreator<bool> TOGGLE_MOUSE_SKULL =
            $"{SliceNames.RIG_SLICE}/ToggleMouseSkull";

        public static readonly ActionCreator<bool> TOGGLE_RAT_SKULL =
            $"{SliceNames.RIG_SLICE}/ToggleRatSkull";

        public static readonly ActionCreator<bool> TOGGLE_IBL_CENTER =
            $"{SliceNames.RIG_SLICE}/ToggleIblCenter";

        public static readonly ActionCreator<bool> TOGGLE_IBL_FRONT =
            $"{SliceNames.RIG_SLICE}/ToggleIblFront";

        public static readonly ActionCreator<bool> TOGGLE_IBL_BACK =
            $"{SliceNames.RIG_SLICE}/ToggleIblBack";

        public static readonly ActionCreator<bool> TOGGLE_UCLA =
            $"{SliceNames.RIG_SLICE}/ToggleUcla";
    }
}
