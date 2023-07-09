using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class BregmaLambdaBehavior : MonoBehaviour
{
    private const float DEFAULT_BREGMA_LAMBDA_DISTANCE = 4.15f;

    [SerializeField] Slider _blSlider;

    public void ResetBLDistance()
    {
        _blSlider.value = DEFAULT_BREGMA_LAMBDA_DISTANCE;
    }
}
