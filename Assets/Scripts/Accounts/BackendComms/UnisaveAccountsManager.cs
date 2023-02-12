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
    [FormerlySerializedAs("activeExpListBehavior")] [SerializeField] private ActiveExperimentUI _activeExperimentUI;

    [SerializeField] private QuickSettingExpList _quickSettingsExperimentList;

    [SerializeField] private EmailLoginForm _emailLoginForm;

    #region Events
    public UnityEvent<string> SetActiveProbeEvent; // Fired when a user clicks on an insertion (to set it to the active probe)

    public UnityEvent ExperimentListChangeEvent; // Fired when the experiment list is updated
    public UnityEvent InsertionListChangeEvent; // Fired when the insertion list is updated or when insertion data is updated
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

        // Update everybody
        ExperimentListChangeEvent.Invoke();
        InsertionListChangeEvent.Invoke();
    }

    public void LogoutCleanup()
    {
        Debug.Log("(AccountsManager) Player logged out");
        _player = null;

        ExperimentListChangeEvent.Invoke();
        InsertionListChangeEvent.Invoke();
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

    #endregion


    #region Experiment editor

    public void NewExperiment()
    {
        string experimentName = $"Experiment {_player.Experiments.Count}";
        _player.Experiments.Add(experimentName);
        _player.Experiment2UUID.Add(experimentName, new HashSet<string>());
        
        ExperimentListChangeEvent.Invoke();

        Dirty = true;
    }

    public void EditExperiment(string origName, string newName)
    {
        if (_player.Experiments.Contains(origName))
        {
            _player.Experiments[_player.Experiments.IndexOf(origName)] = newName;
        }
        else
            Debug.LogError(string.Format("Experiment {0} does not exist", origName));

        ExperimentListChangeEvent.Invoke();

        Dirty = true;
    }

    public void DeleteExperiment(string expName)
    {
        RemoveExperiment(expName);

        ExperimentListChangeEvent.Invoke();

        Dirty = true;
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
        Debug.Log($"Adding {probeManager.UUID} to {experimentName}");

        if (_player.UUID2InsertionData.ContainsKey(probeManager.UUID))
        {
            // This probe is already saved in a different experiment, just add it to the new experiment (assuming it exists)

            if (_player.UUID2Experiment[probeManager.UUID].Contains(experimentName))
                Debug.Log("Experiment already in list");
            else
            {
                Debug.Log("Added new experiemnt to dictionary");
                _player.UUID2Experiment[probeManager.UUID].Add(experimentName);
                _player.Experiment2UUID[experimentName].Add(probeManager.UUID);
            }
        }
        else
        {
            // This probe is not saved yet -- add it for the first time
            _player.UUID2InsertionData.Add(probeManager.UUID, ProbeManager2ServerProbeInsertion(probeManager));
            _player.UUID2Experiment.Add(probeManager.UUID, new HashSet<string>());
            _player.UUID2Experiment[probeManager.UUID].Add(experimentName);
            _player.Experiment2UUID[experimentName].Add(probeManager.UUID);
        }

        Dirty = true;

        InsertionListChangeEvent.Invoke();
    }

    public void RemoveProbeExperiment(ProbeManager probeManager, string experimentName)
    {
        Debug.Log($"Removing {probeManager.UUID} from {experimentName}");

        if (_player.UUID2Experiment.ContainsKey(probeManager.UUID))
        {
            if (_player.UUID2Experiment[probeManager.UUID].Contains(experimentName))
            {
                _player.UUID2Experiment[probeManager.UUID].Remove(experimentName);
                _player.Experiment2UUID[experimentName].Remove(probeManager.UUID);

                // If there are no remaining experiments we should remove the data for this probe entirely
                if (_player.UUID2Experiment[probeManager.UUID].Count == 0)
                {
                    _player.UUID2Experiment.Remove(probeManager.UUID);
                    _player.UUID2InsertionData.Remove(probeManager.UUID);
                }
            }
            else
                Debug.Log("Experiment wasn't in list");
        }

        Dirty = true;

        InsertionListChangeEvent.Invoke();
    }

    public void DeleteProbe(string UUID)
    {
        Debug.Log($"Probe {UUID} deleted");
        RemoveInsertion(UUID);

        InsertionListChangeEvent.Invoke();
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

        InsertionListChangeEvent.Invoke();
    }

    #region Insertions

    
    public void ChangeInsertionVisibility(string UUID, bool visible)
    {
#if UNITY_EDITOR
        Debug.Log(string.Format("(Accounts) Insertion {0} wants to become {1}", UUID, visible));
#endif
        ServerProbeInsertion insertion = GetInsertion(UUID);
        UpdateCallbackEvent(GetProbeInsertionData(UUID), visible);
    }

    /// <summary>
    /// Called when a probe is destroyed in the scene, handles modifying UI elements to reflect the new state
    /// </summary>
    /// <param name="UUID"></param>
    public void ProbeDestroyInScene(string UUID)
    {
        _activeExperimentUI.SetInsertionActiveToggle(UUID, false);
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

    private void AddInsertion(string UUID, ServerProbeInsertion serverProbeInsertion)
    {
        _player.UUID2InsertionData.Add(UUID, serverProbeInsertion);
        _player.UUID2Experiment.Add(UUID, new HashSet<string>());
    }

    private void RemoveInsertion(string UUID)
    {
        HashSet<string> experiments = GetExperimentsFromUUID(UUID);

        // remove the UUID from each experiment
        foreach (string experiment in experiments)
            _player.Experiment2UUID[experiment].Remove(UUID);

        // remove the UUID from the UUID2 lists
        _player.UUID2InsertionData.Remove(UUID);
        _player.UUID2Experiment.Remove(UUID);
    }

    private void AddExperiment(string UUID, string experimentName)
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

    private void RemoveExperiment(string experimentName)
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
        if (_player == null || !_player.UUID2InsertionData.ContainsKey(UUID))
            return null;

        return _player.UUID2InsertionData[UUID];
    }

    public HashSet<string> GetExperimentsFromUUID(string UUID)
    {
        if (_player == null || !_player.UUID2Experiment.ContainsKey(UUID))
            return new HashSet<string>();

        return _player.UUID2Experiment[UUID];
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

    #region Debug
#if UNITY_EDITOR

    public void PrintExperimentList()
    {
        string list = "";

        foreach (string experiment in _player.Experiments)
        {
            list += $"{experiment}\n";
            foreach (string UUID in GetUUIDsFromExperiment(experiment))
            {
                list += $"\t{UUID}\n";
            }
        }

        Debug.Log(list);
    }

#endif
#endregion
}