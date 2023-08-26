using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoordinateSpaces;
using CoordinateTransforms;
using EphysLink;
using TMPro;
using UITabs;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;
using static UnityEngine.InputSystem.InputAction;


#if UNITY_WEBGL
using System.Collections.Specialized;
using System.Runtime.InteropServices;
#endif

#if UNITY_EDITOR

using UnityEditor.Build;
using UnityEditor.Build.Reporting;
// This code fixes a bug that is also fixed by upgrading to 2021.3.14f1 or newer
// see https://forum.unity.com/threads/workaround-for-building-with-il2cpp-with-visual-studio-2022-17-4.1355570/
// please remove this code when Unity version exceeds this!


public class MsvcStdextWorkaround : IPreprocessBuildWithReport
{
    const string kWorkaroundFlag = "/D_SILENCE_STDEXT_HASH_DEPRECATION_WARNINGS";
     
    public int callbackOrder => 0;
     
    public void OnPreprocessBuild(BuildReport report)
    {
        var clEnv = Environment.GetEnvironmentVariable("_CL_");
     
        if (string.IsNullOrEmpty(clEnv))
        {
            Environment.SetEnvironmentVariable("_CL_", kWorkaroundFlag);
        }
        else if (!clEnv.Contains(kWorkaroundFlag))
        {
            clEnv += " " + kWorkaroundFlag;
            Environment.SetEnvironmentVariable("_CL_", clEnv);
        }
    }
}
     
#endif // UNITY_EDITOR

namespace TrajectoryPlanner
{

    public class TrajectoryPlannerManager : MonoBehaviour
    {

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void Copy2Clipboard(string str);
#endif

        #region Editor only
#if UNITY_EDITOR
        public string ProbeString = "";
        public string SettingsString = "";
#endif
#endregion

        #region Events
        // TODO: Expose events for probes moving, UI updating, etc

        /// <summary>
        /// Fired whenever any probe moves
        /// </summary>
        [SerializeField] private UnityEvent _probesChangedEvent;
        
        /// <summary>
        /// Fired whenever a probe is added or removed
        /// </summary>
        [SerializeField] private UnityEvent _probeAddedOrRemovedEvent;

        /// <summary>
        /// Fire whenever the active probe changes
        /// </summary>
        [SerializeField] private UnityEvent _activeProbeChangedEvent;
        #endregion

        // Managers and accessors
        [SerializeField] private CCFModelControl _modelControl;
        [SerializeField] private VolumeDatasetManager _vdmanager;
        [SerializeField] private Transform _probeParentT;
        [FormerlySerializedAs("util")] [SerializeField] private TP_Utils _util;
        [FormerlySerializedAs("accountsManager")] [SerializeField] private UnisaveAccountsManager _accountsManager;
        [SerializeField] private ProbePanelManager _probePanelManager;

        // Settings
        [FormerlySerializedAs("probePrefabs")] [SerializeField] private List<GameObject> _probePrefabs;
        [FormerlySerializedAs("ccfCollider")] [SerializeField] private Collider _ccfCollider;
        [FormerlySerializedAs("inPlaneSlice")] [SerializeField] private TP_InPlaneSlice _inPlaneSlice;

        [FormerlySerializedAs("probeQuickSettings")] [SerializeField] private TP_ProbeQuickSettings _probeQuickSettings;
        [SerializeField] private RelativeCoordinatePanel _relCoordPanel;

        [FormerlySerializedAs("sliceRenderer")] [SerializeField] private TP_SliceRenderer _sliceRenderer;
        [FormerlySerializedAs("searchControl")] [SerializeField] private TP_Search _searchControl;

        [FormerlySerializedAs("settingsPanel")] [SerializeField] private TP_SettingsMenu _settingsPanel;

        [FormerlySerializedAs("CollisionPanelGO")] [SerializeField] private GameObject _collisionPanelGo;
        [FormerlySerializedAs("collisionMaterial")] [SerializeField] private Material _collisionMaterial;

        [FormerlySerializedAs("ProbePanelParentGO")] [SerializeField] private GameObject _probePanelParentGo;
        [FormerlySerializedAs("CraniotomyToolsGO")] [SerializeField] private GameObject _craniotomyToolsGo;
        [FormerlySerializedAs("brainCamController")] [SerializeField] private BrainCameraController _brainCamController;

        [FormerlySerializedAs("meshCenterText")] [SerializeField] private TextAsset _meshCenterText;
        private Dictionary<int, Vector3> meshCenters;

        [FormerlySerializedAs("CanvasParent")] [SerializeField] private GameObject _canvasParent;

        // UI 
        [FormerlySerializedAs("qDialogue")] [SerializeField] QuestionDialogue _qDialogue;
        [SerializeField] GameObject _logPanelGO;

        // Bregma-labmda distance
        [SerializeField] BregmaLambdaBehavior _blDistance;

        // Debug graphics
        [FormerlySerializedAs("surfaceDebugGO")] [SerializeField] private GameObject _surfaceDebugGo;

