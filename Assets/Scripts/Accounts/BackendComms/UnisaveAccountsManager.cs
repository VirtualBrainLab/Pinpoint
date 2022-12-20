using System.Collections.Generic;
using UnityEngine;
using Unisave.Facades;
using System;
using UnityEngine.Serialization;
using TMPro;

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


    #region Insertion variables
    [SerializeField] private Transform _insertionPrefabParentT;
    [SerializeField] private GameObject _insertionPrefabGO;

    // callbacks set by TPManager
    public Action<string> SetActiveProbeCallback { get; set; }
    public Action UpdateCallback { get; set; }
    #endregion

    #region current player data
    private PlayerEntity _player;
    public bool Connected { get { return _player != null; } }
    #endregion

    #region tracking variables
    private Dictionary<string, string> _probeUUID2experiment;

    public bool Dirty { get; private set; }
    private float _lastSave;

    public string ActiveProbeUUID { get; set; }
    public string ActiveExperiment { get; private set; }
    #endregion

    #region Unity
    private void Awake()
    {
        _probeUUID2experiment = new Dictionary<string, string>();
        _lastSave = Time.realtimeSinceStartup;
    }

    private void Update()
    {
        if (Dirty && (Time.realtimeSinceStartup - _lastSave) >= UPDATE_RATE)
        {
            Dirty = false;
            SaveAndUpdate();
        }
    }

    private void OnApplicationQuit()
    {
        SavePlayer();
    }

    #endregion

    public void UpdateProbeData(string UUID, (Vector3 apmldv, Vector3 angles, 
        int type, string spaceName, string transformName, string UUID) data)
    {
        if (_player != null)
        {
            Dirty = true;

            ServerProbeInsertion serverProbeInsertion;
            if (!_probeUUID2experiment.ContainsKey(UUID))
            {
                // this is the first time we've seen this probe, add all of its information
                _probeUUID2experiment.Add(UUID, ActiveExperiment);
                serverProbeInsertion = new ServerProbeInsertion();
            }
            else
            {
                serverProbeInsertion = _player.experiments[_probeUUID2experiment[UUID]][UUID];
            }

            serverProbeInsertion.ap = data.apmldv.x;
            serverProbeInsertion.ml = data.apmldv.y;
            serverProbeInsertion.dv = data.apmldv.z;
            serverProbeInsertion.phi = data.angles.x;
            serverProbeInsertion.theta = data.angles.y;
            serverProbeInsertion.spin = data.angles.z;
            serverProbeInsertion.coordinateSpaceName = data.spaceName;
            serverProbeInsertion.coordinateTransformName = data.transformName;
            serverProbeInsertion.UUID = data.UUID;

            _player.experiments[_probeUUID2experiment[UUID]][UUID] = serverProbeInsertion;
        }
    }

    public void Login()
    {
        OnFacet<PlayerDataFacet>
            .Call<PlayerEntity>(nameof(PlayerDataFacet.LoadPlayerEntity))
            .Then(LoadPlayerCallback)
            .Done();
    }

    private void LoadPlayerCallback(PlayerEntity player)
    {
        this._player = player;
        Debug.Log("Loaded player data: " + player.email);
        SaveAndUpdate();
    }

    public void Logout()
    {
        SavePlayer();
        _player = null;
        Dirty = true;
        _experimentEditor.UpdateList();
        _activeExpListBehavior.UpdateList();
    }

    private void SaveAndUpdate()
    {
        if (_player != null)
        {
            SavePlayer();

            _experimentEditor.UpdateList();
            _activeExpListBehavior.UpdateList();
            _quickSettingsExperimentList.UpdateExperimentList();
            UpdateExperimentInsertions();

            if (UpdateCallback != null)
                UpdateCallback();
        }
    }

    private void SavePlayer()
    {
        Debug.Log("(AccountsManager) Saving data");
        OnFacet<PlayerDataFacet>
            .Call(nameof(PlayerDataFacet.SavePlayerEntity),_player).Done();
    }

    #region Experiment editor

    public void AddExperiment()
    {
        _player.experiments.Add(string.Format("Experiment {0}", _player.experiments.Count), new Dictionary<string, ServerProbeInsertion>());
        // Immediately update, so that user can see effect
        SaveAndUpdate();
    }

    public void EditExperiment(string origName, string newName)
    {
        if (_player.experiments.ContainsKey(origName))
        {
            _player.experiments.Add(newName, _player.experiments[origName]);
            _player.experiments.Remove(origName);
        }
        else
            Debug.LogError(string.Format("Experiment {0} does not exist", origName));
        // Immediately update, so that user can see effect
        SaveAndUpdate();
    }

    public void RemoveExperiment(string expName)
    {
        if (_player.experiments.ContainsKey(expName))
        {
            _player.experiments.Remove(expName);
        }
        else
            Debug.LogError(string.Format("Experiment {0} does not exist", expName));
        // Immediately update, so that user can see effect
        SaveAndUpdate();
    }

    #endregion

    #region Quick settings panel
    public void ChangeProbeExperiment(string UUID, string newExperiment)
    {
        if (_player.experiments.ContainsKey(newExperiment))
        {
            if (_probeUUID2experiment.ContainsKey(UUID))
            {
                // just update the experiment
                ServerProbeInsertion insertionData = _player.experiments[_probeUUID2experiment[UUID]][UUID];
                _player.experiments[_probeUUID2experiment[UUID]].Remove(UUID);

                _probeUUID2experiment[UUID] = newExperiment;
                _player.experiments[newExperiment].Add(UUID, insertionData);
            }
            else
            {
                // this is a totally new probe being added
                _probeUUID2experiment.Add(UUID, newExperiment);
                _player.experiments[newExperiment].Add(UUID, new ServerProbeInsertion());

            }
        }
        else
            Debug.LogError(string.Format("Can't move {0} to {1}, experiment does not exist", UUID, newExperiment));

        Dirty = true;
        UpdateExperimentInsertions();
    }

    public void RemoveProbeExperiment(string probeUUID)
    {
#if UNITY_EDITOR
        Debug.Log($"Removing probe {probeUUID} from its active experiment");
#endif
        if (_probeUUID2experiment.ContainsKey(probeUUID))
        {
            _player.experiments[_probeUUID2experiment[probeUUID]].Remove(probeUUID);
            UpdateExperimentInsertions();
        }
    }
