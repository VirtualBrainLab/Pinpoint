using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    #region Static
    public static UIManager Instance;
    #endregion

    #region Components

    [SerializeField] private List<TMP_InputField> _editorFocusableInputs;
    
    [SerializeField] private List<GameObject> _editorFocusableGOs;

    [SerializeField] private List<TMP_Text> _whiteUIText;

    [SerializeField] private GameObject _ephysCopilotPanelGameObject;
    [SerializeField] private GameObject _copilotDemoPanelGameObject;
    [SerializeField] private GameObject _settingsPanel;

    #endregion

    #region Properties

    public static readonly HashSet<TMP_InputField> FocusableInputs = new();
    public static readonly HashSet<GameObject> FocusableGOs = new();

    #endregion


    private void Awake()
    {
        Instance = this;

        FocusableInputs.UnionWith(_editorFocusableInputs);
        FocusableGOs.UnionWith(_editorFocusableGOs);
    }

    /// <summary>
    /// Return whether any inputs are currently focused or if any of the gameobjects are currently active
    /// </summary>
    public static bool InputsFocused
    {
        get {
            return FocusableInputs.Any(x => x != null && x.isFocused) ||
                   FocusableGOs.Any(x => x != null && x.activeSelf);
        }
    }

    public void EnableEphysCopilotPanel(bool enable = true)
    {
        // Always set the panel to active once started using, but set the scale to zero if we're disabling it
        _ephysCopilotPanelGameObject.SetActive(true);
        
        // Set the scale to zero if we're disabling it
        _ephysCopilotPanelGameObject.transform.localScale = enable ? Vector3.one : Vector3.zero;
    }

    public void EnableCopilotDemoPanel(bool enable = true)
    {
        _copilotDemoPanelGameObject.SetActive(enable);
    }

    public void SetBackgroundWhite(bool state)
    {
        if (state)
        {
            foreach (TMP_Text textC in _whiteUIText)
                textC.color = Color.black;
            Camera.main.backgroundColor = Color.white;
        }
        else
        {
            foreach (TMP_Text textC in _whiteUIText)
                textC.color = Color.white;
            Camera.main.backgroundColor = Color.black;
        }
    }
}