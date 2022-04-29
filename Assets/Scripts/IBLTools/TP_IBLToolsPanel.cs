using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TP_IBLToolsPanel : MonoBehaviour
{
    [SerializeField] TMP_Text apText;
    [SerializeField] TMP_Text mlText;
    [SerializeField] TMP_Text dvText;

    private Vector3 position = new Vector3(216, 0, 229);
    private float size = 1f;
    private float disabledSize = -1f;

    [SerializeField] TP_CraniotomySkull craniotomySkull;

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
        dvText.text = "r: " + Mathf.Round(size * 100f) / 100f;
    }

    private void UpdateCraniotomy()
    {
        craniotomySkull.SetCraniotomyPosition(position);
        craniotomySkull.SetCraniotomySize(size);
    }
}
