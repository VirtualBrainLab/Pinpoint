using System.Collections.Generic;
using UnityEngine;
using Unisave.Facades;
using System;
using UnityEngine.Serialization;
using TMPro;

/// <summary>
/// Handles connection with the Unisave system, and passing data back-and-forth with the TPManager
/// </summary>
public class AccountsManager : MonoBehaviour
{
    private const float UPDATE_RATE = 60f;

    [FormerlySerializedAs("registerPanelGO")] [SerializeField] private GameObject _registerPanelGo;
    [FormerlySerializedAs("experimentEditor")] [SerializeField] private ExperimentEditor _experimentEditor;
    [FormerlySerializedAs("activeExpListBehavior")] [SerializeField] private ActiveExpListBehavior _activeExpListBehavior;

    [SerializeField] private QuickSettingExpList _quickSettingsExperimentList;


    #region Insertion variables
    [SerializeField] private Transform _insertionPrefabParentT;
    [SerializeField] private GameObject _insertionPrefabGO;

    // callback set by TPManager
    public Action<string> SetActiveProbeCallback { get; set; }
    #endregion

    #region current player data
    private PlayerEntity player;
    public bool Connected { get { return player != null; } }
    #endregion

    #region tracking variables
    private Dictionary<string, string> probeUUID2experiment;
    private Action updateCallback;

    public bool Dirty { get; private set; }
    private float lastSave;

    public string ActiveProbeUUID { get; set; }
    public string ActiveExperiment { get; private set; }
    #endregion

    private void Awake()
    {
        probeUUID2experiment = new Dictionary<string, string>();
        lastSave = Time.realtimeSinceStartup;
    }

    private void Update()
    {
        if (Dirty && (Time.realtimeSinceStartup - lastSave) >= UPDATE_RATE)
            SaveAndUpdate();
    }

    public void RegisterUpdateCallback(Action callback)
    {
        updateCallback = callback;
    }

    public void UpdateProbeData(string UUID, (Vector3 apmldv, Vector3 angles, 
        int type, string spaceName, string transformName, string UUID) data)
    {
        if (player != null)
        {
            Dirty = true;

            ServerProbeInsertion serverProbeInsertion = player.experiments[probeUUID2experiment[UUID]][UUID];
            serverProbeInsertion.ap = data.apmldv.x;
            serverProbeInsertion.ml = data.apmldv.y;
            serverProbeInsertion.dv = data.apmldv.z;
            serverProbeInsertion.phi = data.angles.x;
            serverProbeInsertion.theta = data.angles.y;
            serverProbeInsertion.spin = data.angles.z;
            serverProbeInsertion.coordinateSpaceName = data.spaceName;
            serverProbeInsertion.coordinateTransformName = data.transformName;
            serverProbeInsertion.UUID = data.UUID;

            player.experiments[probeUUID2experiment[UUID]][UUID] = serverProbeInsertion;
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
        this.player = player;
        Debug.Log("Loaded player data: " + player.email);
        SaveAndUpdate();
    }

    public void Logout()
    {
        player = null;
        Dirty = true;
        _experimentEditor.UpdateList();
        _activeExpListBehavior.UpdateList();
    }

    private void SaveAndUpdate()
    {
        if (player != null)
        {
            SavePlayer();

            _experimentEditor.UpdateList();
            _activeExpListBehavior.UpdateList();
            _quickSettingsExperimentList.UpdateExperimentList();
            UpdateExperimentInsertions();

            if (updateCallback != null)
                updateCallback();
        }
    }

    private void SavePlayer()
    {
        Debug.Log("(AccountsManager) Saving data");
        OnFacet<PlayerDataFacet>
            .Call(nameof(PlayerDataFacet.SavePlayerEntity),player).Done();
    }

    #region Experiment editor

    public void AddExperiment()
    {
        player.experiments.Add(string.Format("Experiment {0}", player.experiments.Count), new Dictionary<string, ServerProbeInsertion>());
        // Immediately update, so that user can see effect
        SaveAndUpdate();
    }

    public void EditExperiment(string origName, string newName)
    {
        if (player.experiments.ContainsKey(origName))
        {
            player.experiments.Add(newName, player.experiments[origName]);
            player.experiments.Remove(origName);
        }
        else
            Debug.LogError(string.Format("Experiment {0} does not exist", origName));
        // Immediately update, so that user can see effect
        SaveAndUpdate();
    }

    public void RemoveExperiment(string expName)
    {
        if (player.experiments.ContainsKey(expName))
        {
            player.experiments.Remove(expName);
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
        if (player.experiments.ContainsKey(newExperiment))
        {
            if (probeUUID2experiment.ContainsKey(UUID))
            {
                // just update the experiment
                ServerProbeInsertion insertionData = player.experiments[probeUUID2experiment[UUID]][UUID];
                player.experiments[probeUUID2experiment[UUID]].Remove(UUID);

                probeUUID2experiment[UUID] = newExperiment;
                player.experiments[newExperiment].Add(UUID, insertionData);
            }
            else
            {
                // this is a totally new probe being added
                probeUUID2experiment.Add(UUID, newExperiment);
                player.experiments[newExperiment].Add(UUID, new ServerProbeInsertion());

            }
        }
        else
            Debug.LogError(string.Format("Can't move {0} to {1}, experiment does not exist", UUID, newExperiment));

        Dirty = true;
        UpdateExperimentInsertions();
    }

    public void RemoveProbeExperiment(string probeUUID)
    {
        if (probeUUID2experiment[probeUUID].Contains(probeUUID))
            player.experiments[probeUUID2experiment[probeUUID]].Remove(probeUUID);
    }

    #endregion

    public void ShowRegisterPanel()
    {
        _registerPanelGo.SetActive(true);
    }

    public List<string> GetExperiments()
    {
        if (player != null)
            return new List<string>(player.experiments.Keys);
        else
            return new List<string>();
    }

    public Dictionary<string, ServerProbeInsertion> GetExperimentData(string experiment)
    {
        if (player != null)
            return player.experiments[experiment];
        else
            return new Dictionary<string, ServerProbeInsertion>();
    }

    public void SaveRigList(List<int> visibleRigParts) {
        player.visibleRigParts = visibleRigParts;
    }

    public void ActiveExperimentChanged(string experiment)
    {
        Debug.Log(string.Format("Selected experiment: {0}", experiment));
        ActiveExperiment = experiment;
        UpdateExperimentInsertions();
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
        int j = 0;
        var experimentData = GetExperimentData(ActiveExperiment);
        foreach (var insertion in experimentData.Values)
        {
            // Create a new prefab
            GameObject insertionPrefab = Instantiate(_insertionPrefabGO, _insertionPrefabParentT);
            ServerProbeInsertionUI insertionUI = insertionPrefab.GetComponent<ServerProbeInsertionUI>();

            insertionUI.SetInsertionData(this, insertion.UUID);
            insertionUI.UpdateName(j++);
            insertionUI.UpdateDescription(string.Format("AP {0} ML {1} DV {2} Phi {3} Theta {4} Spin {5}",
                insertion.ap, insertion.ml, insertion.dv,
                insertion.phi, insertion.theta, insertion.spin));
        }
    }
    
    public void ChangeInsertionVisibility(int insertionIdx, bool visible)
    {
        // Somehow, tell TPManager that we need to create or destroy a new probe... tbd
        Debug.Log(string.Format("Insertion {0} wants to become {1}", insertionIdx, visible));
    }

    #endregion

    public void SetActiveProbe(string UUID)
    {
        SetActiveProbeCallback(UUID);
    }
}