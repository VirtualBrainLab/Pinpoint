using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TP_IBLToolsPanel : MonoBehaviour
{
    [SerializeField] TMP_Text apText;
    [SerializeField] TMP_Text mlText;
    [SerializeField] TMP_Text rText;

    //5.4f, 5.739f, 0.332f
    // start the craniotomy at bregma
    private Vector3 position = Vector3.zero;

    private float size = 1f;
    private float disabledSize = -1f;

    [SerializeField] TP_CraniotomySkull craniotomySkull;

    private void Start()
    {
        // Start with craniotomy disabled
        UpdateSize(0);
        UpdateCraniotomy();
    }

    public void OnDisable()
    {
        disabledSize = size;
        UpdateSize(0);
    }

    public void OnEnable()
    {
        if (disabledSize >= 0f)
            UpdateSize(disabledSize);
        else
            UpdateSize(size);
    }

    public void UpdateAP(float ap)
    {
        position.x = ap;
        UpdateCraniotomy();
        UpdateText();
    }
    public void UpdateML(float ml)
    {
        position.z = ml;
        UpdateCraniotomy();
        UpdateText();
    }
    public void UpdateSize(float newSize)
    {
        size = newSize;
        UpdateCraniotomy();
        UpdateText();
    }

    private void UpdateText()
    {
        apText.text = "AP: " + Mathf.RoundToInt(position.x);
        mlText.text = "ML: " + Mathf.RoundToInt(position.z);
        rText.text = "r: " + Mathf.RoundToInt(size);
    }

    private void UpdateCraniotomy()
    {
        craniotomySkull.SetCraniotomyPosition(position);
        craniotomySkull.SetCraniotomySize(size);
    }
}
