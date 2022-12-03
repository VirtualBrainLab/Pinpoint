using System.Collections.Generic;
using UnityEngine;
using Unisave.Facades;
using System;

/// <summary>
/// Handles connection with the Unisave system, and passing data back-and-forth with the TPManager
/// </summary>
public class AccountsManager : MonoBehaviour
{
    private const float UPDATE_RATE = 60f;

    [SerializeField] private GameObject _registerPanelGO;
    [SerializeField] private ExperimentEditor _experimentEditor;
    [SerializeField] private ExperimentManager _experimentManager;

    #region current player data
    private PlayerEntity player;
    public bool connected { get { return player != null; } }
    #endregion

    #region tracking variables
    private bool dirty;
    private float lastSave;
    #endregion

    private void Update()
    {
        if (dirty && (Time.realtimeSinceStartup - lastSave) >= UPDATE_RATE)
            SaveAndUpdate();
    }

    #region Probes

    /// <summary>
    /// Changes the current active probe, basically just passes data on to the ExperimentManager
    /// </summary>
    /// <param name="UUID"></param>
    /// <param name="data"></param>
    public void UpdateProbeData(string UUID, (Vector3 apmldv, Vector3 angles, int type, string spaceName, string transformName) data)
    {
        if (!connected)
            return;

        Debug.Log("Update probe data called");
        dirty = true;

        _experimentManager.UpdateProbeData(UUID, data);
    }

    #endregion

    public void LoadPlayer()
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
        _experimentManager.SetAccountExperiments(player.experiments);
    }

    private void SaveAndUpdate()
    {
        SavePlayer();

        _experimentEditor.UpdateList();
        _experimentManager.UpdateAll();
    }

    private void SavePlayer()
    {
        OnFacet<PlayerDataFacet>
            .Call(nameof(PlayerDataFacet.SavePlayerEntity),player).Done();
    }

    #region Experiment editor

    public void AddExperiment()
    {
        player.experiments.Add(string.Format("Experiment {0}", player.experiments.Count), new Dictionary<string, ServerProbeInsertion>());
        dirty = true;
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
        dirty = true;
    }

    public void RemoveExperiment(string expName)
    {
        if (player.experiments.ContainsKey(expName))
        {
            player.experiments.Remove(expName);
        }
        else
            Debug.LogError(string.Format("Experiment {0} does not exist", expName));
        dirty = true;
    }

    #endregion

    public void ShowRegisterPanel()
    {
        _registerPanelGO.SetActive(true);
    }

    public Dictionary<string, ServerProbeInsertion> GetExperimentData(string experiment)
    {
        return player.experiments[experiment];
    }

    public void SaveRigList(List<int> visibleRigParts) {
        player.visibleRigParts = visibleRigParts;
    }
}