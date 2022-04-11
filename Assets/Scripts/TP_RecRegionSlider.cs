using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TP_RecRegionSlider : MonoBehaviour
{
    [SerializeField] private TP_TrajectoryPlannerManager tpmanager;
    [SerializeField] private Slider uiSlider;
    [SerializeField] private TextMeshProUGUI recRegionSizeText;

    private float[] np1Range = { 3.84f, 7.68f };
    private float[] np2Range = { 2.88f, 5.76f };
    private float[] np24Range = { 0.72f, 1.44f, 2.88f, 5.76f };
    private List<float[]> ranges;
    private int[] type2index = { -1, 0, 1, -1, 2 };

    private void Start()
    {
        ranges = new List<float[]>();
        ranges.Add(np1Range);
        ranges.Add(np2Range);
        ranges.Add(np24Range);
    }

    public void SliderValueChanged(float value)
    {
        if (tpmanager.GetActiveProbeController()!=null)
        {
            // Get active probe type from tpmanager
            float[] range = ranges[type2index[tpmanager.GetActiveProbeType()]];
            uiSlider.value = Round2Nearest(value, range);
            tpmanager.GetActiveProbeController().ChangeRecordingRegionSize(uiSlider.value);
            tpmanager.UpdateInPlaneView();

            recRegionSizeText.text = "Recording region size: " + uiSlider.value;
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
