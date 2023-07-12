using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TP_SearchAreaPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text _text;

    public CCFTreeNode Node
    {
        get; set;
    }

    public void SetFontSize(int fontSize)
    {
        _text.fontSize = fontSize;
    }
}