        // Craniotomy
        [SerializeField] private CraniotomyPanel _craniotomyPanel;

        // Coordinate system information
        private Dictionary<string, CoordinateSpace> coordinateSpaceOpts;
        private Dictionary<string, CoordinateTransform> coordinateTransformOpts;

        // Local tracking variables
        private List<Collider> rigColliders;
        private bool _movedThisFrame;


        // Track who got clicked on, probe, camera, or brain

        #region InputSystem
        private ProbeMetaControls inputActions;

        #endregion

        public void SetProbeControl(bool state)
        {
            _brainCamController.SetControlBlock(state);
        }

        private bool spawnedThisFrame = false;

        Task annotationDatasetLoadTask;

        #region Unity
        private void Awake()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLInput.captureAllKeyboardInput = true;
#endif

            SetProbeControl(false);

            // Deal with coordinate spaces and transforms
            coordinateSpaceOpts = new Dictionary<string, CoordinateSpace>();
            coordinateSpaceOpts.Add("CCF", new CCFSpace());
            CoordinateSpaceManager.ActiveCoordinateSpace = coordinateSpaceOpts["CCF"];

            coordinateTransformOpts = new Dictionary<string, CoordinateTransform>();
            CoordinateTransform temp = new CCFTransform();
            coordinateTransformOpts.Add(temp.Name, temp);
            temp = new MRILinearTransform();
            coordinateTransformOpts.Add(temp.Name, temp);
            coordinateTransformOpts.Add("MRI", temp);
            temp = new NeedlesTransform();
            coordinateTransformOpts.Add(temp.Name, temp);
            coordinateTransformOpts.Add("Needles", temp);
            temp = new IBLNeedlesTransform();
            coordinateTransformOpts.Add(temp.Name, temp);
            coordinateTransformOpts.Add("IBL-Needles", temp);

            // Initialize variables
            rigColliders = new List<Collider>();
            meshCenters = new Dictionary<int, Vector3>();

            // Load 3D meshes
            LoadMeshData();
            //Physics.autoSyncTransforms = true;

            // Input system
            inputActions = new();
            inputActions.ProbeMetaControl.Enable();
            inputActions.ProbeMetaControl.NextProbe.performed += NextProbe;
            inputActions.ProbeMetaControl.PrevProbe.performed += PrevProbe;
            inputActions.ProbeMetaControl.SwitchAxisMode.performed += x => Settings.ConvertAPML2Probe = !Settings.ConvertAPML2Probe;

            _accountsManager.UpdateCallbackEvent = AccountsProbeStatusUpdatedCallback;
        }

        private async void Start()
        {
            // Startup CCF
            _modelControl.LateStart(true);
            _modelControl.SetBeryl(Settings.UseBeryl);

            // Set callback
            DelayedModelControlStart();

            // Startup the volume textures
            annotationDatasetLoadTask = _vdmanager.LoadAnnotationDataset();
            await annotationDatasetLoadTask;

            // After annotation loads, check if the user wants to load previously used probes
            var savedProbeTask = CheckForSavedProbes(annotationDatasetLoadTask);
            await savedProbeTask;

            // Finally, load accounts if we didn't load a query string
            if (!savedProbeTask.Result)
                _accountsManager.DelayedStart();

            // Link any events that need to be linked
            ProbeManager.ActiveProbeUIUpdateEvent.AddListener(
                () => _probeQuickSettings.GetComponentInChildren<QuickSettingsLockBehavior>().UpdateSprite(ProbeManager.ActiveProbeManager.ProbeController.Locked));
        }

        void Update()
        {
            if (spawnedThisFrame)
            {
                spawnedThisFrame = false;
                return;
            }

            if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C)) && Input.GetKeyDown(KeyCode.Backspace))
            {
                RecoverActiveProbeController();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.H))
                _settingsPanel.ToggleSettingsMenu();

            if (Input.GetKeyDown(KeyCode.L) && !UIManager.InputsFocused)
                _logPanelGO.SetActive(!_logPanelGO.activeSelf);

            if (Input.anyKey && ProbeManager.ActiveProbeManager != null && !UIManager.InputsFocused)
            {
                if (Input.GetKeyDown(KeyCode.Backspace) && !_canvasParent.GetComponentsInChildren<TMP_InputField>()
                        .Any(inputField => inputField.isFocused))
                {
                    DestroyActiveProbeManager();
                    return;
                }
            }
        }

        private void LateUpdate()
        {
            if (_movedThisFrame && ProbeManager.ActiveProbeManager != null)
            {
                _movedThisFrame = false;

                ColliderManager.CheckForCollisions();

                _inPlaneSlice.UpdateInPlaneSlice();

                if (Settings.ShowSurfaceCoordinate)
                {
                    bool inBrain = ProbeManager.ActiveProbeManager.IsProbeInBrain();
                    SetSurfaceDebugActive(inBrain);
                    if (inBrain)
                        SetSurfaceDebugPosition(ProbeManager.ActiveProbeManager.GetSurfaceCoordinateWorldT());
                }

                if (!_probeQuickSettings.IsFocused())
                    UpdateQuickSettings();

                _sliceRenderer.UpdateSlicePosition();

                _accountsManager.UpdateProbeData();

                _probesChangedEvent.Invoke();
            }
        }

        public void SetMovedThisFrame()
        {
            _movedThisFrame = true;
        }