#endregion

    public void ShowRegisterPanel()
    {
        _registerPanelGo.SetActive(true);
    }

    public List<string> GetExperiments()
    {
        if (_player != null)
            return new List<string>(_player.experiments.Keys);
        else
            return new List<string>();
    }

    public Dictionary<string, ServerProbeInsertion> GetExperimentData(string experiment)
    {
        if (_player != null)
            return _player.experiments[experiment];
        else
            return new Dictionary<string, ServerProbeInsertion>();
    }

    public void SaveRigList(List<int> visibleRigParts) {
        _player.visibleRigParts = visibleRigParts;
    }

    public void ActiveExperimentChanged(string experiment)
    {
#if UNITY_EDITOR
        Debug.Log(string.Format("Selected experiment: {0}", experiment));
#endif
        ActiveExperiment = experiment;
        UpdateExperimentInsertions();
        _quickSettingsExperimentList.UpdateExperimentList();
    }

    #region Input window focus

    [SerializeField] private List<TMP_InputField> _focusableInputs;

    /// <summary>
    /// Return true when any input field on the account manager is actively focused
    /// </summary>
    public bool IsFocused()
    {
        if (_experimentEditor.IsFocused())
            return true;

        foreach (TMP_InputField input in _focusableInputs)
            if (input.isFocused)
                return true;

        return false;
    }

    #endregion

    #region Insertions

    public void UpdateExperimentInsertions()
    {
        Debug.Log("Updating insertions");
        // [TODO: This is inefficient, better to keep prefabs that have been created and just hide extras]

        // Destroy all children
        for (int i = _insertionPrefabParentT.childCount - 1; i >= 0; i--)
            Destroy(_insertionPrefabParentT.GetChild(i).gameObject);

        // Add new child prefabs that have the properties matched to the current experiment
        var experimentData = GetExperimentData(ActiveExperiment);
        
        foreach (ServerProbeInsertion insertion in experimentData.Values)
        {
            // Create a new prefab
            GameObject insertionPrefab = Instantiate(_insertionPrefabGO, _insertionPrefabParentT);
            ServerProbeInsertionUI insertionUI = insertionPrefab.GetComponent<ServerProbeInsertionUI>();

            insertionUI.SetInsertionData(this, insertion.UUID);
            insertionUI.UpdateDescription(string.Format("AP {0} ML {1} DV {2} Phi {3} Theta {4} Spin {5}",
                insertion.ap, insertion.ml, insertion.dv,
                insertion.phi, insertion.theta, insertion.spin));
        }
    }
    
    public void ChangeInsertionVisibility(string UUID, bool visible)
    {
        // Somehow, tell TPManager that we need to create or destroy a new probe... tbd
        Debug.Log(string.Format("Insertion {0} wants to become {1}", UUID, visible));
        UpdateCallback();
    }

    #endregion

    public void SetActiveProbe(string UUID)
    {
        SetActiveProbeCallback(UUID);
    }

    #region Data communication

    public (Vector3 pos, Vector3 angles, string cSpaceName, string cTransformName) GetProbeInsertionData(string UUID)
    {
        if (_probeUUID2experiment.ContainsKey(UUID))
        {
            ServerProbeInsertion serverProbeInsertion = _player.experiments[_probeUUID2experiment[UUID]][UUID];
            // convert to a regular probe insertion
            var probeData = (new Vector3(serverProbeInsertion.ap, serverProbeInsertion.ml, serverProbeInsertion.dv),
                new Vector3(serverProbeInsertion.phi, serverProbeInsertion.theta, serverProbeInsertion.spin),
                serverProbeInsertion.coordinateSpaceName, serverProbeInsertion.coordinateTransformName);

            return probeData;
        }
        else
            return (Vector3.zero, Vector3.zero, null, null);
    }

    /// <summary>
    /// Return a list of all probe insertions that are in the current experiment
    /// </summary>
    /// <returns></returns>
    public List<(string UUID, Vector3 pos, Vector3 angles, string cSpaceName, string cTransformName)> GetActiveProbeInsertions()
    {
        var probeDataList = new List<(string UUID, Vector3 pos, Vector3 angles, string cSpaceName, string cTransformName)>();

        foreach (var probe in _player.experiments[ActiveExperiment])
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
}