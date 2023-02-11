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
    public string UUID;
    private string _displayString;

    public void SetInsertionData(UnisaveAccountsManager accountsManager, string UUID, bool active)
    {
        _accountsManager = accountsManager;
        this.UUID = UUID;
        _displayString = (!string.IsNullOrEmpty(this.UUID)) ?
            this.UUID.Substring(0, 8) :
            "";
        _insertionNameText.text = _displayString;

        SetToggle(active);

        GetComponent<Button>().onClick.AddListener(ActivateProbe);
    }

    public void UpdateDescription(string desc)
    {
        _insertionDescriptionText.text = desc;
    }

    public void ToggleVisibility(bool visible)
    {
        _accountsManager.ChangeInsertionVisibility(UUID, visible);
    }

    public void SetToggle(bool active)
    {
        _insertionActiveToggle.SetIsOnWithoutNotify(active);
    }

    public void DeleteProbe()
    {
        _accountsManager.DeleteProbe(UUID);
    }

    public void ActivateProbe()
    {
        _accountsManager.SetActiveProbe(UUID);
    }
}
