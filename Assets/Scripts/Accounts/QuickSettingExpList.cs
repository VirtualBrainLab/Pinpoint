using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class QuickSettingExpList : MonoBehaviour
{
    [SerializeField] private UnisaveAccountsManager _accountsManager;
    private TMP_Dropdown _experimentDropdown;

    private void Awake()
    {
        _experimentDropdown = GetComponent<TMP_Dropdown>();
    }

    #region Experiment management functionality

    public void UpdateExperimentList()
    {
        List<string> experiments = _accountsManager.GetExperiments();
        _experimentDropdown.ClearOptions();

        List<TMP_Dropdown.OptionData> optList = new List<TMP_Dropdown.OptionData>();
        optList.Add(new TMP_Dropdown.OptionData("Not saved"));
        foreach (string experiment in experiments)
            optList.Add(new TMP_Dropdown.OptionData(experiment));
        _experimentDropdown.AddOptions(optList);
    }

    public void ChangeExperiment(int optIdx)
    {
        if (optIdx > 0)
        {
            string optText = _experimentDropdown.options[optIdx].text;
            _accountsManager.ChangeProbeExperiment(_accountsManager.ActiveProbeUUID, optText);
        }
        else
            _accountsManager.RemoveProbeExperiment(_accountsManager.ActiveProbeUUID);
    }

    #endregion
}
