using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static TMPro.TMP_Dropdown;

/// <summary>
/// Manages experiments and updating the dropdown menus with experiment options
/// </summary>
public class ExperimentManager : MonoBehaviour
{
    [SerializeField] TMP_Dropdown quickSettingsDropdown;
    [SerializeField] TMP_Dropdown activeExperimentDropdown;
    [SerializeField] private ActiveExpListBehavior _activeExpListBehavior;

    //private Dictionary<ProbeManager, string> probeExperiments = new();

    public string activeExperiment { get; private set; }

    public Dictionary<string, Dictionary<string, ServerProbeInsertion>> experiments { get; private set; }
    private Dictionary<string, string> probeUUID2experiment;


    private void Awake()
    {
        UpdateAll();
        quickSettingsDropdown.onValueChanged.AddListener(QuickSettingDropdownChanged);
    }

    public void UpdateAll()
    {
        UpdateQuickSettingDropdown();
        UpdateActiveExperimentDropdown();
        _activeExpListBehavior.UpdateList();
    }

    #region Change experiment lists
    public void ChangeActiveExperiment(int activeExpIdx)
    {
        activeExperiment = activeExperimentDropdown.options[activeExpIdx].text;
    }

    public void SetAccountExperiments(Dictionary<string, Dictionary<string, ServerProbeInsertion>> newExperiments)
    {
        experiments = newExperiments;
        UpdateAll();
    }

    public void UpdateActiveExperimentDropdown()
    {
        activeExperimentDropdown.ClearOptions();

        List<OptionData> options = new List<OptionData>();
        foreach (string experimentName in experiments.Keys)
        {
            options.Add(new OptionData(experimentName));
        }
    }

    #region Active experiment list

    public void SelectActiveExperiment(string experiment)
    {
        Debug.Log(string.Format("Changing active experiment to {0}", experiment));
    }

    #endregion

    #endregion

    #region Quick settings

    public void UpdateQuickSettingDropdown()
    {
        quickSettingsDropdown.ClearOptions();

        List<OptionData> options = new List<OptionData>();
        options.Add(new OptionData("Not saved"));
        foreach (string experimentName in experiments.Keys)
        {
            options.Add(new OptionData(experimentName));
        }

        quickSettingsDropdown.options = options;
    }

    public void QuickSettingDropdownChanged(int option)
    {
        Debug.Log(string.Format("Current probe experiment set to {0}", option));
        if (option==0)
        {
            // probe experiment was cleared
            ClearProbeExperiment();
        }
        //else if (probeUUID2experiment.ContainsKey())
    }
    public void ChangeProbeExperiment(string UUID, string newExperiment)
    {
        //if (player.experiments.ContainsKey(newExperiment))
        //{
        //    if (probeUUID2experiment.ContainsKey(UUID))
        //    {
        //        // just update the experiment
        //        ServerProbeInsertion insertionData = player.experiments[probeUUID2experiment[UUID]][UUID];
        //        player.experiments[probeUUID2experiment[UUID]].Remove(UUID);

        //        probeUUID2experiment[UUID] = newExperiment;
        //        player.experiments[newExperiment].Add(UUID, insertionData);
        //    }
        //    else
        //    {
        //        // this is a totally new probe being added
        //        probeUUID2experiment.Add(UUID, newExperiment);
        //        player.experiments[newExperiment].Add(UUID, new ServerProbeInsertion());

        //    }
        //}
        //else
        //    Debug.LogError(string.Format("Can't move {0} to {1}, experiment does not exist", UUID, newExperiment));

        //SaveAndUpdate();
    }

    public void RemoveProbeExperiment(string probeUUID)
    {
        //if (probeUUID2experiment[probeUUID].Contains(probeUUID))
        //    player.experiments[probeUUID2experiment[probeUUID]].Remove(probeUUID);
    }

    public List<string> GetExperiments()
    {
        return new List<string>(experiments.Keys);
    }

    #endregion

    #region Probe handling

    public void UpdateProbeData(string UUID, (Vector3 apmldv, Vector3 angles, int type, string spaceName, string transformName) data)
    {
        // Check whether this probe already exists or not, and whether we should be saving this probe's data
        if (quickSettingsDropdown.value == 0)
            // this probe is not being saved
            return;

        //if ()

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

    private void AddProbe()
    {

    }

    private void ClearProbeExperiment()
    {

    }

    #endregion
}