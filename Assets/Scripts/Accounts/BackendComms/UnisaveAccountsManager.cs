using System.Collections.Generic;
using UnityEngine;
using Unisave.Facades;
using System;
using UnityEngine.Serialization;
using UnityEngine.Events;
using System.Linq;
using System.Xml.Linq;

/// <summary>
/// Handles connection with the Unisave system, and passing data back-and-forth with the TPManager
/// </summary>
public class UnisaveAccountsManager : AccountsManager
{
    private const float UPDATE_RATE = 60f;

    [FormerlySerializedAs("registerPanelGO")] [SerializeField] private GameObject _registerPanelGo;
    [FormerlySerializedAs("experimentEditor")] [SerializeField] private ExperimentEditor _experimentEditor;
    [FormerlySerializedAs("activeExpListBehavior")] [SerializeField] private ActiveExpListBehavior _activeExpListBehavior;

    [SerializeField] private QuickSettingExpList _quickSettingsExperimentList;

    [SerializeField] private EmailLoginForm _emailLoginForm;

    #region Insertion variables
    [SerializeField] private Transform _insertionPrefabParentT;
    [SerializeField] private GameObject _insertionPrefabGO;
    #endregion

    // callbacks set by TPManager
    #region Events
    public UnityEvent<string> SetActiveProbeEvent;
    public UnityEvent ExperimentChangedEvent;
    public Action<(Vector3 apmldv, Vector3 angles, int type, string spaceName, string transformName, string UUID),bool> UpdateCallbackEvent { get; set; }
    #endregion

    #region current player data
    private PlayerEntity _player;
    public bool Connected { get { return _player != null; } }
    #endregion

    #region tracking variables
    public bool Dirty { get; private set; }
    private float _lastSave;
    public string ActiveExperiment { get; private set; }
    #endregion

    #region Unity
    private void Awake()
    {
        _lastSave = Time.realtimeSinceStartup;
    }

    private void Start()
    {
        _emailLoginForm.AttemptLoginViaTokenAsync();
    }

    private void Update()
    {
        if (Dirty && (Time.realtimeSinceStartup - _lastSave) >= UPDATE_RATE)
        {
            Dirty = false;
            SavePlayer();
        }
    }

    private void OnApplicationQuit()
    {
        SavePlayer();
    }

    #endregion

    #region Login/logout
    public void Login()
    {
        OnFacet<PlayerDataFacet>
            .Call<PlayerEntity>(nameof(PlayerDataFacet.LoadPlayerEntity))
            .Then(LoadPlayerCallback)
            .Done();
    }

    private void LoadPlayerCallback(PlayerEntity player)
    {
        _player = player;

        Debug.Log("(AccountsManager) Player logged in: " + player.email);

        ExperimentChangedEvent.Invoke();
        UpdateExperimentUI();
    }

    public void LogoutCleanup()
    {
        Debug.Log("(AccountsManager) Player logged out");
        _player = null;
        UpdateExperimentUI();
    }

    #endregion

    public void UpdateProbeData()
    {
        string UUID = ProbeManager.ActiveProbeManager.UUID;

        // Check that we are logged in and that this probe is in an experiment
        if (_player != null && _player.UUID2Experiment.ContainsKey(UUID))
        {
            ServerProbeInsertion insertionData = GetInsertion(UUID);

            _player.UUID2InsertionData[UUID] = ProbeManager2ServerProbeInsertion(ProbeManager.ActiveProbeManager, true, insertionData.recorded);

            Dirty = true;
        }
    }

    #region Save and Update

    public void SavePlayer()
    {
        if (_player == null)
            return;
        Debug.Log("(AccountsManager) Saving data");
        OnFacet<PlayerDataFacet>
            .Call(nameof(PlayerDataFacet.SavePlayerEntity), _player).Done();
    }

