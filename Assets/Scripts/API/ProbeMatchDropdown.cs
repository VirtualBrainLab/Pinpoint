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

    public void Register(ProbeManager probeManager)
    {
        _probeManager = probeManager;
        _dropdown.onValueChanged.AddListener(DropdownChanged);
    }

    public void UpdateDropdown(List<string> opts)
    {
        // Save the current option
        string curOption = _dropdown.options[_dropdown.value].text;
        int curIdx = -1;

        List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();

        for (int i = 0; i < opts.Count; i++)
        {
            string opt = opts[i];
            options.Add(new Dropdown.OptionData(opt));
            if (opt.Equals(curOption))
                curIdx = i;
        }

        _dropdown.options = options;
        if (curIdx >= 0)
            _dropdown.SetValueWithoutNotify(curIdx);
        else
        {
            _dropdown.SetValueWithoutNotify(0);
            _probeManager.APITarget = _dropdown.options[0].text;
        }
    }

    public void UpdateText(string text, Color color)
    {
        _text.text = text;
        _text.color = color;
    }

    public void DropdownChanged(int index)
    {
        _probeManager.APITarget = _dropdown.options[index].text;
        Debug.Log(_probeManager.APITarget);
    }
}
