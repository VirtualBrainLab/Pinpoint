using System.Collections.Generic;
using UnityEngine;
using Unisave.Facades;
using System;
using UnityEngine.Serialization;
using TMPro;
using UnityEngine.Events;
using System.Linq;

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
    private Dictionary<string, string> _UUID2Experiment;

    public bool Dirty { get; private set; }
    private float _lastSave;

    public string ActiveExperiment { get; private set; }
    #endregion

    #region Unity
    private void Awake()
    {
        _lastSave = Time.realtimeSinceStartup;
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

    public void UpdateProbeData()
    {
        ProbeManager activeProbeManager = ProbeManager.ActiveProbeManager;

        // Check that we are logged in and that this probe is in an experiment
        if (_player != null && _UUID2Experiment.ContainsKey(activeProbeManager.UUID))
        {
            string experiment = _UUID2Experiment[activeProbeManager.UUID];
            ServerProbeInsertion serverProbeInsertion = _player.experiments[experiment][activeProbeManager.UUID];

            _player.experiments[experiment][activeProbeManager.UUID] = ProbeManager2ServerProbeInsertion(activeProbeManager, true, serverProbeInsertion.recorded);

            Dirty = true;
        }
    }

    private void LoadPlayerCallback(PlayerEntity player)
    {
        _player = player;
        // populate the uuid2experiment list
        foreach (var kvp in _player.experiments)
        {
            Debug.Log(kvp.Key);
            foreach (string UUID in kvp.Value.Keys)
            {
                _UUID2Experiment.Add(UUID, kvp.Key);
                Debug.Log(UUID);
            }
        }
        Debug.Log("(AccountsManager) Player logged in: " + player.email);

        ExperimentChangedEvent.Invoke();
        UpdateExperimentUI();
    }

    public void Login()
    {
        _UUID2Experiment = new();
        Debug.Log("(AccountsManager) Player attemping to log in");
        OnFacet<PlayerDataFacet>
            .Call<PlayerEntity>(nameof(PlayerDataFacet.LoadPlayerEntity))
            .Then(LoadPlayerCallback)
            .Done();
    }

    public void LogoutCleanup()
    {
        Debug.Log("(AccountsManager) Player logged out");
        _player = null;
        UpdateExperimentUI();
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
    }

    #endregion


    #region Experiment editor

    public void AddExperiment()
    {
        _player.experiments.Add(string.Format("Experiment {0}", _player.experiments.Count), new Dictionary<string, ServerProbeInsertion>());
        // Immediately update, so that user can see effect
        SavePlayer();
        UpdateExperimentUI();
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
        SavePlayer();
        UpdateExperimentUI();
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
        SavePlayer();
        UpdateExperimentUI();
    }

    #endregion

    #region Quick settings panel
    public void ChangeProbeExperiment(ProbeManager probeManager, string newExperiment)
    {
        string UUID = probeManager.UUID;

        if (_player.experiments.ContainsKey(newExperiment))
        {

            if (_UUID2Experiment.ContainsKey(UUID))
            {
                Debug.Log($"Changing {probeManager.name} to {newExperiment}");
                // just update the experiment
                ServerProbeInsertion insertionData = _player.experiments[_UUID2Experiment[UUID]][UUID];
                _player.experiments[_UUID2Experiment[UUID]].Remove(UUID);

                _UUID2Experiment[UUID] = newExperiment;
                _player.experiments[newExperiment].Add(UUID, insertionData);
            }
            else
            {
                Debug.Log($"Adding {probeManager.name} to {newExperiment}");
                // this is a totally new probe being added
                _UUID2Experiment.Add(UUID, newExperiment);
                _player.experiments[newExperiment].Add(UUID, ProbeManager2ServerProbeInsertion(probeManager));

            }
        }
        else
            Debug.LogError(string.Format("Can't move {0} to {1}, experiment does not exist", UUID, newExperiment));

        Dirty = true;

        UpdateExperimentInsertionUIPanels();
    }

    public void RemoveProbeExperiment(string UUID)
    {
#if UNITY_EDITOR
        Debug.Log($"Removing probe {UUID} from its active experiment");
#endif

        if (_UUID2Experiment.ContainsKey(UUID))
        {
            _player.experiments[_UUID2Experiment[UUID]].Remove(UUID);
            UpdateExperimentInsertionUIPanels();
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
        var experimentData = GetExperimentData(ActiveExperiment);
        
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
        ServerProbeInsertion insertion = _player.experiments[_UUID2Experiment[UUID]][UUID];
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
        if (_UUID2Experiment.ContainsKey(UUID))
        {
            ServerProbeInsertion serverProbeInsertion = _player.experiments[_UUID2Experiment[UUID]][UUID];
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

    #endregion
}