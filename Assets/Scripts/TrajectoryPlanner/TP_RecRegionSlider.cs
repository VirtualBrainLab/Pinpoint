using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TrajectoryPlanner;
using UnityEngine.Serialization;

public class TP_RecRegionSlider : MonoBehaviour
{
    [FormerlySerializedAs("tpmanager")] [SerializeField] private TrajectoryPlannerManager _tpmanager;
    [FormerlySerializedAs("uiSlider")] [SerializeField] private Slider _uiSlider;
    [FormerlySerializedAs("recRegionSizeText")] [SerializeField] private TextMeshProUGUI _recRegionSizeText;

    private float[] np1Range = { 3.84f, 7.68f };
    private float[] np2Range = { 2.88f, 5.76f };
    private float[] np24Range = { 0.72f, 1.44f, 2.88f, 5.76f };
    private List<float[]> ranges;
    private int[] type2index = { -1, 0, 1, -1, 2, -1, -1, -1, 2 };

    public TP_RecRegionSlider()
    {
        ranges = new List<float[]>();
        ranges.Add(np1Range);
        ranges.Add(np2Range);
        ranges.Add(np24Range);
    }

    public void SliderValueChanged(float value)
    {
        if (_tpmanager.InputsFocused())
            return;

        ProbeManager probeManager = _tpmanager.GetActiveProbeManager();
        if (probeManager != null)
        {
            // Get active probe type from tpmanager
            Debug.Log(probeManager.ProbeType);
            float[] range = ranges[type2index[probeManager.ProbeType]];
            _uiSlider.value = Round2Nearest(value, range);
            probeManager.ChangeRecordingRegionSize(_uiSlider.value);

            _tpmanager.movedThisFrame = true;

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