#endregion

        public Task GetAnnotationDatasetLoadedTask()
        {
            return annotationDatasetLoadTask;
        }

        private async void DelayedModelControlStart()
        {
            await _modelControl.GetDefaultLoadedTask();

            foreach (CCFTreeNode node in _modelControl.GetDefaultLoadedNodes())
            {
                await node.GetLoadedTask(true);
                node.SetNodeModelVisibility(true, false, false);
            }

            // Set the warp setting
            InVivoTransformChanged(Settings.InvivoTransform);
        }

        public void ClickSearchArea(GameObject target)
        {
            _searchControl.ClickArea(target);
        }

        public void TargetSearchArea(int id)
        {
            _searchControl.ClickArea(id);
        }

        // DESTROY AND REPLACE PROBES

        //[TODO] Replace this with some system that handles recovering probes by tracking their coordinate system or something?
        // Or maybe the probe coordinates should be an object that can be serialized?
        private bool _restoredProbe = true; // Can't restore anything at start
        private string _prevProbeData;

        public void DestroyProbe(ProbeManager probeManager)
        {
            var isActiveProbe = ProbeManager.ActiveProbeManager == probeManager;
            
            _prevProbeData = JsonUtility.ToJson(ProbeData.ProbeManager2ProbeData(probeManager));

            // Cannot restore a ghost probe, so we set restored to true
            _restoredProbe = false;

            // Destroy probe
            probeManager.Destroy();
            Destroy(probeManager.gameObject);

            // Cleanup UI if this was last probe in scene
            var realProbes = ProbeManager.Instances.Where(x => x.ProbeType != ProbeProperties.ProbeType.Placeholder && x != probeManager);

            if (realProbes.Count() > 0)
            {
                if (isActiveProbe)
                {
                    SetActiveProbe(realProbes.Last());
                }
            }
            else
            {
                // Invalidate ProbeManager.ActiveProbeManager
                if (probeManager == ProbeManager.ActiveProbeManager)
                {
                    ProbeManager.ActiveProbeManager = null;
                    _activeProbeChangedEvent.Invoke();
                }
                SetSurfaceDebugActive(false);
                UpdateQuickSettings();
                UpdateQuickSettingsProbeIdText();
            }
            
            _probeAddedOrRemovedEvent.Invoke();

            _movedThisFrame = true;
        }

        private void DestroyActiveProbeManager()
        {
            // Remove the probe's insertion from the list of insertions (does nothing if not found)
            // ProbeManager.ActiveProbeManager.ProbeController.Insertion.Targetable = false;

            // Remove Probe
            DestroyProbe(ProbeManager.ActiveProbeManager);
        }

        private void RecoverActiveProbeController()
        {
            if (_restoredProbe) return;

            ProbeData probeData = JsonUtility.FromJson<ProbeData>(_prevProbeData);

            // Don't duplicate probes by accident
            if (!ProbeManager.Instances.Any(x => x.UUID.Equals(probeData.UUID)))
            {
                var probeInsertion = new ProbeInsertion(probeData.APMLDV, probeData.Angles,
                    coordinateSpaceOpts[probeData.CoordSpaceName], coordinateTransformOpts[probeData.CoordTransformName]);

                ProbeManager newProbeManager = AddNewProbe((ProbeProperties.ProbeType)probeData.Type, probeInsertion,
                    probeData.ManipulatorType, probeData.ManipulatorID, probeData.ZeroCoordOffset, probeData.BrainSurfaceOffset,
                    probeData.Drop2SurfaceWithDepth, probeData.IsRightHanded, probeData.UUID);

                newProbeManager.UpdateSelectionLayer(probeData.SelectionLayerName);
                newProbeManager.OverrideName(probeData.Name);
                newProbeManager.Color = probeData.Color;
                newProbeManager.APITarget = probeData.APITarget;
            }

            _restoredProbe = true;
        }

