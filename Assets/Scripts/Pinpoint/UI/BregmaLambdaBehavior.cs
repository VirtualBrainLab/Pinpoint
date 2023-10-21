using UnityEngine;
using UnityEngine.UI;

public class BregmaLambdaBehavior : MonoBehaviour
{
    private float _blDistance = 4.15f;
    public float DefaultBLDistance { get { return _blDistance; } }

    [SerializeField] Slider _blSlider;

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
        _blSlider.value = blDistance;
    }
}
