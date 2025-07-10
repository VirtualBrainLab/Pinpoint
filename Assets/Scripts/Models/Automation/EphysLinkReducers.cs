using System.ComponentModel;
using Unity.AppUI.Redux;

namespace Models.Automation
{
    public static class EphysLinkReducers
    {
        public static EphysLinkState SetSelectedPlatformTypeReducer(
            EphysLinkState state,
            IAction<PlatformType> action
        )
        {
            return state with { SelectedPlatformType = action.payload };
        }
    }

    public static class EphysLinkActions
    {
        public static readonly ActionCreator<PlatformType> SET_SELECTED_PLATFORM_TYPE =
            $"{SliceNames.EPHYS_LINK_SLICE}/SetSelectedPlatformType";
    }
}