    private void UpdateExperimentUI()
    {
        _experimentEditor.UpdateList();
        _activeExpListBehavior.UpdateList();
        UpdateExperimentInsertionUIPanels();
        _quickSettingsExperimentList.UpdateExperimentList();
    }

    #endregion


    #region Experiment editor

    public void NewExperiment()
    {
        _player.Experiments.Add($"Experiment {_player.Experiments.Count}");
        // Immediately update, so that user can see effect
        SavePlayer();
        UpdateExperimentUI();
    }

    public void EditExperiment(string origName, string newName)
    {
        if (_player.Experiments.Contains(origName))
        {
            _player.Experiments[_player.Experiments.IndexOf(origName)] = newName;
        }
        else
            Debug.LogError(string.Format("Experiment {0} does not exist", origName));
        // Immediately update, so that user can see effect
        SavePlayer();
        UpdateExperimentUI();
    }

    public void DeleteExperiment(string expName)
    {
        RemoveExperiment(expName);
        // Immediately update, so that user can see effect
        SavePlayer();
        UpdateExperimentUI();
    }

    #endregion

    #region Quick settings panel

    /// <summary>
    /// Add a new experiment to the list of experiments for a particular probe
    /// </summary>
    /// <param name="probeManager"></param>
    /// <param name="experimentName"></param>
    public void AddProbeExperiment(ProbeManager probeManager, string experimentName)
    {
        if (_player.UUID2Experiment.ContainsKey(probeManager.UUID))
        {
            if (_player.UUID2Experiment[probeManager.UUID].Contains(experimentName))
                Debug.Log("Experiment already in list");
            else
                _player.UUID2Experiment[probeManager.UUID].Add(experimentName);
        }

        Dirty = true;
        UpdateExperimentInsertionUIPanels();
    }

    public void RemoveProbeExperiment(ProbeManager probeManager, string experimentName)
    {
        if (_player.UUID2Experiment.ContainsKey(probeManager.UUID))
        {
            if (_player.UUID2Experiment[probeManager.UUID].Contains(experimentName))
                _player.UUID2Experiment[probeManager.UUID].Remove(experimentName);
            else
                Debug.Log("Experiment wasn't in list");
        }

        Dirty = true;
        UpdateExperimentInsertionUIPanels();
    }

    public void DeleteProbe(string UUID)
    {
        _player.UUID2Experiment.Remove(UUID);
        
    }

#endregion

    public void ShowRegisterPanel()
    {
        _registerPanelGo.SetActive(true);
    }

    #region Public helpers
    public List<string> GetExperiments()
    {
        if (_player != null)
            return _player.Experiments;
        else
            return new List<string>();
    }

    #endregion

    public void SaveRigList(List<int> visibleRigParts) {
        _player.VisibleRigParts = visibleRigParts;
    }

    public void ActiveExperimentChanged(string experiment)
    {
#if UNITY_EDITOR
        Debug.Log(string.Format("(AccountsManager) Selected experiment: {0}", experiment));
#endif
        ActiveExperiment = experiment;
        ExperimentChangedEvent.Invoke();
        UpdateExperimentInsertionUIPanels();
    }

    #region Insertions