#region Add Probe Functions

        /// <summary>
        /// Used in the editor when the add probe buttons are clicked in the scene
        /// </summary>
        /// <param name="probeType">Probe type parameter (e.g. 0/21/24 for neuropixels)</param>
        public void AddNewProbeVoid(int probeType)
        {
            AddNewProbe((ProbeProperties.ProbeType) probeType);
        }

        /// <summary>
        /// Main function for adding new probes (other functions are just overloads)
        /// 
        /// Creates the new probe and then sets it to be active
        /// </summary>
        /// <param name="probeType"></param>
        /// <returns></returns>
        public ProbeManager AddNewProbe(ProbeProperties.ProbeType probeType, string UUID = null)
        {
            _probePanelManager.CountProbePanels();

            GameObject newProbe = Instantiate(_probePrefabs.Find(x => x.GetComponent<ProbeManager>().ProbeType == probeType), _probeParentT);
            var newProbeManager = newProbe.GetComponent<ProbeManager>();

            if (UUID != null)
                newProbeManager.OverrideUUID(UUID);

            SetActiveProbe(newProbeManager);

            spawnedThisFrame = true;

            UpdateQuickSettingsProbeIdText();

            newProbeManager.UIUpdateEvent.AddListener(() => UpdateQuickSettings(newProbeManager));
            newProbeManager.ProbeController.MovedThisFrameEvent.AddListener(SetMovedThisFrame);

            // Add listener for SetActiveProbe
            newProbeManager.ActivateProbeEvent.AddListener(delegate { SetActiveProbe(newProbeManager); });

            // Invoke the movement event
            _probeAddedOrRemovedEvent.Invoke();

            return newProbe.GetComponent<ProbeManager>();
        }

        public ProbeManager AddNewProbe(ProbeProperties.ProbeType probeType, ProbeInsertion insertion, string UUID = null)
        {
            ProbeManager probeManager = AddNewProbe(probeType, UUID);

            probeManager.ProbeController.SetProbePosition(insertion.apmldv);
            probeManager.ProbeController.SetProbeAngles(insertion.angles);
            probeManager.ProbeController.SetSpaceTransform(insertion.CoordinateSpace, insertion.CoordinateTransform);

            return probeManager;
        }
        
        public ProbeManager AddNewProbe(ProbeProperties.ProbeType probeType, ProbeInsertion insertion,
            string manipulatorType, string manipulatorId, Vector4 zeroCoordinateOffset, float brainSurfaceOffset, bool dropToSurfaceWithDepth, bool isRightHanded, string UUID = null)
        {
            var probeManager = AddNewProbe(probeType, UUID);

            probeManager.ProbeController.SetProbePosition(insertion.apmldv);
            probeManager.ProbeController.SetProbeAngles(insertion.angles);
            probeManager.ProbeController.SetSpaceTransform(insertion.CoordinateSpace, insertion.CoordinateTransform);

            // Return data if there is no current Ephys Link data
            if (Settings.IsEphysLinkDataExpired()) return probeManager;
            
            // Repopulate Ephys Link information
            probeManager.ManipulatorBehaviorController.ManipulatorType = manipulatorType;
            probeManager.ManipulatorBehaviorController.ZeroCoordinateOffset = zeroCoordinateOffset;
            probeManager.ManipulatorBehaviorController.BrainSurfaceOffset = brainSurfaceOffset;
            probeManager.ManipulatorBehaviorController.IsSetToDropToSurfaceWithDepth = dropToSurfaceWithDepth;
            probeManager.ManipulatorBehaviorController.IsSetToDropToSurfaceWithDepth = dropToSurfaceWithDepth;
            probeManager.ManipulatorBehaviorController.IsRightHanded = isRightHanded;
            var communicationManager = GameObject.Find("EphysLink").GetComponent<CommunicationManager>();
                
            if (communicationManager.IsConnected && !string.IsNullOrEmpty(manipulatorId))
                probeManager.SetIsEphysLinkControlled(true, manipulatorId,
                    onError: _ => probeManager.SetIsEphysLinkControlled(false));

            return probeManager;
        }

        public void CopyActiveProbe()
        {
            AddNewProbe(ProbeManager.ActiveProbeManager.ProbeType, ProbeManager.ActiveProbeManager.ProbeController.Insertion);
        }

