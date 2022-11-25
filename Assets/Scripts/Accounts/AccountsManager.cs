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
    [SerializeField] private ActiveExpListBehavior _activeExpListBehavior;

    #region current player data
    private PlayerEntity player;
    public bool Connected { get { return player != null; } }
    #endregion

    #region tracking variables
    private Dictionary<string, string> probeUUID2experiment;
    private Action updateCallback;

    private bool dirty;
    private float lastSave;
    #endregion

    private void Awake()
    {
        probeUUID2experiment = new Dictionary<string, string>();
    }

    private void Update()
    {
        if (dirty && (Time.realtimeSinceStartup - lastSave) >= UPDATE_RATE)
            SaveAndUpdate();
    }

    public void RegisterUpdateCallback(Action callback)
    {
        updateCallback = callback;
    }

    public void UpdateProbeData(string UUID, (Vector3 apmldv, Vector3 angles, int type, string spaceName, string transformName) data)
    {
        //dirty = true;

        //ServerProbeInsertion serverProbeInsertion = player.experiments[probeUUID2experiment[UUID]][UUID];
        //serverProbeInsertion.ap = data.apmldv.x;
        //serverProbeInsertion.ml = data.apmldv.y;
        //serverProbeInsertion.dv = data.apmldv.z;
        //serverProbeInsertion.phi = data.angles.x;
        //serverProbeInsertion.theta = data.angles.y;
        //serverProbeInsertion.spin = data.angles.z;
        //serverProbeInsertion.coordinateSpaceName = data.spaceName;
        //serverProbeInsertion.coordinateTransformName = data.transformName;

        //player.experiments[probeUUID2experiment[UUID]][UUID] = serverProbeInsertion;
    }

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
        SaveAndUpdate();
    }

    private void SaveAndUpdate()
    {
        SavePlayer();

        _experimentEditor.UpdateList();
        _activeExpListBehavior.UpdateList();

        if (updateCallback != null)
            updateCallback();
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

        SaveAndUpdate();
    }

    public void RemoveProbeExperiment(string probeUUID)
    {
        if (probeUUID2experiment[probeUUID].Contains(probeUUID))
            player.experiments[probeUUID2experiment[probeUUID]].Remove(probeUUID);
    }

    #endregion

    public void ShowRegisterPanel()
    {
        _registerPanelGO.SetActive(true);
    }

    public List<string> GetExperiments()
    {
        return new List<string>(player.experiments.Keys);
    }

    public Dictionary<string, ServerProbeInsertion> GetExperimentData(string experiment)
    {
        return player.experiments[experiment];
    }

    public void SaveRigList(List<int> visibleRigParts) {
        player.visibleRigParts = visibleRigParts;
    }

    #region Active experiment list

    public void SelectActiveExperiment(string experiment)
    {
        Debug.Log(string.Format("Changing active experiment to {0}", experiment));
    }

    #endregion
}