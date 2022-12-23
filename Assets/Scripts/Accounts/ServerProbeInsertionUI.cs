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

    private UnisaveAccountsManager _accountsManager;
    private string _UUID;
    private string _displayString;

    public void SetInsertionData(UnisaveAccountsManager accountsManager, string UUID, bool active)
    {
        _accountsManager = accountsManager;
        _UUID = UUID;
        _displayString = (!string.IsNullOrEmpty(_UUID)) ?
            _UUID.Substring(0, 8) :
            "";
        _insertionNameText.text = _displayString;

        _insertionActiveToggle.isOn = active;

        GetComponent<Button>().onClick.AddListener(ActivateProbe);
    }

    public void UpdateDescription(string desc)
    {
        _insertionDescriptionText.text = desc;
    }

    public void ToggleVisibility(bool visible)
    {
        _accountsManager.ChangeInsertionVisibility(_UUID, visible);
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
