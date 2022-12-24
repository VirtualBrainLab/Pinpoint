using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class QuestionDialogue : MonoBehaviour
{
    [SerializeField] private TMP_Text _questionText;

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
        _questionText.text = newText;
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
