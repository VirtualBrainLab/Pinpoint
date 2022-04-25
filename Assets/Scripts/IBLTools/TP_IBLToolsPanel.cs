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

    [SerializeField] TP_CraniotomySkull craniotomySkull;

    private void Start()
    {
        UpdateCraniotomy();
    }

    public void UpdateAP(float ap)
    {
        apText.text = "AP: " + Mathf.RoundToInt(ap);
        position.x = ap;
        UpdateCraniotomy();
    }
    public void UpdateML(float ml)
    {
        mlText.text = "ML: " + Mathf.RoundToInt(ml);
        position.z = ml;
        UpdateCraniotomy();
    }
    public void UpdateSize(float newSize)
    {
        dvText.text = "r: " + Mathf.RoundToInt(newSize*100)/100;
        size = newSize;
        UpdateCraniotomy();
    }

    private void UpdateCraniotomy()
    {
        craniotomySkull.SetCraniotomyPosition(position);
        craniotomySkull.SetCraniotomySize(size);
    }
}
