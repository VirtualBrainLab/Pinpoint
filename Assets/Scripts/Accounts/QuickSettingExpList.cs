using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class QuickSettingExpList : MonoBehaviour
{
    [SerializeField] private UnisaveAccountsManager _accountsManager;
    [SerializeField] private TMP_Dropdown _experimentDropdown;

    #region Experiment management functionality

    public void UpdateExperimentList()
    {
        Debug.Log("Updating quick settings experiment list");
        if (_experimentDropdown != null && _experimentDropdown.isActiveAndEnabled)
        {
            List<string> experiments = _accountsManager.GetExperiments();
            _experimentDropdown.ClearOptions();

            List<TMP_Dropdown.OptionData> optList = new List<TMP_Dropdown.OptionData>();
            optList.Add(new TMP_Dropdown.OptionData("Not saved"));
            foreach (string experiment in experiments)
                optList.Add(new TMP_Dropdown.OptionData(experiment));
            _experimentDropdown.AddOptions(optList);
        }
    }

    /// <summary>
    /// Triggered by the quick setting dropdown menu item when the user asks to change which experiment a
    /// probe is saved in.
    /// </summary>
    /// <param name="optIdx"></param>
    public void ChangeProbeExperiment(int optIdx)
    {
        if (optIdx > 0)
        {
            string optText = _experimentDropdown.options[optIdx].text;
            _accountsManager.ChangeProbeExperiment(ProbeManager.ActiveProbeManager, optText);
        }
        else
            _accountsManager.RemoveProbeExperiment(ProbeManager.ActiveProbeManager.UUID);
    }

    #endregion
}
