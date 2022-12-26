using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CoordinateSpaces;
using CoordinateTransforms;
using Settings;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace TrajectoryPlanner
{
    public class TrajectoryPlannerManager : MonoBehaviour
    {
        #region Events
        // TODO: Expose events for probes moving, UI updating, etc
        //UnityEvent ProbeMovedEvent;
        [SerializeField] private UnityEvent _probesChanged;
        #endregion

        // Managers and accessors
        [SerializeField] private CCFModelControl _modelControl;
        [SerializeField] private VolumeDatasetManager _vdmanager;
        [SerializeField] private Transform _brainModel;
        [FormerlySerializedAs("util")] [SerializeField] private Utils _util;
        [FormerlySerializedAs("accountsManager")] [SerializeField] private UnisaveAccountsManager _accountsManager;

        // Settings
        [FormerlySerializedAs("probePrefabs")] [SerializeField] private List<GameObject> _probePrefabs;
        [FormerlySerializedAs("probePrefabIDs")] [SerializeField] private List<int> _probePrefabIDs;
        [FormerlySerializedAs("recRegionSlider")] [SerializeField] private TP_RecRegionSlider _recRegionSlider;
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

        // Debug graphics
        [FormerlySerializedAs("surfaceDebugGO")] [SerializeField] private GameObject _surfaceDebugGo;

        // Text objects that need to stay visible when the background changes
        [FormerlySerializedAs("whiteUIText")] [SerializeField] private List<TMP_Text> _whiteUIText;

        // Craniotomy
        [SerializeField] private CraniotomyPanel _craniotomyPanel;

        // Coordinate system information
        private Dictionary<string, CoordinateSpace> coordinateSpaceOpts;
        private Dictionary<string, CoordinateTransform> coordinateTransformOpts;

        // Local tracking variables
        private List<Collider> rigColliders;
        private List<Collider> allNonActiveColliders;
        private bool _movedThisFrame;


        // Track who got clicked on, probe, camera, or brain
        private bool probeControl;

        public void SetProbeControl(bool state)
        {
            probeControl = state;
            _brainCamController.SetControlBlock(state);
        }

        private bool spawnedThisFrame = false;

        private int visibleProbePanels;

        Task annotationDatasetLoadTask;

        // Track all input fields
        private TMP_InputField[] _allInputFields;

        // Track coen probe
        private EightShankProbeControl _coenProbe;


        #region Unity
        private void Awake()
        {
            SetProbeControl(false);

            // Deal with coordinate spaces and transforms
            coordinateSpaceOpts = new Dictionary<string, CoordinateSpace>();
            coordinateSpaceOpts.Add("CCF", new CCFSpace());
            CoordinateSpaceManager.ActiveCoordinateSpace = coordinateSpaceOpts["CCF"];

            coordinateTransformOpts = new Dictionary<string, CoordinateTransform>();
            coordinateTransformOpts.Add("CCF", new CCFTransform());
            coordinateTransformOpts.Add("MRI", new MRILinearTransform());
            coordinateTransformOpts.Add("Needles", new NeedlesTransform());
            coordinateTransformOpts.Add("IBL-Needles", new IBLNeedlesTransform());

            visibleProbePanels = 0;

            rigColliders = new List<Collider>();
            allNonActiveColliders = new List<Collider>();
            meshCenters = new Dictionary<int, Vector3>();
            LoadMeshData();
            //Physics.autoSyncTransforms = true;

            _accountsManager.UpdateCallbackEvent = AccountsProbeStatusUpdatedCallback;
        }

        private void Start()
        {
            // Startup CCF
            _modelControl.LateStart(true);
            _modelControl.SetBeryl(GetSetting_UseBeryl());

            // Set callback
            DelayedModelControlStart();

            // Startup the volume textures
            annotationDatasetLoadTask = _vdmanager.LoadAnnotationDataset();

            // After annotation loads, check if the user wants to load previously used probes
            CheckForSavedProbes(annotationDatasetLoadTask);

            // Pull settings from PlayerPrefs
            SetSetting_UseAcronyms(PlayerPrefs.GetAcronyms());
            SetSetting_InPlanePanelVisibility(PlayerPrefs.GetInplane());
            SetSetting_UseIBLAngles(PlayerPrefs.GetUseIBLAngles());
            SetSetting_SurfaceDebugSphereVisibility(PlayerPrefs.GetSurfaceCoord());
            SetSetting_RelCoord(PlayerPrefs.GetRelCoord());
            ProbeManager.RightHandedManipulatorIDs = PlayerPrefs.GetRightHandedManipulatorIds();
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

            if (Input.GetKeyDown(KeyCode.H) && !UIManager.InputsFocused)
                _settingsPanel.ToggleSettingsMenu();

            if (Input.anyKey && ProbeManager.ActiveProbeManager != null && !UIManager.InputsFocused)
            {
                if (Input.GetKeyDown(KeyCode.Backspace) && !_canvasParent.GetComponentsInChildren<TMP_InputField>()
                        .Any(inputField => inputField.isFocused))
                {
                    DestroyActiveProbeManager();
                    return;
                }

                // Check if mouse buttons are down, or if probe is under manual control
                if (!Input.GetMouseButton(0) && !Input.GetMouseButton(2) && !probeControl)
                {
                    ProbeManager.ActiveProbeManager.MoveProbe();
                }
            }


            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.P))
                _coenProbe = AddNewProbe(8).GetComponent<EightShankProbeControl>();

            // TEST CODE: Debugging distance of mesh nodes from camera, trying to fix model "pop"
            //List<CCFTreeNode> defaultLoadedNodes = modelControl.GetDefaultLoadedNodes();
            //if (defaultLoadedNodes.Count > 0)
            //{
            //    Camera brainCamera = brainCamController.GetCamera();
            //    Debug.Log(Vector3.Distance(brainCamera.transform.position, defaultLoadedNodes[0].GetMeshCenter()));
            //}
        }

        private void LateUpdate()
        {
            if (_movedThisFrame && ProbeManager.ActiveProbeManager != null)
            {
                _movedThisFrame = false;

                _inPlaneSlice.UpdateInPlaneSlice();

                if (PlayerPrefs.GetSurfaceCoord())
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
            }

            if (_coenProbe != null && _coenProbe.MovedThisFrame)
                ProbeManager.ActiveProbeManager.UpdateUI();
        }

        public void SetMovedThisFrame()
        {
            _movedThisFrame = true;
        }

        #endregion

        public async void CheckForSavedProbes(Task annotationDatasetLoadTask)
        {
            await annotationDatasetLoadTask;

            if (_qDialogue)
            {
                if (UnityEngine.PlayerPrefs.GetInt("probecount", 0) > 0)
                {
                    var questionString = PlayerPrefs.IsEphysLinkDataExpired()
                        ? "Load previously saved probes?"
                        : "Restore previous session?";

                    _qDialogue.SetYesCallback(LoadSavedProbes);
                    _qDialogue.NewQuestion(questionString);
                }
            }
        }

        private void LoadSavedProbes()
        {
            var savedProbes = PlayerPrefs.LoadSavedProbeData();

            foreach (var savedProbe in savedProbes)
            {
                var probeInsertion = new ProbeInsertion(savedProbe.apmldv, savedProbe.angles, 
                    coordinateSpaceOpts[savedProbe.coordinateSpaceName], coordinateTransformOpts[savedProbe.coordinateTransformName]);
                AddNewProbe(savedProbe.type, probeInsertion,
                    savedProbe.manipulatorId, savedProbe.zeroCoordinateOffset, savedProbe.brainSurfaceOffset,
                    savedProbe.dropToSurfaceWithDepth, savedProbe.uuid);
            }

            UpdateQuickSettings();
        }

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
            SetSetting_InVivoTransformState(PlayerPrefs.GetStereotaxic());
        }

        /// <summary>
        /// Transform a coordinate from the active transform space back to CCF space
        /// </summary>
        /// <param name="fromCoord"></param>
        /// <returns></returns>
        public Vector3 CoordinateTransformToCCF(Vector3 fromCoord)
        {
            if (CoordinateSpaceManager.ActiveCoordinateTransform != null)
                return CoordinateSpaceManager.ActiveCoordinateTransform.Transform2Space(fromCoord);
            else
                return fromCoord;
        }

        /// <summary>
        /// Transform a coordinate from CCF space into the active transform space
        /// </summary>
        /// <param name="ccfCoord"></param>
        /// <returns></returns>
        public Vector3 CoordinateTransformFromCCF(Vector3 ccfCoord)
        {
            if (CoordinateSpaceManager.ActiveCoordinateTransform != null)
                return CoordinateSpaceManager.ActiveCoordinateTransform.Space2Transform(ccfCoord);
            else
                return ccfCoord;
        }

        public void ClickSearchArea(GameObject target)
        {
            _searchControl.ClickArea(target);
        }

        public void TargetSearchArea(int id)
        {
            _searchControl.ClickArea(id);
        }

        public TP_InPlaneSlice GetInPlaneSlice()
        {
            return _inPlaneSlice;
        }
        
        public TP_ProbeQuickSettings GetProbeQuickSettings()
        {
            return _probeQuickSettings;
        }
        
        public QuestionDialogue GetQuestionDialogue()
        {
            return _qDialogue;
        }

        public Collider CCFCollider()
        {
            return _ccfCollider;
        }

        public List<Collider> GetAllNonActiveColliders()
        {
            return allNonActiveColliders;
        }

        public bool GetCollisions()
        {
            return PlayerPrefs.GetCollisions();
        }

        // DESTROY AND REPLACE PROBES

        //[TODO] Replace this with some system that handles recovering probes by tracking their coordinate system or something?
        // Or maybe the probe coordinates should be an object that can be serialized?
        private ProbeInsertion _prevInsertion;
        private int _prevProbeType;
        private string _prevManipulatorId;
        private Vector4 _prevZeroCoordinateOffset;
        private float _prevBrainSurfaceOffset;
        private bool _restoredProbe = true; // Can't restore anything at start
        private string _prevUUID;

        public void DestroyProbe(ProbeManager probeManager)
        {
            var isGhost = probeManager.IsGhost;
            var isActiveProbe = ProbeManager.ActiveProbeManager == probeManager;
            
            Debug.Log("Destroying probe type " + _prevProbeType + " with coordinates");

            if (!isGhost)
            {
                _prevProbeType = probeManager.ProbeType;
                _prevInsertion = probeManager.GetProbeController().Insertion;
                _prevManipulatorId = probeManager.ManipulatorId;
                _prevZeroCoordinateOffset = probeManager.ZeroCoordinateOffset;
                _prevBrainSurfaceOffset = probeManager.BrainSurfaceOffset;
                _prevUUID = probeManager.UUID;
            }

            // Cannot restore a ghost probe, so we set restored to true
            _restoredProbe = isGhost;

            // Return color if not a ghost probe
            if (probeManager.IsOriginal) ProbeProperties.ReturnProbeColor(probeManager.GetColor());

            // Destroy probe
            probeManager.Destroy();
            Destroy(probeManager.gameObject);

            // Cleanup UI if this was last probe in scene
            if (ProbeManager.instances.Count > 0)
            {
                if (isActiveProbe)
                {
                    SetActiveProbe(ProbeManager.instances[^1]);
                }

                if (isGhost)
                {
                    UpdateQuickSettings();
                }
                
                probeManager.CheckCollisions(ColliderManager.InactiveColliderInstances);
            }
            else
            {
                // Invalidate ProbeManager.ActiveProbeManager
                if (probeManager == ProbeManager.ActiveProbeManager) ProbeManager.ActiveProbeManager = null;
                _probeQuickSettings.UpdateInteractable(true);
                _probeQuickSettings.SetProbeManager(null);
                SetSurfaceDebugActive(false);
                UpdateQuickSettings();
            }

            _accountsManager.ProbeDestroyInScene(_prevUUID);
            
            // Invoke event
            _probesChanged.Invoke();
        }

        private void DestroyActiveProbeManager()
        {
            // Extra steps for destroying the active probe if it's a ghost probe
            if (ProbeManager.ActiveProbeManager.IsGhost)
            {
                // Remove ghost probe ref from original probe
                ProbeManager.ActiveProbeManager.OriginalProbeManager.GhostProbeManager = null;
                // Disable control UI
                _probeQuickSettings.EnableAutomaticControlUI(false);
            }

            // Remove the probe's insertion from the list of insertions (does nothing if not found)
            ProbeManager.ActiveProbeManager.GetProbeController().Insertion.Targetable = false;

            // Remove Probe
            DestroyProbe(ProbeManager.ActiveProbeManager);
        }

        private void RecoverActiveProbeController()
        {
            if (_restoredProbe) return;
            AddNewProbe(_prevProbeType, _prevInsertion, _prevManipulatorId, _prevZeroCoordinateOffset, _prevBrainSurfaceOffset,
                false, _prevUUID);
            _restoredProbe = true;
        }

        #region Add Probe Functions

        public void AddNewProbeVoid(int probeType)
        {
            AddNewProbe(probeType);
        }

        /// <summary>
        /// Main function for adding new probes (other functions are just overloads)
        /// 
        /// Creates the new probe and then sets it to be active
        /// </summary>
        /// <param name="probeType"></param>
        /// <returns></returns>
        public ProbeManager AddNewProbe(int probeType, string UUID = null)
        {
            CountProbePanels();
            if (visibleProbePanels >= 16)
                return null;

            Debug.Log(probeType);
            GameObject newProbe = Instantiate(_probePrefabs[_probePrefabIDs.FindIndex(x => x == probeType)], _brainModel);
            var newProbeManager = newProbe.GetComponent<ProbeManager>();

            if (UUID != null)
                newProbeManager.OverrideUUID(UUID);

            SetActiveProbe(newProbeManager);
            newProbeManager.GetProbeController().Insertion.Targetable = true;

            spawnedThisFrame = true;
            _accountsManager.AddNewProbe();

            UpdateQuickSettingsProbeIdText();

            newProbeManager.ProbeUIUpdateEvent.AddListener(UpdateQuickSettings);
            newProbeManager.GetProbeController().MovedThisFrameEvent.AddListener(SetMovedThisFrame);

            // Add listener for SetActiveProbe
            newProbeManager.ActivateProbeEvent.AddListener(delegate { SetActiveProbe(newProbeManager); });
            
            // Invoke event
            _probesChanged.Invoke();

            return newProbe.GetComponent<ProbeManager>();
        }

        public ProbeManager AddNewProbe(int probeType, ProbeInsertion insertion, string UUID = null)
        {
            ProbeManager probeManager = AddNewProbe(probeType, UUID);

            probeManager.GetProbeController().SetProbePosition(insertion);

            spawnedThisFrame = true;
            _accountsManager.AddNewProbe();

            return probeManager;
        }
        
        public ProbeManager AddNewProbe(int probeType, ProbeInsertion insertion,
            string manipulatorId, Vector4 zeroCoordinateOffset, float brainSurfaceOffset, bool dropToSurfaceWithDepth,
            // ReSharper disable once InconsistentNaming
            string UUID = null, bool isGhost = false)
        {
            var probeManager = AddNewProbe(probeType, UUID);

            probeManager.GetProbeController().SetProbePosition(insertion);

            // Repopulate Ephys Link information
            if (!PlayerPrefs.IsEphysLinkDataExpired())
            {
                probeManager.ZeroCoordinateOffset = zeroCoordinateOffset;
                probeManager.BrainSurfaceOffset = brainSurfaceOffset;
                probeManager.SetDropToSurfaceWithDepth(dropToSurfaceWithDepth);
                if (!string.IsNullOrEmpty(manipulatorId))
                    probeManager.SetIsEphysLinkControlled(true, manipulatorId, true, null,
                        _ => probeManager.SetIsEphysLinkControlled(false));
            }
            
            // Set original probe manager early on
            if (isGhost) probeManager.OriginalProbeManager = GetActiveProbeManager();

            spawnedThisFrame = true;
            _accountsManager.AddNewProbe();

            return probeManager;
        }

        #endregion

        private void CountProbePanels()
        {
            visibleProbePanels = 0;
            foreach (ProbeManager probeManager in ProbeManager.instances)
                visibleProbePanels += probeManager.GetProbeUIManagers().Count;
        }

        private void RecalculateProbePanels()
        {
            CountProbePanels();

            // Set number of columns based on whether we need 8 probes or more
            GameObject.Find("ProbePanelParent").GetComponent<GridLayoutGroup>().constraintCount = (visibleProbePanels > 8) ? 8 : 4;

            if (visibleProbePanels > 4)
            {
                // Increase the layout to have two rows, by shrinking all the ProbePanel objects to be 500 pixels tall
                GridLayoutGroup probePanelParent = GameObject.Find("ProbePanelParent").GetComponent<GridLayoutGroup>();
                Vector2 cellSize = probePanelParent.cellSize;
                cellSize.y = 700;
                probePanelParent.cellSize = cellSize;

                // now resize all existing probeUIs to be 700 tall
                foreach (ProbeManager probeManager in ProbeManager.instances)
                {
                    probeManager.ResizeProbePanel(700);
                }
            }
            else if (visibleProbePanels <= 4)
            {
                Debug.Log("Resizing panels to be 1400");
                // now resize all existing probeUIs to be 1400 tall
                GridLayoutGroup probePanelParent = GameObject.Find("ProbePanelParent").GetComponent<GridLayoutGroup>();
                Vector2 cellSize = probePanelParent.cellSize;
                cellSize.y = 1400;
                probePanelParent.cellSize = cellSize;

                foreach (ProbeManager probeManager in ProbeManager.instances)
                {
                    probeManager.ResizeProbePanel(1400);
                }
            }

            // Finally, re-order panels if needed to put 2.4 probes first followed by 1.0 / 2.0
            ReOrderProbePanels();
        }

        public Material GetCollisionMaterial()
        {
            return _collisionMaterial;
        }

        public void SetActiveProbe(string UUID)
        {
            // Search for the probemanager corresponding to this UUID
            foreach (ProbeManager probeManager in ProbeManager.instances)
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
                ProbeManager.ActiveProbeManager.SetActive(false);

            // Replace the probe object and set to active
            ProbeManager.ActiveProbeManager = newActiveProbeManager;
            ProbeManager.ActiveProbeManager.SetActive(true);

            foreach (ProbeManager probeManager in ProbeManager.instances)
            {
                // Check visibility
                var isActiveProbe = probeManager == ProbeManager.ActiveProbeManager;
                probeManager.SetUIVisibility(GetSetting_ShowAllProbePanels() || isActiveProbe);

                // Set active state for UI managers
                foreach (ProbeUIManager puimanager in probeManager.GetProbeUIManagers())
                    puimanager.ProbeSelected(isActiveProbe);

                // Update transparency for probe (if not ghost)
                if (probeManager.IsGhost) continue;
                if (isActiveProbe)
                {
                    probeManager.SetMaterialsDefault();
                    continue;
                }

                if (GetSetting_GhostInactive()) probeManager.SetMaterialsTransparent();

            }

            // Change the height of the probe panels, if needed
            RecalculateProbePanels();

            // Also update the recording region size slider
            _recRegionSlider.SliderValueChanged(((DefaultProbeController)ProbeManager.ActiveProbeManager.GetProbeController()).GetRecordingRegionSize());

            // Reset the inplane slice zoom factor
            _inPlaneSlice.ResetZoom();
            
            // Update probe quick settings
            _probeQuickSettings.SetProbeManager(newActiveProbeManager);

            // Let the experiment manager know the active probe UUID
            _accountsManager.UpdateProbeData();
        }

        public void UpdateQuickSettings()
        {
            _probeQuickSettings.UpdateCoordinates();
        }

        public void UpdateQuickSettingsProbeIdText()
        {
            _probeQuickSettings.UpdateProbeIdText();
        }

        public void ResetActiveProbe()
        {
            if (ProbeManager.ActiveProbeManager != null)
                ProbeManager.ActiveProbeManager.GetProbeController().ResetInsertion();
        }


        public ProbeManager GetActiveProbeManager()
        {
            return ProbeManager.ActiveProbeManager;
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

        private void MoveAllProbes()
        {
            foreach (ProbeManager probeController in ProbeManager.instances)
                foreach (ProbeUIManager puimanager in probeController.GetComponents<ProbeUIManager>())
                    puimanager.ProbeMoved();        
        }

        ///
        /// SETTINGS
        /// 

        public void SetBackgroundWhite(bool state)
        {
            if (state)
            {
                foreach (TMP_Text textC in _whiteUIText)
                    textC.color = Color.black;
                Camera.main.backgroundColor = Color.white;
            }
            else
            {
                foreach (TMP_Text textC in _whiteUIText)
                    textC.color = Color.white;
                Camera.main.backgroundColor = Color.black;
            }
        }

        #region Player Preferences
        public void SetSetting_GhostAreas(bool state)
        {
            PlayerPrefs.SetGhostInactiveAreas(state);

            if (state)
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

        public bool GetSetting_GhostAreas()
        {
            return PlayerPrefs.GetGhostInactiveAreas();
        }

        public void SetSetting_GhostInactive(bool state)
        {
            PlayerPrefs.SetGhostInactiveProbes(state);
            foreach (ProbeManager probeManager in ProbeManager.instances)
            {
                if (probeManager == ProbeManager.ActiveProbeManager)
                {
                    probeManager.SetMaterialsDefault();
                    continue;
                }

                if (state) probeManager.SetMaterialsTransparent();
            }
        }

        public bool GetSetting_GhostInactive()
        {
            return PlayerPrefs.GetGhostInactiveProbes();
        }

        public void SetSetting_RelCoord(Vector3 coord)
        {
            PlayerPrefs.SetRelCoord(coord);
            CoordinateSpaceManager.ActiveCoordinateSpace.RelativeOffset = coord;
            _relCoordPanel.SetRelativeCoordinateText(coord);
        }

        public Vector3 GetSetting_RelCoord()
        {
            return PlayerPrefs.GetRelCoord();
        }
        
        public void SetSetting_DisplayUM(bool state)
        {
            PlayerPrefs.SetDisplayUm(state);

            UpdateQuickSettings();
        }

        public bool GetSetting_DisplayUM()
        {
            return PlayerPrefs.GetDisplayUm();
        }
        
        public void SetSetting_UseBeryl(bool state)
        {
            PlayerPrefs.SetUseBeryl(state);
            _modelControl.SetBeryl(state);

            foreach (ProbeManager probeController in ProbeManager.instances)
                foreach (ProbeUIManager puimanager in probeController.GetComponents<ProbeUIManager>())
                    puimanager.ProbeMoved();
        }

        public bool GetSetting_UseBeryl()
        {
            return PlayerPrefs.GetUseBeryl();
        }
        
        public void SetSetting_ShowAllProbePanels(bool state)
        {
            PlayerPrefs.SetShowAllProbePanels(state);
            if (state)
                foreach (ProbeManager probeManager in ProbeManager.instances)
                    probeManager.SetUIVisibility(true);
            else
                foreach (ProbeManager probeManager in ProbeManager.instances)
                    probeManager.SetUIVisibility(ProbeManager.ActiveProbeManager == probeManager);

            RecalculateProbePanels();
        }

        public bool GetSetting_ShowAllProbePanels()
        {
            return PlayerPrefs.GetShowAllProbePanels();
        }

        public void SetSetting_ShowRecRegionOnly(bool state)
        {
            PlayerPrefs.SetRecordingRegionOnly(state);
            MoveAllProbes();
        }

        public bool GetSetting_ShowRecRegionOnly()
        {
            return PlayerPrefs.GetRecordingRegionOnly();
        }

        public void SetSetting_UseAcronyms(bool state)
        {
            PlayerPrefs.SetAcronyms(state);
            _searchControl.RefreshSearchWindow();
            // move probes to update state
            MoveAllProbes();
        }

        public bool GetSetting_UseAcronyms()
        {
            return PlayerPrefs.GetAcronyms();
        }

        public void SetSetting_UseIBLAngles(bool state)
        {
            PlayerPrefs.SetUseIBLAngles(state);
            UpdateQuickSettings();
        }

        public bool GetSetting_UseIBLAngles()
        {
            return PlayerPrefs.GetUseIBLAngles();
        }


        public void SetSetting_GetDepthFromBrain(bool state)
        {
            PlayerPrefs.SetDepthFromBrain(state);
            UpdateQuickSettings();
        }
        public bool GetSetting_GetDepthFromBrain()
        {
            return PlayerPrefs.GetDepthFromBrain();
        }

        public void SetSetting_ConvertAPMLAxis2Probe(bool state)
        {
            PlayerPrefs.SetAPML2ProbeAxis(state);
            UpdateQuickSettings();
        }

        public bool GetSetting_ConvertAPMLAxis2Probe()
        {
            return PlayerPrefs.GetAPML2ProbeAxis();
        }

        public void SetSetting_InVivoTransformState(int invivoOption)
        {
            PlayerPrefs.SetStereotaxic(invivoOption);

            Debug.Log("(tpmanager) Attempting to set transform to: " + coordinateTransformOpts.Values.ElementAt(invivoOption).Name);
            CoordinateSpaceManager.ActiveCoordinateTransform = coordinateTransformOpts.Values.ElementAt(invivoOption);
            WarpBrain();

            // Update the warp functions in the craniotomy control panel
            _craniotomyPanel.World2Space = CoordinateSpaceManager.World2TransformedAxisChange;
            _craniotomyPanel.Space2World = CoordinateSpaceManager.Transformed2WorldAxisChange;

            // Check if active probe is a mis-match
            if (ProbeManager.ActiveProbeManager != null)
                ProbeManager.ActiveProbeManager.CheckProbeTransformState();

            MoveAllProbes();
        }

        public bool GetSetting_InVivoTransformActive()
        {
            return PlayerPrefs.GetStereotaxic() > 0;
        }

        public void SetSetting_SurfaceDebugSphereVisibility(bool state)
        {
            PlayerPrefs.SetSurfaceCoord(state);
            SetSurfaceDebugActive(state);
        }

        public void SetSetting_CollisionInfoVisibility(bool toggleCollisions)
        {
            PlayerPrefs.SetCollisions(toggleCollisions);
            if (ProbeManager.ActiveProbeManager != null)
                ProbeManager.ActiveProbeManager.CheckCollisions(ColliderManager.InactiveColliderInstances);
        }

        public void SetSetting_InPlanePanelVisibility(bool state)
        {
            PlayerPrefs.SetInplane(state);
            _inPlaneSlice.UpdateInPlaneVisibility();
        }

        #endregion

        #region Setting Helper Functions


        public void SetSurfaceDebugActive(bool active)
        {
            if (PlayerPrefs.GetSurfaceCoord() && ProbeManager.ActiveProbeManager != null)
                _surfaceDebugGo.SetActive(active);
            else
                _surfaceDebugGo.SetActive(false);
        }

        public void SetCollisionPanelVisibility(bool visibility)
        {
            _collisionPanelGo.SetActive(visibility);
        }

        public string GetInVivoPrefix()
        {
            return CoordinateSpaceManager.ActiveCoordinateTransform.Prefix;
        }

        #endregion



        public void ReOrderProbePanels()
        {
            Debug.Log("Re-ordering probe panels");
            Dictionary<float, ProbeUIManager> sorted = new Dictionary<float, ProbeUIManager>();

            int probeIndex = 0;
            // first, sort probes so that np2.4 probes go first
            List<ProbeManager> np24Probes = new List<ProbeManager>();
            List<ProbeManager> otherProbes = new List<ProbeManager>();
            foreach (ProbeManager pcontroller in ProbeManager.instances)
                if (pcontroller.ProbeType == 4)
                    np24Probes.Add(pcontroller);
                else
                    otherProbes.Add(pcontroller);
            // now sort by order within each puimanager
            foreach (ProbeManager pcontroller in np24Probes)
            {
                List<ProbeUIManager> puimanagers = pcontroller.GetProbeUIManagers();
                foreach (ProbeUIManager puimanager in pcontroller.GetProbeUIManagers())
                    sorted.Add(probeIndex + puimanager.GetOrder() / 10f, puimanager);
                probeIndex++;
            }
            foreach (ProbeManager pcontroller in otherProbes)
            {
                List<ProbeUIManager> puimanagers = pcontroller.GetProbeUIManagers();
                foreach (ProbeUIManager puimanager in pcontroller.GetProbeUIManagers())
                    sorted.Add(probeIndex + puimanager.GetOrder() / 10f, puimanager);
                probeIndex++;
            }

            // now sort the list according to the keys
            float[] keys = new float[sorted.Count];
            sorted.Keys.CopyTo(keys, 0);
            Array.Sort(keys);

            // and finally, now put the probe panel game objects in order
            for (int i = 0; i < keys.Length; i++)
            {
                GameObject probePanel = sorted[keys[i]].GetProbePanel().gameObject;
                probePanel.transform.SetAsLastSibling();
            }
        }

        public void SetIBLTools(bool state)
        {
            _craniotomyToolsGo.SetActive(state);
        }

        public void SetSurfaceDebugPosition(Vector3 worldPosition)
        {
            _surfaceDebugGo.transform.position = worldPosition;
        }

        private void OnApplicationQuit()
        {
            var nonGhostProbeManagers = ProbeManager.instances.Where(manager => !manager.IsGhost).ToList();
            var probeCoordinates =
                new (Vector3 apmldv, Vector3 angles, 
                int type, string manipulatorId,
                string coordinateSpace, string coordinateTransform,
                Vector4 zeroCoordinateOffset, float brainSurfaceOffset, bool dropToSurfaceWithDepth,
                string uuid)[nonGhostProbeManagers.Count];

            for (int i =0; i< nonGhostProbeManagers.Count; i++)
            {
                ProbeManager probe = nonGhostProbeManagers[i];
                ProbeInsertion probeInsertion = probe.GetProbeController().Insertion;
                probeCoordinates[i] = (probeInsertion.apmldv, 
                    probeInsertion.angles,
                    probe.ProbeType, probe.ManipulatorId,
                    probeInsertion.CoordinateSpace.Name, probeInsertion.CoordinateTransform.Name,
                    probe.ZeroCoordinateOffset, probe.BrainSurfaceOffset, probe.IsSetToDropToSurfaceWithDepth,
                    probe.UUID);
            }
            PlayerPrefs.SaveCurrentProbeData(probeCoordinates);
        }

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
            int berylID = _modelControl.GetBerylID(targetNode.ID);
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

            apmldv = ProbeManager.ActiveProbeManager.GetProbeController().Insertion.CoordinateTransform.Space2Transform(apmldv - CoordinateSpaceManager.ActiveCoordinateSpace.RelativeOffset);
            ProbeManager.ActiveProbeManager.GetProbeController().SetProbePosition(apmldv);

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

        private (Vector3 apmldv, Vector3 angles, int type, string spaceName, string transformName, string UUID) Probe2ServerProbeInsertion(ProbeManager probeManager)
        {
            ProbeInsertion insertion = probeManager.GetProbeController().Insertion;
            return (insertion.apmldv, insertion.angles,
                probeManager.ProbeType, insertion.CoordinateSpace.Name, insertion.CoordinateTransform.Name,
                probeManager.UUID);
        }

        /// <summary>
        /// Called by the AccountsManager class when a probe's visibility is updated
        /// 
        /// TPManager then requests a list of all active probes and updates the scene appropriately
        /// </summary>
        private void AccountsProbeStatusUpdatedCallback((Vector3 apmldv, Vector3 angles, int type, string spaceName, string transformName, string UUID) data,
            bool visible)
        {
            Debug.Log($"Change in visibility for {data.UUID} to {visible}");
            if (!visible)
            {
                // destroy the probe
                ProbeManager probeManager = ProbeManager.instances.Find(x => x.UUID.Equals(data.UUID));
                if (probeManager != null)
                    DestroyProbe(probeManager);
            }
            else
            {
                if (data.spaceName != CoordinateSpaceManager.ActiveCoordinateSpace.Name || data.transformName != CoordinateSpaceManager.ActiveCoordinateTransform.Name)
                {
                    // We have a coordiante space/transform mis-match
                    QuestionDialogue qDialogue = GameObject.Find("QuestionDialoguePanel").GetComponent<QuestionDialogue>();
                    qDialogue.SetYesCallback(new Action(delegate { AccountsNewProbeHelper(data); }));
                    qDialogue.NewQuestion($"The saved insertion uses {data.spaceName}/{data.transformName} while you are using {CoordinateSpaceManager.ActiveCoordinateSpace.Name}/{CoordinateSpaceManager.ActiveCoordinateTransform.Name}. Creating a new probe will override these settings with the active ones.");
                }
                else
                    AccountsNewProbeHelper(data);
            }

            
        }

        private void AccountsNewProbeHelper((Vector3 apmldv, Vector3 angles, int type, string spaceName, string transformName, string UUID) data)
        {
            AddNewProbe(data.type, new ProbeInsertion(data.apmldv, data.angles, CoordinateSpaceManager.ActiveCoordinateSpace, CoordinateSpaceManager.ActiveCoordinateTransform), data.UUID);
        }

        #endregion
    }

}