#endregion

        #region Active probe controls
        public void SetActiveProbe(string UUID)
        {
            // Search for the probemanager corresponding to this UUID
            foreach (ProbeManager probeManager in ProbeManager.Instances)
                if (probeManager.UUID.Equals(UUID))
                {
                    SetActiveProbe(probeManager);
                    return;
                }
            // if we get here we didn't find an active probe
            Debug.LogWarning($"Probe {UUID} doesn't exist in the scene");
        }

        public void SetActiveProbe(ProbeManager newActiveProbeManager)
        {
            if (ProbeManager.ActiveProbeManager == newActiveProbeManager)
                return;

#if UNITY_EDITOR
            Debug.Log("Setting active probe to: " + newActiveProbeManager.gameObject.name);
#endif

            // Tell the old probe that it is now in-active
            if (ProbeManager.ActiveProbeManager != null)
            {
                ProbeManager.ActiveProbeManager.SetActive(false);
            }

            // Replace the probe object and set to active
            ProbeManager.ActiveProbeManager = newActiveProbeManager;
            ProbeManager.ActiveProbeManager.SetActive(true);
            
            // Change the UI manager visibility and set transparency of probes
            foreach (ProbeManager probeManager in ProbeManager.Instances)
            {
                // Check visibility
                var isActiveProbe = probeManager == ProbeManager.ActiveProbeManager;
                probeManager.SetUIVisibility(Settings.ShowAllProbePanels || isActiveProbe);

                // Set active state for UI managers
                foreach (ProbeUIManager puimanager in probeManager.GetProbeUIManagers())
                    puimanager.ProbeSelected(isActiveProbe);

                // Update transparency for probe (if not ghost)
                if (!isActiveProbe && Settings.GhostInactiveProbes)
                    probeManager.SetMaterialsTransparent();
                else
                    probeManager.SetMaterialsDefault();
            }

            // Change the height of the probe panels, if needed
            _probePanelManager.RecalculateProbePanels();
            
            _activeProbeChangedEvent.Invoke();
        }

        public void NextProbe(CallbackContext context)
        {
            if (ProbeManager.Instances.Count == 0 || UIManager.InputsFocused) return;

            int idx = ProbeManager.Instances.FindIndex(x => x.Equals(ProbeManager.ActiveProbeManager));

            // if this is the last probe, wrap around
            idx = (idx + 1) % ProbeManager.Instances.Count;

            SetActiveProbe(ProbeManager.Instances[idx]);
        }

        public void PrevProbe(CallbackContext context)
        {
            if (ProbeManager.Instances.Count == 0 || UIManager.InputsFocused) return;

            int idx = ProbeManager.Instances.FindIndex(x => x.Equals(ProbeManager.ActiveProbeManager));

            // if this is the last probe, wrap around
            idx = (idx - 1) % ProbeManager.Instances.Count;
            if (idx < 0) idx += ProbeManager.Instances.Count;

            SetActiveProbe(ProbeManager.Instances[idx]);
        }

        public void ActiveProbe_ToggleLock()
        {
            ProbeManager.ActiveProbeManager.ProbeController.ToggleControllerLock();
        }

        #endregion

        public void OverrideInsertionName(string UUID, string newName)
        {
            foreach (ProbeManager probeManager in ProbeManager.Instances)
                if (probeManager.UUID.Equals(UUID))
                    probeManager.OverrideName(newName);
        }

        public void UpdateQuickSettings(ProbeManager sourceProbeManager=null)
        {
            // Ignore this call to update quick settings if the source probe manager is not the active probe manager
            if (sourceProbeManager && sourceProbeManager != ProbeManager.ActiveProbeManager)
                return;

            _probeQuickSettings.UpdateCoordinates();
        }

        public void UpdateQuickSettingsProbeIdText()
        {
            _probeQuickSettings.UpdateQuickUI();
        }

        public void ResetActiveProbe()
        {
            if (ProbeManager.ActiveProbeManager != null)
                ProbeManager.ActiveProbeManager.ProbeController.ResetInsertion();
        }

#region Warping

        public void WarpBrain()
        {
            foreach (CCFTreeNode node in _modelControl.GetDefaultLoadedNodes())
                WarpNode(node);
            _searchControl.ChangeWarp();
        }

        public void WarpNode(CCFTreeNode node)
        {
#if UNITY_EDITOR
            Debug.Log(string.Format("Transforming node {0}", node.Name));
#endif
            node.TransformVertices(CoordinateSpaceManager.WorldU2WorldT, true);
        }

        public void UnwarpBrain()
        {
            foreach (CCFTreeNode node in _modelControl.GetDefaultLoadedNodes())
            {
                node.ClearTransform(true);
            }
        }



#endregion

#region Colliders

        public void UpdateRigColliders(IEnumerable<Collider> newRigColliders, bool keep)
        {
            if (keep)
                ColliderManager.AddRigColliderInstances(newRigColliders);
            else
                foreach (Collider collider in newRigColliders)
                    rigColliders.Remove(collider);
        }

#endregion

        public void UpdateAllProbeUI()
        {
            foreach (ProbeManager probeManager in ProbeManager.Instances)
                probeManager.UIUpdateEvent.Invoke();
        }

        public void UpdateAllProbePositions()
        {
            foreach (ProbeManager probeManager in ProbeManager.Instances)
                probeManager.ProbeController.SetProbePosition();
        }

        ///
        /// SETTINGS
        /// 


#region Settings

        public void SetGhostAreaVisibility()
        {
            if (Settings.GhostInactiveAreas)
            {
                List<CCFTreeNode> activeAreas = _searchControl.activeBrainAreas;
                foreach (CCFTreeNode node in _modelControl.GetDefaultLoadedNodes())
                    if (!activeAreas.Contains(node))
                        node.SetNodeModelVisibility();
            }
            else
            {
                foreach (CCFTreeNode node in _modelControl.GetDefaultLoadedNodes())
                    node.SetNodeModelVisibility(true);
            }
        }

        public void SetGhostProbeVisibility()
        {
            foreach (ProbeManager probeManager in ProbeManager.Instances)
            {
                if (probeManager == ProbeManager.ActiveProbeManager)
                {
                    probeManager.SetMaterialsDefault();
                    continue;
                }

                if (Settings.GhostInactiveProbes)
                    probeManager.SetMaterialsTransparent();
                else
                    probeManager.SetMaterialsDefault();
            }
        }
        
        public void SetShowAllProbePanels()
        {
            if (Settings.ShowAllProbePanels)
                foreach (ProbeManager probeManager in ProbeManager.Instances)
                    probeManager.SetUIVisibility(true);
            else
                foreach (ProbeManager probeManager in ProbeManager.Instances)
                    probeManager.SetUIVisibility(ProbeManager.ActiveProbeManager == probeManager);

            _probePanelManager.RecalculateProbePanels();
        }

        public void InVivoTransformChanged(int invivoOption)
        {
            Debug.Log("(tpmanager) Attempting to set transform to: " + coordinateTransformOpts.Values.ElementAt(invivoOption).Name);
            if (Settings.BregmaLambdaDistance == 4.15f)
            {
                // if the BL distance is the default, just set the transform
                SetNewTransform(coordinateTransformOpts.Values.ElementAt(invivoOption));
            }
            else
            {
                // if isn't the default, then we have to adjust the transform now
                SetNewTransform(coordinateTransformOpts.Values.ElementAt(invivoOption));
                ChangeBLDistance(Settings.BregmaLambdaDistance);
            }
        }

