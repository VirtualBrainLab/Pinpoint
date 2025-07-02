using System;
using UnityEngine;

namespace Models.Settings
{
    [Serializable]
    public record SettingsState
    {
        #region Probe Settings
        
        [SerializeField]
        public bool DetectCollisions = true;

        [SerializeField]
        public bool ConvertAPML2Probe = false;

        [SerializeField]
        public string AngleConvention = "Pinpoint";

        [SerializeField]
        public bool AxisControl = true;

        [SerializeField]
        public int ProbeSpeed = 1;

        [SerializeField]
        public bool ProbePrevNextEnabled = false;

        #endregion

        #region Area Settings

        [SerializeField]
        public bool UseAcronyms = true;

        [SerializeField]
        public bool UseBeryl = true;

        #endregion

        #region Graphics Settings

        [SerializeField]
        public bool ShowSurfaceCoordinate = true;

        [SerializeField]
        public bool ShowBregmaAxis = true;

        [SerializeField]
        public bool ShowInPlaneSlice = true;

        [SerializeField]
        public bool GhostInactiveProbes = true;

        [SerializeField]
        public bool GhostInactiveAreas = false;

        [SerializeField]
        public bool DisplayUM = true;

        [SerializeField]
        public bool ShowAllProbePanels = true;

        [SerializeField]
        public float ProbePanelHeight = 1440f;

        #endregion

        #region Atlas Settings

        [SerializeField]
        public string AtlasName = "allen_mouse_25um";

        [SerializeField]
        public int Slice3DDropdownOption = 0;

        [SerializeField]
        public Vector3 ReferenceCoord = new Vector3(float.NaN, float.NaN, float.NaN);

        [SerializeField]
        public string AtlasTransformName = "Default";

        [SerializeField]
        public float BregmaLambdaRatio = 1f;

        #endregion

        #region Ephys Link Settings

        [SerializeField]
        public int EphysLinkManipulatorType = 0;

        [SerializeField]
        public int EphysLinkPathfinderPort = 8080;

        [SerializeField]
        public string EphysLinkServerIp = "";

        [SerializeField]
        public int EphysLinkServerPort = 8081;

        [SerializeField]
        public string EphysLinkProxyAddress = "";

        [SerializeField]
        public string EphysLinkRightHandedManipulators = "";

        #endregion

        #region Account Settings

        [SerializeField]
        public bool StayLoggedIn = true;

        #endregion

        #region API Settings

        [SerializeField]
        public bool OpenEphysToggle = false;

        [SerializeField]
        public string OpenEphysTarget = "http://localhost:37497";

        [SerializeField]
        public bool SpikeGLXToggle = false;

        [SerializeField]
        public string SpikeGLXTarget = "127.0.0.1:4142";

        [SerializeField]
        public string SpikeGLXHelloPath = "C:\\HelloSGLX-win\\HelloSGLX.exe";

        [SerializeField]
        public float APIUpdateRate = 10f;

        #endregion

        #region Camera Settings

        [SerializeField]
        public float CameraZoom = 5f;

        [SerializeField]
        public Vector3 CameraRotation = Vector3.zero;

        #endregion
    }
}
