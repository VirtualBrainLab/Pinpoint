using System.Collections.Generic;
using UnityEngine;
using Unisave.Facades;
using System;
using UnityEngine.Serialization;
using UnityEngine.Events;
using System.Linq;
using UnityEditor;

/// <summary>
/// Handles connection with the Unisave system, and passing data back-and-forth with the TPManager
/// </summary>
public class UnisaveAccountsManager : AccountsManager
{
#if UNITY_WEBGL && !UNITY_EDITOR
    private const float UPDATE_RATE = 60f;
#else
    private const float UPDATE_RATE = 120f;
#endif

    [FormerlySerializedAs("registerPanelGO")] [SerializeField] private GameObject _registerPanelGo;
    [FormerlySerializedAs("experimentEditor")] [SerializeField] private ExperimentEditor _experimentEditor;
    [FormerlySerializedAs("activeExpListBehavior")] [SerializeField] private ActiveExperimentUI _activeExperimentUI;
    [SerializeField] private GameObject _savePanel;

    [SerializeField] private EmailLoginForm _emailLoginForm;

#region Events
    public UnityEvent<string> SetActiveProbeEvent; // Fired when a user clicks on an insertion (to set it to the active probe)

    public UnityEvent ExperimentListChangeEvent; // Fired when the experiment list is updated
    /// <summary>
    /// Fired when the insertion list for the current active experiment was changed
    /// </summary>
    public UnityEvent InsertionListChangeEvent; // Fired when the insertion list is updated or when insertion data is updated

    public UnityEvent<string, string> InsertionNameChangeEvent; // Fired when a probe's name is updated
    public Action<(Vector3 apmldv, Vector3 angles, int type, string spaceName, string transformName, string UUID, string overrideName, Color color),bool> UpdateCallbackEvent { get; set; }
#endregion

#region current player data
    private PlayerEntity _player;
    public bool Connected { get { return _player != null; } }
    public string ActiveExperiment { get { return _player.activeExperiment; } }
#endregion

#region tracking variables
    public bool Dirty { get; private set; }
    private float _lastSave;
#endregion

#region Unity
    private void Awake()
    {
        _lastSave = Time.realtimeSinceStartup;
    }

    public void DelayedStart()
    {
        _emailLoginForm.AttemptLoginViaTokenAsync();
    }

    private void Update()
    {
        if (Dirty)
        {
            if ((Time.realtimeSinceStartup - _lastSave) >= UPDATE_RATE)
            {
                SavePlayer();
            }
            else
            {
                _savePanel.SetActive(true);
            }
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

        // Go through all of the insertions -- if any are marked as active and *DONT* exist in the scene
        // we should create them now
        List<string> keyList = new(_player.UUID2InsertionData.Keys);

        for (int i = 0; i < keyList.Count; i++)
        {
            string UUID = keyList[i];
            ServerProbeInsertion data = _player.UUID2InsertionData[UUID];

            Debug.Log($"Creating probe {UUID} if active: {data.active}");
            if (data.active && !ProbeManager.Instances.Any(x => x.UUID.Equals(UUID)))
                UpdateCallbackEvent(GetProbeInsertionData(data.UUID), true);
        }

        // Update UI
        ExperimentListChangeEvent.Invoke();
        InsertionListChangeEvent.Invoke();
    }

    public void LogoutCleanup()
    {
        Debug.Log("(AccountsManager) Player logged out");
        _player = null;

        ExperimentListChangeEvent.Invoke();
        InsertionListChangeEvent.Invoke();

        _emailLoginForm.ClearToken();
    }

#endregion

    public void UpdateProbeData()
    {
        if (ProbeManager.ActiveProbeManager != null)
        {
            string UUID = ProbeManager.ActiveProbeManager.UUID;

            // Check that we are logged in and that this probe is in an experiment
            if (_player != null && _player.UUID2Experiment.ContainsKey(UUID))
            {
                ServerProbeInsertion insertionData = GetInsertion(UUID);

                _player.UUID2InsertionData[UUID] = ProbeManager2ServerProbeInsertion(ProbeManager.ActiveProbeManager, true, insertionData.recorded);
                InsertionListChangeEvent.Invoke();

                Dirty = true;
            }
        }
    }

    public void OverrideProbeName(string UUID, string newName)
    {
        _player.UUID2InsertionData[UUID].name = newName;
        InsertionNameChangeEvent.Invoke(UUID, newName);
        Dirty = true;
    }

#region Save and Update

    public void SavePlayer()
    {
        Dirty = false;
        _savePanel.SetActive(false);
        _lastSave = Time.realtimeSinceStartup;

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
        string experimentName = $"Experiment {_player.Experiment2UUID.Count}";
        _player.Experiment2UUID.Add(experimentName, new HashSet<string>());

        // If this is the first experiment the player created, save it for future reference
        if (_player.activeExperiment == null)
            ActiveExperimentChanged(experimentName);
        
        ExperimentListChangeEvent.Invoke();

        Dirty = true;
    }

    public void EditExperiment(string origName, string newName)
    {
        if (_player.Experiment2UUID.Keys.Contains(origName))
        {
            // Add me to the experiment list 
            var data = _player.Experiment2UUID[origName];
            _player.Experiment2UUID.Add(newName, data);
            _player.Experiment2UUID.Remove(origName);

            // Go through and fix all the items in the UUID2Experiment list
            foreach (string UUID in _player.Experiment2UUID[newName])
            {
                _player.UUID2Experiment[UUID].Remove(origName);
                _player.UUID2Experiment[UUID].Add(newName);
            }
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
                Debug.Log("Adding new experiment to existing UUID");
                _player.UUID2Experiment[probeManager.UUID].Add(experimentName);
                _player.Experiment2UUID[experimentName].Add(probeManager.UUID);
            }
        }
        else
        {
            Debug.Log("Saving UUID for the first time");
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
#if UNITY_EDITOR
        Debug.Log($"Removing {probeManager.UUID} from {experimentName}");
#endif

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
#if UNITY_EDITOR
            else
                Debug.Log("Experiment wasn't in list");
#endif
        }

        Dirty = true;

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
            return _player.Experiment2UUID.Keys.ToList();
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
        _player.activeExperiment = experiment;

        Dirty = true;

        InsertionListChangeEvent.Invoke();
    }

