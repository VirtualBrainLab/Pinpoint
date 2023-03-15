using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class QuestionDialogue : MonoBehaviour
{
    #region static
    public static QuestionDialogue Instance;
    #endregion

    [SerializeField] private TMP_Text _questionText;

    private static Action yesCallback;
    private static Action noCallback;

    private void Awake()
    {
        if (Instance != null)
            throw new Exception("There should only be one Singleton of QuestionDialogue in the scene");
        Instance = this;
    }

    public static void YesCallback()
    {
        if (yesCallback != null)
            yesCallback();
        Instance.gameObject.SetActive(false);
    }

    public static void NoCallback()
    {
        if (noCallback != null)
            noCallback();
        Instance.gameObject.SetActive(false);
    }

    public static void NewQuestion(string newText)
    {
        Instance.gameObject.SetActive(true);
        Instance._questionText.text = newText;
    }

    public static void SetYesCallback(Action newCallback)
    {
        yesCallback = newCallback;
    }

    public static void SetNoCallback(Action newCallback)
    {
        noCallback = newCallback;
    }
}
