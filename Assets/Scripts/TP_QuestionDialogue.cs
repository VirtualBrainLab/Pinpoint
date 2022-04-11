using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TP_QuestionDialogue : MonoBehaviour
{
    [SerializeField] private TMP_Text questionText;

    private Action yesCallback;
    private Action noCallback;

    public void YesCallback()
    {
        if (yesCallback != null)
            yesCallback();
        gameObject.SetActive(false);
    }

    public void NoCallback()
    {
        if (noCallback != null)
            noCallback();
        gameObject.SetActive(false);
    }

    public void NewQuestion(string newText)
    {
        gameObject.SetActive(true);
        questionText.text = newText;
    }

    public void SetYesCallback(Action newCallback)
    {
        yesCallback = newCallback;
    }

    public void SetNoCallback(Action newCallback)
    {
        noCallback = newCallback;
    }
}
