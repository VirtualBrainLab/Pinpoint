using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ProbePanelHeightSlider : MonoBehaviour
{
    [SerializeField] private Slider _slider;

    private void Awake()
    {
        _slider.onValueChanged.AddListener(SetSetting);
    }

    private void SetSetting(float value)
    {
        Settings.ProbePanelHeight = value;
    }
}
