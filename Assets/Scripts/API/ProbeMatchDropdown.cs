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
    }

    public void UpdateDropdown(List<string> opts)
    {
        List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
        foreach (string s in opts)
            options.Add(new Dropdown.OptionData(s));
        _dropdown.options = options;
    }

    public void UpdateText(string text, Color color)
    {
        _text.text = text;
        _text.color = color;
    }

    public void DropdownChanged(int index)
    {
        _probeManager.APITarget = _dropdown.options[index].text;
    }
}
