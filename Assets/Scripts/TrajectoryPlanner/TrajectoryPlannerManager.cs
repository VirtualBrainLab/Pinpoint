using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EphysLink;
using Settings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TrajectoryPlanner
{
    public class TrajectoryPlannerManager : MonoBehaviour
    {
        // Managers and accessors
        [SerializeField] private CCFModelControl modelControl;
        [SerializeField] private VolumeDatasetManager vdmanager;
        [SerializeField] private PlayerPrefs localPrefs;
        [SerializeField] private Transform brainModel;
        [SerializeField] private Utils util;
        [SerializeField] private AxisControl acontrol;

        // Settings
        [SerializeField] private List<GameObject> probePrefabs;
        [SerializeField] private List<int> probePrefabIDs;
        [SerializeField] private TP_RecRegionSlider recRegionSlider;
        [SerializeField] private Collider ccfCollider;
        [SerializeField] private TP_InPlaneSlice inPlaneSlice;

        [SerializeField] private TP_ProbeQuickSettings probeQuickSettings;

        [SerializeField] private TP_SliceRenderer sliceRenderer;
        [SerializeField] private TP_Search searchControl;
        [SerializeField] private TMP_InputField searchInput;

        [SerializeField] private TP_SettingsMenu settingsPanel;

        [SerializeField] private GameObject CollisionPanelGO;
        [SerializeField] private Material collisionMaterial;

        [SerializeField] private GameObject ProbePanelParentGO;
        [SerializeField] private GameObject IBLToolsGO;
        [SerializeField] private GameObject IBLTrajectoryGO;
        [SerializeField] private BrainCameraController brainCamController;

        [SerializeField] private TextAsset meshCenterText;
        private Dictionary<int, Vector3> meshCenters;

        [SerializeField] private GameObject CanvasParent;

        // UI 
        [SerializeField] TP_QuestionDialogue qDialogue;

        // Debug graphics
        [SerializeField] private GameObject surfaceDebugGO;

        // Text objects that need to stay visible when the background changes
        [SerializeField] private List<TMP_Text> whiteUIText;

        // Coordinate system information
        private CoordinateTransform activeCoordinateTransform;
        private List<CoordinateTransform> availableCoordinateTransforms;

        // Local tracking variables
        private ProbeManager activeProbeController;
        private List<ProbeManager> allProbeManagers;
        private List<Collider> inactiveProbeColliders;
        private List<Collider> allProbeColliders;
        private List<Collider> rigColliders;
        private List<Collider> allNonActiveColliders;

        private static List<Color> probeColors = new List<Color> { ColorFromRGB(114, 87, 242), ColorFromRGB(240, 144, 96), ColorFromRGB(71, 147, 240), ColorFromRGB(240, 217, 48), ColorFromRGB(60, 240, 227),
                                    ColorFromRGB(180, 0, 0), ColorFromRGB(0, 180, 0), ColorFromRGB(0, 0, 180), ColorFromRGB(180, 180, 0), ColorFromRGB(0, 180, 180),
                                    ColorFromRGB(180, 0, 180), ColorFromRGB(240, 144, 96), ColorFromRGB(71, 147, 240), ColorFromRGB(240, 217, 48), ColorFromRGB(60, 240, 227),
                                    ColorFromRGB(114, 87, 242), ColorFromRGB(255, 255, 255), ColorFromRGB(0, 125, 125), ColorFromRGB(125, 0, 125), ColorFromRGB(125, 125, 0)};

        // Values
        [SerializeField] private int probePanelAcronymTextFontSize = 14;
        [SerializeField] private int probePanelAreaTextFontSize = 10;

        // Coord data
        private Vector3 centerOffset = new Vector3(-5.7f, -4.0f, +6.6f);

        // Track who got clicked on, probe, camera, or brain
        private bool probeControl;

        public void SetProbeControl(bool state)
        {
            probeControl = state;
            brainCamController.SetControlBlock(state);
        }

        private bool movedThisFrame;
        private bool spawnedThisFrame = false;

        private int visibleProbePanels;

        Task annotationDatasetLoadTask;

        // Track all input fields
        private TMP_InputField[] _allInputFields;

        #region Ephys Link

        private CommunicationManager _communicationManager;
        private HashSet<int> _rightHandedManipulatorIds = new();

        #endregion

        private void Awake()
        {
            SetProbeControl(false);

            availableCoordinateTransforms = new List<CoordinateTransform>();
            availableCoordinateTransforms.Add(new NeedlesTransform());
            availableCoordinateTransforms.Add(new MRILinearTransform());
            activeCoordinateTransform = null;

            visibleProbePanels = 0;

            allProbeManagers = new List<ProbeManager>();
            allProbeColliders = new List<Collider>();
            inactiveProbeColliders = new List<Collider>();
            rigColliders = new List<Collider>();
            allNonActiveColliders = new List<Collider>();
            meshCenters = new Dictionary<int, Vector3>();
            LoadMeshData();
            //Physics.autoSyncTransforms = true;
        }

        private void Start()
        {
            // Startup CCF
            modelControl.LateStart(true);
            modelControl.SetBeryl(GetSetting_UseBeryl());

            // Set callback
            DelayedModelControlStart();

            // Startup the volume textures
            List<Action> callbacks = new List<Action>();
            callbacks.Add(inPlaneSlice.StartAnnotationDataset);
            annotationDatasetLoadTask = vdmanager.LoadAnnotationDataset(callbacks);

            // After annotation loads, check if the user wants to load previously used probes
            CheckForSavedProbes(annotationDatasetLoadTask);

            // Pull settings from PlayerPrefs
            Debug.Log(localPrefs.GetInplane());
            SetSetting_UseAcronyms(localPrefs.GetAcronyms());
            SetSetting_InPlanePanelVisibility(localPrefs.GetInplane());
            SetSetting_InVivoTransformState(localPrefs.GetStereotaxic());
            SetSetting_UseIBLAngles(localPrefs.GetUseIBLAngles());
            SetSetting_SurfaceDebugSphereVisibility(localPrefs.GetSurfaceCoord());
            _rightHandedManipulatorIds = localPrefs.GetRightHandedManipulatorIds();
        }
        public async void CheckForSavedProbes(Task annotationDatasetLoadTask)
        {
            await annotationDatasetLoadTask;

            if (qDialogue)
            {
                if (UnityEngine.PlayerPrefs.GetInt("probecount", 0) > 0)
                {
                    var questionString = localPrefs.IsLinkDataExpired()
                        ? "Load previously saved probes?"
                        : "Restore previous session?";
                    
                    qDialogue.NewQuestion(questionString);
                    qDialogue.SetYesCallback(this.LoadSavedProbes);
                }
            }
        }

        private void LoadSavedProbes()
        {
            var savedProbes = localPrefs.LoadSavedProbes();

            foreach (var savedProbe in savedProbes)
            {
                var probeInsertion = new ProbeInsertion(savedProbe.tipPos, savedProbe.angles);
                AddNewProbeCCF(savedProbe.type, probeInsertion,
                    savedProbe.manipulatorId, savedProbe.zeroCoordinateOffset, savedProbe.brainSurfaceOffset,
                    savedProbe.dropToSurfaceWithDepth);
            }
        }

        public Task GetAnnotationDatasetLoadedTask()
        {
            return annotationDatasetLoadTask;
        }

        private async void DelayedModelControlStart()
        {
            await modelControl.GetDefaultLoadedTask();

            foreach (CCFTreeNode node in modelControl.GetDefaultLoadedNodes())
            {
                node.SetNodeModelVisibility(true);
                Transform nodeT = node.GetNodeTransform();
                // I don't know why this has to happen, somewhere these are getting set incorrectly?
                nodeT.localPosition = Vector3.zero;
                nodeT.localRotation = Quaternion.identity;
            }
        }

        /// <summary>
        /// Transform a coordinate from the active transform space back to CCF space
        /// </summary>
        /// <param name="fromCoord"></param>
        /// <returns></returns>
        public Vector3 CoordinateTransformToCCF(Vector3 fromCoord)
        {
            if (activeCoordinateTransform != null)
                return activeCoordinateTransform.ToCCF(fromCoord);
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
            if (activeCoordinateTransform != null)
                return activeCoordinateTransform.FromCCF(ccfCoord);
            else
                return ccfCoord;
        }

        public CoordinateTransform GetActiveCoordinateTransform()
        {
            return activeCoordinateTransform;
        }

        public void ClickSearchArea(GameObject target)
        {
            searchControl.ClickArea(target);
        }

        public void TargetSearchArea(int id)
        {
            searchControl.ClickArea(id);
        }

        public TP_InPlaneSlice GetInPlaneSlice()
        {
            return inPlaneSlice;
        }
        
        public TP_ProbeQuickSettings GetProbeQuickSettings()
        {
            return probeQuickSettings;
        }

        public Collider CCFCollider()
        {
            return ccfCollider;
        }

        public int ProbePanelTextFS(bool acronym)
        {
            return acronym ? probePanelAcronymTextFontSize : probePanelAreaTextFontSize;
        }

        public Vector3 GetCenterOffset()
        {
            return centerOffset;
        }

        public AnnotationDataset GetAnnotationDataset()
        {
            return vdmanager.GetAnnotationDataset();
        }

        public Task<bool> LoadIBLCoverageDataset()
        {
            return vdmanager.LoadIBLCoverage();
        }

        public VolumetricDataset GetIBLCoverageDataset()
        {
            return vdmanager.GetIBLCoverageDataset();
        }

        public int GetActiveProbeType()
        {
            return activeProbeController.GetProbeType();
        }

        public bool IsManipulatorRightHanded(int manipulatorId)
        {
            return _rightHandedManipulatorIds.Contains(manipulatorId);
        }
        
        public void AddRightHandedManipulator(int manipulatorId)
        {
            _rightHandedManipulatorIds.Add(manipulatorId);
            localPrefs.SaveRightHandedManipulatorIds(_rightHandedManipulatorIds);
        }
        
        public void RemoveRightHandedManipulator(int manipulatorId)
        {
            if (!IsManipulatorRightHanded(manipulatorId)) return;
            _rightHandedManipulatorIds.Remove(manipulatorId);
            localPrefs.SaveRightHandedManipulatorIds(_rightHandedManipulatorIds);
        }

        // Update is called once per frame
        void Update()
        {
            movedThisFrame = false;

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

            if (Input.GetKeyDown(KeyCode.H) && !InputsFocused())
                settingsPanel.ToggleSettingsMenu();

            if (Input.anyKey && activeProbeController != null && !InputsFocused())
            {
                if (Input.GetKeyDown(KeyCode.Backspace) && !CanvasParent.GetComponentsInChildren<TMP_InputField>()
                        .Any(inputField => inputField.isFocused))
                {
                    DestroyActiveProbeController();
                    return;
                }

                // Check if mouse buttons are down, or if probe is under manual control
                if (!Input.GetMouseButton(0) && !Input.GetMouseButton(2) && !probeControl)
                {
                    movedThisFrame = localPrefs.GetCollisions() ? activeProbeController.MoveProbe(true) : activeProbeController.MoveProbe(false);
                }

                if (movedThisFrame)
                    inPlaneSlice.UpdateInPlaneSlice();
            }

            // TEST CODE: Debugging distance of mesh nodes from camera, trying to fix model "pop"
            //List<CCFTreeNode> defaultLoadedNodes = modelControl.GetDefaultLoadedNodes();
            //if (defaultLoadedNodes.Count > 0)
            //{
            //    Camera brainCamera = brainCamController.GetCamera();
            //    Debug.Log(Vector3.Distance(brainCamera.transform.position, defaultLoadedNodes[0].GetMeshCenter()));
            //}
        }

        public bool InputsFocused()
        {
            return searchInput.isFocused || probeQuickSettings.IsFocused();
        }

        public List<ProbeManager> GetAllProbes()
        {
            return allProbeManagers;
        }

        public List<Collider> GetAllNonActiveColliders()
        {
            return allNonActiveColliders;
        }

        public bool GetCollisions()
        {
            return localPrefs.GetCollisions();
        }

        // DESTROY AND REPLACE PROBES

        //[TODO] Replace this with some system that handles recovering probes by tracking their coordinate system or something?
        // Or maybe the probe coordinates should be an object that can be serialized?
        private ProbeInsertion prevInsertion;
        private int prevProbeType;
        private int _prevManipulatorId;
        private Vector4 _prevZeroCoordinateOffset;
        private float _prevBrainSurfaceOffset;

        private void DestroyActiveProbeController()
        {
            prevProbeType = activeProbeController.GetProbeType();
            prevInsertion = activeProbeController.GetProbeController().GetInsertion();
            _prevManipulatorId = activeProbeController.GetManipulatorId();
            _prevZeroCoordinateOffset = activeProbeController.GetZeroCoordinateOffset();
            _prevBrainSurfaceOffset = activeProbeController.GetBrainSurfaceOffset();
            List<Collider> probeColliders = activeProbeController.GetProbeColliders();

            Debug.Log("Destroying probe type " + prevProbeType + " with coordinates");

            Color returnColor = activeProbeController.GetColor();


            activeProbeController.Destroy();
            Destroy(activeProbeController.gameObject);
            allProbeManagers.Remove(activeProbeController);
            if (allProbeManagers.Count > 0)
                SetActiveProbe(allProbeManagers[allProbeManagers.Count - 1]);
            else
            {
                activeProbeController = null;
                probeQuickSettings.UpdateInteractable(true);
            }

            // remove colliders
            UpdateProbeColliders();

            if (activeProbeController != null)
                activeProbeController.CheckCollisions(GetAllNonActiveColliders());

            ReturnProbeColor(returnColor);

            // Unregister manipulator probe is attached to
            if (_prevManipulatorId != 0) _communicationManager.UnregisterManipulator(_prevManipulatorId);
        }

        private void RecoverActiveProbeController()
        {
            AddNewProbe(prevProbeType, prevInsertion, _prevManipulatorId, _prevZeroCoordinateOffset, _prevBrainSurfaceOffset);
        }

        public void ManualCoordinateEntryTransformed(float ap, float ml, float dv, float phi, float theta, float spin, float depth = 0f)
        {
            activeProbeController.GetProbeController().SetProbeInsertionTransformed(ap, ml, dv, phi, theta, spin, depth);
        }

        public void AddIBLProbes()
        {
            // Add two probes to the scene, one coming from the left and one coming from the right
            StartCoroutine(DelayedIBLProbeAdd(-90, -45, 0f));
            StartCoroutine(DelayedIBLProbeAdd(90, -45, 0.2f));
        }

        IEnumerator DelayedIBLProbeAdd(float phi, float theta, float delay)
        {
            yield return new WaitForSeconds(delay);
            AddNewProbe(1);
            yield return new WaitForSeconds(0.05f);
            activeProbeController.GetProbeController().SetProbeInsertionTransformed(5.4f, 5.7f, 0.332f, phi, theta, 0f);
        }

        IEnumerator DelayedMoveAllProbes()
        {
            yield return new WaitForSeconds(0.05f);
            movedThisFrame = true;
            MoveAllProbes();
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
            if (visibleProbePanels >= 16)
                return null;

            GameObject newProbe = Instantiate(probePrefabs[probePrefabIDs.FindIndex(x => x == probeType)], brainModel);
            SetActiveProbe(newProbe.GetComponent<ProbeManager>());

            RecalculateProbePanels();

            spawnedThisFrame = true;
            StartCoroutine(DelayedMoveAllProbes());

            return newProbe.GetComponent<ProbeManager>();
        }
        
        public ProbeManager AddNewProbeCCF(int probeType, ProbeInsertion insertion,
            int manipulatorId, Vector4 zeroCoordinateOffset, float brainSurfaceOffset, bool dropToSurfaceWithDepth)
        {
            ProbeManager probeController = AddNewProbe(probeType);
            if (!localPrefs.IsLinkDataExpired())
            {
                probeController.SetZeroCoordinateOffset(zeroCoordinateOffset);
                probeController.SetBrainSurfaceOffset(brainSurfaceOffset);
                probeController.SetDropToSurfaceWithDepth(dropToSurfaceWithDepth);
                if (manipulatorId != 0) probeController.SetEphysLinkMovement(true, manipulatorId);
            }

            StartCoroutine(probeController.GetProbeController().SetProbePositionCCF_Delayed(insertion));

            return probeController;
        }

        public ProbeManager AddNewProbeTransformed(int probeType, float ap, float ml, float dv, float phi, float theta, float spin,
            int manipulatorId, Vector4 zeroCoordinateOffset, float brainSurfaceOffset, bool dropToSurfaceWithDepth)
        {
            ProbeManager probeController = AddNewProbe(probeType);
            if (!localPrefs.IsLinkDataExpired())
            {
                probeController.SetZeroCoordinateOffset(zeroCoordinateOffset);
                probeController.SetBrainSurfaceOffset(brainSurfaceOffset);
                probeController.SetDropToSurfaceWithDepth(dropToSurfaceWithDepth);
                if (manipulatorId != 0) probeController.SetEphysLinkMovement(true, manipulatorId);
            }
            
            StartCoroutine(probeController.GetProbeController().SetProbeInsertionTransformed_Delayed(ap, ml, dv, phi, theta, spin, 0.05f));

            return probeController;
        }

        public ProbeManager AddNewProbe(int probeType, ProbeInsertion localInsertion, int manipulatorId = 0,
            Vector4 zeroCoordinateOffset = new Vector4(), float brainSurfaceOffset = 0)
        {
            ProbeManager probeController = AddNewProbe(probeType);
            if (manipulatorId == 0)
            {
                StartCoroutine(probeController.GetProbeController().SetProbeInsertionTransformed_Delayed(
                    localInsertion.ap, localInsertion.ml, localInsertion.dv, 
                    localInsertion.phi, localInsertion.theta, localInsertion.spin,
                    0.05f));
            }
            else
            {
                probeController.SetZeroCoordinateOffset(zeroCoordinateOffset);
                probeController.SetBrainSurfaceOffset(brainSurfaceOffset);
                probeController.SetEphysLinkMovement(true, manipulatorId);
            }

            return probeController;
        }

        #endregion

        private void CountProbePanels()
        {
            visibleProbePanels = 0;
            foreach (ProbeManager probeManager in allProbeManagers)
                visibleProbePanels += probeManager.GetProbeUIManagers().Count;
        }

        private void RecalculateProbePanels()
        {
            CountProbePanels();

            if (visibleProbePanels > 8)
            {
                // Increase the layout to have 8 columns and two rows
                GameObject.Find("ProbePanelParent").GetComponent<GridLayoutGroup>().constraintCount = 8;
            }
            else if (visibleProbePanels > 4)
            {
                // Increase the layout to have two rows, by shrinking all the ProbePanel objects to be 500 pixels tall
                GridLayoutGroup probePanelParent = GameObject.Find("ProbePanelParent").GetComponent<GridLayoutGroup>();
                Vector2 cellSize = probePanelParent.cellSize;
                cellSize.y = 700;
                probePanelParent.cellSize = cellSize;

                // now resize all existing probeUIs to be 700 tall
                foreach (ProbeManager probeController in allProbeManagers)
                {
                    probeController.ResizeProbePanel(700);
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

                foreach (ProbeManager probeController in allProbeManagers)
                {
                    probeController.ResizeProbePanel(1400);
                }
            }

            // Finally, re-order panels if needed to put 2.4 probes first followed by 1.0 / 2.0
            ReOrderProbePanels();
        }

        public void RegisterProbe(ProbeManager probeController)
        {
            Debug.Log("Registering probe: " + probeController.gameObject.name);
            allProbeManagers.Add(probeController);
            probeController.RegisterProbeCallback(allProbeManagers.Count, NextProbeColor());
            UpdateProbeColliders();
        }

        private Color NextProbeColor()
        {
            Color next = probeColors[0];
            probeColors.RemoveAt(0);
            return next;
        }

        public Material GetCollisionMaterial()
        {
            return collisionMaterial;
        }

        public void ReturnProbeColor(Color returnColor)
        {
            probeColors.Insert(0, returnColor);
        }

        public void SetActiveProbe(ProbeManager newActiveProbeController)
        {
            if (activeProbeController == newActiveProbeController)
                return;

            Debug.Log("Setting active probe to: " + newActiveProbeController.gameObject.name);
            activeProbeController = newActiveProbeController;

            foreach (ProbeManager probeManager in allProbeManagers)
            {
                // Check visibility
                bool isActiveProbe = probeManager == activeProbeController;
                if (GetSetting_ShowAllProbePanels())
                    probeManager.SetUIVisibility(true);
                else
                    probeManager.SetUIVisibility(isActiveProbe);

                // Set active state for UI managers
                foreach (ProbeUIManager puimanager in probeManager.GetProbeUIManagers())
                    puimanager.ProbeSelected(isActiveProbe);
            }

            // Change the height of the probe panels, if needed
            RecalculateProbePanels();

            UpdateProbeColliders();

            // Also update the recording region size slider
            recRegionSlider.SliderValueChanged(activeProbeController.GetProbeController().GetRecordingRegionSize());

            // Reset the inplane slice zoom factor
            inPlaneSlice.ResetZoom();
            
            // Update probe quick settings
            probeQuickSettings.SetProbeManager(newActiveProbeController);
        }

        public void UpdateQuickSettings()
        {
            probeQuickSettings.UpdateInteractable();
            probeQuickSettings.UpdateCoordinates();
        }

        public void ResetActiveProbe()
        {
            if (activeProbeController != null)
                activeProbeController.GetProbeController().ResetInsertion();
        }

        public Color GetProbeColor(int probeID)
        {
            return probeColors[probeID];
        }

        public ProbeManager GetActiveProbeController()
        {
            return activeProbeController;
        }

        public bool MovedThisFrame()
        {
            return movedThisFrame;
        }

        public void SetMovedThisFrame()
        {
            movedThisFrame = true;
        }

        public void UpdateInPlaneView()
        {
            inPlaneSlice.UpdateInPlaneSlice();
        }

        #region COLLIDERS

        public void UpdateProbeColliders()
        {
            // Collect *all* colliders from all probes
            allProbeColliders.Clear();
            foreach (ProbeManager probeManager in allProbeManagers)
            {
                foreach (Collider collider in probeManager.GetProbeColliders())
                    allProbeColliders.Add(collider);
            }

            // Sort out which colliders are active vs inactive
            inactiveProbeColliders.Clear();

            List<Collider> activeProbeColliders = (activeProbeController != null) ?
                activeProbeController.GetProbeColliders() :
                new List<Collider>();

            foreach (Collider collider in allProbeColliders)
                if (!activeProbeColliders.Contains(collider))
                    inactiveProbeColliders.Add(collider);

            // Re-build the list of inactive colliders (which includes both probe + rig colliders)
            UpdateNonActiveColliders();
        }

        public void UpdateRigColliders(IEnumerable<Collider> newRigColliders, bool keep)
        {
            if (keep)
                foreach (Collider collider in newRigColliders)
                    rigColliders.Add(collider);
            else
                foreach (Collider collider in newRigColliders)
                    rigColliders.Remove(collider);
            UpdateNonActiveColliders();
        }


        private void UpdateNonActiveColliders()
        {
            allNonActiveColliders.Clear();
            foreach (Collider collider in inactiveProbeColliders)
                allNonActiveColliders.Add(collider);
            foreach (Collider collider in rigColliders)
                allNonActiveColliders.Add(collider);
        }

        #endregion

        private void MoveAllProbes()
        {
            foreach (ProbeManager probeController in allProbeManagers)
                foreach (ProbeUIManager puimanager in probeController.GetComponents<ProbeUIManager>())
                    puimanager.ProbeMoved();
            UpdateInPlaneView();
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
                foreach (TMP_Text textC in whiteUIText)
                    textC.color = Color.black;
                Camera.main.backgroundColor = Color.white;
            }
            else
            {
                foreach (TMP_Text textC in whiteUIText)
                    textC.color = Color.white;
                Camera.main.backgroundColor = Color.black;
            }
        }

        #region Player Preferences
        
        public void SetSetting_UseBeryl(bool state)
        {
            localPrefs.SetUseBeryl(state);

            foreach (ProbeManager probeController in allProbeManagers)
                foreach (ProbeUIManager puimanager in probeController.GetComponents<ProbeUIManager>())
                    puimanager.ProbeMoved();
        }

        public bool GetSetting_UseBeryl()
        {
            return localPrefs.GetUseBeryl();
        }
        
        public void SetSetting_ShowAllProbePanels(bool state)
        {
            localPrefs.SetShowAllProbePanels(state);
            if (state)
                foreach (ProbeManager probeManager in allProbeManagers)
                    probeManager.SetUIVisibility(true);
            else
                foreach (ProbeManager probeManager in allProbeManagers)
                    probeManager.SetUIVisibility(activeProbeController == probeManager);

            RecalculateProbePanels();
        }

        public bool GetSetting_ShowAllProbePanels()
        {
            return localPrefs.GetShowAllProbePanels();
        }

        public void SetSetting_ShowRecRegionOnly(bool state)
        {
            localPrefs.SetRecordingRegionOnly(state);
            MoveAllProbes();
        }

        public bool GetSetting_ShowRecRegionOnly()
        {
            return localPrefs.GetRecordingRegionOnly();
        }

        public void SetSetting_UseAcronyms(bool state)
        {
            localPrefs.SetAcronyms(state);
            searchControl.RefreshSearchWindow();
            // move probes to update state
            MoveAllProbes();
        }

        public bool GetSetting_UseAcronyms()
        {
            return localPrefs.GetAcronyms();
        }

        public void SetSetting_UseIBLAngles(bool state)
        {
            localPrefs.SetUseIBLAngles(state);
            UpdateQuickSettings();
        }

        public bool GetSetting_UseIBLAngles()
        {
            return localPrefs.GetUseIBLAngles();
        }


        public void SetSetting_GetDepthFromBrain(bool state)
        {
            localPrefs.SetDepthFromBrain(state);
            UpdateQuickSettings();
        }
        public bool GetSetting_GetDepthFromBrain()
        {
            return localPrefs.GetDepthFromBrain();
        }

        public void SetSetting_ConvertAPMLAxis2Probe(bool state)
        {
            localPrefs.SetAPML2ProbeAxis(state);
            UpdateQuickSettings();
        }

        public bool GetSetting_ConvertAPMLAxis2Probe()
        {
            return localPrefs.GetAPML2ProbeAxis();
        }

        public void SetSetting_InVivoTransformState(int invivoOption)
        {
            localPrefs.SetStereotaxic(invivoOption);
            invivoOption -= 1;

            if (invivoOption >= 0)
            {
                Debug.Log("(tpmanager) Attempting to set transform to: " + availableCoordinateTransforms[invivoOption].Name);
                activeCoordinateTransform = availableCoordinateTransforms[invivoOption];
            }
            else
                activeCoordinateTransform = null;

            MoveAllProbes();
        }

        public bool GetSetting_InVivoTransformActive()
        {
            return localPrefs.GetStereotaxic() > 0;
        }

        public void SetSetting_SurfaceDebugSphereVisibility(bool state)
        {
            localPrefs.SetSurfaceCoord(state);
            SetSurfaceDebugActive(state);
        }

        public void SetSetting_CollisionInfoVisibility(bool toggleCollisions)
        {
            localPrefs.SetCollisions(toggleCollisions);
            if (activeProbeController != null)
                activeProbeController.CheckCollisions(GetAllNonActiveColliders());
        }

        public void SetSetting_InPlanePanelVisibility(bool state)
        {
            localPrefs.SetInplane(state);
            inPlaneSlice.UpdateInPlaneVisibility();
        }

        public void SetSetting_InVivoMeshMorphState(bool state)
        {
            if (state)
            {
                Debug.LogWarning("not implemented");
            }
            else
            {
                Debug.LogWarning("not implemented");
            }
        }

        #endregion

        #region Setting Helper Functions


        public void SetSurfaceDebugActive(bool active)
        {
            if (localPrefs.GetSurfaceCoord() && activeProbeController != null)
                surfaceDebugGO.SetActive(active);
            else
                surfaceDebugGO.SetActive(false);
        }

        public void SetCollisionPanelVisibility(bool visibility)
        {
            CollisionPanelGO.SetActive(visibility);
        }

        public string GetInVivoPrefix()
        {
            return activeCoordinateTransform.Prefix;
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
            foreach (ProbeManager pcontroller in allProbeManagers)
                if (pcontroller.GetProbeType() == 4)
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
            IBLToolsGO.SetActive(state);
        }

        public void SetSurfaceDebugPosition(Vector3 worldPosition)
        {
            surfaceDebugGO.transform.position = worldPosition;
        }

        private void OnApplicationQuit()
        {
            var probeCoordinates =
                new (float ap, float ml, float dv, float phi, float theta, float spin, int type, int manipulatorId,
                    Vector4
                    zeroCoordinateOffset, float brainSurfaceOffset, bool dropToSurfaceWithDepth)[allProbeManagers
                        .Count];

            for (int i =0; i< allProbeManagers.Count; i++)
            {
                ProbeManager probe = allProbeManagers[i];
                ProbeInsertion probeInsertion = probe.GetProbeController().GetInsertion();
                probeCoordinates[i] = (probeInsertion.ap, probeInsertion.ml, probeInsertion.dv, probeInsertion.phi, probeInsertion.theta, probeInsertion.spin, probe.GetProbeType(), probe.GetManipulatorId(),
                    probe.GetZeroCoordinateOffset(), probe.GetBrainSurfaceOffset(),
                    probe.IsSetToDropToSurfaceWithDepth());
            }
            localPrefs.SaveCurrentProbeData(probeCoordinates);
        }


        #region Axis Control

        public bool GetAxisControlEnabled()
        {
            return localPrefs.GetAxisControl();
        }

        public void SetAxisControlEnabled(bool state)
        {
            localPrefs.SetAxisControl(state);
            if (!state)
                SetAxisVisibility(false, false, false, false, null);
        }

        public void SetAxisVisibility(bool AP, bool ML, bool DV, bool depth, Transform transform)
        {
            if (GetAxisControlEnabled())
            {
                acontrol.SetAxisPosition(transform);
                acontrol.SetAPVisibility(AP);
                acontrol.SetMLVisibility(ML);
                acontrol.SetDVVisibility(DV);
                acontrol.SetDepthVisibility(depth);
            }
        }

        #endregion

        #region Mesh centers

        private int prevTipID;
        private bool prevTipSideLeft;

        private void LoadMeshData()
        {
            List<Dictionary<string,object>> data = CSVReader.ParseText(meshCenterText.text);

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
            if (activeProbeController == null) return;
            int berylID = modelControl.GetBerylID(targetNode.ID);
            Vector3 apmldv = meshCenters[berylID];

            if (berylID==prevTipID && prevTipSideLeft)
            {
                // we already hit this area, switch sides
                    activeProbeController.GetProbeController().SetProbePosition(new Vector3(apmldv.x, 11.4f-apmldv.y, apmldv.z));
                prevTipSideLeft = false;
            }
            else
            {
                // first time, go left
                activeProbeController.GetProbeController().SetProbePosition(apmldv);
                prevTipSideLeft = true;
            }

            prevTipID = berylID;
        }

        #endregion

        #region Text

        public void CopyText()
        {
            activeProbeController.Probe2Text();
        }

        #endregion
    }

}
