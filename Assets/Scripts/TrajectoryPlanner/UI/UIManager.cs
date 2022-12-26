using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class UIManager : MonoBehaviour
{
    #region Components

    [FormerlySerializedAs("EditorFocusableInputs")] [SerializeField]
    private List<TMP_InputField> _editorFocusableInputs;

    [FormerlySerializedAs("EditorFocusableGOs")] [SerializeField]
    private List<GameObject> _editorFocusableGOs;

    [SerializeField] private GameObject _automaticControlPanelGameObject;

    #endregion

    #region Properties

    public static readonly HashSet<TMP_InputField> FocusableInputs = new();
    public static readonly HashSet<GameObject> FocusableGOs = new();

    #endregion


    private void Awake()
    {
        FocusableInputs.UnionWith(_editorFocusableInputs);
        FocusableGOs.UnionWith(_editorFocusableGOs);
    }

    public static bool InputsFocused
    {
        get { return FocusableInputs.Any(x => x.isFocused) || FocusableGOs.Any(x => x.activeSelf); }
    }

    public void EnableAutomaticManipulatorControlPanel(bool enable = true)
    {
        _automaticControlPanelGameObject.SetActive(enable);
    }
}