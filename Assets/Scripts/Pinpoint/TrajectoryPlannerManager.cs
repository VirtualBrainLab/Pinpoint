using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BrainAtlas;
using BrainAtlas.CoordinateSystems;
using EphysLink;
using Models;
using Models.Scene;
using TMPro;
using UI;
using UI.ViewModels;
using UITabs;
using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Urchin.Managers;
using Utils.Types;
using UrchinUtilsUtils = Urchin.Utils.Utils;
using static UnityEngine.InputSystem.InputAction;


#if UNITY_WEBGL
using System.Collections.Specialized;
using System.Runtime.InteropServices;
#endif

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

        #region State

        private IDisposableSubscription _sceneStateSubscription;

        #endregion

        #region Events
        // Startup events
        public UnityEvent StartupEvent_MetaLoaded;
        public UnityEvent StartupEvent_RefAtlasLoaded;
        public UnityEvent<Texture3D> StartupEvent_AnnotationTextureLoaded;
        public UnityEvent StartupEvent_SceneLoaded;
        public UnityEvent StartupEvent_Complete;

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
        [SerializeField] private Transform _probeParentT;
        [SerializeField] private ProbePanelManager _probePanelManager;
        [SerializeField] private AtlasManager _atlasManager;
        [SerializeField] private PinpointAtlasManager _pinpointAtlasManager;

        // Settings
        [FormerlySerializedAs("probePrefabs")][SerializeField] private List<GameObject> _probePrefabs;
        [FormerlySerializedAs("ccfCollider")][SerializeField] private Collider _ccfCollider;
        [FormerlySerializedAs("inPlaneSlice")][SerializeField] private TP_InPlaneSlice _inPlaneSlice;

        [SerializeField] private RelativeCoordinatePanel _relCoordPanel;
        [SerializeField] private ReferenceCoordBehavior _referenceCoordBehavior;

        [FormerlySerializedAs("sliceRenderer")][SerializeField] private TP_SliceRenderer _sliceRenderer;
        [FormerlySerializedAs("searchControl")][SerializeField] private TP_Search _searchControl;

        [FormerlySerializedAs("settingsPanel")][SerializeField] private TP_SettingsMenu _settingsPanel;

        [FormerlySerializedAs("CollisionPanelGO")][SerializeField] private GameObject _collisionPanelGo;
        [FormerlySerializedAs("collisionMaterial")][SerializeField] private Material _collisionMaterial;

        [FormerlySerializedAs("ProbePanelParentGO")][SerializeField] private GameObject _probePanelParentGo;
        [FormerlySerializedAs("CraniotomyToolsGO")][SerializeField] private GameObject _craniotomyToolsGo;
        [FormerlySerializedAs("brainCamController")][SerializeField] private BrainCameraController _brainCamController;

        [FormerlySerializedAs("CanvasParent")][SerializeField] private GameObject _canvasParent;

        // UI 
        [FormerlySerializedAs("qDialogue")][SerializeField] QuestionDialogue _qDialogue;
        [SerializeField] GameObject _logPanelGO;

        // Bregma-labmda distance
        [SerializeField] BregmaLambdaBehavior _blDistance;

        // Debug graphics
        [FormerlySerializedAs("surfaceDebugGO")][SerializeField] private GameObject _surfaceDebugGo;

        // Craniotomy
        [SerializeField] private CraniotomyPanel _craniotomyPanel;

        // Local tracking variables
        private bool _movedThisFrame;

        private const string SCENE_NOT_RESET = "scene-not-reset";
        private bool _sceneWasReset;

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

        TaskCompletionSource<bool> _checkForSavedProbesTaskSource;

        #region Unity
        private void Awake()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLInput.captureAllKeyboardInput = true;