    #region Insertion panel UI events

    public void ChangeInsertionVisibility(string UUID, bool visible)
    {
#if UNITY_EDITOR
        Debug.Log(string.Format("(Accounts) Insertion {0} wants to become {1}", UUID, visible));
#endif
        _player.UUID2InsertionData[UUID].active = visible;
        UpdateCallbackEvent(GetProbeInsertionData(UUID), visible);

        Dirty = true;
    }

    /// <summary>
    /// Called to set a probe as active in the scene, invokes the SetActiveProbeEvent
    /// </summary>
    /// <param name="UUID"></param>
    public void SetActiveProbe(string UUID)
    {
        SetActiveProbeEvent.Invoke(UUID);
    }

    /// <summary>
    /// Called to delete a probe entirely from an experiment
    /// </summary>
    /// <param name="UUID"></param>
    public void RemoveProbeFromActiveExperiment(string UUID)
    {
        Debug.Log($"Probe {UUID} deleted from {_player.activeExperiment}"); 
        RemoveInsertionFromExperiment(UUID, _player.activeExperiment);

        Dirty = true;

        InsertionListChangeEvent.Invoke();
    }
    #endregion

    #region Data communication

    public (Vector3 pos, Vector3 angles, int type, string cSpaceName, string cTransformName, string UUID, string overrideName, Color color) GetProbeInsertionData(string UUID)
    {
        if (_player.UUID2Experiment.ContainsKey(UUID))
        {
            ServerProbeInsertion serverProbeInsertion = GetInsertion(UUID);
            // convert to a regular probe insertion
            return (new Vector3(serverProbeInsertion.ap, serverProbeInsertion.ml, serverProbeInsertion.dv),
                new Vector3(serverProbeInsertion.phi, serverProbeInsertion.theta, serverProbeInsertion.spin),
                serverProbeInsertion.probeType,
                serverProbeInsertion.coordinateSpaceName, serverProbeInsertion.coordinateTransformName,
                UUID,
                serverProbeInsertion.name,
                new Color(serverProbeInsertion.color[0], serverProbeInsertion.color[1], serverProbeInsertion.color[2]));
        }
        else
            return (Vector3.zero, Vector3.zero, -1, null, null, null, null, Color.black);
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
        ProbeInsertion insertion = probeManager.ProbeController.Insertion;
        Vector3 apmldv = insertion.apmldv;
        Vector3 angles = insertion.angles;
        Color color = probeManager.Color;

        ServerProbeInsertion serverProbeInsertion = new ServerProbeInsertion(
            probeManager.name,
            apmldv.x, apmldv.y, apmldv.z,
            angles.x, angles.y, angles.z,
            (int)probeManager.ProbeType,
            insertion.CoordinateSpace.Name,
            insertion.CoordinateTransform.Name,
            active, recorded,
            probeManager.UUID,
            new float[] {color.r, color.g, color.b});

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

    private void RemoveInsertionFromExperiment(string UUID, string experimentName)
    {
        _player.Experiment2UUID[experimentName].Remove(UUID);
        _player.UUID2Experiment[UUID].Remove(experimentName);

        // If this UUID is no longer in *any* experiment, delete it entirely
        if (_player.UUID2Experiment[UUID].Count == 0)
            RemoveInsertion(UUID);
    }

    private void AddExperiment(string UUID, string experimentName)
    {
        if (!_player.Experiment2UUID.Keys.Contains(experimentName))
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
        if (_player.Experiment2UUID.Keys.Contains(experimentName))
        {
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
        if (_player == null || !_player.Experiment2UUID.ContainsKey(experimentName))
            return new HashSet<string>();
        
        return _player.Experiment2UUID[experimentName];
    }

    public Dictionary<string, ServerProbeInsertion> GetActiveExperimentInsertions()
    {
        if (_player == null)
            return new Dictionary<string, ServerProbeInsertion>();

        HashSet<string> UUIDs = GetUUIDsFromExperiment(_player.activeExperiment);
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
        string list = "\n";

        foreach (string experiment in _player.Experiment2UUID.Keys)
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