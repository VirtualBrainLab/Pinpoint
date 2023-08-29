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

    [SerializeField] private GameObject _uiPanel;
    [SerializeField] private TMP_Text _questionText;
    
    public Action YesCallback { private get; set; }
    
    public Action NoCallback { private get; set; }

    private void Awake()
    {
        if (Instance != null)
            throw new Exception("There should only be one Singleton of QuestionDialogue in the scene");
        Instance = this;
    }

    #region Functions
    public void NewQuestion(string newText)
    {
        _uiPanel.SetActive(true);
        _questionText.text = newText;
    }
    public void CallYesCallback()
    {
        YesCallback?.Invoke();
        print("yes callback called");
        _uiPanel.SetActive(false);
        print("Closed panel");
    }

    public void CallNoCallback()
    {
        NoCallback?.Invoke();
        _uiPanel.SetActive(false);
    }
    #endregion
}
