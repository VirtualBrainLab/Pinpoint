using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ServerProbeInsertionUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField _insertionNameInput;
    [SerializeField] private TextMeshProUGUI _insertionText;
    [SerializeField] private TMP_Text _insertionDescriptionText;
    [SerializeField] private Toggle _insertionActiveToggle;

    private UnisaveAccountsManager _accountsManager;
    public string UUID;
    private string _displayString;

    private void OnEnable()
    {
        UIManager.FocusableInputs.Add(_insertionNameInput);
    }

    private void OnDestroy()
    {
        UIManager.FocusableInputs.Remove(_insertionNameInput);
    }

    public void SetInsertionData(UnisaveAccountsManager accountsManager, string UUID, string name, bool active)
    {
        _accountsManager = accountsManager;
        this.UUID = UUID;

        _insertionNameInput.SetTextWithoutNotify(name);

        SetToggle(active);

        GetComponent<Button>().onClick.AddListener(ActivateProbe);
    }

    public void SetColor(float[] color)
    {
        _insertionText.color = new Color(color[0], color[1], color[2]);
    }

    public void NameChanged(string newName)
    {
        _accountsManager.OverrideProbeName(UUID, newName);
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
        _accountsManager.RemoveProbeFromActiveExperiment(UUID);
    }

    public void ActivateProbe()
    {
        _accountsManager.SetActiveProbe(UUID);
    }
}
