using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ServerProbeInsertionUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _insertionNameText;
    [SerializeField] private TMP_Text _insertionDescriptionText;
    [SerializeField] private Toggle _insertionActiveToggle;

    private AccountsManager _accountsManager;
    private int _index;
    private string _UUID;

    public void SetInsertionData(AccountsManager accountsManager, string UUID)
    {
        _accountsManager = accountsManager;
        _UUID = UUID;
    }

    public void UpdateName(int index)
    {
        _index = index;
        _insertionNameText.text = string.Format("Experiment #{0}",index);
    }

    public void UpdateDescription(string desc)
    {
        _insertionDescriptionText.text = desc;
    }

    public void ToggleVisibility(bool visible)
    {
        _accountsManager.ChangeInsertionVisibility(_index, visible);
    }

    public void DeleteProbe()
    {
        _accountsManager.RemoveProbeExperiment(_UUID);
    }

    public void ActivateProbe()
    {
        _accountsManager.SetActiveProbe(_UUID);
    }
}
