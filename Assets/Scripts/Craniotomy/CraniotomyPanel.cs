using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using BrainAtlas;
using System.Collections.Generic;

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

    [SerializeField] private List<CraniotomySkull> _skullList;

    private void Awake()
    {
        SetDefaultCraniotomyPositions();
    }

    private void OnEnable()
    {
        foreach (CraniotomySkull skull in _skullList)
        {
            if (skull.gameObject.activeSelf)
            {
                skull.Enable();
                UpdateCraniotomyIdx(_lastCraniotomyIdx);
            }
        }
    }

    private void SetDefaultCraniotomyPositions()
    {
        foreach (CraniotomySkull skull in _skullList)
        {
            if (skull.gameObject.activeSelf)
            {
                for (int i = 0; i < 5; i++)
                {
                    skull.SetActiveCraniotomy(i);
                    skull.SetCraniotomyPosition(BrainAtlasManager.ActiveReferenceAtlas.Atlas2World(Vector3.zero));
                }
            }
        }
    }

    private void OnDisable()
    {
        foreach (CraniotomySkull skull in _skullList)
        {
            if (skull.gameObject.activeSelf)
            {
                skull.Disable();
            }
        }
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
        foreach (CraniotomySkull skull in _skullList)
        {
            if (skull.gameObject.activeSelf)
            {
                _lastCraniotomyIdx = craniotomyIdx;
                skull.SetActiveCraniotomy(craniotomyIdx);
                _positionWorld = skull.GetCraniotomyPosition();
                _positionSpace = BrainAtlasManager.ActiveReferenceAtlas.World2Atlas(_positionWorld);
                size = skull.GetCraniotomySize();
                UpdateText();
                UpdateSliders();
            }
        }
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
        if (!gameObject.activeSelf)
            return;

        // We need to rotate the x/y coordinates into the current transformed space... 
        _positionWorld = BrainAtlasManager.ActiveReferenceAtlas.Atlas2World(_positionSpace);

        foreach (CraniotomySkull skull in _skullList)
        {
            if (skull.gameObject.activeSelf)
            {
                if (skull != null)
                {
                    skull.SetCraniotomyPosition(_positionWorld);
                    skull.SetCraniotomySize(size);
                }
            }
        }
    }

    public void SnapActiveCraniotomy2Probe()
    {
        Vector3 apmldv = ProbeManager.ActiveProbeManager.ProbeController.Insertion.APMLDV;
        _positionSpace.x = apmldv.x;
        _positionSpace.y = apmldv.y;
        UpdateCraniotomy();
        UpdateText();
    }
}
