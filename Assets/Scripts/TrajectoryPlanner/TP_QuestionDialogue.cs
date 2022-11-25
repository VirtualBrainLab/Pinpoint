using System;
using TMPro;
using UnityEngine;

public class TP_QuestionDialogue : MonoBehaviour
{
    [SerializeField] private TMP_Text _questionText;

    private Action _yesCallback;
    private Action _noCallback;

    public void YesCallback()
    {
        if (_yesCallback != null)
            _yesCallback();
        gameObject.SetActive(false);
    }

    public void NoCallback()
    {
        if (_noCallback != null)
            _noCallback();
        gameObject.SetActive(false);
    }

    public void NewQuestion(string newText)
    {
        gameObject.SetActive(true);
        _questionText.text = newText;
    }

    public void SetYesCallback(Action newCallback)
    {
        _yesCallback = newCallback;
    }

    public void SetNoCallback(Action newCallback)
    {
        _noCallback = newCallback;
    }
}
