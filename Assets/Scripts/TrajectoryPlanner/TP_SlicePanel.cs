using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SlicePanel : MonoBehaviour
{
    [SerializeField] private RawImage _panelImage;
    [SerializeField] private TextMeshProUGUI _panelText;

    public void SetImageColor(Color color)
    {
        _panelImage.color = color;
    }

    public void SetText(string newText)
    {
        _panelText.text = newText;
    }
}
