using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static TMPro.TMP_Dropdown;

public class ExperimentManager : MonoBehaviour
{
    [SerializeField] TMP_Dropdown quickSettingsDropdown;
    [SerializeField] TMP_Dropdown activeExperimentDropdown;

    //private Dictionary<ProbeManager, string> probeExperiments = new();

    private string activeExperiment;

    private Dictionary<string, Dictionary<string, ServerProbeInsertion>> accountExperiments = new();


    private void Awake()
    {
        UpdateQuickSettingDropdown();
        UpdateActiveExperimentDropdown();
    }

    public void UpdateQuickSettingDropdown()
    {
        quickSettingsDropdown.ClearOptions();

        List<OptionData> options = new List<OptionData>();
        options.Add(new OptionData("Not saved"));
        foreach (string experimentName in accountExperiments.Keys)
        {
            options.Add(new OptionData(experimentName));
        }

        quickSettingsDropdown.options = options;
    }

    public void UpdateActiveExperimentDropdown()
    {
        activeExperimentDropdown.ClearOptions();

        List<OptionData> options = new List<OptionData>();
        foreach (string experimentName in accountExperiments.Keys)
        {
            options.Add(new OptionData(experimentName));
        }
    }

    public void ChangeActiveExperiment(int activeExpIdx)
    {
        activeExperiment = activeExperimentDropdown.options[activeExpIdx].text;
    }

    public void SetAccountExperiments(Dictionary<string, Dictionary<string, ServerProbeInsertion>> newExperiments)
    {
        accountExperiments = newExperiments;
    }

    public void AddExperiment()
    {

    }

    public void RemoveExperiment()
    {

    }
}