    public void UpdateExperimentInsertionUIPanels()
    {
        Debug.Log("(AccountsManager) Updating insertions");
        // [TODO: This is inefficient, better to keep prefabs that have been created and just hide extras]

        // Destroy all children
        for (int i = _insertionPrefabParentT.childCount - 1; i >= 0; i--)
            Destroy(_insertionPrefabParentT.GetChild(i).gameObject);

        Debug.Log("All insertion prefabs destroyed");

        // Add new child prefabs that have the properties matched to the current experiment
        var experimentData = GetActiveExperimentInsertions();

        foreach (ServerProbeInsertion insertion in experimentData.Values)
        {
            // Create a new prefab
            GameObject insertionPrefab = Instantiate(_insertionPrefabGO, _insertionPrefabParentT);
            ServerProbeInsertionUI insertionUI = insertionPrefab.GetComponent<ServerProbeInsertionUI>();

            // Insertions should be marked as active if they are in the scene already
            bool active = ProbeManager.instances.Any(x => experimentData.Keys.Contains(x.UUID));

            insertionUI.SetInsertionData(this, insertion.UUID, active);
            if (Settings.DisplayUM)
                insertionUI.UpdateDescription(string.Format("AP {0} ML {1} DV {2} Phi {3} Theta {4} Spin {5}",
                    Mathf.RoundToInt(insertion.ap*1000f), Mathf.RoundToInt(insertion.ml*1000f), Mathf.RoundToInt(insertion.dv*1000f),
                    insertion.phi, insertion.theta, insertion.spin));
            else
                insertionUI.UpdateDescription(string.Format("AP {0:0.00} ML {1:0.00} DV {2:0.00} Phi {3} Theta {4} Spin {5}",
                    insertion.ap, insertion.ml, insertion.dv,
                    insertion.phi, insertion.theta, insertion.spin));
        }
    }
    
    public void ChangeInsertionVisibility(string UUID, bool visible)
    {
#if UNITY_EDITOR
        Debug.Log(string.Format("(Accounts) Insertion {0} wants to become {1}", UUID, visible));
#endif
        ServerProbeInsertion insertion = GetInsertion(UUID);
        UpdateCallbackEvent(GetProbeInsertionData(UUID), visible);
    }

    public void ProbeDestroyInScene(string UUID)
    {
        // Find the UI object and disable the active state
#if UNITY_EDITOR
        Debug.Log($"(Accounts) {UUID} was destroyed, disabling UI element, if it exists");
#endif
        foreach (ServerProbeInsertionUI serverProbeInsertionUI in _insertionPrefabParentT.GetComponentsInChildren<ServerProbeInsertionUI>())
            if (serverProbeInsertionUI.UUID.Equals(UUID))
            {
                serverProbeInsertionUI.SetToggle(false);
                return;
            }
    }

    #endregion

    public void SetActiveProbe(string UUID)
    {
        SetActiveProbeEvent.Invoke(UUID);
    }

    #region Data communication

    /// <summary>
    /// Handle anything that needs to be updated when a new probe is added to the scene
    /// </summary>
    public void AddNewProbe()
    {
        _quickSettingsExperimentList.UpdateExperimentList();
    }

    public (Vector3 pos, Vector3 angles, int type, string cSpaceName, string cTransformName, string UUID) GetProbeInsertionData(string UUID)
    {
        if (_player.UUID2Experiment.ContainsKey(UUID))
        {
            ServerProbeInsertion serverProbeInsertion = GetInsertion(UUID);
            // convert to a regular probe insertion
            return (new Vector3(serverProbeInsertion.ap, serverProbeInsertion.ml, serverProbeInsertion.dv),
                new Vector3(serverProbeInsertion.phi, serverProbeInsertion.theta, serverProbeInsertion.spin),
                serverProbeInsertion.probeType,
                serverProbeInsertion.coordinateSpaceName, serverProbeInsertion.coordinateTransformName,
                UUID);
        }
        else
            return (Vector3.zero, Vector3.zero, -1, null, null, null);
    }

    /// <summary>
    /// Return a list of all probe insertions that are in the current experiment
    /// </summary>
    /// <returns></returns>
    public List<(string UUID, Vector3 pos, Vector3 angles, string cSpaceName, string cTransformName)> GetActiveProbeInsertions()
    {
        var probeDataList = new List<(string UUID, Vector3 pos, Vector3 angles, string cSpaceName, string cTransformName)>();

        foreach (var probe in GetActiveExperimentInsertions())
        {
            ServerProbeInsertion serverProbeInsertion = probe.Value;
            probeDataList.Add((probe.Key,
                new Vector3(serverProbeInsertion.ap, serverProbeInsertion.ml, serverProbeInsertion.dv),
                new Vector3(serverProbeInsertion.phi, serverProbeInsertion.theta, serverProbeInsertion.spin),
                probe.Value.coordinateSpaceName,
                probe.Value.coordinateTransformName));
        }

        return probeDataList;
    }

