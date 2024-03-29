using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BregmaLambdaBehavior : MonoBehaviour
{
    private float _blDistance = -1f;
    public float DefaultBLDistance { get { return _blDistance; } }

    [SerializeField] Slider _blSlider;
    [SerializeField] TMP_Text _sliderText;

    private void Awake()
    {
        _blSlider.onValueChanged.AddListener(SetSetting);
    }

    public void ResetBLDistance()
    {
        _blSlider.value = _blDistance;
    }

    /// <summary>
    /// Set the slider range to a specific min/max and floatDistance
    /// </summary>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <param name="blDistance"></param>
    public void SetBLRange(float min, float max, float blDistance)
    {
        _blDistance = blDistance;

        _blSlider.minValue = min;
        _blSlider.maxValue = max;
        _blSlider.SetValueWithoutNotify(blDistance);
    }

    public void SetSliderAndText(float ratio)
    {
        _blSlider.SetValueWithoutNotify(ratio * _blDistance);
        _sliderText.text = $"{Mathf.RoundToInt(ratio * _blDistance * 100f) / 100f}";
    }

    private void SetSetting(float value)
    {
        // Convert to ratio then set
        Settings.BregmaLambdaRatio = value / _blDistance;
    }
}