#endregion

#region Setting Helper Functions

        private void SetNewTransform(CoordinateTransform newTransform)
        {
            CoordinateSpaceManager.ActiveCoordinateTransform = newTransform;
            WarpBrain();

            // Update the warp functions in the craniotomy control panel
            _craniotomyPanel.World2Space = CoordinateSpaceManager.World2TransformedAxisChange;
            _craniotomyPanel.Space2World = CoordinateSpaceManager.Transformed2WorldAxisChange;

            // Check if active probe is a mis-match

            // Check all probes for mis-matches
            foreach (ProbeManager probeManager in ProbeManager.Instances)
                probeManager.Update2ActiveTransform();

            UpdateAllProbeUI();
        }

        public void SetSurfaceDebugActive(bool active)
        {
            if (active && Settings.ShowSurfaceCoordinate && ProbeManager.ActiveProbeManager != null)
                _surfaceDebugGo.SetActive(true);
            else
                _surfaceDebugGo.SetActive(false);
        }

#endregion



        public void SetIBLTools(bool state)
        {
            _craniotomyToolsGo.SetActive(state);
        }

        public void SetSurfaceDebugPosition(Vector3 worldPosition)
        {
            _surfaceDebugGo.transform.position = worldPosition;
        }

#region Save and load probes on quit

        private void OnApplicationQuit()
        {
            Settings.SaveCurrentProbeData(GetActiveProbeJSON());
        }

        public void ShareLink()
        {
            // Probe data
            var data = GetActiveProbeJSONFlattened();
            var plainTextBytes = Encoding.UTF8.GetBytes(data);
            string encodedStr = Convert.ToBase64String(plainTextBytes);

            // Settings data
            var settingsData = Settings.Data2String();
            string settingsStr = Convert.ToBase64String(Encoding.UTF8.GetBytes(settingsData));

            string url = $"https://data.virtualbrainlab.org/Pinpoint/?Probes={encodedStr}&Settings={settingsStr}";

#if UNITY_EDITOR
            Debug.Log(url);
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
            Copy2Clipboard(url);
#else
            GUIUtility.systemCopyBuffer = url;
#endif
        }

        private List<string> GetActiveProbeJSON()
        {
            var nonGhostProbeManagers = ProbeManager.Instances;
            List<string> data = new();

            for (int i = 0; i < nonGhostProbeManagers.Count; i++)
            {
                ProbeManager probe = nonGhostProbeManagers[i];

                if (probe.Saved)
                    data.Add(JsonUtility.ToJson(ProbeData.ProbeManager2ProbeData(probe)));
            }

            return data;
        }

        private string GetActiveProbeJSONFlattened()
        {
            var nonGhostProbeManagers = ProbeManager.Instances;
            List<string> data = new();

            for (int i = 0; i < nonGhostProbeManagers.Count; i++)
            {
                ProbeManager probe = nonGhostProbeManagers[i];
                data.Add(JsonUtility.ToJson(ProbeData.ProbeManager2ProbeData(probe)));
            }

            return string.Join(";", data);
        }

        /// <summary>
        /// Check for saved probes in the query string on WebGL or by asking the user if they want to re-load the scene
        /// </summary>
        /// <param name="annotationDatasetLoadTask"></param>
        /// <returns>true if WebGL query string contained data</returns>
        public async Task<bool> CheckForSavedProbes(Task annotationDatasetLoadTask)
        {
            await annotationDatasetLoadTask;

            // On WebGL, check for a query string
#if UNITY_WEBGL
            if (LoadSavedProbesWebGL())
                return true;
#endif

#if UNITY_EDITOR
            // In editor, check for probe string field
            if (!(ProbeString==""))
            {
                LoadSavedProbesFromEncodedString(ProbeString);
                if (!(SettingsString==""))
                    LoadSettingsFromEncodedString(SettingsString);
                return true;
            }
#endif

            if (_qDialogue)
            {
                if (PlayerPrefs.GetInt("probecount", 0) > 0)
                {
                    var questionString = Settings.IsEphysLinkDataExpired()
                        ? "Load previously saved probes?"
                        : "Restore previous session?";

                    QuestionDialogue.SetYesCallback(LoadSavedProbesStandalone);
                    QuestionDialogue.NewQuestion(questionString);
                }
            }

            return false;
        }

