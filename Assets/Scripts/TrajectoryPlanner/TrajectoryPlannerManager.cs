using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using SensapexLink;
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

        [SerializeField] private GameObject CollisionPanelGO;
        [SerializeField] private Material collisionMaterial;

        [SerializeField] private GameObject ProbePanelParentGO;
        [SerializeField] private GameObject IBLToolsGO;
        [SerializeField] private GameObject IBLTrajectoryGO;
        [SerializeField] private BrainCameraController brainCamController;

        // UI 
        [SerializeField] TP_QuestionDialogue qDialogue;

        // Debug graphics
        [SerializeField] private GameObject surfaceDebugGO;

        // Text objects that need to stay visible when the background changes
        [SerializeField] private List<TMP_Text> whiteUIText;

        // Which acronym/area name set to use
        [SerializeField] TMP_Dropdown annotationAcronymDropdown;

        // Coordinate system information
        private CoordinateTransform activeCoordinateTransform;
        private List<CoordinateTransform> availableCoordinateTransforms;

        // Local tracking variables
        private ProbeManager activeProbeController;
        private List<ProbeManager> allProbes;
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

        // Manual coordinate entry
        [SerializeField] private TP_CoordinateEntryPanel manualCoordinatePanel;

        // Track who got clicked on, probe, camera, or brain
        private bool probeControl;

        public void SetProbeControl(bool state)
        {
            probeControl = state;
            brainCamController.SetControlBlock(state);
        }

        // Track when brain areas get clicked on
        private List<int> targetedBrainAreas;

        private bool movedThisFrame;
        private bool spawnedThisFrame = false;

        private int visibleProbePanels;

        Task annotationDatasetLoadTask;

        #region Sensapex Link

        private CommunicationManager _communicationManager;

        #endregion

        private void Awake()
        {
            SetProbeControl(false);

            availableCoordinateTransforms = new List<CoordinateTransform>();
            availableCoordinateTransforms.Add(new NeedlesTransform());
            availableCoordinateTransforms.Add(new MRILinearTransform());
            activeCoordinateTransform = null;

            visibleProbePanels = 0;

            allProbes = new List<ProbeManager>();
            allProbeColliders = new List<Collider>();
            inactiveProbeColliders = new List<Collider>();
            rigColliders = new List<Collider>();
            allNonActiveColliders = new List<Collider>();
            targetedBrainAreas = new List<int>();
            //Physics.autoSyncTransforms = true;

            _communicationManager = GameObject.Find("SensapexLink").GetComponent<CommunicationManager>();
        }

        private void Start()
        {

            // Startup CCF
            modelControl.LateStart(true);

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
            SetAcronyms(localPrefs.GetAcronyms());
            SetInPlane(localPrefs.GetInplane());
            SetInVivoTransformState(localPrefs.GetStereotaxic());
            SetUseIBLAngles(localPrefs.GetUseIBLAngles());
            SetSurfaceDebugVisibility(localPrefs.GetSurfaceCoord());
        }
        public async void CheckForSavedProbes(Task annotationDatasetLoadTask)
        {
            await annotationDatasetLoadTask;

            if (qDialogue)
            {
                if (UnityEngine.PlayerPrefs.GetInt("probecount", 0) > 0)
                {
                    qDialogue.NewQuestion("Load previously saved probes?");
                    qDialogue.SetYesCallback(this.LoadSavedProbes);
                }
            }
        }

        private void LoadSavedProbes()
        {
            (Vector3 tipPos, float depth, Vector3 angles, int type)[] savedProbes = localPrefs.LoadSavedProbes();

            foreach (var savedProbe in savedProbes)
            {
                Vector3 tipPos = savedProbe.tipPos;
                Vector3 angles = savedProbe.angles;
                AddNewProbe(savedProbe.type, tipPos.x, tipPos.y, tipPos.z, angles.x, angles.y, angles.z);
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

        public TP_InPlaneSlice GetInPlaneSlice()
        {
            return inPlaneSlice;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public void ToggleBeryl(int value)
        {
            switch (value)
            {
                case 0:
                    modelControl.SetBeryl(false);
                    break;
                case 1:
                    modelControl.SetBeryl(true);
                    break;
                default:
                    modelControl.SetBeryl(false);
                    break;
            }
            foreach (ProbeManager probeController in allProbes)
                foreach (ProbeUIManager puimanager in probeController.GetComponents<ProbeUIManager>())
                    puimanager.ProbeMoved();
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

        // Update is called once per frame
        void Update()
        {
            movedThisFrame = false;

            if (spawnedThisFrame)
            {
                spawnedThisFrame = false;
                return;
            }

            if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C)) && Input.GetKeyDown(KeyCode.Backspace) && !manualCoordinatePanel.gameObject.activeSelf)
            {
                RecoverActiveProbeController();
                return;
            }

            if (Input.anyKey && activeProbeController != null && !searchInput.isFocused)
            {
                if (Input.GetKeyDown(KeyCode.Backspace) && !manualCoordinatePanel.gameObject.activeSelf)
                {
                    DestroyActiveProbeController();
                    return;
                }

                if (Input.GetKeyDown(KeyCode.M))
                {
                    manualCoordinatePanel.gameObject.SetActive(!manualCoordinatePanel.gameObject.activeSelf);
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

        public List<ProbeManager> GetAllProbes()
        {
            return allProbes;
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
        private Vector4 _prevBregmaOffset;
        private float _prevBrainSurfaceOffset;

        private void DestroyActiveProbeController()
        {
            prevProbeType = activeProbeController.GetProbeType();
            prevInsertion = activeProbeController.GetInsertion();
            _prevManipulatorId = activeProbeController.GetManipulatorId();
            _prevBregmaOffset = activeProbeController.GetBregmaOffset();
            _prevBrainSurfaceOffset = activeProbeController.GetBrainSurfaceOffset();
            List<Collider> probeColliders = activeProbeController.GetProbeColliders();

            Debug.Log("Destroying probe type " + prevProbeType + " with coordinates");

            Color returnColor = activeProbeController.GetColor();


            activeProbeController.Destroy();
            Destroy(activeProbeController.gameObject);
            allProbes.Remove(activeProbeController);
            if (allProbes.Count > 0)
                SetActiveProbe(allProbes[allProbes.Count - 1]);
            else
                activeProbeController = null;

            // remove colliders
            UpdateProbeColliders();

            ReturnProbeColor(returnColor);

            // Unregister manipulator probe is attached to
            if (_prevManipulatorId != 0) _communicationManager.UnregisterManipulator(_prevManipulatorId);
        }

        private void RecoverActiveProbeController()
        {
            AddNewProbe(prevProbeType, prevInsertion, _prevManipulatorId, _prevBregmaOffset, _prevBrainSurfaceOffset);
        }

        public void ManualCoordinateEntry(float ap, float ml, float dv, float phi, float theta, float spin)
        {
            activeProbeController.ManualCoordinateEntryTransformed(ap, ml, dv, phi, theta, spin);
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
            activeProbeController.SetProbePositionCCF(new ProbeInsertion(5.4f, 5.7f, 0.332f, phi, theta, 0));
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
        public ProbeManager AddNewProbe(int probeType)
        {
            CountProbePanels();
            if (visibleProbePanels >= 16)
                return null;

            GameObject newProbe = Instantiate(probePrefabs[probePrefabIDs.FindIndex(x => x == probeType)], brainModel);
            SetActiveProbe(newProbe.GetComponent<ProbeManager>());
            if (visibleProbePanels > 4)
                activeProbeController.ResizeProbePanel(700);

            RecalculateProbePanels();

            spawnedThisFrame = true;
            StartCoroutine(DelayedMoveAllProbes());

            return newProbe.GetComponent<ProbeManager>();
        }
        public ProbeManager AddNewProbe(int probeType, float ap, float ml, float dv, float phi, float theta, float spin)
        {
            ProbeManager probeController = AddNewProbe(probeType);
            StartCoroutine(probeController.DelayedManualCoordinateEntryTransformed(0.1f, ap, ml, dv, phi, theta, spin));

            return probeController;
        }

        public ProbeManager AddNewProbe(int probeType, ProbeInsertion localInsertion, int manipulatorId = 0,
            Vector4 bregmaOffset = new Vector4(), float brainSurfaceOffset = 0)
        {
            ProbeManager probeController = AddNewProbe(probeType);
            if (manipulatorId == 0)
            {
                StartCoroutine(probeController.DelayedManualCoordinateEntryTransformed(0.1f, localInsertion.ap,
                    localInsertion.ml, localInsertion.dv, localInsertion.phi,
                    localInsertion.theta, localInsertion.spin));
            }
            else
            {
                probeController.SetBregmaOffset(bregmaOffset);
                probeController.SetBrainSurfaceOffset(brainSurfaceOffset);
                probeController.SetSensapexLinkMovement(true, manipulatorId);
            }

            return probeController;
        }

        #endregion

        private void CountProbePanels()
        {
            visibleProbePanels = GameObject.FindGameObjectsWithTag("ProbePanel").Length;
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
                foreach (ProbeManager probeController in allProbes)
                {
                    probeController.ResizeProbePanel(700);
                }
            }

            // Finally, re-order panels if needed to put 2.4 probes first followed by 1.0 / 2.0
            ReOrderProbePanels();
        }

        public void RegisterProbe(ProbeManager probeController)
        {
            Debug.Log("Registering probe: " + probeController.gameObject.name);
            allProbes.Add(probeController);
            probeController.RegisterProbeCallback(allProbes.Count, NextProbeColor());
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

            foreach (ProbeUIManager puimanager in activeProbeController.gameObject.GetComponents<ProbeUIManager>())
                puimanager.ProbeSelected(true);

            foreach (ProbeManager pcontroller in allProbes)
                if (pcontroller != activeProbeController)
                    foreach (ProbeUIManager puimanager in pcontroller.gameObject.GetComponents<ProbeUIManager>())
                        puimanager.ProbeSelected(false);

            UpdateProbeColliders();

            // Also update the recording region size slider
            recRegionSlider.SliderValueChanged(activeProbeController.GetRecordingRegionSize());

            // Reset the inplane slice zoom factor
            inPlaneSlice.ResetZoom();
            
            // Update probe quick settings
            probeQuickSettings.SetProbeManager(newActiveProbeController);
        }

        public void ResetActiveProbe()
        {
            if (activeProbeController != null)
                activeProbeController.ResetPosition();
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
            foreach (ProbeManager probeManager in allProbes)
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
            foreach (ProbeManager probeController in allProbes)
                foreach (ProbeUIManager puimanager in probeController.GetComponents<ProbeUIManager>())
                    puimanager.ProbeMoved();
        }


        public void SelectBrainArea(int id)
        {
            if (targetedBrainAreas.Contains(id))
            {
                ClearTargetedBrainArea(id);
                targetedBrainAreas.Remove(id);
            }
            else
            {
                TargetBrainArea(id);
                targetedBrainAreas.Add(id);
            }
        }

        private async void TargetBrainArea(int id)
        {
            CCFTreeNode node = modelControl.GetNode(id);
            if (!node.IsLoaded())
            {
                await node.loadNodeModel(false);
                node.GetNodeTransform().localPosition = Vector3.zero;
                node.GetNodeTransform().localRotation = Quaternion.identity;
                modelControl.ChangeMaterial(id, "lit");
            }

            if (modelControl.InDefaults(id))
                modelControl.ChangeMaterial(id, "lit");
            else
                node.SetNodeModelVisibility(true);
        }

        private void ClearTargetedBrainArea(int id)
        {
            if (modelControl.InDefaults(id))
                modelControl.ChangeMaterial(id, "default");
            else
                modelControl.GetNode(id).SetNodeModelVisibility(false);
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

        public void SetRecordingRegion(bool state)
        {
            localPrefs.SetRecordingRegionOnly(state);
            MoveAllProbes();
        }

        public bool RecordingRegionOnly()
        {
            return localPrefs.GetRecordingRegionOnly();
        }

        public void SetAcronyms(bool state)
        {
            localPrefs.SetAcronyms(state);
            searchControl.RefreshSearchWindow();
            // move probes to update state
            MoveAllProbes();
        }

        public bool UseAcronyms()
        {
            return localPrefs.GetAcronyms();
        }

        public void SetUseIBLAngles(bool state)
        {
            localPrefs.SetUseIBLAngles(state);
            foreach (ProbeManager probeController in allProbes)
                probeController.UpdateText();
        }

        public bool UseIBLAngles()
        {
            return localPrefs.GetUseIBLAngles();
        }

        public void SetDepth(bool state)
        {
            localPrefs.SetDepthFromBrain(state);
            foreach (ProbeManager probeController in allProbes)
                probeController.UpdateText();
        }
        public bool GetDepthFromBrain()
        {
            return localPrefs.GetDepthFromBrain();
        }

        public void SetConvertToProbe(bool state)
        {
            localPrefs.SetAPML2ProbeAxis(state);
            foreach (ProbeManager probeController in allProbes)
                probeController.UpdateText();
        }

        public bool GetConvertAPML2Probe()
        {
            return localPrefs.GetAPML2ProbeAxis();
        }

        public void SetCollisions(bool toggleCollisions)
        {
            localPrefs.SetCollisions(toggleCollisions);
            if (activeProbeController != null)
                activeProbeController.CheckCollisions(GetAllNonActiveColliders());
        }

        public void SetCollisionPanelVisibility(bool visibility)
        {
            CollisionPanelGO.SetActive(visibility);
        }

        public void SetInPlane(bool state)
        {
            localPrefs.SetInplane(state);
            inPlaneSlice.UpdateInPlaneVisibility();
        }

        public void SetInVivoMeshMorphState(bool state)
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

        public void SetInVivoTransformState(int invivoOption)
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

            foreach (ProbeManager pcontroller in allProbes)
                pcontroller.UpdateText();
        }

        public bool GetInVivoTransformState()
        {
            return localPrefs.GetStereotaxic() > 0;
        }
        public string GetInVivoPrefix()
        {
            return activeCoordinateTransform.Prefix;
        }

        public void ReOrderProbePanels()
        {
            Debug.Log("Re-ordering probe panels");
            Dictionary<float, ProbeUIManager> sorted = new Dictionary<float, ProbeUIManager>();

            int probeIndex = 0;
            // first, sort probes so that np2.4 probes go first
            List<ProbeManager> np24Probes = new List<ProbeManager>();
            List<ProbeManager> otherProbes = new List<ProbeManager>();
            foreach (ProbeManager pcontroller in allProbes)
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

        public void SetIBLTrajectory(bool state)
        {
            Debug.LogError("NOT IMPLEMENTED");
            //IBLTrajectoryGO.SetActive(state);
            //if (state)
            //    IBLTrajectoryGO.GetComponent<TP_IBLTrajectories>().Load();
        }

        public void SetSurfaceDebugPosition(Vector3 worldPosition)
        {
            surfaceDebugGO.transform.position = worldPosition;
        }

        public void SetSurfaceDebugActive(bool active)
        {
            if (localPrefs.GetSurfaceCoord() && activeProbeController != null)
                surfaceDebugGO.SetActive(active);
            else
                surfaceDebugGO.SetActive(false);
        }

        public void SetSurfaceDebugVisibility(bool state)
        {
            localPrefs.SetSurfaceCoord(state);
            SetSurfaceDebugActive(state);
        }

        public void SetProbeTipPositionToCCFNode(CCFTreeNode targetNode)
        {
            // Not implemented yet
            //Vector3 meshCenterWorld = targetNode.GetMeshCenter();
            //activeProbeController.SetProbePosition()
        }

        private void OnApplicationQuit()
        {
            (float ap, float ml, float dv, float phi, float theta, float spin, int type)[] probeCoordinates = new (float ap, float ml, float dv, float phi, float theta, float spin, int type)[allProbes.Count];

            for (int i =0; i< allProbes.Count; i++)
            {
                ProbeManager probe = allProbes[i];
                (float ap, float ml, float dv, float phi, float theta, float spin) = probe.GetCoordinates();
                probeCoordinates[i] = (ap, ml, dv, phi, theta, spin, probe.GetProbeType());
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
                SetAxisVisibility(false, false, false, Vector3.zero);
        }

        public void SetAxisVisibility(bool AP, bool ML, bool DV, Vector3 pos)
        {
            if (GetAxisControlEnabled())
            {
                acontrol.SetAxisPosition(pos);
                acontrol.SetAPVisibility(AP);
                acontrol.SetMLVisibility(ML);
                acontrol.SetDVVisibility(DV);
            }
        }

        #endregion
    }

}
