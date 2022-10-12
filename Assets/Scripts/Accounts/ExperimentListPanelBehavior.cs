using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExperimentListPanelBehavior : MonoBehaviour
{
    [SerializeField] private TMP_InputField _experimentNameText;
    public TMP_InputField ExperimentNameText { get { return _experimentNameText; } }

    [SerializeField] private Button _deleteButton;
    public Button DeleteButton { get { return _deleteButton; } }
}