#if UNITY_WEBGL
        private bool LoadSavedProbesWebGL()
        {
            bool queryStr = false;

            // get the url
            string appURL = Application.absoluteURL;

            // parse for query strings
            int queryIdx = appURL.IndexOf("?");
            if (queryIdx > 0)
            {
                Debug.Log("Found query string");

                string queryString = appURL.Substring(queryIdx);

                NameValueCollection qscoll = System.Web.HttpUtility.ParseQueryString(queryString);
                foreach (string query in qscoll)
                {
                    if (query.Equals("Probes"))
                    {
                        string encodedStr = qscoll[query];

                        LoadSavedProbesFromEncodedString(encodedStr);
                        queryStr = true;
                    }
                    if (query.Equals("Settings"))
                    {
                        string settingsQuery = qscoll[query];
                        
                        LoadSettingsFromEncodedString(settingsQuery);
                    }
                }
            }

            return queryStr;
        }
#endif

        private void LoadSettingsFromEncodedString(string encodedSettingsStr)
        {
            var bytes = System.Convert.FromBase64String(encodedSettingsStr);
            string settingsStr = System.Text.Encoding.UTF8.GetString(bytes);

            Settings.Load(settingsStr);
        }

        private void LoadSavedProbesFromEncodedString(string encodedProbesStr)
        {
            var bytes = System.Convert.FromBase64String(encodedProbesStr);
            string probeArrayStr = System.Text.Encoding.UTF8.GetString(bytes);

            string[] savedProbesArray = probeArrayStr.Split(';');

            LoadSavedProbesFromStringArray(savedProbesArray);
        }

        private void LoadSavedProbesStandalone()
        {
            var savedProbes = Settings.LoadSavedProbeData();
            LoadSavedProbesFromStringArray(savedProbes);
        }

        private void LoadSavedProbesFromStringArray(string[] savedProbes)
        {
            foreach (string savedProbe in savedProbes)
            {
                Debug.Log(savedProbe);

                ProbeData probeData = JsonUtility.FromJson<ProbeData>(savedProbe);

                // Don't duplicate probes by accident
                if (!ProbeManager.Instances.Any(x => x.UUID.Equals(probeData.UUID)))
                {
                    CoordinateSpace probeOrigSpace = coordinateSpaceOpts[probeData.CoordSpaceName];
                    CoordinateTransform probeOrigTransform = coordinateTransformOpts[probeData.CoordTransformName];

                    ProbeInsertion probeInsertion;
                    if (probeOrigTransform.Name != CoordinateSpaceManager.ActiveCoordinateTransform.Name)
                    {
                        Debug.LogError($"[TODO] Need to warn user when transforming a probe into the active coordinate space!!");
                        Vector3 newAPMLDV = CoordinateSpaceManager.ActiveCoordinateTransform.Space2Transform(probeOrigTransform.Transform2Space(probeData.APMLDV));

                        probeInsertion = new ProbeInsertion(newAPMLDV, probeData.Angles,
                            CoordinateSpaceManager.ActiveCoordinateSpace, CoordinateSpaceManager.ActiveCoordinateTransform);
                    }
                    else
                    {
                        probeInsertion = new ProbeInsertion(probeData.APMLDV, probeData.Angles,
                            CoordinateSpaceManager.ActiveCoordinateSpace, CoordinateSpaceManager.ActiveCoordinateTransform);
                    }


                    ProbeManager newProbeManager = AddNewProbe((ProbeProperties.ProbeType)probeData.Type, probeInsertion,
                        probeData.ManipulatorType, probeData.ManipulatorID, probeData.ZeroCoordOffset, probeData.BrainSurfaceOffset,
                        probeData.Drop2SurfaceWithDepth, probeData.IsRightHanded, probeData.UUID);

                    newProbeManager.UpdateSelectionLayer(probeData.SelectionLayerName);
                    newProbeManager.OverrideName(probeData.Name);
                    newProbeManager.Color = probeData.Color;
                    newProbeManager.APITarget = probeData.APITarget;
                }
            }

            UpdateQuickSettings();
        }

#endregion

#region Mesh centers

        private int prevTipID;
        private bool prevTipSideLeft;

        private void LoadMeshData()
        {
            List<Dictionary<string,object>> data = CSVReader.ParseText(_meshCenterText.text);

            for (int i = 0; i < data.Count; i++)
            {
                Dictionary<string, object> row = data[i];

                int ID = (int)row["id"];
                float ap = (float)row["ap"];
                float ml = (float)row["ml"];
                float dv = (float)row["dv"];

                meshCenters.Add(ID, new Vector3(ap, ml, dv));
            }
        }

        public void SetProbeTipPositionToCCFNode(CCFTreeNode targetNode)
        {
            if (ProbeManager.ActiveProbeManager == null) return;
            int berylID = CCFModelControl.GetBerylID(targetNode.ID);
            Vector3 apmldv = meshCenters[berylID];

            if (berylID==prevTipID && prevTipSideLeft)
            {
                // we already hit this area, switch sides
                apmldv.y = 11.4f - apmldv.y;
                prevTipSideLeft = false;
            }
            else
            {
                // first time, go left
                prevTipSideLeft = true;
            }

            apmldv = ProbeManager.ActiveProbeManager.ProbeController.Insertion.CoordinateTransform.Space2Transform(apmldv - CoordinateSpaceManager.ActiveCoordinateSpace.RelativeOffset);
            ProbeManager.ActiveProbeManager.ProbeController.SetProbePosition(apmldv);

            prevTipID = berylID;
        }

