using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class QuestionDialogue : MonoBehaviour
{
    [FormerlySerializedAs("questionText")] [SerializeField] private static TMP_Text _questionText;

    private static Action yesCallback;
    private static Action noCallback;

    private static GameObject _gameObject;

    private void Awake()
    {
        _gameObject = gameObject;
    }

    public static void YesCallback()
    {
        if (yesCallback != null)
            yesCallback();
        _gameObject.SetActive(false);
    }

    public static void NoCallback()
    {
        if (noCallback != null)
            noCallback();
        _gameObject.SetActive(false);
    }

    public static void NewQuestion(string newText)
    {
        _gameObject.SetActive(true);
        _questionText.text = newText;
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
