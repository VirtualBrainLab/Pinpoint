using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ActiveExpListBehavior : MonoBehaviour
{
    [SerializeField] private UnisaveAccountsManager _accountsManager;

    private TMP_Dropdown _optionList;

    private void Awake()
    {
        _optionList = GetComponent<TMP_Dropdown>();
    }

    public void UpdateList()
    {
        List<string> experimentList = _accountsManager.GetExperiments();

        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        foreach (string experiment in experimentList)
            options.Add(new TMP_Dropdown.OptionData(experiment));
        _optionList.ClearOptions();
        _optionList.AddOptions(options);
        SelectExperiment(0);
    }

    public void SelectExperiment(int optIdx)
    {
        _accountsManager.ActiveExperimentChanged(_optionList.options[optIdx].text);
    }
}