#endif
            ProbeProperties.InitializeColors();

            Settings.ProbePrevNextEnabled = true;

            SetProbeControl(false);

            // Input system
            inputActions = new();
            inputActions.ProbeMetaControl.Enable();
            inputActions.ProbeMetaControl.NextProbe.performed += NextProbe;
            inputActions.ProbeMetaControl.PrevProbe.performed += PrevProbe;
            inputActions.ProbeMetaControl.SwitchAxisMode.performed += x =>
            {
#if APP_UI
                var storeService = PinpointApp.Services.GetRequiredService<Services.StoreService>();
                var settingsState = storeService.Store.GetState<Models.Settings.SettingsState>(Models.SliceNames.SETTINGS_SLICE);
                storeService.Store.Dispatch(Models.Settings.SettingsActions.SET_CONVERT_APML2PROBE, !settingsState.ConvertAPML2Probe);
#endif
            };

            // _accountsManager.UpdateCallbackEvent = AccountsProbeStatusUpdatedCallback;
        }

        public async void Startup()
        {

            // Determine startup flags, we check two things:
            // (1) Are the PlayerPrefs cleared? If they are, we will load the null transform and bregma reference coordinate
            // (2) If set, check if this is an atlas reset, if it is, load the null transform and bregma reference coordinate
            // Otherwise, load all the previous settings
            bool _firstTime = !PlayerPrefs.HasKey("scene-atlas-reset");
            bool _atlasReset = PlayerPrefs.GetInt("scene-atlas-reset", 0) == 1;

            // STARTUP SEQUENCE
            StartupEvent_MetaLoaded.Invoke();

            // Load Atlas
#if APP_UI
            var storeService = PinpointApp.Services.GetService<Services.StoreService>();
            var atlasSettingsState = storeService.Store.GetState<Models.Settings.AtlasSettingsState>(Models.SliceNames.ATLAS_SETTINGS_SLICE);
            string atlasName = atlasSettingsState.AtlasName;
#else
         string atlasName = Settings.AtlasName;
#endif
            if (!BrainAtlasManager.AtlasNames.Contains(atlasName))
                atlasName = "allen_mouse_25um";

            await BrainAtlasManager.LoadAtlas(atlasName);
            ReferenceAtlas referenceAtlas = BrainAtlasManager.ActiveReferenceAtlas;

            // Set the reference coordinate before anything else happen
            // if this is the first time, load bregma
            if (_firstTime || _atlasReset)
            {
#if APP_UI
                if (UrchinUtilsUtils.BregmaDefaults.ContainsKey(atlasName))
                {
                    referenceAtlas.AtlasSpace.ReferenceCoord = UrchinUtilsUtils.BregmaDefaults[atlasName];
                    storeService.Store.Dispatch(Models.Settings.AtlasSettingsActions.SET_REFERENCE_COORD, referenceAtlas.AtlasSpace.ReferenceCoord);
                }
#else
     if (UrchinUtilsUtils.BregmaDefaults.ContainsKey(Settings.AtlasName))
    referenceAtlas.AtlasSpace.ReferenceCoord = UrchinUtilsUtils.BregmaDefaults[Settings.AtlasName];
    Settings.ReferenceCoord = referenceAtlas.AtlasSpace.ReferenceCoord;
#endif
            }
            else
            {
#if APP_UI
                referenceAtlas.AtlasSpace.ReferenceCoord = atlasSettingsState.ReferenceCoord;
#else
      referenceAtlas.AtlasSpace.ReferenceCoord = Settings.ReferenceCoord;
  Settings.ReferenceCoord = referenceAtlas.AtlasSpace.ReferenceCoord;
#endif
            }

            var nodeTask = _atlasManager.LoadDefaultAreas();

            await nodeTask;

            foreach (var node in nodeTask.Result)
            {
                node.SetVisibility(true, OntologyNode.OntologyNodeSide.Full);
                node.SetVisibility(false, OntologyNode.OntologyNodeSide.Left);
                node.SetVisibility(false, OntologyNode.OntologyNodeSide.Right);
                node.SetMaterial(BrainAtlasManager.BrainRegionMaterials["transparent-unlit"]);
                node.ResetColor();
                node.SetShaderProperty("_Alpha", 0.25f, OntologyNode.OntologyNodeSide.Full);
                _pinpointAtlasManager.DefaultNodes.Add(node);
            }

            referenceAtlas.LoadAnnotations();
            referenceAtlas.LoadAnnotationTexture();

            await Task.WhenAll(new Task[] { referenceAtlas.AnnotationsTask, referenceAtlas.AnnotationTextureTask });

            StartupEvent_RefAtlasLoaded.Invoke();
            PinpointApp.Services.GetRequiredService<AtlasViewModel>().LoadAtlasDataCommand.Execute();

            StartupEvent_AnnotationTextureLoaded.Invoke(BrainAtlasManager.ActiveReferenceAtlas.AnnotationTexture);

            _checkForSavedProbesTaskSource = new TaskCompletionSource<bool>();

            StartupEvent_SceneLoaded.Invoke();

            // Link any events that need to be linked
            ProbeManager.ActiveProbeUIUpdateEvent.AddListener(() => SetSurfaceDebugColor(ProbeManager.ActiveProbeManager.Color));


            // Complete
            PlayerPrefs.SetInt("scene-atlas-reset", 0);
            StartupEvent_Complete.Invoke();

#if APP_UI
            _sceneStateSubscription = PinpointApp.StoreServiceStore.Subscribe(
    state => state.Get<SceneState>(SliceNames.SCENE_SLICE), OnSceneStateChanged,
                new SubscribeOptions<SceneState> { fireImmediately = true });
#else
     // After annotation loads, check if the user wants to load previously used probes
            CheckForSavedProbes();
       await _checkForSavedProbesTaskSource.Task;
     // Finally, load accounts if we didn't load a query string or a saved set of probes
       // if (!_checkForSavedProbesTaskSource.Task.Result)
    //     _accountsManager.DelayedStart();
#endif
        }

        void Update()
        {
            if (spawnedThisFrame)
            {
                spawnedThisFrame = false;
                return;
            }

            //if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C)) && Input.GetKeyDown(KeyCode.Backspace))
            //{
            //    RecoverActiveProbeController();
            //    return;
            //}

            if (Input.GetKeyDown(KeyCode.Escape))
                _settingsPanel.ToggleSettingsMenu();

            if (Input.GetKeyDown(KeyCode.L) && !UIManager.InputsFocused)
                _logPanelGO.SetActive(!_logPanelGO.activeSelf);

            //if (Input.anyKey && ProbeManager.ActiveProbeManager != null && !UIManager.InputsFocused)
            //{
            //    if (Input.GetKeyDown(KeyCode.Backspace) && !_canvasParent.GetComponentsInChildren<TMP_InputField>()
            //            .Any(inputField => inputField.isFocused))
            //    {
            //        DestroyActiveProbeManager();
            //        return;
            //    }
            //}
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
                    {
                        SetSurfaceDebugPosition(ProbeManager.ActiveProbeManager.GetSurfaceCoordinateWorldT());
                        SetSurfaceDebugColor(ProbeManager.ActiveProbeManager.Color);
                    }
                }

                _sliceRenderer.UpdateSlicePosition();

                // _accountsManager.UpdateProbeData();

                _probesChangedEvent.Invoke();
            }
        }

        public void SetMovedThisFrame()
        {
            _movedThisFrame = true;
        }

        private void OnDestroy()
        {
            // Unsubscribe from state changes.
            _sceneStateSubscription?.Dispose();
        }

        #endregion

        #region State Handlers

        private void OnSceneStateChanged(SceneState state)
        {
            // Remove probes that don't exist in the state anymore.
            foreach (var removedProbeManagers in ProbeManager.Instances.Where(manager =>
                         !state.Probes.Select(probeState => probeState.Name).Contains(manager.name)))
            {
                removedProbeManagers.Cleanup();
                Destroy(removedProbeManagers.gameObject);
            }

            // Add new probes that don't exist in the scene yet and give them the state name.
            foreach (var newProbe in state.Probes.Where(probeState =>
                         !ProbeManager.Instances.Select(manager => manager.name).Contains(probeState.Name)))
                Instantiate(
                    _probePrefabs.Find(prefab =>
                        prefab.GetComponent<ProbeManager>().ProbeType == newProbe.ProbeType), _probeParentT).name =
                    newProbe.Name;
        }

        #endregion

        public Task GetAnnotationDatasetLoadedTask()
        {
            return annotationDatasetLoadTask;
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

        ////[TODO] Replace this with some system that handles recovering probes by tracking their coordinate system or something?
        //// Or maybe the probe coordinates should be an object that can be serialized?
        //private bool _restoredProbe = true; // Can't restore anything at start
        //private string _prevProbeData;

        //public void DestroyProbe(ProbeManager probeManager)
        //{
        //    var isActiveProbe = ProbeManager.ActiveProbeManager == probeManager;

        //    _prevProbeData = JsonUtility.ToJson(ProbeManagerData.ProbeManager2ProbeData(probeManager));

        //    // Cannot restore a ghost probe, so we set restored to true
        //    _restoredProbe = false;

        //    var remainingProbes =
        //        ProbeManager.Instances.Where(x => x.ProbeType != ProbeType.Placeholder && x != probeManager);

        //    // Destroy probe
        //    probeManager.Cleanup();
        //    Destroy(probeManager.gameObject);

        //    PostDestroyHandler(isActiveProbe, remainingProbes);

        //    _probeAddedOrRemovedEvent.Invoke();

        //    _movedThisFrame = true;
        //}

        ///// <summary>
        ///// Handle TPManager cleanup after a probe was destroyed
        ///// </summary>
        //private void PostDestroyHandler(bool wasActiveProbe, IEnumerable<ProbeManager> remainingProbes)
        //{

        //    if (remainingProbes.Count() > 0)
        //    {
        //        if (wasActiveProbe)
        //        {
        //            SetActiveProbe(remainingProbes.Last());

        //            StartCoroutine(_probePanelManager.RecalculateProbePanels_Delayed());
        //        }
        //    }
        //    else
        //    {
        //        // Cleanup UI if this was last probe in scene
        //        // Invalidate ProbeManager.ActiveProbeManager
        //        if (wasActiveProbe)
        //        {
        //            // TODO: Remove old probe manager behavior.
        //            ProbeManager.ActiveProbeManager = null;
        //            PinpointApp.StoreServiceStore.Dispatch(SceneActions.SET_ACTIVE_PROBE, string.Empty);
        //            _activeProbeChangedEvent.Invoke();
        //        }
        //        SetSurfaceDebugActive(false);
        //    }
        //}

        //private void DestroyActiveProbeManager()
        //{
        //    // Remove the probe's insertion from the list of insertions (does nothing if not found)
        //    // ProbeManager.ActiveProbeManager.ProbeController.Insertion.Targetable = false;

        //    // Remove Probe
        //    DestroyProbe(ProbeManager.ActiveProbeManager);
        //}

        //private void RecoverActiveProbeController()
        //{
        //    if (_restoredProbe) return;

        //    ProbeManagerData probeData = JsonUtility.FromJson<ProbeManagerData>(_prevProbeData);

        //    // Don't duplicate probes by accident
        //    if (!ProbeManager.Instances.Any(x => x.UUID.Equals(probeData.UUID)))
        //    {
        //        var probeInsertion = new ProbeInsertion(probeData.APMLDV, probeData.Angles,
        //            BrainAtlasManager.ActiveReferenceAtlas.AtlasSpace.Name, BrainAtlasManager.ActiveAtlasTransform.Name);

        //        ProbeManager newProbeManager = AddNewProbe((ProbeType)probeData.Type, probeInsertion,
        //            probeData.NumAxes, probeData.ManipulatorID, probeData.ZeroCoordOffset, probeData.BrainSurfaceOffset,
        //            probeData.Drop2SurfaceWithDepth, probeData.IsRightHanded, probeData.UUID);

        //        newProbeManager.UpdateSelectionLayer(probeData.SelectionLayerName);
        //        newProbeManager.OverrideName = probeData.Name;
        //        newProbeManager.Color = probeData.Color;
        //        newProbeManager.APITarget = probeData.APITarget;
        //    }

        //    _restoredProbe = true;
        //}

        #region Add Probe Functions

        /// <summary>
        /// Used in the editor when the add probe buttons are clicked in the scene
        /// </summary>
        /// <param name="probeType">Probe type parameter (e.g. 0/21/24 for neuropixels)</param>
        public void AddNewProbeVoid(int probeType)
        {
            AddNewProbe((ProbeType)probeType);
        }

        /// <summary>
        /// Main function for adding new probes (other functions are just overloads)
        ///
        /// Creates the new probe and then sets it to be active
        /// </summary>
        /// <param name="probeType"></param>
        /// <returns></returns>
        public ProbeManager AddNewProbe(ProbeType probeType, string UUID = null)
        {
            _probePanelManager.CountProbePanels();

            GameObject newProbe = Instantiate(_probePrefabs.Find(x => x.GetComponent<ProbeManager>().ProbeType == probeType), _probeParentT);
            var newProbeManager = newProbe.GetComponent<ProbeManager>();

            if (UUID != null)
                newProbeManager.OverrideUUID(UUID);

            SetActiveProbe(newProbeManager);

            spawnedThisFrame = true;

            newProbeManager.ProbeController.MovedThisFrameEvent.AddListener(SetMovedThisFrame);

            // Invoke the movement event
            _probeAddedOrRemovedEvent.Invoke();

            return newProbe.GetComponent<ProbeManager>();
        }

        public ProbeManager AddNewProbe(ProbeType probeType, ProbeInsertion insertion, string UUID = null)
        {
            ProbeManager probeManager = AddNewProbe(probeType, UUID);

            probeManager.ProbeController.SetProbePosition(insertion.APMLDV);
            probeManager.ProbeController.SetProbeAngles(insertion.Angles);

            return probeManager;
        }

        public ProbeManager AddNewProbe(ProbeType probeType, ProbeInsertion insertion,
            int numAxes, string manipulatorId, Vector4 zeroCoordinateOffset, float brainSurfaceOffset, bool dropToSurfaceWithDepth, bool isRightHanded, string UUID = null)
        {
            var probeManager = AddNewProbe(probeType, UUID);

            probeManager.ProbeController.SetProbePosition(insertion.APMLDV);
            probeManager.ProbeController.SetProbeAngles(insertion.Angles);

            // Return data if there is no current Ephys Link data
            if (Settings.IsEphysLinkDataExpired()) return probeManager;

            // Repopulate Ephys Link information
            probeManager.ManipulatorBehaviorController.NumAxes = numAxes;
            probeManager.ManipulatorBehaviorController.ReferenceCoordinateOffset = zeroCoordinateOffset;
            probeManager.ManipulatorBehaviorController.BrainSurfaceOffset = brainSurfaceOffset;
            // probeManager.ManipulatorBehaviorController.IsSetToDropToSurfaceWithDepth = dropToSurfaceWithDepth;
            // probeManager.ManipulatorBehaviorController.IsSetToDropToSurfaceWithDepth = dropToSurfaceWithDepth;
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
            // TODO: Remove old probe manager behavior.
            ProbeManager.ActiveProbeManager = null;
            PinpointApp.StoreServiceStore.Dispatch(SceneActions.SET_ACTIVE_PROBE, string.Empty);
            _activeProbeChangedEvent.Invoke();
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
            // TODO: Remove old probe manager behavior.
            ProbeManager.ActiveProbeManager = newActiveProbeManager;
            PinpointApp.StoreServiceStore.Dispatch(SceneActions.SET_ACTIVE_PROBE, newActiveProbeManager.UUID);
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
                    probeManager.ProbeDisplay = ProbeDisplayType.Transparent;
                else
                    probeManager.ProbeDisplay = ProbeDisplayType.Opaque;
            }

            // Change the height of the probe panels, if needed
            _probePanelManager.RecalculateProbePanels();

            _activeProbeChangedEvent.Invoke();
        }

        public void NextProbe(CallbackContext context)
        {
            if (ProbeManager.Instances.Count == 0 || UIManager.InputsFocused || !Settings.ProbePrevNextEnabled) return;

            int idx = ProbeManager.Instances.FindIndex(x => x.Equals(ProbeManager.ActiveProbeManager));

            // if this is the last probe, wrap around
            idx = (idx + 1) % ProbeManager.Instances.Count;

            SetActiveProbe(ProbeManager.Instances[idx]);
        }

        public void PrevProbe(CallbackContext context)
        {
            if (ProbeManager.Instances.Count == 0 || UIManager.InputsFocused || !Settings.ProbePrevNextEnabled) return;

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
                    probeManager.OverrideName = newName;
        }

        public void ResetActiveProbe()
        {
            if (ProbeManager.ActiveProbeManager != null)
                ProbeManager.ActiveProbeManager.ProbeController.ResetInsertion();
        }

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

        public void UpdateReferenceCoord()
        {
            _referenceCoordBehavior.UpdateReferenceCoordinate();
        }

        ///
        /// SETTINGS
        /// 


        #region Settings

        public void SetGhostAreaVisibility()
        {
      if (BrainAtlasManager.Instance == null || BrainAtlasManager.ActiveReferenceAtlas == null)
      return;
   
            if (Settings.GhostInactiveAreas)
     {
        List<int> activeAreas = TP_Search.VisibleSearchedAreas;
   List<OntologyNode> activeNodes = activeAreas.ConvertAll(x => BrainAtlasManager.ActiveReferenceAtlas.Ontology.ID2Node(x));

 foreach (OntologyNode node in _pinpointAtlasManager.DefaultNodes)
    if (!activeNodes.Contains(node))
     node.SetVisibility(false);
        }
            else
      {
    foreach (OntologyNode node in _pinpointAtlasManager.DefaultNodes)
node.SetVisibility(true);
 }
  }

        public void SetGhostProbeVisibility()
        {
            foreach (ProbeManager probeManager in ProbeManager.Instances)
            {
                if (probeManager == ProbeManager.ActiveProbeManager)
                {
                    probeManager.ProbeDisplay = ProbeDisplayType.Opaque;
                    continue;
                }

                if (Settings.GhostInactiveProbes)
                    probeManager.ProbeDisplay = ProbeDisplayType.Transparent;
                else
                    probeManager.ProbeDisplay = ProbeDisplayType.Opaque;
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

        #endregion

        #region Setting Helper Functions


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

        public void SetSurfaceDebugColor(Color color)
        {
            _surfaceDebugGo.GetComponent<Renderer>().material.color = color;
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
                    data.Add(JsonUtility.ToJson(ProbeManagerData.ProbeManager2ProbeData(probe)));
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
                data.Add(JsonUtility.ToJson(ProbeManagerData.ProbeManager2ProbeData(probe)));
            }

            return string.Join(";", data);
        }

        /// <summary>
        /// Check for saved probes in the query string on WebGL or by asking the user if they want to re-load the scene
        /// </summary>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async void CheckForSavedProbes()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            // On WebGL, check for a query string
#if UNITY_WEBGL
            if (LoadSavedProbesWebGL())
                _checkForSavedProbesTaskSource.SetResult(true);
#endif

#if UNITY_EDITOR
            // In editor, check for probe string field
            if (!(ProbeString == ""))
            {
                LoadSavedProbesFromEncodedString(ProbeString);
                if (!(SettingsString == ""))
                    LoadSettingsFromEncodedString(SettingsString);
                _checkForSavedProbesTaskSource.SetResult(true);
            }
#endif

            if (_qDialogue)
            {
                if (PlayerPrefs.GetInt("probecount", 0) > 0)
                {
                    var questionString = Settings.IsEphysLinkDataExpired()
                        ? "Load previously saved probes?"
                        : "Restore previous session?";

                    QuestionDialogue.Instance.YesCallback = LoadSavedProbesStandalone;
                    QuestionDialogue.Instance.NoCallback = CheckForSavedProbesNoCallbackHelper;
                    QuestionDialogue.Instance.NewQuestion(questionString);
                }
            }
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private async void CheckForSavedProbesNoCallbackHelper()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            _checkForSavedProbesTaskSource.SetResult(false);
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

                var queryDict = Utils.ParseQueryString();

                foreach (var queryKVP in queryDict)
                {
                    if (queryKVP.Key.Equals("Probes"))
                    {
                        string encodedStr = queryKVP.Value;

                        LoadSavedProbesFromEncodedString(encodedStr);
                        queryStr = true;
                    }
                    if (queryKVP.Key.Equals("Settings"))
                    {
                        string settingsQuery = queryKVP.Value;
                        
                        LoadSettingsFromEncodedString(settingsQuery);
                    }
                }
            }

            return queryStr;
        }
