using System;
using UnityEngine;
using Utils.Types;

namespace Models.Settings
{
    [Serializable]
    public record SettingsState
    {
        [NonSerialized]
        public int TabIndex;

        #region Probe Settings

        public bool DetectCollisions = true;

        public bool ConvertAPML2Probe;

        public string AngleConvention = "Pinpoint";

        public bool AxisControl = true;

        public int ProbeSpeed = 1;

        public bool ProbePrevNextEnabled;

        #endregion

        #region Area Settings

        public bool UseAcronyms = true;

        public bool UseBeryl = true;

        #endregion

        #region Graphics Settings

        public bool ShowSurfaceCoordinate = true;

        public bool ShowBregmaAxis = true;

        public bool ShowInPlaneSlice = true;

        public bool GhostInactiveProbes = true;

        public bool GhostInactiveAreas;

        public bool DisplayUM = true;

        public bool ShowAllProbePanels = true;

        public float ProbePanelHeight = 1600f;

        #endregion

        #region Atlas Settings

        public string AtlasName = "allen_mouse_25um";

        public int Slice3DDropdownOption;

        public Vector3 ReferenceCoord = new(float.NaN, float.NaN, float.NaN);

        public string AtlasTransformName = "Default";

        public float BregmaLambdaRatio = 1f;

        #endregion

        #region Ephys Link

        public EphysLinkPlatformType SelectedEphysLinkPlatformType;

        public int NewScalePathfinderMpmPort = 8080;

        public string CustomServerIpAddress = "localhost";

        public int CustomServerPort = 3000;

        [NonSerialized]
        public EphysLinkConnectionState EphysLinkConnectionState;

        #endregion

        #region Account Settings

        public bool StayLoggedIn = true;

        #endregion

        #region API Settings

        public bool OpenEphysToggle;

        public string OpenEphysTarget = "http://localhost:37497";

        public bool SpikeGLXToggle;

        public string SpikeGLXTarget = "127.0.0.1:4142";

        public string SpikeGLXHelloPath = "C:\\HelloSGLX-win\\HelloSGLX.exe";

        public float APIUpdateRate = 10f;

        #endregion

        #region Camera Settings

        public float CameraZoom = 5f;

        public Vector3 CameraRotation = Vector3.zero;

        #endregion
    }
}