    #endregion

    #region Helpers

    private ServerProbeInsertion ProbeManager2ServerProbeInsertion(ProbeManager probeManager, bool active = true, bool recorded = false)
    {
        ProbeInsertion insertion = probeManager.GetProbeController().Insertion;
        Vector3 apmldv = insertion.apmldv;
        Vector3 angles = probeManager.GetProbeController().Insertion.angles;

        ServerProbeInsertion serverProbeInsertion = new ServerProbeInsertion(
            apmldv.x, apmldv.y, apmldv.z,
            angles.x, angles.y, angles.z,
            probeManager.ProbeType,
            insertion.CoordinateSpace.Name,
            insertion.CoordinateTransform.Name,
            active, recorded,
            probeManager.UUID);

        return serverProbeInsertion;
    }

    public void AddInsertion(string UUID, ServerProbeInsertion serverProbeInsertion)
    {
        _player.UUIDs.Add(UUID);
        _player.UUID2InsertionData.Add(UUID, serverProbeInsertion);
        _player.UUID2Experiment.Add(UUID, new HashSet<string>());
    }

    public void RemoveInsertion(string UUID)
    {
        _player.UUIDs.Remove(UUID);
        HashSet<string> experiments = GetExperimentsFromUUID(UUID);

        // remove the UUID from each experiment
        foreach (string experiment in experiments)
            _player.Experiment2UUID[experiment].Remove(UUID);

        // remove the UUID from the UUID2 lists
        _player.UUID2InsertionData.Remove(UUID);
        _player.UUID2Experiment.Remove(UUID);
    }

    public void AddExperiment(string UUID, string experimentName)
    {
        if (!_player.Experiments.Contains(experimentName))
        {
            Debug.Log("Experiment doesn't exist");
            return;
        }
        if (_player.UUID2Experiment.ContainsKey(UUID))
            _player.UUID2Experiment[UUID].Add(experimentName);
        if (_player.Experiment2UUID.ContainsKey(experimentName))
            _player.Experiment2UUID[experimentName].Add(UUID);
    }

    public void RemoveExperiment(string experimentName)
    {
        if (_player.Experiments.Contains(experimentName))
        {
            _player.Experiments.Remove(experimentName);
            HashSet<string> UUIDs = GetUUIDsFromExperiment(experimentName);

            // remove the experiment from each UUID
            foreach (string UUID in UUIDs)
                _player.UUID2Experiment[UUID].Remove(experimentName);

            _player.Experiment2UUID.Remove(experimentName);
        }
        else
            Debug.LogError(string.Format("Experiment {0} does not exist", experimentName));
    }

    public ServerProbeInsertion GetInsertion(string UUID)
    {
        if (_player.UUID2InsertionData.ContainsKey(UUID))
            return _player.UUID2InsertionData[UUID];
        else
            return null;
    }

    public HashSet<string> GetExperimentsFromUUID(string UUID)
    {
        if (_player.UUID2Experiment.ContainsKey(UUID))
            return _player.UUID2Experiment[UUID];
        else
            return new HashSet<string>();
    }

    public HashSet<string> GetUUIDsFromExperiment(string experimentName)
    {
        if (_player.Experiment2UUID.ContainsKey(experimentName))
            return _player.Experiment2UUID[experimentName];
        else
            return new HashSet<string>();
    }

    public Dictionary<string, ServerProbeInsertion> GetActiveExperimentInsertions()
    {
        HashSet<string> UUIDs = GetUUIDsFromExperiment(ActiveExperiment);
        Dictionary<string, ServerProbeInsertion> activeExperimentInsertions = new();

        foreach (string UUID in UUIDs)
            activeExperimentInsertions.Add(UUID, GetInsertion(UUID));

        return activeExperimentInsertions;
    }

    #endregion
}