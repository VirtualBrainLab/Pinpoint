using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CoordinateSpaces;
using CoordinateTransforms;
using EphysLink;
using UITabs;
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

        /// <summary>
        /// Fired whenever any probe moves
        /// </summary>
        [SerializeField] private UnityEvent _probesChangedEvent;

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

        // Debug graphics
        [FormerlySerializedAs("surfaceDebugGO")] [SerializeField] private GameObject _surfaceDebugGo;

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
            CheckForSavedProbes(annotationDatasetLoadTask);

            // Finally, load accounts
            _accountsManager.DelayedStart();
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


            //if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.P))
            //    _coenProbe = AddNewProbe(8).GetComponent<EightShankProbeControl>();
        }

        private void LateUpdate()
        {
            if (_movedThisFrame && ProbeManager.ActiveProbeManager != null)
            {
                _movedThisFrame = false;

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
                _prevProbeType = (int)probeManager.ProbeType;
                _prevInsertion = probeManager.GetProbeController().Insertion;
                _prevManipulatorId = probeManager.ManipulatorId;
                _prevZeroCoordinateOffset = probeManager.ZeroCoordinateOffset;
                _prevBrainSurfaceOffset = probeManager.BrainSurfaceOffset;
                _prevUUID = probeManager.UUID;
            }

            // Cannot restore a ghost probe, so we set restored to true
            _restoredProbe = isGhost;

            // Destroy probe
            probeManager.Destroy();
            Destroy(probeManager.gameObject);

            // Cleanup UI if this was last probe in scene
            if (ProbeManager.Instances.Count > 0)
            {
                if (isActiveProbe)
                {
                    SetActiveProbe(ProbeManager.Instances[^1]);
                }

                if (isGhost)
                {
                    UpdateQuickSettings();
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
                _probeQuickSettings.UpdateInteractable(true);
                SetSurfaceDebugActive(false);
                UpdateQuickSettings();
            }

            ColliderManager.CheckForCollisions();
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
            // ProbeManager.ActiveProbeManager.GetProbeController().Insertion.Targetable = false;

            // Remove Probe
            DestroyProbe(ProbeManager.ActiveProbeManager);
        }

        private void RecoverActiveProbeController()
        {
            if (_restoredProbe) return;
            AddNewProbe((ProbeProperties.ProbeType)_prevProbeType, _prevInsertion, _prevManipulatorId, _prevZeroCoordinateOffset, _prevBrainSurfaceOffset,
                false, _prevUUID);
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
            CountProbePanels();
            if (visibleProbePanels >= 16)
                return null;

            GameObject newProbe = Instantiate(_probePrefabs.Find(x => x.GetComponent<ProbeManager>().ProbeType == probeType), _probeParentT);
            var newProbeManager = newProbe.GetComponent<ProbeManager>();

            if (UUID != null)
                newProbeManager.OverrideUUID(UUID);

            SetActiveProbe(newProbeManager);
            newProbeManager.GetProbeController().Insertion.Targetable = true;

            spawnedThisFrame = true;

            UpdateQuickSettingsProbeIdText();

            newProbeManager.UIUpdateEvent.AddListener(UpdateQuickSettings);
            newProbeManager.GetProbeController().MovedThisFrameEvent.AddListener(SetMovedThisFrame);

            // Add listener for SetActiveProbe
            newProbeManager.ActivateProbeEvent.AddListener(delegate { SetActiveProbe(newProbeManager); });

            // Invoke the movement event
            _probesChangedEvent.Invoke();

            return newProbe.GetComponent<ProbeManager>();
        }

        public ProbeManager AddNewProbe(ProbeProperties.ProbeType probeType, ProbeInsertion insertion, string UUID = null)
        {
            ProbeManager probeManager = AddNewProbe(probeType, UUID);

            probeManager.GetProbeController().SetProbePosition(insertion);

            return probeManager;
        }
        
        public ProbeManager AddNewProbe(ProbeProperties.ProbeType probeType, ProbeInsertion insertion,
            string manipulatorId, Vector4 zeroCoordinateOffset, float brainSurfaceOffset, bool dropToSurfaceWithDepth,
            // ReSharper disable once InconsistentNaming
            string UUID = null, bool isGhost = false)
        {
            var probeManager = AddNewProbe(probeType, UUID);

            probeManager.GetProbeController().SetProbePosition(insertion);

            // Repopulate Ephys Link information
            if (!Settings.IsEphysLinkDataExpired())
            {
                probeManager.ZeroCoordinateOffset = zeroCoordinateOffset;
                probeManager.BrainSurfaceOffset = brainSurfaceOffset;
                probeManager.SetDropToSurfaceWithDepth(dropToSurfaceWithDepth);
                var communicationManager = GameObject.Find("EphysLink").GetComponent<CommunicationManager>();
                
                if (communicationManager.IsConnected && !string.IsNullOrEmpty(manipulatorId))
                    probeManager.SetIsEphysLinkControlled(true, manipulatorId, true, null,
                        _ => probeManager.SetIsEphysLinkControlled(false));
            }
            
            // Set original probe manager early on
            if (isGhost) probeManager.OriginalProbeManager = ProbeManager.ActiveProbeManager;

            return probeManager;
        }

        public void CopyActiveProbe()
        {
            AddNewProbe(ProbeManager.ActiveProbeManager.ProbeType, ProbeManager.ActiveProbeManager.GetProbeController().Insertion);
        }

        #endregion

        private void CountProbePanels()
        {
            visibleProbePanels = 0;
            if (Settings.ShowAllProbePanels)
                foreach (ProbeManager probeManager in ProbeManager.Instances)
                    visibleProbePanels += probeManager.GetProbeUIManagers().Count;
            else
                visibleProbePanels = ProbeManager.ActiveProbeManager != null ? 1 : 0;
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
                cellSize.y = 720;
                probePanelParent.cellSize = cellSize;

                // now resize all existing probeUIs to be 720 tall
                foreach (ProbeManager probeManager in ProbeManager.Instances)
                {
                    probeManager.ResizeProbePanel(720);
                }
            }
            else if (visibleProbePanels <= 4)
            {
                Debug.Log("Resizing panels to be 1440");
                // now resize all existing probeUIs to be 1400 tall
                GridLayoutGroup probePanelParent = GameObject.Find("ProbePanelParent").GetComponent<GridLayoutGroup>();
                Vector2 cellSize = probePanelParent.cellSize;
                cellSize.y = 1440;
                probePanelParent.cellSize = cellSize;

                foreach (ProbeManager probeManager in ProbeManager.Instances)
                {
                    probeManager.ResizeProbePanel(1440);
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
                ProbeManager.ActiveProbeManager.SetActive(false);

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
                if (probeManager.IsGhost) continue;

                if (!isActiveProbe && Settings.GhostInactiveProbes)
                    probeManager.SetMaterialsTransparent();
                else
                    probeManager.SetMaterialsDefault();
            }

            // Change the height of the probe panels, if needed
            RecalculateProbePanels();
            
            _activeProbeChangedEvent.Invoke();
        }

        public void OverrideInsertionName(string UUID, string newName)
        {
            foreach (ProbeManager probeManager in ProbeManager.Instances)
                if (probeManager.UUID.Equals(UUID))
                    probeManager.OverrideName(newName);
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

        public void LockActiveProbe(bool locked)
        {
            ProbeManager.ActiveProbeManager.SetLock(locked);
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

            RecalculateProbePanels();
        }

        public void InVivoTransformChanged(int invivoOption)
        {
            Debug.Log("(tpmanager) Attempting to set transform to: " + coordinateTransformOpts.Values.ElementAt(invivoOption).Name);
            CoordinateSpaceManager.ActiveCoordinateTransform = coordinateTransformOpts.Values.ElementAt(invivoOption);
            WarpBrain();

            // Update the warp functions in the craniotomy control panel
            _craniotomyPanel.World2Space = CoordinateSpaceManager.World2TransformedAxisChange;
            _craniotomyPanel.Space2World = CoordinateSpaceManager.Transformed2WorldAxisChange;

            // Check if active probe is a mis-match
            if (ProbeManager.ActiveProbeManager != null)
                ProbeManager.ActiveProbeManager.CheckProbeTransformState();

            UpdateAllProbeUI();
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
            foreach (ProbeManager pcontroller in ProbeManager.Instances)
                if (pcontroller.ProbeType == ProbeProperties.ProbeType.Neuropixels24)
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

        #region Save and load probes on quit

        private void OnApplicationQuit()
        {
            var nonGhostProbeManagers = ProbeManager.Instances.Where(manager => !manager.IsGhost).ToList();
            string[] data = new string[nonGhostProbeManagers.Count];

            for (int i =0; i< nonGhostProbeManagers.Count; i++)
            {
                ProbeManager probe = nonGhostProbeManagers[i];
                data[i] = JsonUtility.ToJson(ProbeData.ProbeManager2ProbeData(probe));
            }
            Settings.SaveCurrentProbeData(data);
        }

        public async void CheckForSavedProbes(Task annotationDatasetLoadTask)
        {
            await annotationDatasetLoadTask;

            if (_qDialogue)
            {
                if (PlayerPrefs.GetInt("probecount", 0) > 0)
                {
                    var questionString = Settings.IsEphysLinkDataExpired()
                        ? "Load previously saved probes?"
                        : "Restore previous session?";

                    _qDialogue.SetYesCallback(LoadSavedProbes);
                    _qDialogue.NewQuestion(questionString);
                }
            }
        }

        private void LoadSavedProbes()
        {
            var savedProbes = Settings.LoadSavedProbeData();

            foreach (var savedProbe in savedProbes)
            {
                ProbeData probeData = JsonUtility.FromJson<ProbeData>(savedProbe);

                // Don't duplicate probes by accident
                if (!ProbeManager.Instances.Any(x => x.UUID.Equals(probeData.UUID)))
                {
                    var probeInsertion = new ProbeInsertion(probeData.APMLDV, probeData.Angles,
                        coordinateSpaceOpts[probeData.CoordSpaceName], coordinateTransformOpts[probeData.CoordTransformName]);

                    ProbeManager newProbeManager = AddNewProbe((ProbeProperties.ProbeType)probeData.Type, probeInsertion,
                        probeData.ManipulatordID, probeData.ZeroCoordOffset, probeData.BrainSurfaceOffset,
                        probeData.Drop2SurfaceWithDepth, probeData.UUID);

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
                (int)probeManager.ProbeType, insertion.CoordinateSpace.Name, insertion.CoordinateTransform.Name,
                probeManager.UUID);
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
                //if (data.spaceName != CoordinateSpaceManager.ActiveCoordinateSpace.Name || data.transformName != CoordinateSpaceManager.ActiveCoordinateTransform.Name)
                //{
                //    // We have a coordiante space/transform mis-match
                //    QuestionDialogue qDialogue = GameObject.Find("QuestionDialoguePanel").GetComponent<QuestionDialogue>();
                //    qDialogue.SetYesCallback(new Action(delegate { AccountsNewProbeHelper(data); }));
                //    qDialogue.NewQuestion($"The saved insertion uses {data.spaceName}/{data.transformName} while you are using {CoordinateSpaceManager.ActiveCoordinateSpace.Name}/{CoordinateSpaceManager.ActiveCoordinateTransform.Name}. Creating a new probe will override these settings with the active ones.");
                //}
                //else
                AccountsNewProbeHelper(data);
            }

        }

        private void AccountsNewProbeHelper((Vector3 apmldv, Vector3 angles, int type, string spaceName, string transformName, string UUID, string overrideName, Color color) data)
        {
            ProbeManager newProbeManager = AddNewProbe((ProbeProperties.ProbeType)data.type, new ProbeInsertion(data.apmldv, data.angles, CoordinateSpaceManager.ActiveCoordinateSpace, CoordinateSpaceManager.ActiveCoordinateTransform), data.UUID);
            if (data.overrideName != null)
                newProbeManager.OverrideName(data.overrideName);
            newProbeManager.Color = data.color;
        }

        #endregion
    }

}
