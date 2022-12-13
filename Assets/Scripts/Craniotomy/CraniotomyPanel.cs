using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class CraniotomyPanel : MonoBehaviour
{
    [FormerlySerializedAs("apText")] [SerializeField]
    private TMP_Text _apText;
    [FormerlySerializedAs("mlText")] [SerializeField]
    private TMP_Text _mlText;
    [FormerlySerializedAs("rText")] [SerializeField]
    private TMP_Text _rText;

    [SerializeField] private Slider _apSlider;
    [SerializeField] private Slider _mlSlider;
    [SerializeField] private Slider _rSlider;

    //5.4f, 5.739f, 0.332f
    // start the craniotomy at bregma
    private Vector3 _positionWorld = Vector3.zero;
    private Vector3 _positionSpace = Vector3.zero;
    private float size = 0f;

    private int _lastCraniotomyIdx = 0;

    public Func<Vector3, Vector3> Space2World;
    public Func<Vector3, Vector3> World2Space;

    [FormerlySerializedAs("craniotomySkull")] [SerializeField] private CraniotomySkull _craniotomySkull;

    private void Awake()
    {
        SetDefaultCraniotomyPositions();
    }

    private void OnEnable()
    {
        _craniotomySkull.Enable();
        UpdateCraniotomyIdx(_lastCraniotomyIdx);
    }

    private void SetDefaultCraniotomyPositions()
    {
        for (int i = 0; i < 5; i++)
        {
            _craniotomySkull.SetActiveCraniotomy(i);
            _craniotomySkull.SetCraniotomyPosition(Space2World(Vector3.zero));
        } 
    }

    private void OnDisable()
    {
        _craniotomySkull.Disable();
    }

    public void UpdateAP(float ap)
    {
        _positionSpace.x = ap;
        UpdateCraniotomy();
        UpdateText();
    }
    public void UpdateML(float ml)
    {
        _positionSpace.y = ml;
        UpdateCraniotomy();
        UpdateText();
    }
    public void UpdateSize(float newSize)
    {
        size = newSize;
        UpdateCraniotomy();
        UpdateText();
    }

    public void UpdateCraniotomyIdx(int craniotomyIdx)
    {
        _lastCraniotomyIdx = craniotomyIdx;
        _craniotomySkull.SetActiveCraniotomy(craniotomyIdx);
        _positionWorld = _craniotomySkull.GetCraniotomyPosition();
        _positionSpace = World2Space(_positionWorld);
        size = _craniotomySkull.GetCraniotomySize();
        UpdateText();
        UpdateSliders();
    }

    private void UpdateSliders()
    {
        _apSlider.value = _positionSpace.x;
        _mlSlider.value = _positionSpace.y;
        _rSlider.value = size;
    }

    private void UpdateText()
    {
        _apText.text = "AP: " + Mathf.RoundToInt(_positionSpace.x * 1000f);
        _mlText.text = "ML: " + Mathf.RoundToInt(_positionSpace.y * 1000f);
        _rText.text = "r: " + Mathf.RoundToInt(size * 1000f);
    }

    private void UpdateCraniotomy()
    {
        // We need to rotate the x/y coordinates into the current transformed space... 
        _positionWorld = Space2World(_positionSpace);

        if (_craniotomySkull != null)
        {
            _craniotomySkull.SetCraniotomyPosition(_positionWorld);
            _craniotomySkull.SetCraniotomySize(size);
        }
    }
}