#endregion

#region Text

        public void CopyText()
        {
            ProbeManager.ActiveProbeManager.Probe2Text();
        }

#endregion

#region Accounts

        public (Vector3 apmldv, Vector3 angles, CoordinateSpace space, CoordinateTransform transform, bool targetable) ServerProbeInsertion2ProbeInsertion(ServerProbeInsertion serverInsertion)
        {
            return (new Vector3(serverInsertion.ap, serverInsertion.ml, serverInsertion.dv),
                new Vector3(serverInsertion.phi, serverInsertion.theta, serverInsertion.spin),
                coordinateSpaceOpts[serverInsertion.coordinateSpaceName],
                coordinateTransformOpts[serverInsertion.coordinateTransformName],
                true);
        }

        /// <summary>
        /// Called by the AccountsManager class when a probe's visibility is updated
        /// 
        /// TPManager then requests a list of all active probes and updates the scene appropriately
        /// </summary>
        private void AccountsProbeStatusUpdatedCallback((Vector3 apmldv, Vector3 angles, int type, string spaceName, string transformName, string UUID, string overrideName, Color color) data,
            bool visible)
        {
            Debug.Log($"Change in visibility for {data.UUID} to {visible}");
            if (!visible)
            {
                // destroy the probe
                ProbeManager probeManager = ProbeManager.Instances.Find(x => x.UUID.Equals(data.UUID));
                if (probeManager != null)
                    DestroyProbe(probeManager);
            }
            else
            {
                AccountsNewProbeHelper(data);
            }

        }

        private void AccountsNewProbeHelper((Vector3 apmldv, Vector3 angles, int type, string spaceName, string transformName, string UUID, string overrideName, Color color) data)
        {
            ProbeManager newProbeManager = AddNewProbe((ProbeProperties.ProbeType)data.type, new ProbeInsertion(data.apmldv, data.angles, CoordinateSpaceManager.ActiveCoordinateSpace, CoordinateSpaceManager.ActiveCoordinateTransform), data.UUID);
            if (data.overrideName != null)
                newProbeManager.OverrideName(data.overrideName);
            Debug.Log($"Overriding color: {data.color}");
            newProbeManager.Color = data.color;
        }

        #endregion

        #region BLDistance

        /// <summary>
        /// Change the bregma-lamba distance. By default this is 4.15f, so if it isn't that value, then we need to add an isometric scaling to the current transform
        /// </summary>
        /// <param name="blDistance"></param>
        public void ChangeBLDistance(float blDistance)
        {
            float blRatio = blDistance / 4.15f;
#if UNITY_EDITOR
            Debug.Log($"(BL Distance) Re-scaling to {blRatio}");
#endif

            if (CoordinateSpaceManager.ActiveCoordinateTransform.Name != "Custom")
                CoordinateSpaceManager.OriginalTransform = CoordinateSpaceManager.ActiveCoordinateTransform;

            // There's no easy way to implement this without a refactor of the CoordinateTransform code, because you can't pull out the transform matrix.

            // For now what we'll do is switch through the current transform, and replace it with a new version that's been scaled

            CoordinateTransform newTransform;

            switch (CoordinateSpaceManager.OriginalTransform.Prefix)
            {
                case "ccf":
                    // the ccf transform is the unity transform, so just build a new affine transform that scales
                    newTransform = new CustomAffineTransform(blRatio * Vector3.one, Vector3.zero);
                    break;

                case "q18":
                    newTransform = new CustomAffineTransform(blRatio * new Vector3(-1.031f, 0.952f, -0.885f), new Vector3(0f, -5f, 0f));
                    break;

                case "d08":
                    newTransform = new CustomAffineTransform(blRatio * new Vector3(-1.087f, 1f, -0.952f), new Vector3(0f, -5f, 0f));
                    break;

                case "i-d08":
                    newTransform = new CustomAffineTransform(blRatio * new Vector3(-1.087f, 1f, -0.952f), new Vector3(0f, 0f, 0f));
                    break;

                default:
                    Debug.LogError("Previous transform is not scalable");
                    return;
            }

            // Apply the new transform
            SetNewTransform(newTransform);
        }

        #endregion

        #region Misc

        public void LinkToVBLSite()
        {
            Application.OpenURL("https://virtualbrainlab.org");
        }

#endregion
    }
}
