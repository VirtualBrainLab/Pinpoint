using CoordinateSpaces;
using CoordinateTransforms;
using EphysLink;
using Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TrajectoryPlanner
{
    public class TrajectoryPlannerManager : MonoBehaviour
    {
        // Managers and accessors
        [SerializeField] private CCFModelControl _modelControl;
        [SerializeField] private VolumeDatasetManager _vdManager;
        [SerializeField] private PlayerPrefs _localPrefs;
        [SerializeField] private Transform _brainModel;
        [SerializeField] private Utils _util;
        [SerializeField] private AxisControl _aControl;
        [SerializeField] private AccountsManager _accountsManager;

        // Settings
        [SerializeField] private List<GameObject> _probePrefabs;
        [SerializeField] private List<int> _probePrefabIDs;
        [SerializeField] private TP_RecRegionSlider _recRegionSlider;
        [SerializeField] private Collider _CCFCollider;
        [SerializeField] private TP_InPlaneSlice _inPlaneSlice;

        [SerializeField] private TP_ProbeQuickSettings _probeQuickSettings;
        [SerializeField] private RelativeCoordinatePanel _relCoordPanel;

        [SerializeField] private TP_SliceRenderer _sliceRenderer;
        [SerializeField] private TP_Search _searchControl;
        [SerializeField] private TMP_InputField _searchInput;

        [SerializeField] private TP_SettingsMenu _settingsPanel;

        [SerializeField] private GameObject _collisionPanelGO;
        [SerializeField] private Material _collisionMaterial;

        [SerializeField] private GameObject _probePanelParentGO;
        [SerializeField] private GameObject _craniotomyToolsGO;
        [SerializeField] private BrainCameraController _brainCamController;

        [SerializeField] private TextAsset _meshCenterText;
        private Dictionary<int, Vector3> _meshCenters;

        [SerializeField] private GameObject _canvasParent;

        // UI 
        [SerializeField] private TP_QuestionDialogue _qDialogue;

        // Debug graphics
        [SerializeField] private GameObject _surfaceDebugGO;

        // Text objects that need to stay visible when the background changes
        [SerializeField] private List<TMP_Text> _whiteUIText;

        // Coordinate system information
        private Dictionary<string, CoordinateSpace> _coordinateSpaceOpts;
        private Dictionary<string, CoordinateTransform> _coordinateTransformOpts;
        // tracking
        private CoordinateSpace _activeCoordinateSpace;
        private CoordinateTransform _activeCoordinateTransform;

        #region tracking variables
        private ProbeManager _activeProbe;
        private List<ProbeManager> _allProbeManagers;
        private List<Collider> _inactiveProbeColliders;
        private List<Collider> _allProbeColliders;
        private List<Collider> _rigColliders;
        private List<Collider> _allNonActiveColliders;
        #endregion

        #region probe colors
        private static List<Color> probeColors = new List<Color> { ColorFromRGB(114, 87, 242), ColorFromRGB(240, 144, 96), ColorFromRGB(71, 147, 240), ColorFromRGB(240, 217, 48), ColorFromRGB(60, 240, 227),
                                    ColorFromRGB(180, 0, 0), ColorFromRGB(0, 180, 0), ColorFromRGB(0, 0, 180), ColorFromRGB(180, 180, 0), ColorFromRGB(0, 180, 180),
                                    ColorFromRGB(180, 0, 180), ColorFromRGB(240, 144, 96), ColorFromRGB(71, 147, 240), ColorFromRGB(240, 217, 48), ColorFromRGB(60, 240, 227),
                                    ColorFromRGB(114, 87, 242), ColorFromRGB(255, 255, 255), ColorFromRGB(0, 125, 125), ColorFromRGB(125, 0, 125), ColorFromRGB(125, 125, 0)};
        #endregion

        // Values
        [SerializeField] private int _probePanelAcronymTextFontSize = 14;
        [SerializeField] private int _probePanelAreaTextFontSize = 10;

        // Track who got clicked on, probe, camera, or brain
        private bool _probeControl;

        public void SetProbeControl(bool state)
        {
            _probeControl = state;
            _brainCamController.SetControlBlock(state);
        }

        public bool MovedThisFrame { get; set; }
        private bool _spawnedThisFrame = false;

        private int _visibleProbePanels;

        private Task _annotationDatasetLoadTask;

        // Track coen probe
        private EightShankProbeControl _coenProbe;

        #region Ephys Link

        private CommunicationManager _communicationManager;
        private HashSet<string> _rightHandedManipulatorIds = new();

        #endregion

        #region Unity
        private void Awake()
        {
            SetProbeControl(false);

            // Deal with coordinate spaces and transforms
            _coordinateSpaceOpts = new Dictionary<string, CoordinateSpace>();
            _coordinateSpaceOpts.Add("CCF", new CCFSpace());
            _activeCoordinateSpace = _coordinateSpaceOpts["CCF"];

            _coordinateTransformOpts = new Dictionary<string, CoordinateTransform>();
            _coordinateTransformOpts.Add("CCF", new CCFTransform());
            _coordinateTransformOpts.Add("MRI", new MRILinearTransform());
            _coordinateTransformOpts.Add("Needles", new NeedlesTransform());
            _coordinateTransformOpts.Add("IBL-Needles", new IBLNeedlesTransform());

            _visibleProbePanels = 0;

            _allProbeManagers = new List<ProbeManager>();
            _allProbeColliders = new List<Collider>();
            _inactiveProbeColliders = new List<Collider>();
            _rigColliders = new List<Collider>();
            _allNonActiveColliders = new List<Collider>();
            _meshCenters = new Dictionary<int, Vector3>();
            LoadMeshData();
            //Physics.autoSyncTransforms = true;

            _communicationManager = GameObject.Find("EphysLink").GetComponent<CommunicationManager>();
        }


        private void Start()
        {
            // Startup CCF
            _modelControl.LateStart(true);
            _modelControl.SetBeryl(GetSetting_UseBeryl());

            // Set callback
            DelayedModelControlStart();

            // Startup the volume textures
            List<Action> callbacks = new List<Action>();
            callbacks.Add(_inPlaneSlice.StartAnnotationDataset);
            _annotationDatasetLoadTask = _vdManager.LoadAnnotationDataset(callbacks);

            // After annotation loads, check if the user wants to load previously used probes
            CheckForSavedProbes(_annotationDatasetLoadTask);

            // Pull settings from PlayerPrefs
            SetSetting_UseAcronyms(_localPrefs.GetAcronyms());
            SetSetting_InPlanePanelVisibility(_localPrefs.GetInplane());
            SetSetting_UseIBLAngles(_localPrefs.GetUseIBLAngles());
            SetSetting_SurfaceDebugSphereVisibility(_localPrefs.GetSurfaceCoord());
            SetSetting_RelCoord(_localPrefs.GetRelCoord());
            _rightHandedManipulatorIds = _localPrefs.GetRightHandedManipulatorIds();
        }

        void Update()
        {
            if (_spawnedThisFrame)
            {
                _spawnedThisFrame = false;
                return;
            }

            if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C)) && Input.GetKeyDown(KeyCode.Backspace))
            {
                RecoverActiveProbeController();
                return;
            }

            if (Input.GetKeyDown(KeyCode.H) && !InputsFocused())
                _settingsPanel.ToggleSettingsMenu();

            if (Input.anyKey && _activeProbe != null && !InputsFocused())
            {
                if (Input.GetKeyDown(KeyCode.Backspace) && !_canvasParent.GetComponentsInChildren<TMP_InputField>()
                        .Any(inputField => inputField.isFocused))
                {
                    DestroyActiveProbeManager();
                    return;
                }

                // Check if mouse buttons are down, or if probe is under manual control
                if (!Input.GetMouseButton(0) && !Input.GetMouseButton(2) && !_probeControl)
                {
                    MovedThisFrame = _localPrefs.GetCollisions() ? _activeProbe.MoveProbe(true) : _activeProbe.MoveProbe(false);
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
            if (MovedThisFrame && _activeProbe != null)
            {
                MovedThisFrame = false;

                _inPlaneSlice.UpdateInPlaneSlice();

                bool inBrain = _activeProbe.IsProbeInBrain();
                SetSurfaceDebugActive(inBrain);
                if (inBrain)
                    SetSurfaceDebugPosition(_activeProbe.GetSurfaceCoordinateWorldT());

                if (!_probeQuickSettings.IsFocused())
                    UpdateQuickSettings();

                _sliceRenderer.UpdateSlicePosition();

                _accountsManager.UpdateProbeData(_activeProbe.UUID, Probe2ServerProbeInsertion(_activeProbe));
            }

            if (_coenProbe != null && _coenProbe.MovedThisFrame)
                _activeProbe.UpdateUI();
        }

        #endregion

        public async void CheckForSavedProbes(Task annotationDatasetLoadTask)
        {
            await annotationDatasetLoadTask;

            if (_qDialogue)
            {
                if (UnityEngine.PlayerPrefs.GetInt("probecount", 0) > 0)
                {
                    var questionString = PlayerPrefs.IsLinkDataExpired()
                        ? "Load previously saved probes?"
                        : "Restore previous session?";
                    
                    _qDialogue.NewQuestion(questionString);
                    _qDialogue.SetYesCallback(this.LoadSavedProbes);
                }
            }
        }

        private void LoadSavedProbes()
        {
            var savedProbes = _localPrefs.LoadSavedProbeData();

            foreach (var savedProbe in savedProbes)
            {
                var probeInsertion = new ProbeInsertion(savedProbe.apmldv, savedProbe.angles, 
                    _coordinateSpaceOpts[savedProbe.coordinateSpaceName], _coordinateTransformOpts[savedProbe.coordinateTransformName]);
                AddNewProbeTransformed(savedProbe.type, probeInsertion,
                    savedProbe.manipulatorId, savedProbe.zeroCoordinateOffset, savedProbe.brainSurfaceOffset,
                    savedProbe.dropToSurfaceWithDepth, savedProbe.uuid);
            }

            UpdateQuickSettings();
        }

        public Task GetAnnotationDatasetLoadedTask()
        {
            return _annotationDatasetLoadTask;
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
            SetSetting_InVivoTransformState(_localPrefs.GetStereotaxic());
        }

        /// <summary>
        /// Transform a coordinate from the active transform space back to CCF space
        /// </summary>
        /// <param name="fromCoord"></param>
        /// <returns></returns>
        public Vector3 CoordinateTransformToCCF(Vector3 fromCoord)
        {
            if (_activeCoordinateTransform != null)
                return _activeCoordinateTransform.Transform2Space(fromCoord);
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
            if (_activeCoordinateTransform != null)
                return _activeCoordinateTransform.Space2Transform(ccfCoord);
            else
                return ccfCoord;
        }

        public CoordinateTransform GetActiveCoordinateTransform()
        {
            return _activeCoordinateTransform;
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
        
        public TP_QuestionDialogue GetQuestionDialogue()
        {
            return _qDialogue;
        }

        public Collider CCFCollider()
        {
            return _CCFCollider;
        }

        public int ProbePanelTextFS(bool acronym)
        {
            return acronym ? _probePanelAcronymTextFontSize : _probePanelAreaTextFontSize;
        }

        public CCFAnnotationDataset GetAnnotationDataset()
        {
            return _vdManager.GetAnnotationDataset();
        }

        public bool IsManipulatorRightHanded(string manipulatorId)
        {
            return _rightHandedManipulatorIds.Contains(manipulatorId);
        }
        
        public void AddRightHandedManipulator(string manipulatorId)
        {
            _rightHandedManipulatorIds.Add(manipulatorId);
            PlayerPrefs.SaveRightHandedManipulatorIds(_rightHandedManipulatorIds);
        }
        
        public void RemoveRightHandedManipulator(string manipulatorId)
        {
            if (!IsManipulatorRightHanded(manipulatorId)) return;
            _rightHandedManipulatorIds.Remove(manipulatorId);
            PlayerPrefs.SaveRightHandedManipulatorIds(_rightHandedManipulatorIds);
        }


        public bool InputsFocused()
        {
            return _searchInput.isFocused || _probeQuickSettings.IsFocused();
        }

        public List<ProbeManager> GetAllProbes()
        {
            return _allProbeManagers;
        }

        public List<Collider> GetAllNonActiveColliders()
        {
            return _allNonActiveColliders;
        }

        public bool GetCollisions()
        {
            return _localPrefs.GetCollisions();
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

        public void DestroyProbe(ProbeManager probeManager)
        {
            var isGhost = probeManager.IsGhost;
            var isActiveProbe = _activeProbe == probeManager;
            
            Debug.Log("Destroying probe type " + _prevProbeType + " with coordinates");

            if (!isGhost)
            {
                _prevProbeType = probeManager.ProbeType;
                _prevInsertion = probeManager.GetProbeController().Insertion;
                _prevManipulatorId = probeManager.ManipulatorId;
                _prevZeroCoordinateOffset = probeManager.ZeroCoordinateOffset;
                _prevBrainSurfaceOffset = probeManager.BrainSurfaceOffset;
            }

            // Cannot restore a ghost probe, so we set restored to true
            _restoredProbe = isGhost;

            // Return color if not a ghost probe
            if (probeManager.IsOriginal) ReturnProbeColor(probeManager.GetColor());

            // Destroy probe
            probeManager.Destroy();
            Destroy(probeManager.gameObject);
            _allProbeManagers.Remove(probeManager);

            // Cleanup UI if this was last probe in scene
            if (_allProbeManagers.Count > 0)
            {
                if (isActiveProbe)
                {
                    SetActiveProbe(_allProbeManagers[^1]);
                }

                if (isGhost)
                {
                    UpdateQuickSettings();
                }
                
                probeManager.CheckCollisions(GetAllNonActiveColliders());
            }
            else
            {
                // Invalidate activeProbe
                if (probeManager == _activeProbe) _activeProbe = null;
                _probeQuickSettings.UpdateInteractable(true);
                _probeQuickSettings.SetProbeManager(null);
                SetSurfaceDebugActive(false);
                UpdateQuickSettings();
            }

            // update colliders
            UpdateProbeColliders();
        }

        private void DestroyActiveProbeManager()
        {
            // Extra steps for destroying the active probe if it's a ghost probe
            if (_activeProbe.IsGhost)
            {
                // Remove ghost probe ref from original probe
                _activeProbe.OriginalProbeManager.GhostProbeManager = null;
                // Disable control UI
                _probeQuickSettings.EnableAutomaticControlUI(false);
            }

            // Remove Probe
            DestroyProbe(_activeProbe);
        }

        private void RecoverActiveProbeController()
        {
            if (_restoredProbe) return;
            AddNewProbe(_prevProbeType, _prevInsertion, _prevManipulatorId, _prevZeroCoordinateOffset, _prevBrainSurfaceOffset);
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
        public ProbeManager AddNewProbe(int probeType)
        {
            CountProbePanels();
            if (_visibleProbePanels >= 16)
                return null;

            GameObject newProbe = Instantiate(_probePrefabs[_probePrefabIDs.FindIndex(x => x == probeType)], _brainModel);
            SetActiveProbe(newProbe.GetComponent<ProbeManager>());

            _spawnedThisFrame = true;

            return newProbe.GetComponent<ProbeManager>();
        }
        
        public ProbeManager AddNewProbeTransformed(int probeType, ProbeInsertion insertion,
            string manipulatorId, Vector4 zeroCoordinateOffset, float brainSurfaceOffset, bool dropToSurfaceWithDepth,
            string uuid = null, bool isGhost = false)
        {
            ProbeManager probeManager = AddNewProbe(probeType);

            if (uuid != null)
                probeManager.OverrideUUID(uuid);

            if (!PlayerPrefs.IsLinkDataExpired())
            {
                probeManager.ZeroCoordinateOffset = zeroCoordinateOffset;
                probeManager.BrainSurfaceOffset = brainSurfaceOffset;
                probeManager.SetDropToSurfaceWithDepth(dropToSurfaceWithDepth);
                if (!string.IsNullOrEmpty(manipulatorId)) probeManager.SetIsEphysLinkControlled(true, manipulatorId);
            }

            probeManager.GetProbeController().SetProbePosition(insertion);
            
            // Set original probe manager early on
            if (isGhost) probeManager.OriginalProbeManager = GetActiveProbeManager();

            _spawnedThisFrame = true;

            return probeManager;
        }

        public ProbeManager AddNewProbe(int probeType, ProbeInsertion localInsertion, string manipulatorId,
            Vector4 zeroCoordinateOffset = new Vector4(), float brainSurfaceOffset = 0)
        {
            ProbeManager probeController = AddNewProbe(probeType);
            if (string.IsNullOrEmpty(manipulatorId))
            {
                Debug.LogError("TODO IMPLEMENT");
                //StartCoroutine(probeController.GetProbeController().SetProbeInsertionTransformed_Delayed(
                //    localInsertion.ap, localInsertion.ml, localInsertion.dv, 
                //    localInsertion.phi, localInsertion.theta, localInsertion.spin,
                //    0.05f));
            }
            else
            {
                probeController.ZeroCoordinateOffset = zeroCoordinateOffset;
                probeController.BrainSurfaceOffset = brainSurfaceOffset;
                probeController.SetIsEphysLinkControlled(true, manipulatorId);
            }

            _spawnedThisFrame = true;

            return probeController;
        }

        #endregion

        private void CountProbePanels()
        {
            _visibleProbePanels = 0;
            foreach (ProbeManager probeManager in _allProbeManagers)
                _visibleProbePanels += probeManager.GetProbeUIManagers().Count;
        }

        private void RecalculateProbePanels()
        {
            CountProbePanels();

            // Set number of columns based on whether we need 8 probes or more
            GameObject.Find("ProbePanelParent").GetComponent<GridLayoutGroup>().constraintCount = (_visibleProbePanels > 8) ? 8 : 4;

            if (_visibleProbePanels > 4)
            {
                // Increase the layout to have two rows, by shrinking all the ProbePanel objects to be 500 pixels tall
                GridLayoutGroup probePanelParent = GameObject.Find("ProbePanelParent").GetComponent<GridLayoutGroup>();
                Vector2 cellSize = probePanelParent.cellSize;
                cellSize.y = 700;
                probePanelParent.cellSize = cellSize;

                // now resize all existing probeUIs to be 700 tall
                foreach (ProbeManager probeController in _allProbeManagers)
                {
                    probeController.ResizeProbePanel(700);
                }
            }
            else if (_visibleProbePanels <= 4)
            {
                Debug.Log("Resizing panels to be 1400");
                // now resize all existing probeUIs to be 1400 tall
                GridLayoutGroup probePanelParent = GameObject.Find("ProbePanelParent").GetComponent<GridLayoutGroup>();
                Vector2 cellSize = probePanelParent.cellSize;
                cellSize.y = 1400;
                probePanelParent.cellSize = cellSize;

                foreach (ProbeManager probeController in _allProbeManagers)
                {
                    probeController.ResizeProbePanel(1400);
                }
            }

            // Finally, re-order panels if needed to put 2.4 probes first followed by 1.0 / 2.0
            ReOrderProbePanels();
        }

        public void RegisterProbe(ProbeManager probeManager)
        {
            Debug.Log("Registering probe: " + probeManager.gameObject.name);
            _allProbeManagers.Add(probeManager);
            
            // Update collider records
            UpdateProbeColliders();
        }

        public int GetNextProbeId()
        {
            var thisProbeId = 1;
            HashSet<int> usedIds = new();
            foreach (var probeId in _allProbeManagers.Select(manager => manager.ProbeID)) usedIds.Add(probeId);
            while (usedIds.Contains(thisProbeId)) thisProbeId++;
            return thisProbeId;
        }

        public Color GetNextProbeColor()
        {
            Color next = probeColors[0];
            probeColors.RemoveAt(0);
            return next;
        }

        public Material GetCollisionMaterial()
        {
            return _collisionMaterial;
        }

        public void ReturnProbeColor(Color returnColor)
        {
            probeColors.Insert(0, returnColor);
        }

        public void SetActiveProbe(ProbeManager newActiveProbeManager)
        {
            if (_activeProbe == newActiveProbeManager)
                return;

#if UNITY_EDITOR
            Debug.Log("Setting active probe to: " + newActiveProbeManager.gameObject.name);
#endif
            _activeProbe = newActiveProbeManager;
            _activeProbe.SetActive();

            foreach (ProbeManager probeManager in _allProbeManagers)
            {
                // Check visibility
                var isActiveProbe = probeManager == _activeProbe;
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

            UpdateProbeColliders();

            // Also update the recording region size slider
            _recRegionSlider.SliderValueChanged(((DefaultProbeController)_activeProbe.GetProbeController()).GetRecordingRegionSize());

            // Reset the inplane slice zoom factor
            _inPlaneSlice.ResetZoom();
            
            // Update probe quick settings
            _probeQuickSettings.SetProbeManager(newActiveProbeManager);
        }

        public void UpdateQuickSettings()
        {
            _probeQuickSettings.UpdateInteractable();
            _probeQuickSettings.UpdateCoordinates();
        }

        public void UpdateQuickSettingsProbeIdText()
        {
            _probeQuickSettings.UpdateProbeIdText();
        }

        public void ResetActiveProbe()
        {
            if (_activeProbe != null)
                _activeProbe.GetProbeController().ResetInsertion();
        }

        public Color GetProbeColor(int probeID)
        {
            return probeColors[probeID];
        }

        public ProbeManager GetActiveProbeManager()
        {
            return _activeProbe;
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
            node.TransformVertices(WorldU2WorldT, true);
        }

        public void UnwarpBrain()
        {
            foreach (CCFTreeNode node in _modelControl.GetDefaultLoadedNodes())
            {
                node.ClearTransform(true);
            }
        }

        /// <summary>
        /// Convert a world coordinate into the corresponding world coordinate after transformation
        /// </summary>
        /// <param name="coordWorld"></param>
        /// <returns></returns>
        public Vector3 WorldU2WorldT(Vector3 coordWorld)
        {
            return _activeCoordinateSpace.Space2World(_activeCoordinateTransform.Transform2SpaceAxisChange(_activeCoordinateTransform.Space2Transform(_activeCoordinateSpace.World2Space(coordWorld))));
        }

        public Vector3 WorldT2WorldU(Vector3 coordWorldT)
        {
            return _activeCoordinateSpace.Space2World(_activeCoordinateTransform.Transform2Space(_activeCoordinateTransform.Space2TransformAxisChange(_activeCoordinateSpace.World2Space(coordWorldT))));
        }

        #endregion

        #region Colliders

        public void UpdateProbeColliders()
        {
            // Collect *all* colliders from all probes
            _allProbeColliders.Clear();
            foreach (ProbeManager probeManager in _allProbeManagers)
            {
                foreach (Collider collider in probeManager.GetProbeColliders())
                    _allProbeColliders.Add(collider);
            }

            // Sort out which colliders are active vs inactive
            _inactiveProbeColliders.Clear();

            List<Collider> activeProbeColliders = (_activeProbe != null) ?
                _activeProbe.GetProbeColliders() :
                new List<Collider>();

            foreach (Collider collider in _allProbeColliders)
                if (!activeProbeColliders.Contains(collider))
                    _inactiveProbeColliders.Add(collider);

            // Re-build the list of inactive colliders (which includes both probe + rig colliders)
            UpdateNonActiveColliders();
        }

        public void UpdateRigColliders(IEnumerable<Collider> newRigColliders, bool keep)
        {
            if (keep)
                foreach (Collider collider in newRigColliders)
                    _rigColliders.Add(collider);
            else
                foreach (Collider collider in newRigColliders)
                    _rigColliders.Remove(collider);
            UpdateNonActiveColliders();
        }


        private void UpdateNonActiveColliders()
        {
            _allNonActiveColliders.Clear();
            foreach (Collider collider in _inactiveProbeColliders)
                _allNonActiveColliders.Add(collider);
            foreach (Collider collider in _rigColliders)
                _allNonActiveColliders.Add(collider);
        }

        #endregion

        private void MoveAllProbes()
        {
            foreach (ProbeManager probeController in _allProbeManagers)
                foreach (ProbeUIManager puimanager in probeController.GetComponents<ProbeUIManager>())
                    puimanager.ProbeMoved();

            MovedThisFrame = true;
        }

        ///
        /// HELPER FUNCTIONS
        /// 
        public static Color ColorFromRGB(int r, int g, int b)
        {
            return new Color(r / 255f, g / 255f, b / 255f, 1f);
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
            _localPrefs.SetGhostInactiveAreas(state);

            if (state)
            {
                List<CCFTreeNode> activeAreas = _searchControl._activeBrainAreas;
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
            return _localPrefs.GetGhostInactiveAreas();
        }

        public void SetSetting_GhostInactive(bool state)
        {
            _localPrefs.SetGhostInactiveProbes(state);
            foreach (ProbeManager probeManager in _allProbeManagers)
            {
                if (probeManager == _activeProbe)
                {
                    probeManager.SetMaterialsDefault();
                    continue;
                }

                if (state) probeManager.SetMaterialsTransparent();
            }
        }

        public bool GetSetting_GhostInactive()
        {
            return _localPrefs.GetGhostInactiveProbes();
        }

        public void SetSetting_RelCoord(Vector3 coord)
        {
            _localPrefs.SetRelCoord(coord);
            _activeCoordinateSpace.RelativeOffset = coord;
            _relCoordPanel.SetRelativeCoordinateText(coord);
        }

        public Vector3 GetSetting_RelCoord()
        {
            return _localPrefs.GetRelCoord();
        }
        
        public void SetSetting_DisplayUM(bool state)
        {
            _localPrefs.SetDisplayUm(state);

            UpdateQuickSettings();
        }

        public bool GetSetting_DisplayUM()
        {
            return _localPrefs.GetDisplayUm();
        }
        
        public void SetSetting_UseBeryl(bool state)
        {
            _localPrefs.SetUseBeryl(state);
            _modelControl.SetBeryl(state);

            foreach (ProbeManager probeController in _allProbeManagers)
                foreach (ProbeUIManager puimanager in probeController.GetComponents<ProbeUIManager>())
                    puimanager.ProbeMoved();
        }

        public bool GetSetting_UseBeryl()
        {
            return _localPrefs.GetUseBeryl();
        }
        
        public void SetSetting_ShowAllProbePanels(bool state)
        {
            _localPrefs.SetShowAllProbePanels(state);
            if (state)
                foreach (ProbeManager probeManager in _allProbeManagers)
                    probeManager.SetUIVisibility(true);
            else
                foreach (ProbeManager probeManager in _allProbeManagers)
                    probeManager.SetUIVisibility(_activeProbe == probeManager);

            RecalculateProbePanels();
        }

        public bool GetSetting_ShowAllProbePanels()
        {
            return _localPrefs.GetShowAllProbePanels();
        }

        public void SetSetting_ShowRecRegionOnly(bool state)
        {
            _localPrefs.SetRecordingRegionOnly(state);
            MoveAllProbes();
        }

        public bool GetSetting_ShowRecRegionOnly()
        {
            return _localPrefs.GetRecordingRegionOnly();
        }

        public void SetSetting_UseAcronyms(bool state)
        {
            _localPrefs.SetAcronyms(state);
            _searchControl.RefreshSearchWindow();
            // move probes to update state
            MoveAllProbes();
        }

        public bool GetSetting_UseAcronyms()
        {
            return _localPrefs.GetAcronyms();
        }

        public void SetSetting_UseIBLAngles(bool state)
        {
            _localPrefs.SetUseIBLAngles(state);
            UpdateQuickSettings();
        }

        public bool GetSetting_UseIBLAngles()
        {
            return _localPrefs.GetUseIBLAngles();
        }


        public void SetSetting_GetDepthFromBrain(bool state)
        {
            _localPrefs.SetDepthFromBrain(state);
            UpdateQuickSettings();
        }
        public bool GetSetting_GetDepthFromBrain()
        {
            return _localPrefs.GetDepthFromBrain();
        }

        public void SetSetting_ConvertAPMLAxis2Probe(bool state)
        {
            _localPrefs.SetAPML2ProbeAxis(state);
            UpdateQuickSettings();
        }

        public bool GetSetting_ConvertAPMLAxis2Probe()
        {
            return _localPrefs.GetAPML2ProbeAxis();
        }

        public void SetSetting_InVivoTransformState(int invivoOption)
        {
            _localPrefs.SetStereotaxic(invivoOption);

            Debug.Log("(tpmanager) Attempting to set transform to: " + _coordinateTransformOpts.Values.ElementAt(invivoOption).Name);
            _activeCoordinateTransform = _coordinateTransformOpts.Values.ElementAt(invivoOption);
            WarpBrain();

            // Check if active probe is a mis-match
            if (_activeProbe != null)
                _activeProbe.SetActive();

            MoveAllProbes();
        }

        public bool GetSetting_InVivoTransformActive()
        {
            return _localPrefs.GetStereotaxic() > 0;
        }

        public void SetSetting_SurfaceDebugSphereVisibility(bool state)
        {
            _localPrefs.SetSurfaceCoord(state);
            SetSurfaceDebugActive(state);
        }

        public void SetSetting_CollisionInfoVisibility(bool toggleCollisions)
        {
            _localPrefs.SetCollisions(toggleCollisions);
            if (_activeProbe != null)
                _activeProbe.CheckCollisions(GetAllNonActiveColliders());
        }

        public void SetSetting_InPlanePanelVisibility(bool state)
        {
            _localPrefs.SetInplane(state);
            _inPlaneSlice.UpdateInPlaneVisibility();
        }

        #endregion

        #region Setting Helper Functions


        public void SetSurfaceDebugActive(bool active)
        {
            if (_localPrefs.GetSurfaceCoord() && _activeProbe != null)
                _surfaceDebugGO.SetActive(active);
            else
                _surfaceDebugGO.SetActive(false);
        }

        public void SetCollisionPanelVisibility(bool visibility)
        {
            _collisionPanelGO.SetActive(visibility);
        }

        public string GetInVivoPrefix()
        {
            return _activeCoordinateTransform.Prefix;
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
            foreach (ProbeManager pcontroller in _allProbeManagers)
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
            _craniotomyToolsGO.SetActive(state);
        }

        public void SetSurfaceDebugPosition(Vector3 worldPosition)
        {
            _surfaceDebugGO.transform.position = worldPosition;
        }

        private void OnApplicationQuit()
        {
            var nonGhostProbeManagers = _allProbeManagers.Where(manager => !manager.IsGhost).ToList();
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
                probeCoordinates[i] = (probeInsertion.APMLDV, 
                    probeInsertion.Angles,
                    probe.ProbeType, probe.ManipulatorId,
                    probeInsertion.CoordinateSpace.Name, probeInsertion.CoordinateTransform.Name,
                    probe.ZeroCoordinateOffset, probe.BrainSurfaceOffset, probe.IsSetToDropToSurfaceWithDepth,
                    probe.UUID);
            }
            _localPrefs.SaveCurrentProbeData(probeCoordinates);
        }


        #region Axis Control

        public bool GetAxisControlEnabled()
        {
            return _localPrefs.GetAxisControl();
        }

        public void SetAxisControlEnabled(bool state)
        {
            _localPrefs.SetAxisControl(state);
            if (!state)
                SetAxisVisibility(false, false, false, false, null);
        }

        public void SetAxisVisibility(bool AP, bool ML, bool DV, bool depth, Transform transform)
        {
            if (GetAxisControlEnabled())
            {
                _aControl.SetAxisPosition(transform);
                _aControl.SetAPVisibility(AP);
                _aControl.SetMLVisibility(ML);
                _aControl.SetDVVisibility(DV);
                _aControl.SetDepthVisibility(depth);
            }
        }

        #endregion

        #region Coordinate spaces

        public CoordinateSpace GetCoordinateSpace() { return _activeCoordinateSpace; }
        public void SetCoordinateSpace(CoordinateSpace newSpace) { _activeCoordinateSpace = newSpace; }
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

                _meshCenters.Add(ID, new Vector3(ap, ml, dv));
            }
        }

        public void SetProbeTipPositionToCCFNode(CCFTreeNode targetNode)
        {
            if (_activeProbe == null) return;
            int berylID = _modelControl.GetBerylID(targetNode.ID);
            Vector3 apmldv = _meshCenters[berylID];

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

            apmldv = _activeProbe.GetProbeController().Insertion.CoordinateTransform.Space2Transform(apmldv - _activeCoordinateSpace.RelativeOffset);
            _activeProbe.GetProbeController().SetProbePosition(apmldv);

            prevTipID = berylID;
        }

        #endregion

        #region Text

        public void CopyText()
        {
            _activeProbe.Probe2Text();
        }

        #endregion

        #region Accounts

        private (Vector3 apmldv, Vector3 angles, int type, string spaceName, string transformName) Probe2ServerProbeInsertion(ProbeManager probeManager)
        {
            ProbeInsertion insertion = probeManager.GetProbeController().Insertion;
            return (insertion.APMLDV, insertion.Angles,
                probeManager.ProbeType, insertion.CoordinateSpace.Name, insertion.CoordinateTransform.Name);
           
        }

        #endregion
    }

}
