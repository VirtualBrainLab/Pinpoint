using TMPro;
using UnityEngine;

public class TP_SearchAreaPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text _text;

    // TODO
    public string Node
    {
        get; set;
    }

    public void SetFontSize(int fontSize)
    {
        _text.fontSize = fontSize;
    }
}
