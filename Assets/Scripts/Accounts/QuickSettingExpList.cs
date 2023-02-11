using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RainbowArt.CleanFlatUI;
using UnityEngine.UI;

public class QuickSettingExpList : MonoBehaviour
{
    [SerializeField] private UnisaveAccountsManager _accountsManager;
    [SerializeField] private DropdownMultiCheck _experimentDropdown;

    #region Unity
    private void Awake()
    {
        _experimentDropdown.toggleEvent.AddListener(ChangeProbeExperiment);
    }
    #endregion

    #region Experiment management functionality

    public void UpdateExperimentList()
    {
        Debug.Log("Updating quick settings experiment list");

        if (_experimentDropdown != null && _experimentDropdown.isActiveAndEnabled)
        {
            List<string> experiments = _accountsManager.GetExperiments();

            _experimentDropdown.ClearOptions();

            List<Dropdown.OptionData> optList = new();
            optList.Add(new Dropdown.OptionData("Not saved"));
            foreach (string experiment in experiments)
                optList.Add(new Dropdown.OptionData(experiment));
            _experimentDropdown.AddOptions(optList);
            _experimentDropdown.UpdateToggleList();
        }
    }

    /// <summary>
    /// Triggered by the quick setting dropdown menu item when the user asks to change which experiment a
    /// probe is saved in.
    /// </summary>
    /// <param name="optIdx"></param>
    public void ChangeProbeExperiment(int index, Toggle toggle)
    {
#if UNITY_EDITOR
        Debug.Log($"Toggle {toggle.GetComponentInChildren<Text>().text} is {(toggle.isOn ? "on" : "off")}");
#endif

        if (index== 0)
        {
            // Remove all other experiments, this probe is not saved anymore
            for (int i = 1; i < _experimentDropdown.ToggleList.Length; i++)
            {
                string experimentName = _experimentDropdown.options[i].text;
                _accountsManager.RemoveProbeExperiment(ProbeManager.ActiveProbeManager, experimentName);
                _experimentDropdown.ToggleList[i].SetIsOnWithoutNotify(false);
            }
        }
        else
        {
            // Disable the "Not saved" toggle if it was checked
            _experimentDropdown.ToggleList[0].SetIsOnWithoutNotify(false);

            string experimentName = _experimentDropdown.options[index].text;
            if (toggle.isOn)
                _accountsManager.AddProbeExperiment(ProbeManager.ActiveProbeManager, experimentName);
            else
                _accountsManager.RemoveProbeExperiment(ProbeManager.ActiveProbeManager, experimentName);
        }
    }

    #endregion
}