#endif

        private void LoadSettingsFromEncodedString(string encodedSettingsStr)
        {
            var bytes = Convert.FromBase64String(encodedSettingsStr);
            string settingsStr = Encoding.UTF8.GetString(bytes);

            Settings.Load(settingsStr);
        }

        private void LoadSavedProbesFromEncodedString(string encodedProbesStr)
        {
            var bytes = Convert.FromBase64String(encodedProbesStr);
            string probeArrayStr = Encoding.UTF8.GetString(bytes);

            string[] savedProbesArray = probeArrayStr.Split(';');

            LoadSavedProbesFromStringArray(savedProbesArray);
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private async void LoadSavedProbesStandalone()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            _checkForSavedProbesTaskSource.SetResult(true);

            var savedProbes = Settings.LoadSavedProbeData();
            LoadSavedProbesFromStringArray(savedProbes);
        }

        private void LoadSavedProbesFromStringArray(string[] savedProbes)
        {
            foreach (string savedProbe in savedProbes)
            {
                Debug.Log(savedProbe);

                ProbeManagerData probeData = JsonUtility.FromJson<ProbeManagerData>(savedProbe);

                // Don't duplicate probes by accident
                if (!ProbeManager.Instances.Any(x => x.UUID.Equals(probeData.UUID)))
                {
                    if (probeData.AtlasSpaceName != BrainAtlasManager.ActiveReferenceAtlas.AtlasSpace.Name)
                        Debug.LogError("[TODO] Need to warn user when transforming a probe into the active coordinate space!!");

                    CoordinateSpace probeOrigSpace = BrainAtlasManager.ActiveReferenceAtlas.AtlasSpace;

                    AtlasTransform origTransform = BrainAtlasManager.AtlasTransforms.Find(x => x.Name.Equals(probeData.AtlasTransformName));

                    ProbeInsertion probeInsertion;
                    if (origTransform != null && origTransform.Name != BrainAtlasManager.ActiveAtlasTransform.Name)
                    {
                        Debug.LogError($"[TODO] Need to warn user when transforming a probe into the active coordinate transform!!");
                        Vector3 newAPMLDV = BrainAtlasManager.ActiveAtlasTransform.U2T(origTransform.T2U(probeData.APMLDV));

                        probeInsertion = new ProbeInsertion(newAPMLDV, probeData.Angles,
                            BrainAtlasManager.ActiveReferenceAtlas.AtlasSpace.Name, BrainAtlasManager.ActiveAtlasTransform.Name);
                    }
                    else
                    {
                        probeInsertion = new ProbeInsertion(probeData.APMLDV, probeData.Angles,
                            BrainAtlasManager.ActiveReferenceAtlas.AtlasSpace.Name, BrainAtlasManager.ActiveAtlasTransform.Name);
                    }


                    ProbeManager newProbeManager = AddNewProbe((ProbeType)probeData.Type, probeInsertion,
                        probeData.NumAxes, probeData.ManipulatorID, probeData.ZeroCoordOffset, probeData.BrainSurfaceOffset,
                        probeData.Drop2SurfaceWithDepth, probeData.IsRightHanded, probeData.UUID);

                    newProbeManager.UpdateSelectionLayer(probeData.SelectionLayerName);
                    newProbeManager.OverrideName = probeData.Name;
                    newProbeManager.Color = probeData.Color;
                    newProbeManager.APITarget = probeData.APITarget;
                }
            }
        }

        #endregion

        #region Mesh centers

        private int prevTipID;
        private bool prevTipSideLeft;

        public void SetProbeTipPosition2AreaID(int atlasID)
        {
      if (ProbeManager.ActiveProbeManager == null) return;
 
   if (BrainAtlasManager.Instance == null || BrainAtlasManager.ActiveReferenceAtlas == null)
  return;
      
     (Vector3 leftCoordU, Vector3 rightCoordU) = BrainAtlasManager.ActiveReferenceAtlas.MeshCenters[atlasID];

    Vector3 dims = BrainAtlasManager.ActiveReferenceAtlas.Dimensions;

            // coordinates are really broken right now, the right coordinate is the left, and the left is just missing
            leftCoordU = rightCoordU;
            rightCoordU.y = dims.y / 2f + dims.y / 2f - rightCoordU.y;

            // switch to right side if needed
            if (atlasID == prevTipID)
                prevTipSideLeft = !prevTipSideLeft;
            else
                prevTipSideLeft = true; // always start on left

            // transform the coordinate

            Vector3 coordT = BrainAtlasManager.ActiveAtlasTransform.U2T(
                (prevTipSideLeft ? leftCoordU : rightCoordU) - BrainAtlasManager.ActiveReferenceAtlas.AtlasSpace.ReferenceCoord);

            ProbeManager.ActiveProbeManager.ProbeController.SetProbePosition(coordT);

            prevTipID = atlasID;
        }

        #endregion

        #region Text

        public void CopyText()
        {
            ProbeManager.ActiveProbeManager.Probe2Text();
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
