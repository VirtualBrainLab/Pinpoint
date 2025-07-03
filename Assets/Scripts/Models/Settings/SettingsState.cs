using System;
using UnityEngine;

namespace Models.Settings
{
    [Serializable]
    public record SettingsState
    {
        #region Probe Settings

        public bool DetectCollisions = true;

        public bool ConvertAPML2Probe = false;

        public string AngleConvention = "Pinpoint";

        public bool AxisControl = true;

        public int ProbeSpeed = 1;

        public bool ProbePrevNextEnabled = false;

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

        public bool GhostInactiveAreas = false;

        public bool DisplayUM = true;

        public bool ShowAllProbePanels = true;

        public float ProbePanelHeight = 1440f;

        #endregion

        #region Atlas Settings

        public string AtlasName = "allen_mouse_25um";

        public int Slice3DDropdownOption = 0;

        public Vector3 ReferenceCoord = new Vector3(float.NaN, float.NaN, float.NaN);

        public string AtlasTransformName = "Default";

        public float BregmaLambdaRatio = 1f;

        #endregion

        #region Ephys Link Settings

        public int EphysLinkManipulatorType = 0;

        public int EphysLinkPathfinderPort = 8080;

        public string EphysLinkServerIp = "";

        public int EphysLinkServerPort = 8081;

        public string EphysLinkProxyAddress = "";

        public string EphysLinkRightHandedManipulators = "";

        #endregion

        #region Account Settings

        public bool StayLoggedIn = true;

        #endregion

        #region API Settings

        public bool OpenEphysToggle = false;

        public string OpenEphysTarget = "http://localhost:37497";

        public bool SpikeGLXToggle = false;

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
