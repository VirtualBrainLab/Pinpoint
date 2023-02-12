using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RainbowArt.CleanFlatUI;
using UnityEngine.UI;

public class QuickSettingExpList : MonoBehaviour
{
    [SerializeField] private UnisaveAccountsManager _accountsManager;
    [SerializeField] private QuickSettingsMultiDropdown _experimentDropdown;

    #region Unity
    private void Awake()
    {
        _experimentDropdown.toggleEvent.AddListener(ChangeProbeExperiment);
        _experimentDropdown.showEvent.AddListener(UpdateExperimentList);
    }
    #endregion

    #region Experiment management functionality

    public void UpdateExperimentList()
    {
#if UNITY_EDITOR
        Debug.Log("Updating quick settings experiment list");
#endif

        if (_experimentDropdown != null && _experimentDropdown.isActiveAndEnabled)
        {
            // Get the full list of experiments
            List<string> experiments = _accountsManager.GetExperiments();
            // Also get the experiments the current probe is in
            HashSet<string> probeExperiments = _accountsManager.GetExperimentsFromUUID(ProbeManager.ActiveProbeManager.UUID);
            bool[] toggleOn = new bool[experiments.Count + 1];

            _experimentDropdown.ClearOptions();

            // Create the new option list
            List<Dropdown.OptionData> optList = new();
            optList.Add(new Dropdown.OptionData("Not saved"));

            bool anyOn = false;
            for (int i = 0; i < experiments.Count; i++)
            {
                optList.Add(new Dropdown.OptionData(experiments[i]));
                if (probeExperiments.Contains(experiments[i]))
                {
                    toggleOn[i+1] = probeExperiments.Contains(experiments[i]);
                    anyOn = true;
                }
            }

            if (!anyOn)
                toggleOn[0] = true;

            _experimentDropdown.AddOptions(optList);
            // Update the toggles
            _experimentDropdown.IsOnList = toggleOn;
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
            // If the user tried to disable the "not saved" option, force it on
            _experimentDropdown.ToggleList[0].SetIsOnWithoutNotify(true);
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
