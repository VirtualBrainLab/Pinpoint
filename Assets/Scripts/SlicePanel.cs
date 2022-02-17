using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SlicePanel : MonoBehaviour
{
    [SerializeField] private RawImage panelImage;
    [SerializeField] private TextMeshProUGUI panelText;

    public void SetImageColor(Color color)
    {
        panelImage.color = color;
    }

    public void SetText(string newText)
    {
        panelText.text = newText;
    }
}
