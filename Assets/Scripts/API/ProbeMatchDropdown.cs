using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ProbeMatchDropdown : MonoBehaviour
{
    [SerializeField] private TMP_Text _text;
    [SerializeField] private Dropdown _dropdown;
    private ProbeManager _probeManager;

    public UnityEvent DropdownChangedEvent;

    public void Register(ProbeManager probeManager)
    {
        _probeManager = probeManager;

        _dropdown.onValueChanged.RemoveAllListeners();
        _dropdown.onValueChanged.AddListener(DropdownChanged);

        UpdateText(probeManager.name, probeManager.Color);
    }

    public void UpdateDropdown(List<string> opts)
    {
        // Save the current option
        int targetIdx = -1;

        List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();

        for (int i = 0; i < opts.Count; i++)
        {
            string opt = opts[i];
            options.Add(new Dropdown.OptionData(opt));
            if (opt.Equals(_probeManager.APITarget))
                targetIdx = i;
        }

        _dropdown.options = options;
        if (targetIdx >= 0)
            _dropdown.SetValueWithoutNotify(targetIdx);
        else
        {
            // If it's not possible to target this particular target, reset the APITarget
            _dropdown.SetValueWithoutNotify(0); // None option
            _probeManager.APITarget = null;
        }
    }

    private void UpdateText(string text, Color color)
    {
        _text.text = text;
        color.a = 1;
        _text.color = color;
    }

    public void DropdownChanged(int index)
    {
        _probeManager.APITarget = _dropdown.options[index].text;
        DropdownChangedEvent.Invoke();
    }
}
