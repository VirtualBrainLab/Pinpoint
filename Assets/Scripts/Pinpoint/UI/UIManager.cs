using System.Collections.Generic;
using System.Linq;
using Models;
using Models.Settings;
using Services;
using TMPro;
using UI;
using UI.Views;
using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.UIElements;

public class UIManager : MonoBehaviour
{
    #region Static

    public static UIManager Instance;

    #endregion

    #region Components

    [SerializeField]
    private List<TMP_InputField> _editorFocusableInputs;

    [SerializeField]
    private List<GameObject> _editorFocusableGOs;

    [SerializeField]
    private List<TMP_Text> _whiteUIText;

    [SerializeField]
    private GameObject _ephysCopilotPanelGameObject;

    [SerializeField]
    private GameObject _copilotDemoPanelGameObject;

    [SerializeField]
    private GameObject _settingsPanel;

    #endregion

    #region Properties

    public static readonly HashSet<TMP_InputField> FocusableInputs = new();
    public static readonly HashSet<GameObject> FocusableGOs = new();

    #endregion

#if APP_UI
    private IDisposableSubscription _settingsStateSubscription;
#endif

    private void Awake()
    {
        Instance = this;

        FocusableInputs.UnionWith(_editorFocusableInputs);
        FocusableGOs.UnionWith(_editorFocusableGOs);
    }

#if APP_UI
    public void Initialize()
    {
        var storeService = PinpointApp.Services.GetRequiredService<StoreService>();
        _settingsStateSubscription = storeService.Store.Subscribe(
 state => state.Get<SettingsState>(SliceNames.SETTINGS_SLICE),
   OnSettingsStateChanged,
   new SubscribeOptions<SettingsState> { fireImmediately = true }
    );
    }

    private void OnDestroy()
    {
        _settingsStateSubscription?.Dispose();
    }

    private void OnSettingsStateChanged(SettingsState state)
    {
        SetBackgroundWhite(state.Background == Color.white);
    }
#endif

    /// <summary>
    /// Return whether any inputs are currently focused or if any of the gameobjects are currently active
    /// </summary>
#if APP_UI
    public static bool InputsFocused =>
        PinpointApp.RootVisualElement.focusController.focusedElement
      is Unity.AppUI.UI.TextField
          or Unity.AppUI.UI.FloatField
         or Unity.AppUI.UI.Vector3Field
     or Unity.AppUI.UI.Vector4Field;
#else
    public static bool InputsFocused => FocusableGOs.Any(x => x != null && x.activeSelf);
#endif

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
        if (Camera.main)
        {
            if (state)
            {
                Camera.main.backgroundColor = Color.white;
            }
            else
            {
                Camera.main.backgroundColor = Color.black;
            }
        }
    }
}
