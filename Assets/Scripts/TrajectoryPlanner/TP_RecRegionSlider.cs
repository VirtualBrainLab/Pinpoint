using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TrajectoryPlanner;

public class TP_RecRegionSlider : MonoBehaviour
{
    [SerializeField] private TrajectoryPlannerManager _tpmanager;
    [SerializeField] private Slider _uiSlider;
    [SerializeField] private TextMeshProUGUI _recRegionSizeText;

    private float[] _np1Range = { 3.84f, 7.68f };
    private float[] _np2Range = { 2.88f, 5.76f };
    private float[] _np24Range = { 0.72f, 1.44f, 2.88f, 5.76f };
    private List<float[]> _ranges;
    private int[] _type2index = { -1, 0, 1, -1, 2, -1, -1, -1, 2 };

    public TP_RecRegionSlider()
    {
        _ranges = new List<float[]>();
        _ranges.Add(_np1Range);
        _ranges.Add(_np2Range);
        _ranges.Add(_np24Range);
    }

    public void SliderValueChanged(float value)
    {
        ProbeManager probeManager = _tpmanager.GetActiveProbeManager();
        if (probeManager != null)
        {
            // Get active probe type from tpmanager
            Debug.Log(probeManager.ProbeType);
            float[] range = _ranges[_type2index[probeManager.ProbeType]];
            _uiSlider.value = Round2Nearest(value, range);
            probeManager.ChangeRecordingRegionSize(_uiSlider.value);

            _tpmanager.MovedThisFrame = true;

            _recRegionSizeText.text = "Recording region size: " + _uiSlider.value;
        }
    }

    public float Round2Nearest(float value, float[] range)
    {
        float minRangeValue = 0f;
        float minDist = float.MaxValue;

        foreach (float val in range)
        {
            float dist = Mathf.Abs(value - val);
            if (dist < minDist)
            {
                minDist = dist;
                minRangeValue = val;
            }
        }

        return minRangeValue;
    }
}
